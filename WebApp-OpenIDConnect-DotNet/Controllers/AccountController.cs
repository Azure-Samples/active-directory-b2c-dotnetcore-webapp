using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Http.Authentication;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

// For more information on enabling MVC for empty projects, visit http://go.microsoft.com/fwlink/?LinkID=397860

namespace WebApp_OpenIDConnect_DotNet.Controllers
{
    public class AccountController : Controller
    {
        // GET: /Account/Login
        [HttpGet]
        public async Task Login()
        {
            var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
            authenticationProperties.Items.Add(Startup.PolicyKey, Startup.SignInPolicyId);

            if (HttpContext.User == null || !HttpContext.User.Identity.IsAuthenticated)
                await HttpContext.Authentication.ChallengeAsync(
                    OpenIdConnectDefaults.AuthenticationScheme,
                    authenticationProperties);
        }

        // GET: /Account/LogOff
        [HttpGet]
        public async Task LogOff()
        {
            if (HttpContext.User.Identity.IsAuthenticated)
            {
                var authenticationProperties = new AuthenticationProperties { RedirectUri = "/" };
                authenticationProperties.Items.Add(Startup.PolicyKey, HttpContext.User.FindFirst(Startup.AcrClaimType).Value);

                await HttpContext.Authentication.SignOutAsync(OpenIdConnectDefaults.AuthenticationScheme, authenticationProperties);
                await HttpContext.Authentication.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            }
        }
    }
}
