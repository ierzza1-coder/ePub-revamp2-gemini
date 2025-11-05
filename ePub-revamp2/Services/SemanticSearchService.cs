//using ePub.Models;
//using ePub.Services;
//using System;
//using System.Collections.Generic;
//using System.Linq;

//namespace ePub.Services
//{
//    public class SemanticSearchService
//    {
//        private readonly List<ContentItem> _items;
//        private readonly GeminiService _ollama;

//        public SemanticSearchService(List<ContentItem> items)
//        {
//            _items = items;
//            _ollama = new GeminiService();
//        }

//        public List<ContentItem> Search(string query)
//        {
//            var queryEmbedding = _ollama.GetEmbedding(query);
//            float MIN_SIMILARITY = 0.6f;

//            return _items
//                .Select(item => new
//                {
//                    Item = item,
//                    Score = CosineSimilarity(queryEmbedding, item.Embedding)
//                })
//                .OrderByDescending(x => x.Score)
//                .Take(5)
//                .Select(x => x.Item)
//                .ToList();
//        }

//        private float CosineSimilarity(float[] v1, float[] v2)
//        {
//            float dot = 0f, mag1 = 0f, mag2 = 0f;
//            for (int i = 0; i < v1.Length; i++)
//            {
//                dot += v1[i] * v2[i];
//                mag1 += v1[i] * v1[i];
//                mag2 += v2[i] * v2[i];
//            }
//            return (float)(dot / (Math.Sqrt(mag1) * Math.Sqrt(mag2)));
//        }
//    }
//}
