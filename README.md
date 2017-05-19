---
services: active-directory-b2c
platforms: dotnet
author: gsacavdm
---

# An ASP.NET Core web app with Azure AD B2C 
This sample shows how to build an MVC web application that performs identity management with Azure AD B2C using the ASP.Net Core OpenID Connect middleware.  It assumes you have some familiarity with Azure AD B2C.  If you'd like to learn all that B2C has to offer, start with our documentation at [aka.ms/aadb2c](http://aka.ms/aadb2c). 

The app is a dead simple web application that performs three functions: sign-in, sign-up, and sign-out.  It is intended to help get you started with Azure AD B2C in a ASP.NET Core application, giving you the necessary tools to execute Azure AD B2C policies & securely identify uses in your application.  

## How To Run This Sample

Getting started is simple! To run this sample you will need:

- To install .NET Core for Windows by following the instructions at [dot.net/core](http://dot.net/core), which will include Visual Studio 2015 Update 3.
- An Internet connection
- An Azure subscription (a free trial is sufficient)

### Step 1:  Clone or download this repository

From your shell or command line:

`git clone https://github.com/Azure-Samples/active-directory-dotnet-webapp-openidconnect-aspnetcore-b2c.git`

### Step 2: Run the sample using our sample tenant

If you'd like to see the sample working immediately, you can simply run the app as-is without any code changes. Open the solution in Visual Studio, and run the app.  Visual Studio should take care of restoring the necessary packages and launching the application using IIS Express.

The default configuration for this application performs sign-in & sign-up using our sample B2C tenant, `fabrikamb2c.onmicrosoft.com`.  It uses a single [policy](https://azure.microsoft.com/documentation/articles/active-directory-b2c-reference-policies) (`b2c_1_susi`) for handling both sign-in and sign-up. Sign up for the app using any of the available account types, and try signing in again with the same account.

### Step 3: Get your own Azure AD B2C tenant

You can also modify the sample to use your own Azure AD B2C tenant.  First, you'll need to create an Azure AD B2C tenant by following [these instructions](https://azure.microsoft.com/documentation/articles/active-directory-b2c-get-started).

### Step 4: Create your own policies

This sample uses a single sign-in and sign-up policy. Create the policy by following [the instructions here](https://azure.microsoft.com/documentation/articles/active-directory-b2c-reference-policies).  You may choose to include as many or as few identity providers as you wish; our sample policies use Facebook, Google, and email-based local accounts.

When you create a sign-up or sign-in policy (with local accounts), the consumer will see a "Forgot password?" link on the first page of the experience. Clicking on this link doesn't automatically trigger a password reset policy. Instead a specific error code AADB2C90118 is returned back to your app. Your app needs to handle this and invoke a specific password reset policy as shown in this sample. To enable this flow you will also need to create a password reset policy.

If you already have existing policies in your B2C tenant, feel free to re-use those. No need to create new ones just for this sample.

### Step 5: Create your own application

Now you need to create your own appliation in your B2C tenant, so that your app has its own client ID.  You can do so following [the generic instructions here](https://azure.microsoft.com/documentation/articles/active-directory-b2c-app-registration). Be sure to include the following information in your app registration:

- Enable the **Web App/Web API** setting for your application.
- Add the redirect URI for your app: `https://localhost:44316/signin-oidc`.
- Copy the client ID generated for your application, so you can use it in the next step.

### Step 6: Configure the sample to use your B2C tenant

Now you can replace the app's default configuration with your own.  Open the `appsettings.json` file and replace the following values with the ones you created in the previous steps. 

```json
{
  "Authentication": {
    "AzureAdB2C": {
      "ClientId": "90c0fe63-bcf2-44d5-8fb7-b8bbc0b29dc6",
      "Tenant": "fabrikamb2c.onmicrosoft.com",
      "SignUpSignInPolicyId": "b2c_1_susi",
      "ResetPasswordPolicyId": "b2c_1_reset",
      "EditProfilePolicyId": "b2c_1_edit_profile"
    }
  }
}
```

### Step 7:  Run the sample

Clean the solution, rebuild the solution, and run it.  You can now sign up & sign in to your application using the accounts you configured in your respective policies

## About the code

Here there's a quick guide to the most interesting authentication related bits of the sample.

### Sign in 

As it is standard practice for ASP.NET Core MVC apps, the sign in functionality is implemented with the OpenID Connect OWIN middleware. Here there's a relevant snippet from the middleware initialization:  

```
  // In Startup.cs
  public void ConfigureServices(IServiceCollection services)
  {
    //...
    services.Configure<AzureAdB2COptions>(Configuration.GetSection("Authentication:AzureAdB2C"));
    services.AddSingleton<IConfigureOptions<OpenIdConnectOptions>, OpenIdConnectOptionsSetup>();
  }
  
  // In OpenIdConnectOptionsSetup.cs
  public void Configure(OpenIdConnectOptions options)
  {
    options.ClientId = AzureAdB2COptions.ClientId;
    options.Authority = AzureAdB2COptions.Authority;
    options.UseTokenLifetime = true;
    options.TokenValidationParameters = new TokenValidationParameters() { NameClaimType = "name" };
    options.Events = new OpenIdConnectEvents()
    {
      OnRedirectToIdentityProvider = OnRedirectToIdentityProvider,
      OnRemoteFailure = OnRemoteFailure,
      OnAuthorizationCodeReceived = OnAuthorizationCodeReceived
    };
  }
  
  // In AzureAdB2COptions.cs
  public class AzureAdB2COptions
  {
    public const string PolicyAuthenticationProperty = "Policy";

    public AzureAdB2COptions()
    {
      AzureAdB2CInstance = "https://login.microsoftonline.com/tfp";
    }

    public string ClientId { get; set; }
    public string AzureAdB2CInstance { get; set; }
    public string Tenant { get; set; }
    public string SignUpSignInPolicyId { get; set; }
    public string SignInPolicyId { get; set; }
    public string SignUpPolicyId { get; set; }
    public string ResetPasswordPolicyId { get; set; }
    public string EditProfilePolicyId { get; set; }

    public string DefaultPolicy => SignUpSignInPolicyId;
    public string Authority => $"{AzureAdB2CInstance}/{Tenant}/{DefaultPolicy}/v2.0";
    
    public string ClientSecret { get; set; }
    public string ApiUrl { get; set; }
    public string ApiScopes { get; set; }
  }
```
Important things to notice:
- The Authority points is constructed using the **tfp** path, the tenant name and the default (sign-up/sign-in) policy.
- The OnRedirectToIdentityProvider notification is used in order to support EditProfile and Password Reset.
- The OnRemoteFailure notification is used in order to support Password Reset.
- The OnAuthorizationCodeReceived notification is used to redeem the access code using MSAL.

### Initial token acquisition

This sample makes use of OpenId Connect hybrid flow, where at authentication time the app receives both sign in info (the id_token) and artifacts (in this case, an authorization code) that the app can use for obtaining an access token. That token can be used to access other resources - in this sample, the a Demo web API which echoes back the user's name.
This sample shows how to use MSAL to redeem the authorization code into an access token, which is saved in a cache along with any other useful artifact (such as associated refresh_tokens) so that it can be used later on in the application.
The redemption takes place in the `AuthorizationCodeReceived` notification of the authorization middleware. Here there's the relevant code:

```
var code = context.ProtocolMessage.Code;
string signedInUserID = context.Ticket.Principal.FindFirst(ClaimTypes.NameIdentifier).Value;
TokenCache userTokenCache = new MSALSessionCache(signedInUserID, context.HttpContext).GetMsalCacheInstance();

ConfidentialClientApplication cca = new ConfidentialClientApplication(AzureAdB2COptions.ClientId, AzureAdB2COptions.Authority, AzureAdB2COptions.RedirectUri, new ClientCredential(AzureAdB2COptions.ClientSecret), userTokenCache, null);

try
{
  AuthenticationResult result = await cca.AcquireTokenByAuthorizationCodeAsync(code, AzureAdB2COptions.ApiScopes.Split(' '));
  context.HandleCodeRedemption(result.AccessToken, result.IdToken);
```

Important things to notice:
- The `ConfidentialClientApplication` is the primitive that MSAL uses to model the application. As such, it is initialized with the main application's coordinates.
- `MSALSessionCache` is a sample implementation of a custom MSAL token cache, which saves tokens in the current HTTP session. In a real-life application, you would likely want to save tokens in a long lived store instead, so that you don't need to retrieve new ones more often than necessary.
- The scope requested by `AcquireTokenByAuthorizationCodeAsync` is just the one required for invoking the API targeted by the application as part of its essential features. We'll see later that the app allows for extra scopes, but you can ignore those at this point. 

### Using access tokens in the app, handling token expiration

The `Api` action in the `HomeController` class demonstrates how to take advantage of MSAL for getting access to protected API easily and securely. Here there's the relevant code:

```
var scope = AzureAdB2COptions.ApiScopes.Split(' ');
string signedInUserID = HttpContext.User.FindFirst(ClaimTypes.NameIdentifier).Value;
TokenCache userTokenCache = new MSALSessionCache(signedInUserID, this.HttpContext).GetMsalCacheInstance();

ConfidentialClientApplication cca = new ConfidentialClientApplication(AzureAdB2COptions.ClientId, AzureAdB2COptions.Authority, AzureAdB2COptions.RedirectUri, new ClientCredential(AzureAdB2COptions.ClientSecret), userTokenCache, null);
AuthenticationResult result = await cca.AcquireTokenSilentAsync(scope, cca.Users.FirstOrDefault(), AzureAdB2COptions.Authority, false);
```

The idea is very simple. The code creates a new instance of `ConfidentialClientApplication` with the exact same coordinates as the ones used when redeeming the authorization code at authentication time. In particular, note that the exact same cache is used.
That done, all you need to do is to invoke `AcquireTokenSilentAsync`, asking for the scopes you need. MSAL will look up the cache and return any cached token which match with the requirement. If such access tokens are expired or no suitable access tokens are present, but there is an associated refresh token, MSAL will automatically use that to get a new access token and return it transparently.    

In the case in which refresh tokens are not present or they fail to obtain a new access token, MSAL will throw `MsalUiRequiredException`. That means that in order to obtain the requested token, the user must go through an interactive experience.
