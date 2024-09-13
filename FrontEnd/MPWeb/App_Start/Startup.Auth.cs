using Owin;
using System;

//The following libraries were added to this sample.
using System.Threading.Tasks;
using Microsoft.Owin.Security.Cookies;
using Microsoft.Owin.Security.OpenIdConnect;
using Microsoft.Owin.Security;

//The following libraries were defined and added to this sample.
using System.Globalization;
using MPWeb.Utils;

namespace MPWeb
{
    public partial class Startup
    {
        /// <summary>
        /// Configures OpenIDConnect Authentication & Adds Custom Application Authorization Logic on User Login.
        /// </summary>
        /// <param name="app">The application represented by a <see cref="IAppBuilder"/> object.</param>
        public void ConfigureAuth(IAppBuilder app)
        {

            //app.UseWindowsAzureActiveDirectoryBearerAuthentication(
            //    new WindowsAzureActiveDirectoryBearerAuthenticationOptions
            //    {
            //        TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
            //        {
            //            ValidAudience = ConfigHelper.ClientId
            //        },
            //        Tenant = ConfigHelper.Tenant
            //    });
            //app.SetDefaultSignInAsAuthenticationType(CookieAuthenticationDefaults.AuthenticationType);

            //app.UseCookieAuthentication(new CookieAuthenticationOptions
            //{
            //    CookieManager = new SystemWebCookieManager()
            //});

            //Configure OpenIDConnect, register callbacks for OpenIDConnect Notifications
            /*
            app.UseOpenIdConnectAuthentication(
                new OpenIdConnectAuthenticationOptions
                {
                    ClientId = ConfigHelper.ClientId,
                    Authority = String.Format(CultureInfo.InvariantCulture, ConfigHelper.AadInstance, ConfigHelper.Tenant), // For Single-Tenant
                                                                                                                            //Authority = ConfigHelper.CommonAuthority, // For Multi-Tenant
                    PostLogoutRedirectUri = ConfigHelper.PostLogoutRedirectUri,
                    //ProtocolValidator = new Microsoft.IdentityModel.Protocols.OpenIdConnectProtocolValidator()
                    //{
                    //    RequireNonce = false
                    //},
                    // Here, we've disabled issuer validation for the multi-tenant sample.  This enables users
                    // from ANY tenant to sign into the application (solely for the purposes of allowing the sample
                    // to be run out-of-the-box.  For a real multi-tenant app, reference the issuer validation in 
                    // WebApp-MultiTenant-OpenIDConnect-DotNet.  If you're running this sample as a single-tenant
                    // app, you can delete the ValidateIssuer property below.
                    //TokenValidationParameters = new System.IdentityModel.Tokens.TokenValidationParameters
                    //{
                    //    //ValidateIssuer = false, // For Multi-Tenant Only
                    //    RoleClaimType = "roles",
                    //    NameClaimType = "name"
                    //},

                    Notifications = new OpenIdConnectAuthenticationNotifications()
                    {
                        AuthenticationFailed = (context) =>
                        {
                            if (context.Exception.Message.StartsWith("OICE_20004") || context.Exception.Message.Contains("IDX10311"))
                            {
                                context.SkipToNextMiddleware();
                                return Task.FromResult(0);
                            }
                            context.HandleResponse();
                            context.Response.Redirect("/Error/ShowError?signIn=true&errorMessage=" + context.Exception.Message);
                            return Task.FromResult(0);
                        }
                    }
                });
                */
        }
    }
}