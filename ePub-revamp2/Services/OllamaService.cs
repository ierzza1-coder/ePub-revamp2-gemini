using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using RestSharp;
using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace ePub.Services
{
    public class OllamaService
    {
        private static readonly HttpClient _http;

        // Static constructor ensures HttpClient is initialized once
        static OllamaService()
        {
            _http = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:11434/")
            };
        }

        public float[] GetEmbedding(string text)
        {
            var client = new RestClient("http://localhost:11434");
            var request = new RestRequest("/api/embeddings", Method.Post);

            var body = new
            {
                model = "mxbai-embed-large",
                prompt = text
            };
            request.AddJsonBody(body);

            var response = client.Execute(request);

            if (!response.IsSuccessful || string.IsNullOrWhiteSpace(response.Content))
                throw new Exception("Failed to get embedding: " + response.Content);

            var result = JsonConvert.DeserializeObject<EmbeddingResponse>(response.Content);

            if (result?.embedding == null)
                throw new Exception("No embeddings returned: " + response.Content);

            return result.embedding;
        }

        private class EmbeddingResponse
        {
            public float[] embedding { get; set; }
        }

        public async Task<string> GenerateAsync(string model, string userPrompt)
        {
            var payload = new
            {
                model = model,
                messages = new[]
                {
                    new { role = "user", content = userPrompt }
                },
                stream = false
            };

            var json = JsonConvert.SerializeObject(payload);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            // Full relative URI works now because BaseAddress is set
            var resp = await _http.PostAsync("http://localhost:11434/api/chat", content);

            resp.EnsureSuccessStatusCode();

            var responseString = await resp.Content.ReadAsStringAsync();
            var jobj = JObject.Parse(responseString);

            return jobj["message"]?["content"]?.ToString() ?? "";
        }
    }
}
