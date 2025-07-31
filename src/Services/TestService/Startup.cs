using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Microsoft.TestService.Startup
{
    /// <summary>
    /// ASP.NET Core Startup configuration
    /// </summary>
    public class CustomMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<CustomMiddleware> _logger;

        public CustomMiddleware(RequestDelegate next, ILogger<CustomMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Custom middleware logic
            _logger.LogInformation("Handling request: {Path}", context.Request.Path);
            await _next(context);
            _logger.LogInformation("Finished handling request.");
        }
    }

    public static class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add required services here
            // Example: Add authentication services
            // builder.Services.AddAuthentication(options =>
            // {
            //     options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //     options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //     options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            // }).AddCookie();

            var app = builder.Build();

            // Configure ASP.NET Core middleware
            app.Use(async (context, next) =>
            {
                var logger = context.RequestServices.GetRequiredService<ILogger<CustomMiddleware>>();
                logger.LogInformation("Handling request: {Path}", context.Request.Path);
                await next();
                logger.LogInformation("Finished handling request.");
            });
            // Example: app.UseAuthentication();
            // Example: app.UseAuthorization();

            app.Run(async (context) =>
            {
                await context.Response.WriteAsync("Hello World!", context.RequestAborted);
            });
        }
    }
}