using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;

namespace WebApp_OpenIDConnect_DotNet
{
    public class AzureAdB2COpenIdConnectOptionsSetup : IConfigureOptions<OpenIdConnectOptions>
    {
        public AzureAdB2COpenIdConnectOptionsSetup(IOptions<AzureAdB2COptions> b2cOptions)
        {
            AzureAdB2COptions = b2cOptions.Value;
        }

        public AzureAdB2COptions AzureAdB2COptions { get; set; }

        public void Configure(OpenIdConnectOptions options)
        {
            options.ClientId = AzureAdB2COptions.ClientId;
            options.Authority = AzureAdB2COptions.Authority;

            // TODO: Is this needed? Is the default sufficient?
            options.TokenValidationParameters = new TokenValidationParameters() { NameClaimType = "name" };

            options.Events = new OpenIdConnectEvents()
            {
                OnRedirectToIdentityProvider = OnRedirectToIdentityProvider,
                OnRemoteFailure = OnRemoteFailure
            };
        }

        public async Task OnRedirectToIdentityProvider(RedirectContext context)
        {
            var defaultPolicy = AzureAdB2COptions.DefaultPolicy;
            if (context.Properties.Items.TryGetValue("Policy", out var policy) && !policy.Equals(defaultPolicy))
            {
                context.ProtocolMessage.Scope = OpenIdConnectParameterNames.Scope;
                context.ProtocolMessage.ResponseType = OpenIdConnectParameterNames.IdToken;
                context.ProtocolMessage.IssuerAddress = context.ProtocolMessage.IssuerAddress.Replace(defaultPolicy, policy);
            }
        }

        public async Task OnRemoteFailure(FailureContext context)
        {
            context.HandleResponse();
            // TODO: Figure out what to do about error when user tries to reset their password
            // See https://github.com/Azure-Samples/b2c-dotnet-webapp-and-webapi/blob/master/TaskWebApp/App_Start/Startup.Auth.cs#L106-L112
            if (context.Failure is OpenIdConnectProtocolException && context.Failure.Message.Contains("access_denied"))
            {
                // TODO: Use UrlHelper?
                context.Response.Redirect("/");
            }
            else
            {
                // TODO: Use UrlHelper?
                context.Response.Redirect("/Home/Error?message=" + context.Failure.Message);
            }
        }
    }
}
