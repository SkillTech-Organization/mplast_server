using System;
using System.Web;
using System.Web.Mvc;

namespace MPWeb.Controllers
{
    public class AppController : Controller
    {
        [Authorize]
        public ActionResult Index()
        {
            var root = VirtualPathUtility.ToAbsolute("~/");
            var applicationPath = Request.ApplicationPath;
            var path = Request.Path;
            var hasTraillingSlash = root.Equals(applicationPath
                                          , StringComparison.InvariantCultureIgnoreCase)
                    || !applicationPath.Equals(path
                                          , StringComparison.InvariantCultureIgnoreCase);
            if (!hasTraillingSlash)
            {
                return Redirect(root + "#");
            }
            return View();
        }
    }
}