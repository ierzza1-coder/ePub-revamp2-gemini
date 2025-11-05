using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Http;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;
using Umbraco.Web.WebApi;

namespace ePub_revamp2.Controller
{
    public class ManualListController : UmbracoApiController
    {
        // GET: ManualList
        [HttpGet]
        public IHttpActionResult GetManualsUnder1207(string currentCulture)
        {

            var folderId = "";

            if (currentCulture.Contains("pbb"))
            {
                folderId = "1207";
            }
            else if (currentCulture.Contains("pibb"))
            {
                folderId = "1319";
            }



            var parent = Umbraco.Content(folderId); // Parent node ID
            if (parent == null)
                return NotFound();

            var children = parent.Children
                .Where(x => x.IsVisible())
                 .OrderBy(x => x.Value<string>("title"))
                .Select(x => new
                {
                    id = x.Id,
                    name = x.Value("title"),
                    url = x.Url,
                    folder= x.Parent.Value<string>("title"),
                    date= x.UpdateDate.ToString("d MMMM yyyy"),
                    urlName = x.Value("umbracoUrlName"),

                    children = x.Children
                        .Where(c => c.IsVisible())
                        .Select(c => new
                        {
                            id = c.Id,
                            name = c.Value("title"),
                            url = c.Url,
                            date = c.UpdateDate.ToString("d MMMM yyyy")
                        })
                }).ToList();

            var result = new
            {
                total = children.Count,
                items = children
            };

            return Ok(result);
        }

    }
}