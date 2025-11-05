using System;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text.RegularExpressions;

namespace ePub.Services
{
    /// <summary>
    /// Service to interact with Ollama (embeddings) and Qdrant (vector store).
    /// Class name: QdrantOllamaService
    /// </summary>
    public class QdrantOllamaService : IDisposable
    {
        private readonly HttpClient _httpClient;
        private readonly string _qdrantBaseUrl;
        private readonly string _ollamaBaseUrl;
        private readonly string _collectionName;
        private readonly string _modelName;

        public QdrantOllamaService(
            string qdrantBaseUrl = "http://localhost:6333",
            string ollamaBaseUrl = "http://localhost:11434",
            string collectionName = "umbraco_docs",
            string modelName = "nomic-embed-text")
        {
            _qdrantBaseUrl = qdrantBaseUrl.TrimEnd('/');
            _ollamaBaseUrl = ollamaBaseUrl.TrimEnd('/');
            _collectionName = collectionName;
            _modelName = modelName;
            _httpClient = new HttpClient();
            // Optional: set timeouts or default headers here
        }

        // Ensure the collection exists in Qdrant. If not, detect embedding size (from model) and create it.
        public async Task EnsureCollectionExistsAsync()
        {
            var url = $"{_qdrantBaseUrl}/collections/{_collectionName}";
            var resp = await _httpClient.GetAsync(url);

            if (resp.IsSuccessStatusCode)
            {
                // collection exists
                return;
            }

            // If collection not found, detect embedding size and create it
            int vectorSize = await GetEmbeddingVectorSizeAsync();
            var createPayload = new
            {
                vectors = new
                {
                    size = vectorSize,
                    distance = "Cosine"
                }
            };

            var createJson = JsonConvert.SerializeObject(createPayload);
            var putResp = await _httpClient.PutAsync(
                $"{_qdrantBaseUrl}/collections/{_collectionName}",
                new StringContent(createJson, Encoding.UTF8, "application/json"));

            if (!putResp.IsSuccessStatusCode)
            {
                var body = await putResp.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create Qdrant collection. Status: {putResp.StatusCode}, Body: {body}");
            }
        }

        // Get embedding vector size by asking the model for a small embedding and returning length.
        public async Task<int> GetEmbeddingVectorSizeAsync()
        {
            var emb = await GetEmbeddingAsync("test vector size detection");
            return emb.Length;
        }

        // Request embedding from Ollama for the given text
        public async Task<float[]> GetEmbeddingAsync(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                text = ".";

            var payloadObj = new
            {
                model = _modelName,
                input = text
            };

            var json = JsonConvert.SerializeObject(payloadObj);
            var resp = await _httpClient.PostAsync(
                $"{_ollamaBaseUrl}/api/embeddings",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var respBody = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Ollama embeddings request failed: {resp.StatusCode}. Body: {respBody}");
            }

            var j = JObject.Parse(respBody);

            // Ollama shape: { "embedding": [ ... ] } (this is typical)
            var arr = j["embedding"] as JArray;
            if (arr == null)
            {
                throw new Exception("Unexpected Ollama response format: 'embedding' array not found. Response: " + respBody);
            }

            var floats = arr.Select(t => t.Value<float>()).ToArray();
            return floats;
        }

        // Index or upsert a single document into Qdrant
        public async Task IndexDocumentAsync(long id, string title, string body, IDictionary<string, object> metadata = null)
        {
            // prepare text for embedding (concatenate fields; keep under model token limits)
            var combined = $"{title}\n\n{StripHtml(body)}";
            var vector = await GetEmbeddingAsync(combined);

            // build payload
            var payloadDict = new Dictionary<string, object>
            {
                { "title", title ?? "" },
                { "body", StripHtml(body) ?? "" }
            };

            if (metadata != null)
            {
                foreach (var kv in metadata)
                {
                    payloadDict[kv.Key] = kv.Value;
                }
            }

            var upsertObj = new
            {
                points = new[] {
                    new {
                        id = id,
                        vector = vector,
                        payload = payloadDict
                    }
                }
            };

            var json = JsonConvert.SerializeObject(upsertObj);
            var resp = await _httpClient.PutAsync(
                $"{_qdrantBaseUrl}/collections/{_collectionName}/points",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Failed to index document into Qdrant: {resp.StatusCode}. Body: {respText}");
            }
        }

        // Bulk index a list of documents (simple implementation, sends them in batches)
        public async Task IndexDocumentsBulkAsync(IEnumerable<Document> docs, int batchSize = 64)
        {
            var batch = new List<object>(batchSize);
            foreach (var doc in docs)
            {
                var combined = $"{doc.Title}\n\n{StripHtml(doc.Body)}";
                var vector = await GetEmbeddingAsync(combined);

                var payloadDict = new Dictionary<string, object>
                {
                    { "title", doc.Title ?? "" },
                    { "body", StripHtml(doc.Body) ?? "" }
                };

                if (doc.Metadata != null)
                {
                    foreach (var kv in doc.Metadata)
                        payloadDict[kv.Key] = kv.Value;
                }

                batch.Add(new
                {
                    id = doc.Id,
                    vector = vector,
                    payload = payloadDict
                });

                if (batch.Count >= batchSize)
                {
                    await SendBatchAsync(batch);
                    batch.Clear();
                }
            }

            if (batch.Count > 0)
                await SendBatchAsync(batch);
        }

        private async Task SendBatchAsync(List<object> batch)
        {
            var upsertObj = new { points = batch };
            var json = JsonConvert.SerializeObject(upsertObj);
            var resp = await _httpClient.PutAsync(
                $"{_qdrantBaseUrl}/collections/{_collectionName}/points",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Qdrant bulk upsert failed: {resp.StatusCode}. Body: {respText}");
            }
        }

        // Search Qdrant by converting query -> vector -> search
        public async Task<List<SearchResult>> SearchAsync(string query, int limit = 10)
        {
            var queryVector = await GetEmbeddingAsync(query);
            var payload = new
            {
                vector = queryVector,
                limit = limit,
                with_payload = true,
                // with_vector = false    // optional
            };

            var json = JsonConvert.SerializeObject(payload);
            var resp = await _httpClient.PostAsync(
                $"{_qdrantBaseUrl}/collections/{_collectionName}/points/search",
                new StringContent(json, Encoding.UTF8, "application/json"));

            var respText = await resp.Content.ReadAsStringAsync();
            if (!resp.IsSuccessStatusCode)
            {
                throw new Exception($"Qdrant search failed: {resp.StatusCode}. Body: {respText}");
            }

            var j = JObject.Parse(respText);
            var results = new List<SearchResult>();

            // Qdrant typical response: { "result": [ { "id":..., "payload": {...}, "score": ... }, ... ] }
            var arr = j["result"] as JArray;
            if (arr != null)
            {
                foreach (var item in arr)
                {
                    var id = item["id"]?.Value<long>() ?? 0;
                    var score = item["score"]?.Value<float?>() ?? item["payload"]?.Value<float?>("score") ?? 0f;
                    var payloadObj = item["payload"] as JObject;

                    var title = payloadObj?["title"]?.Value<string>();
                    var body = payloadObj?["body"]?.Value<string>();

                    // you can include the whole payload as dictionary
                    var payloadDict = payloadObj != null
                        ? payloadObj.Properties().ToDictionary(p => p.Name, p => (object)p.Value.ToString())
                        : new Dictionary<string, object>();

                    results.Add(new SearchResult
                    {
                        Id = id,
                        Score = score,
                        Title = title,
                        Body = body,
                        Payload = payloadDict
                    });
                }
            }
            else
            {
                // fallback - attempt to find a 'result' array at a different path
                throw new Exception("Unexpected Qdrant search response: " + respText);
            }

            return results;
        }

        // VERY simple HTML stripper; replace with HtmlAgilityPack if you want better results
        public static string StripHtml(string input)
        {
            if (string.IsNullOrEmpty(input)) return string.Empty;
            // Replace common HTML entities
            input = System.Net.WebUtility.HtmlDecode(input);
            // remove tags
            var noTags = Regex.Replace(input, "<.*?>", " ");
            // collapse whitespace
            noTags = Regex.Replace(noTags, @"\s{2,}", " ").Trim();
            return noTags;
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }

        // Helpful DTOs
        public class Document
        {
            public long Id { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }
            public IDictionary<string, object> Metadata { get; set; }
        }

        public class SearchResult
        {
            public long Id { get; set; }
            public float Score { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }
            public IDictionary<string, object> Payload { get; set; }
        }
    }
}
