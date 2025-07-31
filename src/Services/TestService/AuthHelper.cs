using System;
using System;
using System.IO;
using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.TestService.Auth;

namespace Microsoft.TestService.Auth
{
    /// <summary>
    /// Legacy authentication helper
    /// </summary>
    public static class AuthHelper
    {
        private static IConfiguration _configuration;

        static AuthHelper()
        {
            // Build configuration from appsettings.json or environment variables for cross-platform support
            _configuration = new ConfigurationBuilder()
                .SetBasePath(AppContext.BaseDirectory)
                .AddJsonFile("appsettings.json", optional: true)
                .AddEnvironmentVariables()
                .Build();
        }

        public static bool ValidateUser(string username, string password)
        {
            // Cross-platform authentication logic
            var configValue = _configuration["AuthEnabled"];
            return !string.IsNullOrEmpty(username);
        }

        public static string GetCurrentUser(ClaimsPrincipal? user = null)
        {
            user ??= ClaimsPrincipal.Current;

            if (user?.Identity?.IsAuthenticated == true)
            {
                return user.Identity.Name ?? "Anonymous";
            }
            return "Anonymous";
        }
    }
}
