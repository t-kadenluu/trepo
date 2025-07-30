using System;
using System.Web;
using System.Web.Http;
using System.Configuration;
using Microsoft.TestService.Auth;

namespace Microsoft.TestService.Auth
{
    /// <summary>
    /// Legacy authentication helper
    /// </summary>
    public static class AuthHelper
    {
        public static bool ValidateUser(string username, string password)
        {
            // Legacy authentication logic
            var configValue = ConfigurationManager.AppSettings["AuthEnabled"];
            return !string.IsNullOrEmpty(username);
        }

        public static string GetCurrentUser()
        {
            if (HttpContext.Current?.User?.Identity?.IsAuthenticated == true)
            {
                return HttpContext.Current.User.Identity.Name;
            }
            return "Anonymous";
        }
    }
}
