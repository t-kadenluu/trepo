using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.TestService.Controllers
{
    /// <summary>
    /// Web API Controller for handling user operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
        {
            var activity = Activity.Current ?? new Activity("GetUser");
            activity.Start();
            try
            {
                var user = new { Id = id, Name = "Test User" };
                return Ok(user);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetUser");
                throw;
            }
            finally
            {
                activity.Stop();
            }
        }

        [HttpPost]
        public async Task<IActionResult> CreateUser(CancellationToken cancellationToken)
        {
            var activity = Activity.Current ?? new Activity("CreateUser");
            activity.Start();
            try
            {
                // Read and deserialize request body using cross-platform compatible stream and encoding
                object? userData;
                using (var reader = new StreamReader(Request.Body, Encoding.UTF8, leaveOpen: true))
                using (var jsonReader = new JsonTextReader(reader))
                {
                    var serializer = new JsonSerializer
                    {
                        DateParseHandling = DateParseHandling.None, // Explicitly specify date handling to avoid OS-dependent variants
                    };

                    // Use Task.Run to offload synchronous deserialization and support cancellation
                    userData = await Task.Run(() =>
                    {
                        cancellationToken.ThrowIfCancellationRequested();
                        return serializer.Deserialize<object>(jsonReader);
                    }, cancellationToken).ConfigureAwait(false);
                }

                // Simulate user creation
                return Ok("User created successfully");
            }
            catch (OperationCanceledException)
            {
                _logger.LogWarning("CreateUser operation was canceled.");
                return StatusCode(499, "Client Closed Request");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CreateUser");
                throw;
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}