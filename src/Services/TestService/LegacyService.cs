using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace Microsoft.TestService.Services
{
    /// <summary>
    /// Minimal API for legacy operations (migrated from WCF)
    /// </summary>
    public static class LegacyServiceEndpoints
    {
        public static void MapLegacyServiceEndpoints(this IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGet("/legacy/{id}", async (string id, CancellationToken cancellationToken) =>
            {
                // Simulate async operation and support cancellation
                await Task.Yield();
                cancellationToken.ThrowIfCancellationRequested();
                return Results.Ok($"Legacy data for ID: {id}");
            });
        }
    }
}