using System;
using System.Security.Claims;
using DotOPDS.DbLayer;
using DotOPDS.Server.Services;
using DotOPDS.Shared;
using idunno.Authentication.Basic;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using OpenIddict.Server.AspNetCore;
using OpenIddict.Validation.AspNetCore;

namespace DotOPDS.Server.Extensions;

public static class ServiceRegistrationExtension
{
    public static void RegisterAuth(this IServiceCollection services)
    {
        services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
            .AddBasic(options =>
            {
                options.AllowInsecureProtocol = false;
                options.Realm = Constants.ServerName;
                options.Events = new BasicAuthenticationEvents
                {
                    OnValidateCredentials = async context =>
                    {
                        /*
                         *In your OnValidateCredentials implementation keep a count of failed login attempts, and the IP addresses they come from.

                           Lock out accounts after X failed login attempts, where X is a count you feel is reasonable for your situation.

                           Implement the lock out so it unlocks after Y minutes. In case of repeated attacks increase Y.

                           Be careful when locking out your administration accounts. Have at least one admin account that is not exposed via basic auth, so an attacker cannot lock you out of your site just by sending an incorrect password.

                           Throttle attempts from an IP address, especially one which sends lots of incorrect passwords. Considering dropping/banning attempts from an IP address that appears to be under the control of an attacker. Only you can decide what this means, what consitutes legimate traffic varies from application to application.
                         */

                        var validationService = context.HttpContext.RequestServices.GetService<IOwnUserManagerService>();
                        if (await validationService.DoLogin(context.Username, context.Password))
                        {
                            var claims = new[] {new Claim(ClaimTypes.NameIdentifier, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer), new Claim(ClaimTypes.Name, context.Username, ClaimValueTypes.String, context.Options.ClaimsIssuer)};

                            context.Principal = new ClaimsPrincipal(new ClaimsIdentity(claims, context.Scheme.Name));
                            context.Success();
                        }
                    }
                };
            })
            .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, options =>
            {
                options.ExpireTimeSpan = TimeSpan.FromDays(1);
                options.SlidingExpiration = true;
                options.LogoutPath = "/account/logout";
                options.LoginPath = "/account/login";
            });
            
        services.AddAuthorization(options =>
        {
            var basicAuthPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(BasicAuthenticationDefaults.AuthenticationScheme)
                .Build();

            var cookieAuthPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                .Build();
            var bearerAuthPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .Build();
            
            var allPolicy = new AuthorizationPolicyBuilder()
                .RequireAuthenticatedUser()
                .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme, BasicAuthenticationDefaults.AuthenticationScheme, OpenIddictValidationAspNetCoreDefaults.AuthenticationScheme)
                .Build();

            options.AddPolicy(Constants.AuthBasicPolicy, basicAuthPolicy);
            options.AddPolicy(Constants.AuthBearerPolicy, bearerAuthPolicy);
            options.AddPolicy(Constants.AuthCookiePolicy, cookieAuthPolicy);
            options.AddPolicy(Constants.AuthAllPolicies, allPolicy);
            options.DefaultPolicy = options.GetPolicy(Constants.AuthCookiePolicy)!;
        });


        services.AddOpenIddict()

            // Register the OpenIddict core components.
            .AddCore(options =>
            {
                // Configure OpenIddict to use the EF Core stores/models.
                options.UseEntityFrameworkCore()
                    .UseDbContext<DotOPDSDbContext>();
            })

            // Register the OpenIddict server components.
            .AddServer(options =>
            {
                options
                    .AllowClientCredentialsFlow()
                    .AllowAuthorizationCodeFlow()
                    .RequireProofKeyForCodeExchange()
                    .AllowRefreshTokenFlow();

                options
                    .SetTokenEndpointUris("/connect/token")
                    .SetAuthorizationEndpointUris("/connect/authorize")
                    .SetUserinfoEndpointUris("/connect/userinfo");

                // Encryption and signing of tokens
                options
                    .AddEphemeralEncryptionKey()
                    .AddEphemeralSigningKey()
                    .DisableAccessTokenEncryption();

                // Register scopes (permissions)
                options.RegisterScopes("api");

                // Register the ASP.NET Core host and configure the ASP.NET Core-specific options.
                options
                    .UseAspNetCore()
                    .EnableTokenEndpointPassthrough()
                    .EnableAuthorizationEndpointPassthrough()
                    .EnableUserinfoEndpointPassthrough();
            })

            .AddValidation(c =>
            {
                c.UseAspNetCore(config => config.SetRealm(Constants.ServerName));
                c.UseLocalServer();
            });

    }
}
