using ePub.Services;
using ePub2.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using System.Web.Http;
using Umbraco.Web;
using Umbraco.Web.Models.ContentEditing;
using Umbraco.Web.WebApi;

namespace ePub_revamp2.Controllers.Api
{
    public class SemanticSearchController : UmbracoApiController
    {
        private static readonly HttpClient _http = new HttpClient();
        private const string OllamaEmbedUrl = "http://localhost:11434/api/embed";
        private const string QdrantUrl = "http://localhost:6333";
        private const string CollectionName = "manuals2";
        //private const int ExpectedVectorSize = 1024; // Must match Qdrant collection

        // === 1. Get embedding from Ollama ===

        private readonly string _geminiApiKey = "AIzaSyDKpvbCe077l9lzytqeVcKljZNiQTLSVrE";
        private const int ExpectedVectorSize = 1024; // Gemini embedding-001 output size

        private async Task<float[]> GetEmbeddingAsync(string text)
        {



            var payload = new
            {
                model = "mxbai-embed-large", // Match your Qdrant collection
                input = text
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");
            var response = await _http.PostAsync(OllamaEmbedUrl, content);
            response.EnsureSuccessStatusCode();

            var jsonString = await response.Content.ReadAsStringAsync();
            var json = JObject.Parse(jsonString);

            var embeddingsArray = json["embeddings"]?.FirstOrDefault()?.ToObject<float[]>();
            if (embeddingsArray == null || embeddingsArray.Length == 0)
                throw new Exception("Embedding not found in Ollama response: " + jsonString);

            if (embeddingsArray.Length != ExpectedVectorSize)
                throw new Exception($"Embedding size mismatch. Expected {ExpectedVectorSize}, got {embeddingsArray.Length}");

            return embeddingsArray;
        }

        // === 2. Search endpoint ===
        [HttpGet]
        public async Task<IHttpActionResult> Search(string query, string sortBy, int limit = 10, int page = 1)
        {
            var sw = Stopwatch.StartNew();

            if (string.IsNullOrWhiteSpace(query))
                return BadRequest("Query is required.");

            int pageSize = 10;
            StoreRecentSearch(query);

            float[] embedding;
            try
            {
                embedding = await GetEmbeddingAsync(query);
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            var searchPayload = new
            {
                vector = embedding,
                limit = 50,
                with_payload = new { include = new[] { "nodeId", "chunk" } }
            };

            var jsonPayload = JsonConvert.SerializeObject(searchPayload);
            Console.WriteLine("📤 Sending to Qdrant:\n" + jsonPayload);

            var content = new StringContent(jsonPayload, Encoding.UTF8, "application/json");

            HttpResponseMessage resp;
            try
            {
                resp = await _http.PostAsync($"{QdrantUrl}/collections/{CollectionName}/points/search", content);

                if (!resp.IsSuccessStatusCode)
                {
                    var errMsg = await resp.Content.ReadAsStringAsync();
                    throw new Exception($"Qdrant returned {(int)resp.StatusCode}: {errMsg}");
                }
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }

            var jsonResponse = await resp.Content.ReadAsStringAsync();
            var searchResult = JObject.Parse(jsonResponse);

            var results = new List<SearchResult2>();

            foreach (var item in searchResult["result"] ?? new JArray())
            {
                var payloadData = item["payload"];
                string umbracoId = payloadData?.Value<string>("nodeId");

                if (!int.TryParse(umbracoId, out int nodeId))
                    continue;

                var contentNode = Umbraco.Content(nodeId);
                if (contentNode == null) continue;

                if (!string.Equals(contentNode.ContentType.Alias, "manualItem", StringComparison.OrdinalIgnoreCase))
                    continue;

                var body = payloadData.Value<string>("chunk") ?? "";
                var snippet = HighlightHighestScoreLine(body.Length > 200 ? body.Substring(0, 200) + "..." : body, query);

                results.Add(new SearchResult2
                {
                    Id = contentNode.Id,
                    Name = contentNode.Name,
                    ParentName = contentNode.Parent.Name,
                    Url = contentNode.Url(),
                    directedUrl = contentNode.Parent.Url() + "#" + contentNode.Name,
                    publishedDate = contentNode?.UpdateDate.ToString("d MMMM yyyy"),
                    Snippet = body,
                    Score = item.Value<float>("score")
                });
            }

            if (results.Any())
            {
                results = results
                    .GroupBy(r => r.Id)
                    .Select(g => g.OrderByDescending(r => r.Score).First())
                    .Take(5)
                    .ToList();

                var candidateTexts = results.Select((r, i) => new { index = i, text = r.Snippet }).ToList();

                // Build the Gemini prompt
                var prompt = $@"
Query: {query}

Here are candidate results:
{string.Join("\n", candidateTexts.Select(c => $"{c.index}: {c.text}"))}

Please rerank these results by relevance to the query.
Respond ONLY with valid JSON in the format:
[
  {{ ""index"": number, ""score"": number, ""reason"": string }},
  ...
]
Do not include extra text or explanations outside the JSON.
";

                var rerankPayload = new
                {
                    contents = new[]
                    {
                new {
                    role = "user",
                    parts = new[] { new { text = prompt } }
                }
            }
                };

                var jsonPayload2 = JsonConvert.SerializeObject(rerankPayload);
                var content2 = new StringContent(jsonPayload2, Encoding.UTF8, "application/json");

                using (var client = new HttpClient())
                {
                    try
                    {
                        var resp2 = await client.PostAsync(
                             $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=AIzaSyAuq6DDMslaT10wYc3aFWta85K40QGoZIQ",
                            content2
                        );
                        resp2.EnsureSuccessStatusCode();

                        var respText = await resp2.Content.ReadAsStringAsync();
                        var obj = JObject.Parse(respText);

                        var textResp = obj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString();



                        var matchJson = Regex.Match(textResp, @"\[[\s\S]*\]");
                        if (matchJson.Success)
                        {
                            var cleanJson = matchJson.Value;

                            var rankedList = JsonConvert.DeserializeObject<List<RerankResult>>(cleanJson);

                            if (rankedList != null && rankedList.Count > 0)
                            {
                                var reorderedResults = new List<SearchResult2>();
                                foreach (var rerank in rankedList.OrderByDescending(r => r.score))
                                {
                                    var match = results.ElementAtOrDefault(rerank.index);
                                    if (match != null)
                                    {
                                        match.Reasoning = rerank.reason;
                                        match.Score = rerank.score;
                                        reorderedResults.Add(match);
                                    }
                                }
                                results = reorderedResults;
                            }
                        }
                        else
                        {
                            Console.WriteLine("⚠️ No valid JSON found in Gemini response");
                        }




                        //if (!string.IsNullOrWhiteSpace(textResp))
                        //{
                        //    // Parse Gemini JSON
                        //    var rankedList = JsonConvert.DeserializeObject<List<RerankResult>>(textResp);

                        //    if (rankedList != null && rankedList.Count > 0)
                        //    {
                        //        // Create new list ordered exactly as Gemini suggested
                        //        var reorderedResults = new List<SearchResult2>();

                        //        foreach (var rerank in rankedList.OrderByDescending(r => r.score))
                        //        {
                        //            var match = results.ElementAtOrDefault(rerank.index);
                        //            if (match != null)
                        //            {
                        //                match.Reasoning = rerank.reason;
                        //                match.Score = rerank.score;
                        //                reorderedResults.Add(match);
                        //            }
                        //        }

                        //        results = reorderedResults;
                        //    }
                        //}



                        //if (!string.IsNullOrWhiteSpace(textResp))
                        //{
                        //    // Try parsing Gemini JSON
                        //    var rankedList = JsonConvert.DeserializeObject<List<RerankResult>>(textResp);

                        //    if (rankedList != null && rankedList.Count > 0)
                        //    {
                        //        results = rankedList
                        //            .OrderByDescending(r => r.score)
                        //            .Select(r => {
                        //                var match = results.ElementAtOrDefault(r.index);
                        //                if (match != null)
                        //                {
                        //                    match.Reasoning = r.reason; // attach reasoning
                        //                    match.Score = r.score;   // update score
                        //                }
                        //                return match;
                        //            })
                        //            .Where(r => r != null)
                        //            .ToList();
                        //    }
                        //}
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("⚠️ Reranker failed: " + ex.Message);
                    }
                }
            }

            int totalResults = results.Count;
            int totalPages = (int)Math.Ceiling((double)totalResults / pageSize);
            page = Math.Max(1, Math.Min(page, totalPages));

            var pagedResults = results
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            if (sortBy != null)
            {
                switch (sortBy.ToLower())
                {
                    case "title":
                        results = results.OrderBy(r => r.Name).ToList();
                        break;
                    case "relevance":
                        results = results.OrderByDescending(r => r.Score).ToList();
                        break;
                    case "date-asc":
                        results = results.OrderBy(r => r.publishedDate).ToList();
                        break;
                    case "date-desc":
                        results = results.OrderByDescending(r => r.publishedDate).ToList();
                        break;
                    default:
                        results = results.OrderByDescending(r => r.Score).ToList();
                        break;
                }
            }

            sw.Stop();
            Console.WriteLine($"Search took {sw.ElapsedMilliseconds}ms");

            return Ok(new
            {
                results = pagedResults,
                currentPage = page,
                query,
                pageSize,
                totalPages,
                topResultId = pagedResults.FirstOrDefault()?.Id
            });
        }

        // DTO for rerank response
        public class RerankResult
        {
            public int index { get; set; }
            public float score { get; set; }
            public string reason { get; set; }
        }



        [HttpPost]
        [Route("api/search/reasoning")]
        public async Task<IHttpActionResult> GetReasoning([FromBody] ReasoningRequest req)
        {
            var reasoningResults = new List<ReasoningResult>();
            var client = new HttpClient();

            //add all candidate in one string
            var documentsText = string.Join("\n\n", req.Candidates.Select(c =>
    $"Document ID: {c.Name}\nSnippet: {c.Snippet}"));

            // Prepare a single prompt with all candidates
            //            var prompt = $@"
            //You are a financial search assistant. The user asked: '{req.Query}'.
            //You are given multiple document: 

            //{req.Candidates[0].Snippet}
            //Summarise **why this document is relevant to the user's query**. Focus on how the features, limits, or requirements in the document directly satisfy the user's needs without any internal monologue, self-reflection, or 'thinking' sentences..Return your answer as a single short paragraph under 50 words.
            //";

            var prompt = $@"
You are a financial search assistant. The user asked: '{req.Query}'.

You are given several candidate documents:

{documentsText}

Instructions:
1. Evaluate all the documents for how well they answer the user's query.
2. Choose the single most relevant document.
3. Summarise **why that document is relevant** to the user's query — focus on its specific content (features, terms, or requirements) that directly satisfy the user's needs.
4. Keep your answer under 50 words.
5. Return your response in this format:

Explanation: <state the name of the document and give short reason why this document is the best match and also share some details in the document>.
";



            var payload = new
            {
                contents = new[]
     {
        new {
            role = "user",
            parts = new[] {
                new { text = prompt }
            }
        }
    }
            };

            var content = new StringContent(JsonConvert.SerializeObject(payload), Encoding.UTF8, "application/json");



            try
            {
                var resp = await client.PostAsync(
                $"https://generativelanguage.googleapis.com/v1beta/models/gemini-2.5-flash:generateContent?key=AIzaSyAuq6DDMslaT10wYc3aFWta85K40QGoZIQ",
                content);

                resp.EnsureSuccessStatusCode();

                var respText = await resp.Content.ReadAsStringAsync();
                var jobj = JObject.Parse(respText);
                string reasoningText = jobj["candidates"]?[0]?["content"]?["parts"]?[0]?["text"]?.ToString() ?? "";



                

                //var respText = await resp.Content.ReadAsStringAsync();
                //var jobj = JObject.Parse(respText);

                //// Grab the reasoning text as raw string
                //string reasoningText = jobj["response"]?.ToString() ?? "";

                // Assign to all candidates (or top 1 if you prefer)
                reasoningResults = req.Candidates.Select(c => new ReasoningResult
                {
                    Id = c.Id,
                    Reasoning = reasoningText
                }).ToList();
            }
            catch
            {
                // fallback
                reasoningResults = req.Candidates.Select(c => new ReasoningResult
                {
                    Id = c.Id,
                    Reasoning = "Reasoning unavailable"
                }).ToList();
            }

            return Ok(reasoningResults);
        }





        // DTOs
        public class ReasoningRequest
        {
            public string Query { get; set; }
            public List<SearchResult2> Candidates { get; set; }
        }

        public class ReasoningResult
        {
            public int Id { get; set; }
            public string Reasoning { get; set; }
        }




        public class RerankScore
        {
            public int Index { get; set; }
            public float Score { get; set; }
        }

      

        // Helper class to deserialize the reranker response
        public class RankedCandidate
        {
            public string text { get; set; }
            public double score { get; set; }
            public string Reasoning { get; set; }
        }






        //639eIe2QKkwnNUCRrOqMLzUQa2VoICAMxW7Nzavf

        //   public class RerankResult
        //   {
        //       public int ResultId { get; set; }
        //       public float Score { get; set; }
        //   }

        //   private async Task<List<SearchResult>> RerankWithDeepSeek(string query, List<SearchResult> candidates)
        //   {
        //       if (candidates == null || !candidates.Any())
        //           return candidates;

        //       using (var client = new HttpClient { BaseAddress = new Uri("http://localhost:11434") })
        //       {
        //           // Build the prompt
        //           var prompt =
        //"You are a strict ranking function.\n" +
        //"Use ONLY the Query provided below. Do not infer any other scenario.\n" +
        //"Return ONLY a raw JSON array (no markdown, no <think>, no text). Use 0-based indices.\n" +
        //"Format: [{\"index\": number, \"score\": number}]\n\n" +
        //"Query: " + query + "\n\nResults:\n" +
        //string.Join("\n", candidates.Select((c, i) => $"{i}: {c.Name} - {c.Snippet}")) +
        //"\n\nTask: Rerank the results by relevance to the Query. Output only the JSON array.";

        //           var payload = new
        //           {
        //               model = "deepseek-r1:1.5b",
        //               prompt = prompt,
        //               stream = false
        //           };

        //           var json = JsonConvert.SerializeObject(payload);
        //           using (var content = new StringContent(json, Encoding.UTF8, "application/json"))
        //           {
        //               var response = await client.PostAsync("/api/generate", content);
        //               var result = await response.Content.ReadAsStringAsync();

        //               if (!response.IsSuccessStatusCode)
        //                   throw new Exception($"Ollama API error {response.StatusCode}: {result}");

        //               // Ollama returns JSON per line, so parse carefully
        //               var lines = result.Split(new[] { '\n' }, StringSplitOptions.RemoveEmptyEntries);
        //               var outputText = "";
        //               foreach (var line in lines)
        //               {
        //                   var obj = JObject.Parse(line);
        //                   if (obj["response"] != null)
        //                       outputText += obj["response"].ToString();
        //               }

        //               // Parse the JSON array returned by the model
        //               var rankedIndices = JArray.Parse(outputText)
        //                   .Select(r => new
        //                   {
        //                       Index = r["index"].Value<int>(),
        //                       Score = r["score"].Value<float>()
        //                   })
        //                   .OrderByDescending(x => x.Score)
        //                   .ToList();

        //               // Reorder candidates according to the rank
        //               var reranked = rankedIndices.Select(x => candidates[x.Index]).ToList();
        //               return reranked;
        //           }
        //       }
        //   }














        // === 3. Highlight matching words in snippet ===
        private string HighlightHighestScoreLine(string body, string query)
        {
            if (string.IsNullOrWhiteSpace(body) || string.IsNullOrWhiteSpace(query))
                return body;

            var words = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                             .Select(Regex.Escape)
                             .ToArray();
            if (words.Length == 0) return body;

            var lines = Regex.Split(body, @"(?<=[\.!?])\s+|\r?\n");
            string bestLine = null;
            int bestScore = 0;

            foreach (var line in lines)
            {
                int score = words.Count(word => Regex.IsMatch(line, @"\b" + word + @"\b", RegexOptions.IgnoreCase));
                if (score > bestScore)
                {
                    bestScore = score;
                    bestLine = line;
                }
            }

            if (bestLine == null) return body;

            string pattern = @"\b(" + string.Join("|", words) + @")\b";
            return Regex.Replace(bestLine, pattern, "<mark>$1</mark>", RegexOptions.IgnoreCase).Trim();
        }

        // === 4. Numeric extraction from query ===
        private double? ExtractIncomeFromQuery(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            var match = Regex.Match(query, @"RM\s?([\d,]+)", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var numberStr = match.Groups[1].Value.Replace(",", "");
            if (double.TryParse(numberStr, out double monthlyIncome))
                return monthlyIncome * 12; // Convert monthly to annual

            return null;
        }

        // === 5. Numeric extraction from document ===
        private double? ExtractDocIncome(JToken payloadData)
        {
            if (payloadData == null) return null;

            string incomeStr = payloadData.Value<string>("MinimumAnnualIncome");
            if (string.IsNullOrWhiteSpace(incomeStr)) return null;

            incomeStr = incomeStr.Replace("RM", "").Replace(",", "").Trim();
            if (double.TryParse(incomeStr, out double docIncome))
                return docIncome;

            return null;
        }

        // === 6. Hybrid score computation ===
        private float ComputeHybridScore(float semanticScore, double? docIncome, double? queryIncome)
        {
            if (!docIncome.HasValue || !queryIncome.HasValue)
                return semanticScore; // fallback

            double numericScore = 1.0 / (1.0 + Math.Abs(docIncome.Value - queryIncome.Value));
            return (float)(semanticScore * 0.7 + numericScore * 0.3);
        }

        // === 7. Store recent searches in cookie ===
        private void StoreRecentSearch(string query)
        {
            var httpContext = (HttpContextBase)Request.Properties["MS_HttpContext"];
            var request = httpContext.Request;
            var response = httpContext.Response;

            var cookie = request.Cookies["recentSearches"];
            List<string> searches = new List<string>();

            if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
            {
                searches = HttpUtility.UrlDecode(cookie.Value)
                    .Split('|')
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(4)
                    .ToList();
            }

            if (!searches.Contains(query, StringComparer.OrdinalIgnoreCase))
                searches.Insert(0, query);

            searches = searches.Take(5).ToList();

            var newCookie = new HttpCookie("recentSearches", HttpUtility.UrlEncode(string.Join("|", searches)))
            {
                Expires = DateTime.Now.AddDays(7)
            };
            response.Cookies.Set(newCookie);
        }


        private double? ExtractQueryIncome(string query)
        {
            if (string.IsNullOrWhiteSpace(query)) return null;

            // Match RM10,000, 10,000 RM, etc.
            var match = Regex.Match(query, @"RM\s?([\d,]+)", RegexOptions.IgnoreCase);
            if (!match.Success) return null;

            var numberStr = match.Groups[1].Value.Replace(",", "");
            if (!double.TryParse(numberStr, out double income))
                return null;

            // Convert monthly income to annual if query mentions month/monthly
            if (Regex.IsMatch(query, @"\bmonth|monthly\b", RegexOptions.IgnoreCase))
                income *= 12;

            return income;
        }



    }
}

public class RerankResult
{
    public int ResultId { get; set; }
    public float Score { get; set; }
}


