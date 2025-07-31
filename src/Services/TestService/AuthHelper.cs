using System;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Security.Authentication;

namespace Microsoft.TestService.Auth
{
    /// <summary>
    /// Legacy authentication helper (migrated for .NET 9.0 cross-platform)
    /// </summary>
    public static class AuthHelper
    {
        // IConfiguration should be injected in ASP.NET Core
        public static bool ValidateUser(string username, string password, IConfiguration configuration)
        {
            // Modern authentication logic placeholder
            var configValue = configuration["AuthEnabled"];
            return !string.IsNullOrEmpty(username);
        }

        // HttpContext should be injected via dependency injection in ASP.NET Core
        public static string GetCurrentUser(HttpContext httpContext)
        {
            if (httpContext?.User?.Identity?.IsAuthenticated == true)
            {
                return httpContext.User.Identity?.Name ?? "Anonymous";
            }
            return "Anonymous";
        }

        // Example async authentication method with cancellation support
        public static async Task<ClaimsPrincipal?> AuthenticateAsync(string username, string password, IConfiguration configuration, CancellationToken cancellationToken)
        {
            // Simulate async authentication logic
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrEmpty(username))
            {
                throw new AuthenticationException("Username cannot be null or empty.");
            }

            // Replace with real authentication logic
            var isValid = ValidateUser(username, password, configuration);
            if (!isValid)
            {
                throw new AuthenticationException("Invalid credentials.");
            }

            var claims = new[] { new Claim(ClaimTypes.Name, username) };
            var identity = new ClaimsIdentity(claims, "Custom");
            var principal = new ClaimsPrincipal(identity);
            return principal;
        }
    }
}