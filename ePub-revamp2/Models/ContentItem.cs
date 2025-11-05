using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ePub.Models
{

        public class ContentItem
        {
            public string folderId { get; set; }
            public string parentsId { get; set; }
            public string Id { get; set; }
            public string Title { get; set; }
            public string Body { get; set; }
            public float[] Embedding { get; set; }
        }
    }


