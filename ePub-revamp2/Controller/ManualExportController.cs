//using ePub.Models;
//using ePub.Services;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Text.RegularExpressions;
//using System.Web.Http;
//using Umbraco.Core.Models.PublishedContent;
//using Umbraco.Web;
//using Umbraco.Web.WebApi;

//namespace ePub_revamp2.Controller
//{
//    public class ManualExportController : UmbracoApiController
//    {
//        private static readonly HttpClient _http = new HttpClient();

//        // change to your Qdrant URL (local or cloud)
//        private const string QdrantUrl = "http://localhost:6333";
//        private const string CollectionName = "manuals";

//        [HttpGet]
//        public IHttpActionResult ExportManualsToQdrant(string currentCulture)
//        {
//            string folderId = "";
//            if (currentCulture.Contains("pbb")) folderId = "1207";
//            else if (currentCulture.Contains("pibb")) folderId = "1319";

//            var rootParentId = folderId;
//            var parent = Umbraco.Content(folderId);
//            if (parent == null)
//                return NotFound();

//            var parentItems = parent.Children.Where(x => x.IsVisible()).ToList();
//            var ollama = new OllamaService();

//            var allPoints = new List<object>();
//            int vectorSize = -1;

//            foreach (var item in parentItems)
//            {
//                ProcessItem(item, rootParentId, ollama, ref vectorSize, allPoints);

//                foreach (var child in item.Children.Where(c => c.IsVisible()))
//                {
//                    ProcessItem(child, rootParentId, ollama, ref vectorSize, allPoints);
//                }
//            }



//            // ensure collection exists
//            EnsureCollection(CollectionName, vectorSize);

//            // upsert points to Qdrant
//            UpsertPoints(CollectionName, allPoints);

//            return Ok(new
//            {
//                message = "Manuals exported to Qdrant.",
//                collection = CollectionName,
//                itemCount = allPoints.Count
//            });
//        }

//        private void ProcessItem(IPublishedContent content, string rootParentId, OllamaService ollama, ref int vectorSize, List<object> allPoints)
//        {
//            string parentId = content.Parent?.Id.ToString() ?? rootParentId;
//            string id = content.Id.ToString();
//            string body = CleanBody(content.Value<string>("bodyContent") ?? "");
//            string title = content.Name ?? "";

//            string combined = (title + " " + body).Trim();
//            if (string.IsNullOrWhiteSpace(combined))
//                return;

//            float[] embedding = ollama.GetEmbedding(combined);

//            if (vectorSize < 0)
//                vectorSize = embedding.Length;

//            allPoints.Add(new
//            {
//                id = long.Parse(id),
//                vector = embedding,
//                payload = new
//                {
//                    folderId = rootParentId,
//                    parentsId = parentId,
//                    umbracoId = id,
//                    title,
//                    body
//                }
//            });
//        }

//        private static string CleanBody(string input)
//        {
//            string noHtml = Regex.Replace(input, "<.*?>", string.Empty);
//            string singleLine = Regex.Replace(noHtml, @"[\r\n\t]+", " ");
//            singleLine = singleLine.Replace("&amp;", "and");
//            string decoded = System.Net.WebUtility.HtmlDecode(singleLine);
//            return decoded.Trim();
//        }

//        private void EnsureCollection(string name, int vectorSize)
//        {
//            var body = new
//            {
//                vectors = new
//                {
//                    size = vectorSize,
//                    distance = "Cosine"
//                }
//            };
//            var resp = _http.PutAsync(
//                $"{QdrantUrl}/collections/{name}",
//                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
//            ).Result;

//            if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
//            {
//                // collection exists, so just return
//                return;
//            }

//            resp.EnsureSuccessStatusCode();
//        }

//        private void UpsertPoints(string name, List<object> points)
//        {
//            var body = new { points };
//            var resp = _http.PutAsync(
//                $"{QdrantUrl}/collections/{name}/points?wait=true",
//                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
//            ).Result;

//            resp.EnsureSuccessStatusCode();
//        }
//    }
//}



//2

using ePub.Models;
using ePub.Services;
using HtmlAgilityPack;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Http;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.WebApi;

namespace ePub_revamp2.Controller
{
    public class ManualExportController : UmbracoApiController
    {
        private static readonly HttpClient _http = new HttpClient();

        private const string QdrantUrl = "http://localhost:6333";
        private const string CollectionName = "manuals2";

        [HttpGet]
        public IHttpActionResult ExportManualsToQdrant(string currentCulture)
        {
            try
            {
                // Map culture to root folder ID
                string folderId = currentCulture.Contains("pbb") ? "1207" :
                                  currentCulture.Contains("pibb") ? "1319" : null;

                if (string.IsNullOrEmpty(folderId))
                    return BadRequest("Invalid culture: cannot determine folder ID.");

                var parent = Umbraco.Content(folderId);
                if (parent == null)
                    return NotFound();

                var parentItems = parent.Children.Where(x => x.IsVisible()).ToList();
                if (!parentItems.Any())
                    return Ok("No manuals found to export.");

                var ollama = new OllamaService();
                var allPoints = new List<object>();
                int vectorSize = -1;

                // Process parent and children manuals
                foreach (var item in parentItems)
                {
                    ProcessItemWithChunks(item, folderId, ollama, ref vectorSize, allPoints);

                    foreach (var child in item.Children.Where(c => c.IsVisible()))
                    {
                        ProcessItemWithChunks(child, folderId, ollama, ref vectorSize, allPoints);
                    }
                }

                if (allPoints.Count == 0)
                    return Ok("No content with embeddings to export.");

                // Create collection if missing
                EnsureCollection(CollectionName, vectorSize);

                // Upsert points
                UpsertPoints(CollectionName, allPoints);

                return Ok(new
                {
                    message = "Manuals exported to Qdrant with chunking.",
                    collection = CollectionName,
                    vectorSize,
                    itemCount = allPoints.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        /// <summary>
        /// Process a single item and chunk its body content before embedding
        /// </summary>
        private void ProcessItemWithChunks(IPublishedContent item, string folderId, OllamaService ollama,
                                           ref int vectorSize, List<object> allPoints)
        {
            string title = item.Name;
            string body = item.Value<string>("bodyContent") ?? "";

            body = StripHtml(body);

            // Split into chunks of 1000–1200 characters with 200 character overlap
            foreach (var chunk in ChunkText(body, 1200, 200))
            {
                var embedding = ollama.GetEmbedding(chunk);
                if (embedding == null) continue;

                if (vectorSize == -1)
                    vectorSize = embedding.Length;

                var point = new
                {
                    id = Guid.NewGuid().ToString(),
                    vector = embedding,
                    payload = new
                    {
                        title,
                        chunk,
                        nodeId = item.Id,
                        parentId = folderId
                    }
                };

                allPoints.Add(point);
            }
        }


private string StripHtml(string input)
    {
        if (string.IsNullOrEmpty(input)) return string.Empty;

        // 1. Remove HTML tags
        string output = Regex.Replace(input, "<.*?>", string.Empty);

        // 2. Decode HTML entities
        output = WebUtility.HtmlDecode(output);

        // 3. Remove *empty lines* only (lines that are whitespace or nothing)
        output = Regex.Replace(output, @"^\s*$[\r\n]*", string.Empty, RegexOptions.Multiline);

        // 4. Trim spaces on each line but preserve line breaks
        output = Regex.Replace(output, @"[ \t]+", " "); // collapse spaces/tabs
        output = Regex.Replace(output, @"\s+\r?\n", "\n"); // trim trailing spaces before newline

        return output.Trim();
    }



    /// <summary>
    /// Split text into overlapping chunks
    /// </summary>
    private List<string> ChunkText(string text, int chunkSize, int overlap)
        {
            var chunks = new List<string>();

            if (string.IsNullOrWhiteSpace(text))
                return chunks;

            int start = 0;
            while (start < text.Length)
            {
                int length = Math.Min(chunkSize, text.Length - start);
                chunks.Add(text.Substring(start, length));
                start += (chunkSize - overlap); // move forward with overlap
                if (start < 0) break; // safety
            }

            return chunks;
        }


        //private IEnumerable<string> ChunkText(string text, int chunkSize = 600)
        //{
        //    if (string.IsNullOrWhiteSpace(text))
        //        yield break;

        //    for (int i = 0; i < text.Length; i += chunkSize)
        //    {
        //        yield return text.Substring(i, Math.Min(chunkSize, text.Length - i));
        //    }
        //}




        [HttpGet]
        public IHttpActionResult ExportManualsLibraryFuzzy(string currentCulture)
        {
            try
            {
                // Map culture to folder ID
                string folderId = currentCulture.Contains("pbb") ? "1207" :
                                  currentCulture.Contains("pibb") ? "1319" : null;

                if (string.IsNullOrEmpty(folderId))
                    return BadRequest("Invalid culture: cannot determine folder ID.");

                var parent = Umbraco.Content(folderId);
                if (parent == null)
                    return NotFound();

                var allLines = new List<string>();

                foreach (var item in parent.Children.Where(x => x.IsVisible()))
                {
                    allLines.Add(CleanText(item.Name + " " + item.Value<string>("body")));
                    foreach (var child in item.Children.Where(c => c.IsVisible()))
                    {
                        allLines.Add(CleanText(child.Name + " " + child.Value<string>("body")));
                    }
                }

                string filePath = HttpContext.Current.Server.MapPath("~/App_Data/FuzzyLibrary.txt");
                File.WriteAllLines(filePath, allLines);

                return Ok(new
                {
                    message = "Fuzzy search library exported.",
                    file = filePath,
                    count = allLines.Count
                });
            }
            catch (Exception ex)
            {
                return InternalServerError(ex);
            }
        }

        private string CleanText(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";

            string noHtml = Regex.Replace(input, "<.*?>", string.Empty);
            string decoded = System.Net.WebUtility.HtmlDecode(noHtml);
            decoded = decoded.Replace("&amp;", "and");
            decoded = Regex.Replace(decoded, @"\s+", " ").Trim();

            var words = decoded.Split(' ');
            var result = new List<string>();
            string lastWord = "";

            foreach (var w in words)
            {
                if (!string.Equals(w, lastWord, StringComparison.OrdinalIgnoreCase))
                {
                    result.Add(w);
                    lastWord = w;
                }
            }
            return string.Join(" ", result);
        }


        /// <summary>
        /// Remove HTML tags and decode entities
        /// </summary>
        private string CleanHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return "";
            string noHtml = Regex.Replace(input, "<.*?>", string.Empty);
            return System.Net.WebUtility.HtmlDecode(noHtml)
                         .Replace("&amp;", "and") // Replace &amp; with and
                         .Trim();
        }






        //private void ProcessItem(IPublishedContent item, string folderId, OllamaService ollama, ref int vectorSize, List<object> allPoints)
        //{
        //    var title = item.Name;
        //    var body = item.Value<string>("bodyContent"); // adjust alias as needed

        //    // Split into 600-char chunks
        //    var chunks = ChunkText(body, 600).ToList();

        //    foreach (var (chunk, index) in chunks.Select((c, i) => (c, i)))
        //    {
        //        // Generate embedding for each chunk
        //        var embedding = ollama.GetEmbedding(chunk);

        //        if (vectorSize == -1)
        //            vectorSize = embedding.Length;

        //        allPoints.Add(new
        //        {
        //            id = $"{item.Id}-{index}", // unique ID per chunk
        //            vector = embedding,
        //            payload = new
        //            {
        //                itemId = item.Id,
        //                folderId,
        //                title,
        //                chunkIndex = index,
        //                text = chunk
        //            }
        //        });
        //    }
        //}


        private static string CleanBody(string input)
        {
            string noHtml = Regex.Replace(input, "<.*?>", string.Empty);
            string singleLine = Regex.Replace(noHtml, @"[\r\n\t]+", " ");
            singleLine = singleLine.Replace("&amp;", "and");
            string decoded = System.Net.WebUtility.HtmlDecode(singleLine);
            return decoded.Trim();
        }

        private void EnsureCollection(string name, int vectorSize)
        {
            var body = new
            {
                vectors = new
                {
                    size = vectorSize,
                    distance = "Cosine"
                }
            };

            var resp = _http.PutAsync(
                $"{QdrantUrl}/collections/{name}",
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            ).Result;

            if (resp.StatusCode == System.Net.HttpStatusCode.Conflict)
            {
                // Already exists
                return;
            }

            resp.EnsureSuccessStatusCode();
        }

        private void UpsertPoints(string name, List<object> points)
        {
            var body = new { points };
            var resp = _http.PutAsync(
                $"{QdrantUrl}/collections/{name}/points?wait=true",
                new StringContent(JsonConvert.SerializeObject(body), Encoding.UTF8, "application/json")
            ).Result;

            resp.EnsureSuccessStatusCode();
        }
    }
}
