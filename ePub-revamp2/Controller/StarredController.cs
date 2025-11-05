using System.Web.Http;
using Umbraco.Web.WebApi;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Web;

namespace ePub_revamp2.Controller
{
    public class StarredController : UmbracoApiController
    {
        [HttpGet]
        public IHttpActionResult GetManualContentById(int id)
        {
            var content = Umbraco.Content(id);
            if (content == null) return NotFound();

            return Ok(new
            {
                id = content.Id,
                name = content.Name,
                manualName = content.Parent.Value<string>("title"),
                url = content.Url,
                updateDate = content?.UpdateDate.ToString("d MMMM yyyy"),
                content = content.Value("bodyText") // Adjust alias to match your content
            });
        }
    }
}