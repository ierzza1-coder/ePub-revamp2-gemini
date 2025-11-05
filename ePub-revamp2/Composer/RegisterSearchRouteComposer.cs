using System.Web.Mvc;
using System.Web.Routing;
using Umbraco.Core;
using Umbraco.Core.Composing;

namespace ePub.Composers
{
    public class RegisterSearchRouteComposer : IUserComposer
    {
        public void Compose(Composition composition)
        {
            composition.Components().Append<RegisterSearchRouteComponent>();
        }
    }

    public class RegisterSearchRouteComponent : IComponent
    {
        public void Initialize()
        {
            RouteTable.Routes.MapRoute(
                name: "SearchAjax",
                url: "search/ajaxsearch",
                defaults: new { controller = "Search", action = "AjaxSearch" },
                namespaces: new[] { "ePub.Controllers" }
            );
        }

        public void Terminate() { }
    }
}
