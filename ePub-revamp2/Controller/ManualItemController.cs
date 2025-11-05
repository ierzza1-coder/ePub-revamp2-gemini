using System.Web.Mvc;
using Umbraco.Core.Composing;
using Umbraco.Core.Models;
using Umbraco.Core.Models.PublishedContent;
using Umbraco.Core.Services;
using Umbraco.Web;
using Umbraco.Web.Mvc;

public class ManualItemViewModel
{
    public IPublishedContent Content { get; set; }
    public int ViewCount { get; set; }
}

namespace ePub.Controllers
{

    public class ManualItemController : Umbraco.Web.Mvc.SurfaceController
    {
        private readonly IContentService _contentService;

        public ManualItemController()
            : base()
        {
            _contentService = Current.Services.ContentService;
        }

        public ActionResult RenderManualItem(int nodeId)
        {

            return Content("Hello from ManualItemController");

            var content = Umbraco.Content(nodeId);
            if (content == null)
            {
                return HttpNotFound();
            }

            IncrementViewCount(nodeId);

            var viewModel = new ManualItemViewModel
            {
                Content = content,
                ViewCount = GetViewCount(nodeId)
            };

            return PartialView("ManualItem", viewModel);
        }

        private void IncrementViewCount(int contentId)
        {
            var content = _contentService.GetById(contentId);
            if (content != null)
            {
                var currentCount = content.GetValue<int>("viewCount");
                content.SetValue("viewCount", currentCount + 1);
                _contentService.SaveAndPublish(content);
            }
        }

        private int GetViewCount(int contentId)
        {
            var content = _contentService.GetById(contentId);
            return content?.GetValue<int>("viewCount") ?? 0;
        }
    }
}
