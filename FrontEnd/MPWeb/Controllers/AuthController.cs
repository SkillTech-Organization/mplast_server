using System.Web.Mvc;
using System.Security.Claims;
using System.Collections.Generic;
using System;
using System.IdentityModel.Services;
using MPWeb.Models;
using Newtonsoft.Json;
using MPWeb.Code;
using System.Configuration;
using MPWeb.Logic.BLL;
using MPWeb.Logic.Helpers;


namespace MPWeb.Controllers
{
    public class AuthController : CustomControllerBase
    {
        private string AUTH_TOKEN_CLIENT_LOCAL_STORAGE_KEY = "authToken";
        private string m_authTokenRedirectReplaceUrl;

        public AuthController()
        {
            if (string.IsNullOrEmpty(ConfigurationManager.AppSettings["tokenLoginRedirectReplaceUrl"]))
            {
                throw new AppException("Parameter tokenLoginRedirectReplaceUrl is not set.");
            }
            m_authTokenRedirectReplaceUrl = ConfigurationManager.AppSettings["tokenLoginRedirectReplaceUrl"];
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Auth/Login")]
        public ActionResult Login(string username, string password)
        {
            // TODO write to persistent storage that the user has logged in
            var bllAuth = new BllAuth();
            var userCtx = bllAuth.AuthenticateUser(username, password);

            if (userCtx == null)
            {
                return new HttpUnauthorizedResult(); ;
            }

            try
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userCtx.Id) };
                var identity = new ClaimsIdentity(claims, "Ticket");
                identity.AddClaim(new Claim("UserName", userCtx.Id));
                userCtx.Roles.ForEach(x => identity.AddClaim(new Claim("Roles", x)));
                var principal = new ClaimsPrincipal(identity);
                var authManager = new AuthenticationManager();
                authManager.Authenticate(string.Empty, principal);
            }
            catch (Exception ex)
            {
                throw new AppException(ViewBag.CorrelationId, ex);
            }

            Session.Clear();

            var serverUserCtx = new ServerUserContext {
                Id = userCtx.Id,
                Name = userCtx.Name,
                SessionTimeoutMinutes = Session.Timeout,
                Roles = userCtx.Roles
            };

            Session["ServerUserContext"] = serverUserCtx;
            return ClientUserContext(serverUserCtx);
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Auth/TokenLogin")]
        public ActionResult TokenLogin(string token)
        {
            // TODO write to persistent storage that the user has logged in
            var bllAuth = new BllAuth();
            var userCtx = bllAuth.AuthenticateUserByToken(token);

            if (userCtx == null)
            {
                return new HttpUnauthorizedResult(); ;
            }

            try
            {
                var claims = new List<Claim> { new Claim(ClaimTypes.NameIdentifier, userCtx.Id) };
                var identity = new ClaimsIdentity(claims, "Ticket");
                identity.AddClaim(new Claim("UserName", userCtx.Id));
                userCtx.Roles.ForEach(x => identity.AddClaim(new Claim("Roles", x)));
                
                var principal = new ClaimsPrincipal(identity);
                var authManager = new AuthenticationManager();
                authManager.Authenticate(string.Empty, principal);
            }
            catch (Exception ex)
            {
                throw new AppException(ViewBag.CorrelationId, ex);
            }

            Session.Clear();

            var serverUserCtx = new ServerUserContext
            {
                Id = userCtx.Id,
                Name = userCtx.Name,
                SessionTimeoutMinutes = Session.Timeout,
                Roles = userCtx.Roles,
                TourPointList = userCtx.PMTracedTourList
            };
            Session["ServerUserContext"] = serverUserCtx;

            return ClientUserContext(serverUserCtx);
        }

        [AllowAnonymous]
        [Route("Auth/GenerateTempUserToken")]
        public ActionResult GenerateTempUserToken(string tokenContent)
        {
            var bllAuth = new BllAuth();

            var decryptedTokenContent = bllAuth.GetTempUATokenReqContent(tokenContent);

            if (decryptedTokenContent == null)
            {
                // TODO throw error
            }
            
            var tuToken = bllAuth.GetTemporaryUserAccessToken(decryptedTokenContent);

            return Content(JsonConvert.SerializeObject(new
            {
                temporaryUserToken = tuToken
            }), "application/json");
        }

        public class TempUserTokenReq
        {
            public string tokenContent { get; set; }
        }

        [AllowAnonymous]
        [HttpPost]
        [Route("Auth/GenerateTempUserToken2")]
        public ActionResult GenerateTempUserToken2([System.Web.Http.FromBody] TempUserTokenReq req)
        {
            var bllAuth = new BllAuth();
                var decryptedTokenContent = bllAuth.GetTempUATokenReqContent(req.tokenContent);
            
            if (decryptedTokenContent == null)
            {
                // TODO throw error
            }

            var tuToken = bllAuth.GetTemporaryUserAccessToken(decryptedTokenContent);

            return Content(JsonConvert.SerializeObject(new
            {
                temporaryUserToken = tuToken
            }), "application/json");
        }

        [AllowAnonymous]
        [HttpGet]
        public ActionResult TokenLoginRedirect(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                // TODO throw error
            }

   
           var ret = Content("<html><head></head><body><script type=\"text/javascript\"> "
                + "localStorage.setItem(\"" + AUTH_TOKEN_CLIENT_LOCAL_STORAGE_KEY + "\",\" " + token + "\"); "
                + "window.location.replace(\"" + m_authTokenRedirectReplaceUrl + "\"); "
                + "</script>"
                + "</body><html>", "text/html", System.Text.Encoding.UTF8);
            
   //         Console.WriteLine(ret);
            return ret;

        }

        [Route("Auth/Logout")]
        public ActionResult Logout()
        {
            Session.Clear();
            FederatedAuthentication.SessionAuthenticationModule.SignOut();
            // TODO write to persistent storage that the user has logged out
            return ToJSON(new
            {
                IsSuccess = true
            });
        }

        protected ActionResult ToJSON(object o, DateFormatHandling dfh = DateFormatHandling.IsoDateFormat)
        {
            var settings = new JsonSerializerSettings { DateFormatHandling = dfh };

            var so = JsonConvert.SerializeObject(o, settings);

            return Content(so, "application/json");
        }

        [NonAction]
        private ActionResult ClientUserContext(ServerUserContext suc) => ToJSON(new
        {
            IsSuccess = true,
            Name = suc.Name,
            Roles = suc.Roles,
            SessionTimeoutMinutes = suc.SessionTimeoutMinutes
        });
    }
}