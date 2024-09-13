using MPWeb.Models;
using System.Web.Mvc;

namespace MPWeb.Code
{
    public class AuthorizeByRoleAttribute : AuthorizeAttribute
    {
        private string RequiredRole;

        public AuthorizeByRoleAttribute()
        {
        }

        public AuthorizeByRoleAttribute(string requiredRole)
        {
            this.RequiredRole = requiredRole;
        }

        public override void OnAuthorization(AuthorizationContext filterContext)
        {
            var allowAnonymous = filterContext.ActionDescriptor.IsDefined(typeof(AllowAnonymousAttribute), true);
            if (allowAnonymous) {
                return;
            }

            if (filterContext.HttpContext.Session != null)
            {
                var suc = filterContext.HttpContext.Session["ServerUserContext"] as ServerUserContext;
                if (suc == null)
                {
                    filterContext.Result = new HttpUnauthorizedResult();
                    return;
                }

                if (suc.Roles.Contains(RequiredRole)) {
                    return;
                }
            }

            filterContext.Result = new HttpUnauthorizedResult();
        }
    }
}