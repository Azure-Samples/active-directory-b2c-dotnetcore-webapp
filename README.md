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

If you'd like to see the sample working immediately, you can simply run the app as-is without any code changes.  Open the solution in Visual Studio, and run the app.  Visual Studio should take care of restoring the necessary packages and launching the application using IIS Express.

The default configuration for this application performs sign-in & sign-up using our sample B2C tenant, `fabrikamb2c.onmicrosoft.com`.  It uses two [policies](https://azure.microsoft.com/documentation/articles/active-directory-b2c-reference-policies); a sign-up policy named `b2c_1_sign_up` and a sign-in policy named `b2c_1_sign_in`.  Sign up for the app using any of the available account types, and try signing in again with the same account.

### Step 3: Get your own Azure AD B2C tenant

You can also modify the sample to use your own Azure AD B2C tenant.  First, you'll need to create an Azure AD B2C tenant by following [these instructions](https://azure.microsoft.com/documentation/articles/active-directory-b2c-get-started).

### Step 4: Create your own policies

This sample uses two types of policies: a sign-in policy & a sign-up policy.  Create one policy of each type by following [the instructions here](https://azure.microsoft.com/documentation/articles/active-directory-b2c-reference-policies).  You may choose to include as many or as few identity providers as you wish; our sample policies use Facebook, Google, and email-based local accounts.

If you already have existing policies in your B2C tenant, feel free to re-use those.  No need to create new ones just for this sample.

### Step 5: Create your own application

Now you need to create your own appliation in your B2C tenant, so that your app has its own client ID.  You can do so following [the generic instructions here](https://azure.microsoft.com/documentation/articles/active-directory-b2c-app-registration).  Be sure to include the following information in your app registration:

- Enable the **Web App/Web API** setting for your application.
- Add **two** redirect_uris for your app, one for each policy used in the sample.  Their values should take the form `https://localhost:44316/{your-policy-name}`, where your policy names begin with `b2c_1_`.
- Copy the client ID generated for your application, so you can use it in the next step.

### Step 6: Configure the sample to use your B2C tenant

Now you can replace the app's default configuration with your own.  Open the `config.json` file and replace the following values with the ones you created in the previous steps.  You can leave the `AadInstance` and `PostLogoutRedirectUri` values unchanged.

```json
{
    "AzureAd": {
        "ClientId": "90c0fe63-bcf2-44d5-8fb7-b8bbc0b29dc6",
        "Tenant": "fabrikamb2c.onmicrosoft.com",
        "AadInstance": "https://login.microsoftonline.com/{0}/v2.0/.well-known/openid-configuration?p={1}", // This is the public instance of Azure AD
        "PostLogoutRedirectUri": "https://localhost:44316/",

        "SignUpPolicyId": "B2C_1_sign_up",
        "SignInPolicyId": "B2C_1_sign_in",
    }
}
```

### Step 7:  Run the sample

Clean the solution, rebuild the solution, and run it.  You can now sign up & sign in to your application using the accounts you configured in your respective policies
