#nullable enable

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;

namespace Microsoft.TestService.Services
{
    /// <summary>
    /// RESTful controller for legacy operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class LegacyServiceController : ControllerBase
    {
        [HttpGet("{id}")]
        public async Task<ActionResult<string>> GetLegacyDataAsync(string id, CancellationToken cancellationToken)
        {
            // Simulate async operation and check for cancellation
            await Task.Yield();
            cancellationToken.ThrowIfCancellationRequested();

            return Ok($"Legacy data for ID: {id}");
        }
    }
}