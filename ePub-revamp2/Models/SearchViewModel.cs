using System.Collections.Generic;

namespace ePub2.Models
{
    public class SearchResult2
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string ParentName { get; set; }
        public string Url { get; set; }
        public string directedUrl { get; set; }
        public string publishedDate { get; set; }
        public string Snippet { get; set; }
        public string body { get; set; }
        public float Score { get; set; }
        public int TriggerMatches { get; set; } // new
        public int rank { get; set; }
        public string suggestion { get; set; }

        public string Reasoning { get; set; } = null;
    }

}
