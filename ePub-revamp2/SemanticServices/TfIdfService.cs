using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace ManualTFIDFDemo.Services
{
    public class TfIdfService
    {
        private Dictionary<int, Dictionary<string, double>> _docVectors = new Dictionary<int, Dictionary<string, double>>();
        private Dictionary<string, double> _idf = new Dictionary<string, double>();

        private static readonly HashSet<string> StopWords = new HashSet<string>
{
    "the","and","is","to","of","a","in","for","on","with","at","by","an","be","this","does","what"
};


        // --- Tokenize ---
        public List<string> Tokenize(string text)
        {
            // Cast MatchCollection to IEnumerable<Match> before using LINQ
            return Regex.Matches(text, @"\b[a-z]{2,}\b")
                        .Cast<Match>()                        // <--- cast needed
                        .Select(m => m.Value)
                        .Where(w => !StopWords.Contains(w))
                        .ToList();
        }

        // --- Build Vectors ---
        public void Build(Dictionary<int, string> files)
        {
            var tokenizedFiles = new Dictionary<int, List<string>>();

            foreach (var kv in files)
            {
                tokenizedFiles[kv.Key] = Tokenize(kv.Value);
            }

            ComputeIdf(tokenizedFiles);
            ComputeTfIdf(tokenizedFiles);
        }

        private void ComputeIdf(Dictionary<int, List<string>> tokenizedFiles)
        {
            int totalDocs = tokenizedFiles.Count;
            var wordDocs = new Dictionary<string, int>();

            foreach (var tokens in tokenizedFiles.Values)
            {
                foreach (var word in tokens.Distinct())
                {
                    if (wordDocs.ContainsKey(word))
                        wordDocs[word] += 1;
                    else
                        wordDocs[word] = 1;
                }
            }


            _idf = wordDocs.ToDictionary(w => w.Key, w => Math.Log((double)totalDocs / (1 + w.Value)));
        }

        private void ComputeTfIdf(Dictionary<int, List<string>> tokenizedFiles)
        {
            foreach (var kv in tokenizedFiles)
            {
                var tokens = kv.Value;

                // Compute term frequency (TF)
                var tf = tokens.GroupBy(w => w)
                               .ToDictionary(
                                   g => g.Key,
                                   g => (double)g.Count() / tokens.Count
                               );

                // Compute TF-IDF
                var tfidf = new Dictionary<string, double>();
                foreach (var t in tf)
                {
                    double idfValue = _idf.ContainsKey(t.Key) ? _idf[t.Key] : 0; // C# 7.3 compatible
                    tfidf[t.Key] = t.Value * idfValue;
                }

                _docVectors[kv.Key] = tfidf;
            }

        }

        // --- Cosine Similarity ---
        private double CosineSimilarity(Dictionary<string, double> v1, Dictionary<string, double> v2)
        {
            var intersection = v1.Keys.Intersect(v2.Keys);
            double dot = intersection.Sum(k => v1[k] * v2[k]);
            double mag1 = Math.Sqrt(v1.Values.Sum(x => x * x));
            double mag2 = Math.Sqrt(v2.Values.Sum(x => x * x));
            if (mag1 == 0 || mag2 == 0) return 0;
            return dot / (mag1 * mag2);
        }

        // --- Search Query ---
        public List<(int FileId, double Score)> Search(string query, int topN = 3)
        {
            var queryTokens = Tokenize(query);

            // Compute TF for query
            var tfQuery = queryTokens.GroupBy(w => w)
                                     .ToDictionary(
                                         g => g.Key,
                                         g => (double)g.Count() / queryTokens.Count
                                     );

            // Compute TF-IDF for query using manual IDF check
            var queryVector = new Dictionary<string, double>();
            foreach (var kv in tfQuery)
            {
                double idfValue = _idf.ContainsKey(kv.Key) ? _idf[kv.Key] : 0;
                queryVector[kv.Key] = kv.Value * idfValue;
            }

            // Compute cosine similarity for each document
            var results = new List<(int FileId, double Score)>();
            foreach (var doc in _docVectors)
            {
                double score = CosineSimilarity(queryVector, doc.Value);
                results.Add((doc.Key, score));
            }

            // Sort by descending score and take top N
            results.Sort((a, b) => b.Score.CompareTo(a.Score));
            if (results.Count > topN)
                results = results.GetRange(0, topN);

            return results;
        }


        // --- Expose DocVectors and IDF for saving ---
        public Dictionary<int, Dictionary<string, double>> GetDocVectors() => _docVectors;
        public Dictionary<string, double> GetIdf() => _idf;
        public void SetVectors(Dictionary<int, Dictionary<string, double>> docVectors, Dictionary<string, double> idf)
        {
            _docVectors = docVectors;
            _idf = idf;
        }
    }
}
