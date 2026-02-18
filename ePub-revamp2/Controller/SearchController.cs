using ePub.Models;
using ePub.Services;
using ePub_revamp2.Services;
using ePub2.SemanticServices;
using Examine;
using Examine.LuceneEngine;
using Examine.Search;
using FuzzySharp;
using HtmlAgilityPack;
using Lucene.Net.Search;
using ManualTFIDFDemo.Services;
using NHunspell;
using StackExchange.Profiling.Internal;
using System;
using System.Collections.Generic;
using System.Drawing.Printing;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using System.Web.UI.WebControls;
using Umbraco.Core;
using Umbraco.Core.Models;
using Umbraco.Web;
using Umbraco.Web.Mvc;
using static Umbraco.Core.Constants.Conventions;

namespace ePub.Controllers
{
    public class SearchSurfaceController : SurfaceController
    {
        [HttpGet]
        public JsonResult AjaxSearch(string query, string matchType, string searchIn, string currentCulture, string sortBy, int page = 1, string currentCategory = "")
        {
            try
            {
                int pageSize = 10;
                string folderId = "";

                StoreRecentSearch(query);

                if (currentCulture == "pbb")
                {
                    folderId = "1207";
                }
                else if (currentCulture == "pibb")
                {
                    folderId = "1319";
                }


                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = false, message = "Empty query." }, JsonRequestBehavior.AllowGet);
                }

                if (!ExamineManager.Instance.TryGetIndex("ExternalIndex", out IIndex index))
                {
                    return Json(new { success = false, message = "Search index not found." }, JsonRequestBehavior.AllowGet);
                }

                string[] fields = null;
                var searcher = index.GetSearcher();

                if (searchIn.Equals("all"))
                {
                    fields = new[] { "title", "bodyContent"};
                }
                else if (searchIn.Equals("title"))
                {
                    fields = new[] { "title" };
                }
                else if (searchIn.Equals("bodycontent"))
                {
                    fields = new[] { "bodyContent" };
                }
                else if (searchIn.Equals("url"))
                {
                    fields = new[] { "urlName" };
                }


                //var terms = query
                //.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                //.Select(word => word.MultipleCharacterWildcard())
                //.ToArray();

                ISearchResults results = null;
                var queryBuilder = searcher.CreateQuery("content");

                switch (matchType?.ToLowerInvariant())
                {
                    case "exact":
                        results = queryBuilder.GroupedOr(fields, query).Execute();
                        break;

                    case "all":
                        var words = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (words.Length > 0)
                        {
                            var tempQuery = queryBuilder.GroupedOr(fields.ToList(), words[0].MultipleCharacterWildcard());

                            for (int i = 1; i < words.Length; i++)
                            {
                                tempQuery = tempQuery.And().GroupedOr(fields.ToList(), words[i].MultipleCharacterWildcard());
                            }

                            results = tempQuery.Execute();
                        }
                        break;

                    case "any":
                    default:
                        var terms = query
                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => word.MultipleCharacterWildcard())
                            .ToArray();

                        results = queryBuilder.GroupedOr(fields, terms).Execute();
                        break;
                }

                //var terms2 = query
                //.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                //.Select(word => word.MultipleCharacterWildcard())
                //.ToArray();

                //results = queryBuilder.GroupedOr(new[] { "title" }, terms2).Execute();

                //var results = searcher
                //        .CreateQuery("content")
                //        .GroupedOr(fields, terms)
                //        .Execute();

                var searchSelection = "";

                //if(results.TotalItemCount == 0)
                //{
                //    searchSelection = "fuzzy";



                ////    var termsFuzzy = query
                ////.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                ////.Select(word => word.Fuzzy())
                ////.ToArray();


                ////    results = searcher
                ////    .CreateQuery("content")
                ////    .GroupedOr(fields, termsFuzzy)          // fuzzy match
                ////    .Execute();                 
                //}



                string didYouMean = null;

                if (results.TotalItemCount == 0)
                {
                    searchSelection = "fuzzy";

                    // Suggest correction
                    didYouMean = SuggestCorrection(query);

                    if (!string.Equals(didYouMean, query, StringComparison.OrdinalIgnoreCase))
                    {
                        // Optional: run fuzzy search on suggested term
                        var termsFuzzy = didYouMean
                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => word.Fuzzy())
                            .ToArray();

                        results = searcher
                            .CreateQuery("content")
                            .GroupedOr(fields, termsFuzzy)
                            .Execute();
                    }
                }






                var formattedResults = results
                    .Where(r =>
                        r.Values.ContainsKey("__Path") &&
                        r.Values["__Path"].Split(',').Contains(folderId) &&
                        r.Values.ContainsKey("__NodeTypeAlias") &&
                        r.Values["__NodeTypeAlias"].Equals("manualItem", StringComparison.OrdinalIgnoreCase))
                    .Select(r =>
                    {
                        var content = Umbraco.Content(r.Id);
                        if (content == null)
                            return null;

                        // Check parent tags only if category is specified
                        if (!string.IsNullOrWhiteSpace(currentCategory))
                        {
                            var parent = content.Parent;
                            if (parent == null || !parent.HasProperty("tagging"))
                                return null;

                            var tags = parent.Value<IEnumerable<string>>("tagging");
                            if (tags == null || !tags.Contains(currentCategory, StringComparer.OrdinalIgnoreCase))
                                return null;
                        }

                        var rawBody = content.Value<string>("bodyContent") ?? "";
                        var plainText = StripHtmlTags(rawBody);

                        var bodyContent = GetSnippetWithHighlight(plainText, query, matchType);

                        if (bodyContent == null)
                        {
                            var html = content.Value<string>("bodyContent");
                            var doc = new HtmlDocument();
                            doc.LoadHtml(html);

                            var liNodes = doc.DocumentNode.SelectNodes("//li");
                            string firstTextOnly = "";

                            if (liNodes != null)
                            {
                                var firstThree = liNodes.Take(3)
                                    .Select(li =>
                                    {
                                        var firstTextNode = li.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlAgilityPack.HtmlNodeType.Text);
                                        return firstTextNode != null
                                            ? firstTextNode.InnerText.Trim()
                                            : li.InnerText.Trim();
                                    });

                                // Example: combine with newlines or bullets
                                bodyContent = string.Join("<br>", firstThree); // Or use "<br>" for HTML
                            }

                        }

                        return new
                        {
                            name = content.Name,
                            title = content?.Value<string>("title"),
                            manualName = content.Parent.Value<string>("title"),
                            url = content.Url,
                            didYouMean,
                            score = r.Score,
                            updateDate = content?.UpdateDate.ToString("d MMMM yyyy"),
                            bodyContent = bodyContent,
                        };
                    })
                    .Where(r => r != null) // Remove nulls from rejected category
                    .ToList();

                if (sortBy != null)
                {
                    switch (sortBy.ToLower())
                    {
                        case "title":
                            formattedResults = formattedResults.OrderBy(r => r.title).ToList();
                            break;
                        case "relevance":
                            formattedResults = formattedResults.OrderByDescending(r => r.score).ToList();
                            break;
                        case "date-asc":
                            formattedResults = formattedResults.OrderBy(r => r.updateDate).ToList();
                            break;
                        case "date-desc":
                            formattedResults = formattedResults.OrderByDescending(r => r.updateDate).ToList();
                            break;
                        default:
                            formattedResults = formattedResults.OrderByDescending(r => r.score).ToList();
                            break;
                    }
                }


                var highestScoredResult = formattedResults
                .OrderByDescending(r => r.score)
                .FirstOrDefault();


                var highestTitle = highestScoredResult?.name;

                int totalResults = formattedResults.Count;
                int totalPages = (int)Math.Ceiling((double)totalResults / pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedResults = formattedResults
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return Json(new
                {
                    success = true,
                    totalResults,
                    currentPage = page,
                    searchSelection,
                    highestTitle, // ⬅ Add this
                    pageSize,
                    totalPages,
                    results = pagedResults
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    stack = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
            }
        }



        public JsonResult getManualFromSearch(string resultId)
        {
            var content = Umbraco.Content(resultId);


            return Json(new
            {
                name = content.Name,
                title = content?.Value<string>("title"),
                manualName = content.Parent.Value<string>("title"),
                url = content.Url,
                updateDate = content?.UpdateDate.ToString("d MMMM yyyy"),
                bodyContent = content?.Value<string>("bodyContent"),

            }, JsonRequestBehavior.AllowGet);

        }



        [HttpGet]
        public JsonResult AjaxSearch2(string query, string matchType, string searchIn, string currentCulture, string sortBy, int page = 1, string currentCategory = "")
        {
            try
            {
                int pageSize = 10;
                string folderId = "";

                StoreRecentSearch(query);

                if (currentCulture == "pbb")
                {
                    folderId = "1207";
                }
                else if (currentCulture == "pibb")
                {
                    folderId = "1319";
                }


                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = false, message = "Empty query." }, JsonRequestBehavior.AllowGet);
                }

                if (!ExamineManager.Instance.TryGetIndex("ExternalIndex", out IIndex index))
                {
                    return Json(new { success = false, message = "Search index not found." }, JsonRequestBehavior.AllowGet);
                }

                string[] fields = null;
                var searcher = index.GetSearcher();

                if (searchIn.Equals("all"))
                {
                    fields = new[] { "title", "bodyContent", "umbracoUrlName", "mainText", "description" };
                }
                else if (searchIn.Equals("title"))
                {
                    fields = new[] { "title" };
                }
                else if (searchIn.Equals("bodycontent"))
                {
                    fields = new[] { "bodyContent" };
                }
                else if (searchIn.Equals("url"))
                {
                    fields = new[] { "urlName" };
                }


                //var terms = query
                //.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                //.Select(word => word.MultipleCharacterWildcard())
                //.ToArray();

                ISearchResults results = null;
                var queryBuilder = searcher.CreateQuery("content");

                switch (matchType?.ToLowerInvariant())
                {
                    case "exact":
                        results = queryBuilder.GroupedOr(fields, query).Execute();

                        if (!results.Any())
                        {
                            results = queryBuilder.GroupedOr(fields, query.Fuzzy(0.7f)).Execute();
                        }

                        break;

                    case "all":
                        var words = query.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);

                        if (words.Length > 0)
                        {
                            var tempQuery = queryBuilder.GroupedOr(fields.ToList(), words[0].MultipleCharacterWildcard());

                            for (int i = 1; i < words.Length; i++)
                            {
                                tempQuery = tempQuery.And().GroupedOr(fields.ToList(), words[i].MultipleCharacterWildcard());
                            }

                            results = tempQuery.Execute();


                            if (!results.Any())
                            {
                                var fuzzyWords = words.Select(w => w.Fuzzy(0.7f)).ToArray();

                                var fuzzyQuery = queryBuilder.GroupedOr(fields.ToList(), fuzzyWords[0]);

                                for (int i = 1; i < fuzzyWords.Length; i++)
                                {
                                    fuzzyQuery = fuzzyQuery.And().GroupedOr(fields.ToList(), fuzzyWords[i]);
                                }

                                results = fuzzyQuery.Execute();
                            }

                        }
                        break;

                    case "any":
                    default:
                        var terms = query
                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => word)
                            .ToArray();

                        results = queryBuilder.GroupedOr(fields, terms).Execute();

                        if (!results.Any())
                        {
                            var fuzzyTerms = terms.Select(t => t.Fuzzy(0.7f)).ToArray();
                            results = queryBuilder.GroupedOr(fields, fuzzyTerms).Execute();
                        }


                        break;
                }



                //var results = searcher
                //        .CreateQuery("content")
                //        .GroupedOr(fields, terms)
                //        .Execute();

                var searchSelection = "";

                //if(results.TotalItemCount == 0)
                //{
                //    searchSelection = "fuzzy";



                ////    var termsFuzzy = query
                ////.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                ////.Select(word => word.Fuzzy())
                ////.ToArray();


                ////    results = searcher
                ////    .CreateQuery("content")
                ////    .GroupedOr(fields, termsFuzzy)          // fuzzy match
                ////    .Execute();                 
                //}



                string didYouMean = null;

                if (results.TotalItemCount == 0)
                {
                    searchSelection = "fuzzy";

                    didYouMean = SuggestCorrection(query);
                    string searchTermForFuzzy = didYouMean;
                    bool correctionApplied = !string.Equals(didYouMean, query, StringComparison.OrdinalIgnoreCase);

                    if (string.IsNullOrWhiteSpace(searchTermForFuzzy))
                    {
                        searchTermForFuzzy = query;
                    }

                    var termsFuzzy = searchTermForFuzzy
                        .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                        .Select(word => word.Fuzzy())
                        .ToArray();

                    results = searcher
                        .CreateQuery("content")
                        .GroupedOr(fields, termsFuzzy)
                        .Execute();
                }

                var formattedResults = results
                    .Where(r =>
                        r.Values.ContainsKey("__Path") &&
                        r.Values["__Path"].Split(',').Contains(folderId) &&
                        r.Values.ContainsKey("__NodeTypeAlias") &&
                        r.Values["__NodeTypeAlias"].Equals("manualItem", StringComparison.OrdinalIgnoreCase))
                    .Select(r =>
                    {
                        var content = Umbraco.Content(r.Id);
                        if (content == null)
                            return null;

                        // Check parent tags only if category is specified
                        if (!string.IsNullOrWhiteSpace(currentCategory))
                        {
                            var parent = content.Parent;
                            if (parent == null || !parent.HasProperty("tagging"))
                                return null;

                            var tags = parent.Value<IEnumerable<string>>("tagging");
                            if (tags == null || !tags.Contains(currentCategory, StringComparer.OrdinalIgnoreCase))
                                return null;
                        }

                        var rawBody = content.Value<string>("bodyContent") ?? "";
                        var plainText = StripHtmlTags(rawBody);

                        var bodyContent = "";


                        if (searchIn == "all" || searchIn == "bodycontent")
                        {
                             bodyContent = GetSnippetWithHighlight(plainText, query, matchType);
                        }

                        if (bodyContent == null)
                        {
                            var html = content.Value<string>("bodyContent");
                            var doc = new HtmlDocument();
                            doc.LoadHtml(html);

                            var liNodes = doc.DocumentNode.SelectNodes("//li");
                            string firstTextOnly = "";

                            if (liNodes != null)
                            {
                                var firstThree = liNodes.Take(3)
                                    .Select(li =>
                                    {
                                        var firstTextNode = li.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlAgilityPack.HtmlNodeType.Text);
                                        return firstTextNode != null
                                            ? firstTextNode.InnerText.Trim()
                                            : li.InnerText.Trim();
                                    });

                                // Example: combine with newlines or bullets
                                bodyContent = string.Join("<br>", firstThree); // Or use "<br>" for HTML
                            }

                        }

                        return new
                        {
                            name = content.Name,
                            title = content?.Value<string>("title"),
                            manualName = content.Parent.Value<string>("title"),
                            url = content.Url,
                            directedurl = content.Parent.Url + "#" + content.Name,
                            score = r.Score,
                            updateDate = content?.UpdateDate.ToString("d MMMM yyyy"),
                            bodyContent = bodyContent,
                        };
                    })
                    .Where(r => r != null) // Remove nulls from rejected category
                    .ToList();

                if (sortBy != null)
                {
                    switch (sortBy.ToLower())
                    {
                        case "title":
                            formattedResults = formattedResults.OrderBy(r => r.title).ToList();
                            break;
                        case "relevance":
                            formattedResults = formattedResults.OrderByDescending(r => r.score).ToList();
                            break;
                        case "date-asc":
                            formattedResults = formattedResults.OrderBy(r => r.updateDate).ToList();
                            break;
                        case "date-desc":
                            formattedResults = formattedResults.OrderByDescending(r => r.updateDate).ToList();
                            break;
                        default:
                            formattedResults = formattedResults.OrderByDescending(r => r.score).ToList();
                            break;
                    }
                }


                var highestScoredResult = formattedResults
                .OrderByDescending(r => r.score)
                .FirstOrDefault();


                var highestTitle = highestScoredResult?.name;

                int totalResults = formattedResults.Count;
                int totalPages = (int)Math.Ceiling((double)totalResults / pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedResults = formattedResults
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return Json(new
                {
                    success = true,
                    totalResults,
                    currentPage = page,
                    searchSelection,
                    highestTitle, // ⬅ Add this
                    pageSize,
                    didYouMean,
                    totalPages,
                    results = pagedResults
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    stack = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
            }
        }

























        //search recomendation engine

        [HttpGet]
        public JsonResult searchRecommendation(string query, string currentCulture, string currentCategory)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new string[] { }, JsonRequestBehavior.AllowGet);

            if (!ExamineManager.Instance.TryGetIndex("ExternalIndex", out IIndex index))
                return Json(new string[] { }, JsonRequestBehavior.AllowGet);

            var searcher = index.GetSearcher();
            var fields = new[] {"title"};
            string folderId = "";


            if (currentCulture.Contains("pbb"))
            {
                folderId = "1207";
            }
            else if (currentCulture.Contains("pibb"))
            {
                folderId = "1319";
            }


            var terms = query
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.MultipleCharacterWildcard()) // use wildcard for prefix search
            .ToArray();

          var results = searcher
                .CreateQuery("content")
                .GroupedOr(fields, terms) 
                .Execute()
                .Where(r =>
                    r.Values.ContainsKey("__Path") &&
                    r.Values["__Path"].Split(',').Contains(folderId) &&
                    r.Values.ContainsKey("__NodeTypeAlias") &&
                    r.Values["__NodeTypeAlias"].Equals("manualItem", StringComparison.OrdinalIgnoreCase))
                .Select(r =>
                {
                    var content = Umbraco.Content(r.Id);


                    // Check parent tags only if category is specified
                    if (!string.IsNullOrWhiteSpace(currentCategory))
                    {
                        var parent = content.Parent;
                        if (parent == null || !parent.HasProperty("tagging"))
                            return null;

                        var tags = parent.Value<IEnumerable<string>>("tagging");
                        if (tags == null || !tags.Contains(currentCategory, StringComparer.OrdinalIgnoreCase))
                            return null;
                    }


                    return new
                    {
                        name = content?.Name,
                        title = content?.Value<string>("title") // Replace "title" with your actual alias if different
                    };
                })
                .Where(item => !string.IsNullOrWhiteSpace(item?.name) || !string.IsNullOrWhiteSpace(item?.title))
                .Distinct()
                .Take(10)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }




        [HttpGet]
        public JsonResult searchRecommendationManual(string query, string currentCulture, string currentId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new string[] { }, JsonRequestBehavior.AllowGet);

            if (!ExamineManager.Instance.TryGetIndex("ExternalIndex", out IIndex index))
                return Json(new string[] { }, JsonRequestBehavior.AllowGet);

            var searcher = index.GetSearcher();
            var fields = new[] { "bodyContent" };

            var terms = query
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.MultipleCharacterWildcard()) // use wildcard for prefix search
            .ToArray();

            var results = searcher
                .CreateQuery("content")
                .GroupedOr(fields, terms)
                .Execute()
                .Where(r =>
                    r.Values.ContainsKey("__Path") &&
                    r.Values["__Path"].Split(',').Contains(currentId) &&
                    r.Values.ContainsKey("__NodeTypeAlias") &&
                    r.Values["__NodeTypeAlias"].Equals("manualItem", StringComparison.OrdinalIgnoreCase))
                .Select(r =>
                {
                    var content = Umbraco.Content(r.Id);

                    var rawBody = content.Value<string>("bodyContent") ?? "";
                    var plainText = StripHtmlTags(rawBody);

                    return new
                    {
                        name = content?.Name,
                        title = content?.Value<string>("title"), // Replace "title" with your actual alias if different
                        bodyContent = GetSnippetWithHighlight(plainText, query, "all")
                    };
                })
                .Where(item => !string.IsNullOrWhiteSpace(item?.name) || !string.IsNullOrWhiteSpace(item?.title))
                .Distinct()
                .Take(5)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }

        //if search bank, will higlight banking also
        //private string GetSnippetWithHighlight(string input, string query, string matchType)
        //{
        //    if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(query))
        //        return "";

        //    string highlighted = input;
        //    string pattern = "";

        //    if (matchType == "exact")
        //    {
        //        // Match exact phrase
        //        pattern = Regex.Escape(query);
        //        highlighted = Regex.Replace(input, pattern, m => $"<mark>{m.Value}</mark>", RegexOptions.IgnoreCase);
        //    }
        //    else
        //    {
        //        // Split into words
        //        var words = query
        //            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
        //            .Select(Regex.Escape)
        //            .ToList();

        //        // Only proceed if:
        //        // - Any word exists in input for matchType=any
        //        // - ALL words exist for matchType=all
        //        bool shouldHighlight = matchType == "any"
        //            ? words.Any(word => Regex.IsMatch(input, word, RegexOptions.IgnoreCase))
        //            : words.All(word => Regex.IsMatch(input, word, RegexOptions.IgnoreCase));

        //        if (shouldHighlight)
        //        {
        //            foreach (var word in words)
        //            {
        //                highlighted = Regex.Replace(highlighted, word, m => $"<mark>{m.Value}</mark>", RegexOptions.IgnoreCase);
        //            }
        //        }
        //        else
        //        {
        //            highlighted = input;
        //        }
        //    }

        //    // Get first match and its surrounding context
        //    var snippetMatch = Regex.Match(highlighted, $@"(.{{0,150}}<mark>.*?</mark>.{{0,500}})", RegexOptions.IgnoreCase);
        //    if (snippetMatch.Success)
        //    {
        //        return snippetMatch.Value;
        //    }

        //    // Fallback
        //    return highlighted.Substring(0, Math.Min(100, highlighted.Length));
        //}

        private string GetSnippetWithHighlight(string input, string query, string matchType)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(query))
                return "";

            string highlighted = input;
            string pattern = "";

            if (matchType == "exact")
            {
                // Match the exact phrase as a whole word
                pattern = $@"\b{Regex.Escape(query)}\b";
                highlighted = Regex.Replace(input, pattern, m => $"<mark>{m.Value}</mark>", RegexOptions.IgnoreCase);
            }
            else
            {
                var words = query
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(word => $@"\b{Regex.Escape(word)}\b") // Add word boundaries
                    .ToList();

                bool shouldHighlight = matchType == "any"
                    ? words.Any(w => Regex.IsMatch(input, w, RegexOptions.IgnoreCase))
                    : words.All(w => Regex.IsMatch(input, w, RegexOptions.IgnoreCase));

                if (shouldHighlight)
                {
                    foreach (var word in words)
                    {
                        highlighted = Regex.Replace(highlighted, word, m => $"<mark style=\"font-weight: bold\">{m.Value}</mark>", RegexOptions.IgnoreCase);
                    }
                }
                else
                {
                    highlighted = input;
                }


            }

            // Extract snippet with highlight in context
            var snippetMatch = Regex.Match(highlighted, $@"(.{{0,150}}<mark style=\""font-weight: bold\"">.*?</mark>.{{0,500}})", RegexOptions.IgnoreCase);
            if (snippetMatch.Success)
            {
                return snippetMatch.Value + "...";
            }

            // Fallback: if no <mark> found, return the first 100 characters or the query
            return string.IsNullOrWhiteSpace(highlighted)
                ? query
                : highlighted.Substring(0, Math.Min(300, highlighted.Length)) + "...";
        }

        public static string StripHtmlTags(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return string.Empty;

            // Remove all HTML tags
            string noHtml = Regex.Replace(input, "<.*?>", string.Empty);

            // Optionally decode HTML entities like &nbsp;
            return System.Net.WebUtility.HtmlDecode(noHtml);
        }



        private void StoreRecentSearch(string query)
        {
            var request = ControllerContext.HttpContext.Request;
            var response = ControllerContext.HttpContext.Response;

            var cookie = request.Cookies["recentSearches"];
            List<string> searches = new List<string>();

            if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
            {
                searches = HttpUtility.UrlDecode(cookie.Value)
                    .Split('|')
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .Distinct(StringComparer.OrdinalIgnoreCase)
                    .Take(4) // keep 4 to add new one for total 5
                    .ToList();
            }

            if (!searches.Contains(query, StringComparer.OrdinalIgnoreCase))
            {
                searches.Insert(0, query); // newest first
            }

            // Limit to 5
            searches = searches.Take(5).ToList();

            var newCookie = new HttpCookie("recentSearches", HttpUtility.UrlEncode(string.Join("|", searches)))
            {
                Expires = DateTime.Now.AddDays(7)
            };

            response.Cookies.Set(newCookie);
        }

        [HttpGet]
        public JsonResult GetRecentSearches()
        {
            var request = ControllerContext.HttpContext.Request;

            var cookie = request.Cookies["recentSearches"];
            List<string> searches = new List<string>();

            if (cookie != null && !string.IsNullOrWhiteSpace(cookie.Value))
            {
                searches = HttpUtility.UrlDecode(cookie.Value)
                    .Split('|')
                    .Where(q => !string.IsNullOrWhiteSpace(q))
                    .ToList();
            }

            return Json(new { success = true, searches }, JsonRequestBehavior.AllowGet);
        }

        //public string SuggestCorrection(string input)
        //{
        //    var affEn = HostingEnvironment.MapPath("~/App_Data/Hunspell/en_US.aff");
        //    var dicEn = HostingEnvironment.MapPath("~/App_Data/Hunspell/en_US.dic");

        //    var affMy = HostingEnvironment.MapPath("~/App_Data/Hunspell/ms_MY.aff");
        //    var dicMy = HostingEnvironment.MapPath("~/App_Data/Hunspell/ms_MY.dic");

        //    using (var hunspellEn = new Hunspell(affEn, dicEn))
        //    using (var hunspellMy = new Hunspell(affMy, dicMy))
        //    {
        //        var corrected = input.Split(' ')
        //            .Select(word =>
        //            {
        //                if (hunspellEn.Spell(word) || hunspellMy.Spell(word))
        //                    return word;

        //                var suggestion = hunspellMy.Suggest(word).FirstOrDefault()
        //                                 ?? hunspellEn.Suggest(word).FirstOrDefault();

        //                return suggestion ?? word;
        //            });

        //        return string.Join(" ", corrected);
        //    }
        //}


        public string SuggestCorrection(string input)
        {
            var affPath = HostingEnvironment.MapPath("~/App_Data/Hunspell/en_US.aff");
            var dicPath = HostingEnvironment.MapPath("~/App_Data/Hunspell/en_US.dic");

            using (var hunspell = new Hunspell(affPath, dicPath))
            {
                var suggestions = input
                    .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(word =>
                    {
                        if (!hunspell.Spell(word))
                        {
                            var sugg = hunspell.Suggest(word);
                            return sugg.FirstOrDefault() ?? word;
                        }
                        return word;
                    });

                return string.Join(" ", suggestions);
            }
        }


        //search guideline page
        public JsonResult SearchInManuals(string query, string currentCulture, int page = 1)
        {
            try
            {
                int pageSize = 10;

                if (string.IsNullOrWhiteSpace(query))
                {
                    return Json(new { success = false, message = "Empty query." }, JsonRequestBehavior.AllowGet);
                }

                if (!ExamineManager.Instance.TryGetIndex("ExternalIndex", out IIndex index))
                {
                    return Json(new { success = false, message = "Search index not found." }, JsonRequestBehavior.AllowGet);
                }

                string folderId = "";

                if (currentCulture.Contains("pbb"))
                {
                    folderId = "1207";
                }
                else if (currentCulture.Contains("pibb"))
                {
                    folderId = "1319";
                }



                var searcher = index.GetSearcher();
                var queryBuilder = searcher.CreateQuery("content");

                            var terms = query
                .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.MultipleCharacterWildcard())
                .ToArray();

                ISearchResults results = queryBuilder
                    .GroupedOr(new[] { "title" }, terms)
                    .Execute();

                string searchSelection = "";
                string didYouMean = null;

                if (results.TotalItemCount == 0)
                {
                    searchSelection = "fuzzy";
                    didYouMean = SuggestCorrection(query);

                    if (!string.Equals(didYouMean, query, StringComparison.OrdinalIgnoreCase))
                    {
                        var fuzzyTerms = didYouMean
                            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                            .Select(word => word.Fuzzy())
                            .ToArray();

                        results = searcher
                            .CreateQuery("content")
                            .GroupedOr(new[] { "title" }, fuzzyTerms)
                            .Execute();
                    }
                }

                var formattedResults = results
                    .Where(r =>
                        r.Values.ContainsKey("__Path") &&
                        r.Values["__Path"].Split(',').Contains(folderId) &&
                        r.Values.ContainsKey("__NodeTypeAlias") &&
                        r.Values["__NodeTypeAlias"].Equals("manual", StringComparison.OrdinalIgnoreCase))
                    .Select(r =>
                    {
                        var content = Umbraco.Content(r.Id);
                        if (content == null) return null;

                        return new
                        {
                            title = content.Value<string>("title"),
                            url = content.Url,
                            updateDate = content.UpdateDate.ToString("d MMMM yyyy"),
                            score = r.Score,
                            didYouMean
                        };
                    })
                    .Where(x => x != null)
                    .OrderByDescending(r => r.score)
                    .ToList();

                int totalResults = formattedResults.Count;
                int totalPages = (int)Math.Ceiling((double)totalResults / pageSize);
                page = Math.Max(1, Math.Min(page, totalPages));

                var pagedResults = formattedResults
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize);

                return Json(new
                {
                    success = true,
                    totalResults,
                    currentPage = page,
                    pageSize,
                    totalPages,
                    searchSelection,
                    didYouMean,
                    results = pagedResults
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new
                {
                    success = false,
                    message = ex.Message,
                    stack = ex.StackTrace
                }, JsonRequestBehavior.AllowGet);
            }
        }




        //for help
        [HttpGet]
        public JsonResult searchRecommendationHelp(string query, string helpId)
        {
            if (string.IsNullOrWhiteSpace(query))
                return Json(new string[] { }, JsonRequestBehavior.AllowGet);

            if (!ExamineManager.Instance.TryGetIndex("ExternalIndex", out IIndex index))
                return Json(new string[] { }, JsonRequestBehavior.AllowGet);

            var searcher = index.GetSearcher();
            var fields = new[] { "bodyContent" };

            var terms = query
            .Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(word => word.MultipleCharacterWildcard()) // use wildcard for prefix search
            .ToArray();

            var results = searcher
                .CreateQuery("content")
                .GroupedOr(fields, terms)
                .Execute()
                .Where(r =>
                    r.Values.ContainsKey("__Path") &&
                    r.Values["__Path"].Split(',').Contains(helpId) &&
                    r.Values.ContainsKey("__NodeTypeAlias") &&
                    r.Values["__NodeTypeAlias"].Equals("manualItem", StringComparison.OrdinalIgnoreCase))
                .Select(r =>
                {
                    var content = Umbraco.Content(r.Id);

                    var rawBody = content.Value<string>("bodyContent") ?? "";
                    var plainText = StripHtmlTags(rawBody);

                    return new
                    {
                        name = content?.Name,
                        title = content?.Value<string>("title"), // Replace "title" with your actual alias if different
                        bodyContent = GetSnippetWithHighlight(plainText, query, "all"),
                        parents = content?.Parent.Value<string>("title")
                    };
                })
                .Where(item => !string.IsNullOrWhiteSpace(item?.name) || !string.IsNullOrWhiteSpace(item?.title))
                .Distinct()
                .Take(5)
                .ToList();

            return Json(results, JsonRequestBehavior.AllowGet);
        }


        [HttpPost]
        public ActionResult BuildVectors()
        {
            try
            {
                // Step 1: Map file IDs to HTML file paths
                var files = new Dictionary<int, string>
                {
                    {1, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc.html"},
                    {2, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc1.html"},
                    {3, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc3.html"},
                    {4, @"C:\Users\pbbuser\Desktop\will-dnv3\brproc4.html"},
                    {5, @"C:\Users\pbbuser\Desktop\will-dnv3\contact.html"},
                    {6, @"C:\Users\pbbuser\Desktop\will-dnv3\default.html"},
                    {7, @"C:\Users\pbbuser\Desktop\will-dnv3\estate.html"},
                    {8, @"C:\Users\pbbuser\Desktop\will-dnv3\FAQ.html"},
                    {9, @"C:\Users\pbbuser\Desktop\will-dnv3\fc-compare.html"},
                    {10, @"C:\Users\pbbuser\Desktop\will-dnv3\fees.html"},
                    {11, @"C:\Users\pbbuser\Desktop\will-dnv3\guideG-S.html"},
                    {12, @"C:\Users\pbbuser\Desktop\will-dnv3\pbtsb.html"},
                    {13, @"C:\Users\pbbuser\Desktop\will-dnv3\sampleforms.html"},
                    {14, @"C:\Users\pbbuser\Desktop\will-dnv3\willwrite.html"}
                };

                // Step 2: Extract text from HTML files
                var fileTexts = new Dictionary<int, string>();
                foreach (var kv in files)
                {
                    fileTexts[kv.Key] = HtmlTextExtractor.ReadHtmlFile(kv.Value);
                }

                // Step 3: Build TF-IDF vectors
                var tfidf = new TfIdfService();
                tfidf.Build(fileTexts);

                // Save vectors to .txt
                string vectorFile = @"C:\source\ePub-revamp2 - 1208 - gemini\ePub-revamp2\tfidf_vector.txt";
                FileVectorStorage.Save(vectorFile, tfidf.GetDocVectors(), tfidf.GetIdf());

                return Content("TF-IDF vectors built and saved successfully.");
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }

        private readonly  string vectorFile = @"C:\source\ePub-revamp2 - 1208 - gemini\ePub-revamp2\tfidf_vector.txt";

        [HttpGet]
        public ActionResult SearchVectors(string q, int topN = 3)
        {
            if (string.IsNullOrWhiteSpace(q))
                return Content("Query cannot be empty.");

            try
            {
                // Step 4: Load vectors from .txt
                var tfidf = new TfIdfService();
                var loadedData = FileVectorStorage.Load(vectorFile);
                tfidf.SetVectors(loadedData.Item1, loadedData.Item2);

                // Step 5–7: Compute query TF-IDF + cosine similarity + ranking
                var results = tfidf.Search(q, topN);

                // Step 8: Return top matching file IDs + scores
                var resultText = "Top matching files:\n";

                object resultItem = null;

                foreach (var r in results)
                {
                    resultText += string.Format("File {0} -> Score: {1:F4}\n", r.FileId, r.Score);


                    var content = Umbraco.Content(r.FileId);
                    if (content == null)
                        return null;

                    var rawBody = content.Value<string>("bodyContent") ?? "";
                    var plainText = StripHtmlTags(rawBody);

                    var bodyContent = "";

                    if (bodyContent == null)
                    {
                        var html = content.Value<string>("bodyContent");
                        var doc = new HtmlDocument();
                        doc.LoadHtml(html);

                        var liNodes = doc.DocumentNode.SelectNodes("//li");
                        string firstTextOnly = "";

                        if (liNodes != null)
                        {
                            var firstThree = liNodes.Take(3)
                                .Select(li =>
                                {
                                    var firstTextNode = li.ChildNodes.FirstOrDefault(n => n.NodeType == HtmlAgilityPack.HtmlNodeType.Text);
                                    return firstTextNode != null
                                        ? firstTextNode.InnerText.Trim()
                                        : li.InnerText.Trim();
                                });

                            // Example: combine with newlines or bullets
                            bodyContent = string.Join("<br>", firstThree); // Or use "<br>" for HTML
                        }

                    }

                     resultItem = new
                    {
                        name = content.Name,
                        title = content?.Value<string>("title"),
                        manualName = content.Parent.Value<string>("title"),
                        url = content.Url,
                        directedurl = content.Parent.Url + "#" + content.Name,
                        score = r.Score,
                        updateDate = content?.UpdateDate.ToString("d MMMM yyyy"),
                        bodyContent = bodyContent
                    };
                }

                return Json(new
                {
                    success = true,
                    totalResults = 5,
                    currentPage = 1,
                    searchSelection = "null",
                    highestTitle = "null", // ⬅ Add this
                    pageSize = 5,
                    didYouMean = "null",
                    totalPages = 1,
                    results = resultItem
                }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Content("Error: " + ex.Message);
            }
        }



    }
}
