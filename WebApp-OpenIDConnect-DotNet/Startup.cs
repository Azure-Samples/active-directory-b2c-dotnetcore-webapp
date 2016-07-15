using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using WebApp_OpenIDConnect_DotNet.PolicyAuthHelpers;
using System.Globalization;
using System.Threading;
using Microsoft.AspNetCore.Authentication;

namespace WebApp_OpenIDConnect_DotNet
{
    public class Startup
    {
        // The ACR claim is used to indicate which policy was executed
        public const string AcrClaimType = "http://schemas.microsoft.com/claims/authnclassreference";
        public const string PolicyKey = "b2cpolicy";
        public const string OIDCMetadataSuffix = "/.well-known/openid-configuration";

        public static string SignUpPolicyId;
        public static string SignInPolicyId;
        public static string ProfilePolicyId;

        public Startup(IHostingEnvironment env)
        {
            // Set up configuration sources.
            Configuration = new ConfigurationBuilder()
                .SetBasePath(env.ContentRootPath)
                .AddJsonFile("config.json")
                .AddJsonFile("appsettings.json")
                .Build();
        }

        public IConfigurationRoot Configuration { get; set; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // Add MVC services to the services container.
            services.AddMvc();

            // Add Authentication services.
            services.AddAuthentication(sharedOptions => sharedOptions.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            // Add the console logger.
            loggerFactory.AddConsole(Configuration.GetSection("Logging"));

            // Configure error handling middleware.
            app.UseExceptionHandler("/Home/Error");

            // Add static files to the request pipeline.
            app.UseStaticFiles();

            // Configure the OWIN pipeline to use cookie auth.
            app.UseCookieAuthentication(new CookieAuthenticationOptions());
            
            // App config settings
            var clientId = Configuration["AzureAD:ClientId"];
            var aadInstance = Configuration["AzureAD:AadInstance"];
            var tenant = Configuration["AzureAD:Tenant"];
            var redirectUri = Configuration["AzureAD:RedirectUri"];

            // B2C policy identifiers
            SignUpPolicyId = Configuration["AzureAD:SignUpPolicyId"];
            SignInPolicyId = Configuration["AzureAD:SignInPolicyId"];
            ProfilePolicyId = Configuration["AzureAD:UserProfilePolicyId"];

        // Configure the OWIN pipeline to use OpenID Connect auth.
        app.UseOpenIdConnectAuthentication(new OpenIdConnectOptions
            {
                // These are standard OpenID Connect parameters, with values pulled from web.config
                ClientId = clientId,
                //RedirectUri = redirectUri,
                PostLogoutRedirectUri = redirectUri,
                Events = new OpenIdConnectEvents
                {
                    OnRemoteFailure = RemoteFailure,
                    OnRedirectToIdentityProvider = RedirectToIdentityProvider,
                },
                ResponseType = "id_token",
                
                // The PolicyConfigurationManager takes care of getting the correct Azure AD authentication
                // endpoints from the OpenID Connect metadata endpoint.  It is included in the PolicyAuthHelpers folder.
                ConfigurationManager = new PolicyConfigurationManager(
                    string.Format(CultureInfo.InvariantCulture, aadInstance, tenant, "/v2.0", OIDCMetadataSuffix),
                    new string[] { SignUpPolicyId, SignInPolicyId, ProfilePolicyId }),

                // This piece is optional - it is used for displaying the user's name in the navigation bar.
                TokenValidationParameters = new TokenValidationParameters
                {
                    NameClaimType = "name",
                },
            });

            // Configure MVC routes
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }

        // This notification can be used to manipulate the OIDC request before it is sent.  Here we use it to send the correct policy.
        private async Task RedirectToIdentityProvider(RedirectContext context)
        {
            PolicyConfigurationManager mgr = context.Options.ConfigurationManager as PolicyConfigurationManager;
            if (context.ProtocolMessage.RequestType == OpenIdConnectRequestType.Logout)
            {
                OpenIdConnectConfiguration config = await mgr.GetConfigurationByPolicyAsync(CancellationToken.None, context.Properties.Items[Startup.PolicyKey]);
                context.ProtocolMessage.IssuerAddress = config.EndSessionEndpoint;
            }
            else
            {
                OpenIdConnectConfiguration config = await mgr.GetConfigurationByPolicyAsync(CancellationToken.None, context.Properties.Items[Startup.PolicyKey]);
                context.ProtocolMessage.IssuerAddress = config.AuthorizationEndpoint;
            }
        }

        // Used for avoiding yellow-screen-of-death
        private Task RemoteFailure(FailureContext context)
        {
            context.HandleResponse();
            context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
            return Task.FromResult(0);
        }
    }
}
