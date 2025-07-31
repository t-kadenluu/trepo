using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;

namespace Microsoft.TestService.Startup
{
    /// <summary>
    /// ASP.NET Core Startup configuration
    /// </summary>
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        public void Configure(WebApplication app)
        {
            // Configure ASP.NET Core middleware
            app.UseMiddleware<CustomMiddleware>();
        }
    }

    public class CustomMiddleware
    {
        private readonly RequestDelegate _next;

        public CustomMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Custom middleware logic
            await _next(context);
        }
    }
}