using System;
using System.IdentityModel.Services;
using System.IdentityModel.Tokens;
using System.Security.Claims;

namespace MPWeb.Code
{
    class AuthenticationManager : ClaimsAuthenticationManager
    {
        public override ClaimsPrincipal Authenticate(string resName, ClaimsPrincipal incPrincipal)
        {
            if (!incPrincipal.Identity.IsAuthenticated)
                return base.Authenticate(resName, incPrincipal);

            CreateSession(incPrincipal);
            return base.Authenticate(resName, incPrincipal);
        }

        private void CreateSession(ClaimsPrincipal principal)
        {
            var sessionSecurityToken = new SessionSecurityToken(principal, TimeSpan.FromHours(8))
            {
                IsPersistent = false,
                IsReferenceMode = true
            };
            FederatedAuthentication.SessionAuthenticationModule.WriteSessionTokenToCookie(sessionSecurityToken);
        }
    }
}