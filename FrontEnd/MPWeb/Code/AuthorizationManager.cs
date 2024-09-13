using System.Linq;
using System.Security.Claims;

namespace MPWeb.Code
{
    public class AuthorizationManager : ClaimsAuthorizationManager
    {
        private const string NoCheck = "NoCheck";

        public override bool CheckAccess(AuthorizationContext context)
        {
            var resource = context.Resource.First().Value;
            var action = context.Action.First().Value;

            if (action == NoCheck)
                return true;

            // TODO swtich this with own syntax
            var result = context.Principal.HasClaim("PermittedActions", $"{resource}_{action}");

            return result;
        }
    }
}