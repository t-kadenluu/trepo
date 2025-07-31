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
    public class CustomMiddleware : Microsoft.AspNetCore.Http.IMiddleware
    {
        private readonly ILogger<CustomMiddleware> _logger;
        private static readonly Action<ILogger, string, Exception?> _handlingRequest =
            LoggerMessage.Define<string>(
                LogLevel.Information,
                new EventId(1, nameof(InvokeAsync)),
                "Handling request: {Path}");

        private static readonly Action<ILogger, Exception?> _finishedHandlingRequest =
            LoggerMessage.Define(
                LogLevel.Information,
                new EventId(2, nameof(InvokeAsync)),
                "Finished handling request.");

        public CustomMiddleware(ILogger<CustomMiddleware> logger)
        {
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, RequestDelegate next)
        {
            _handlingRequest(_logger, context.Request.Path, null);
            await next(context);
            _finishedHandlingRequest(_logger, null);
        }
    }

    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Configure logging using appsettings.json or code-based configuration
            builder.Logging.ClearProviders();
            builder.Logging.AddConsole();
            builder.Logging.AddDebug();
            // Optionally add Serilog or other cross-platform providers here

            // Add required services here
            // Example: Add authentication services
            // builder.Services.AddAuthentication(options =>
            // {
            //     options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //     options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            //     options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            // }).AddCookie();

            // Example: Add distributed session services if needed
            // builder.Services.AddDistributedMemoryCache();
            // builder.Services.AddSession();

            // Register CustomMiddleware as a transient service with DI
            builder.Services.AddTransient<CustomMiddleware>();

            // Add OpenTelemetry instrumentation for logging and metrics if needed
            // builder.Services.AddOpenTelemetry()
            //     .WithMetrics(metrics => { /* configure metrics */ })
            //     .WithTracing(tracing => { /* configure tracing */ });

            var app = builder.Build();

            // Example: Use session middleware if needed
            // app.UseSession();

            // Configure ASP.NET Core middleware
            app.UseMiddleware<CustomMiddleware>();
            // Example: app.UseAuthentication();
            // Example: app.UseAuthorization();

            app.MapGet("/", async (HttpContext context) =>
            {
                await context.Response.WriteAsync("Hello World!", context.RequestAborted);
            });

            await app.RunAsync();
        }
    }
}