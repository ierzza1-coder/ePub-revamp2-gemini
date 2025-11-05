using ePub.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace ePub_revamp2.Services
{
    public static class SpellLibrary
    {
        public static HashSet<string> BuildLibraryFromBodies(List<ContentItem> items)
        {
            var library = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var item in items)
            {
                if (string.IsNullOrWhiteSpace(item.Body))
                    continue;

                // Split into words
                var words = item.Body
                    .ToLower()
                    .Split(new char[] { ' ', '\t', '\n', '\r', '.', ',', ';', ':', '-', '_', '(', ')', '[', ']', '{', '}', '"', '\'' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (var word in words)
                {
                    // Skip very short words
                    if (word.Length > 2)
                        library.Add(word);
                }
            }

            return library;
        }
    }
}