//using ePub.Services;
//using Newtonsoft.Json;
//using Newtonsoft.Json.Linq;
//using System;
//using System.Linq;
//using System.Net.Http;
//using System.Text;
//using System.Threading.Tasks;
//using System.Web.Http;
//using Umbraco.Web.WebApi;

//using System.Threading.Tasks;
//using System.Web.Http;

//namespace ePub_revamp2.Controllers
//{
//    public class ChatController : UmbracoApiController
//    {
//        private static readonly HttpClient _http = new HttpClient();
//        private const string QdrantUrl = "http://localhost:6333"; // adjust
//        private const string CollectionName = "your_collection";


//        [HttpPost]
//        public async Task<IHttpActionResult> Chat([FromBody] ChatRequest req)
//        {

//            if (req == null || string.IsNullOrWhiteSpace(req.Query))
//                return BadRequest("Query is required");

//            var ollama = new OllamaService();

//            try
//            {
//                // Example: call your Ollama service
//                var reply = await ollama.GenerateAsync("llama3", req.Query);

//                return Ok(new { reply });
//            }
//            catch (Exception ex)
//            {
//                // Log error so you can see what’s wrong
//                //_Umbraco.Logger.Error<ChatController>(ex, "Chat endpoint failed");
//                return InternalServerError(ex);
//            }
//        }
    


//    public class ChatRequest
//    {
//        public string Query { get; set; }
//}


//        private async Task<float[]> GetEmbeddingAsync(string text)
//        {
//            // your embedding code calling Ollama embed
//            throw new NotImplementedException();
//        }
//    }

//    public class ChatRequest
//    {
//        public string Query { get; set; }
//    }
//}
