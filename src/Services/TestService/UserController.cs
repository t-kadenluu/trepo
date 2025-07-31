using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Microsoft.TestService.Controllers
{
    /// <summary>
    /// Web API Controller for handling user operations
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class UserController : ControllerBase
    {
        private static readonly Action<ILogger, int, Exception?> _getUserError =
            LoggerMessage.Define<int>(
                LogLevel.Error,
                new EventId(1, nameof(GetUser)),
                "Error in GetUser for user id {UserId}");

        private static readonly Action<ILogger, Exception?> _getUserCanceled =
            LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(2, nameof(GetUser)),
                "GetUser operation was canceled.");

        private static readonly Action<ILogger, Exception?> _createUserError =
            LoggerMessage.Define(
                LogLevel.Error,
                new EventId(3, nameof(CreateUser)),
                "Error in CreateUser");

        private static readonly Action<ILogger, Exception?> _createUserCanceled =
            LoggerMessage.Define(
                LogLevel.Warning,
                new EventId(4, nameof(CreateUser)),
                "CreateUser operation was canceled.");

        private readonly ILogger<UserController> _logger;

        public UserController(ILogger<UserController> logger)
        {
            _logger = logger;
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetUser(int id, CancellationToken cancellationToken)
        {
            using var activity = Activity.Current ?? new Activity("GetUser");
            activity.Start();
            try
            {
                cancellationToken.ThrowIfCancellationRequested();
                var user = new { Id = id, Name = "Test User" };
                return Ok(user);
            }
            catch (OperationCanceledException)
            {
                _getUserCanceled(_logger, null);
                return StatusCode(499, "Client Closed Request");
            }
            catch (ArgumentNullException ex)
            {
                _getUserError(_logger, id, ex);
                throw;
            }
            catch (ArgumentException ex)
            {
                _getUserError(_logger, id, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _getUserError(_logger, id, ex);
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
            using var activity = Activity.Current ?? new Activity("CreateUser");
            activity.Start();
            try
            {
                object? userData;
                // Explicitly specify UTF8 encoding and newline handling for cross-platform consistency
                await using (var reader = new StreamReader(Request.Body, Encoding.UTF8, detectEncodingFromByteOrderMarks: false, bufferSize: 1024, leaveOpen: true))
                using (var jsonReader = new JsonTextReader(reader) { CloseInput = false, SupportMultipleContent = false, LineInfoHandling = LineInfoHandling.Load })
                {
                    var serializer = new JsonSerializer
                    {
                        DateParseHandling = DateParseHandling.None,
                    };

                    // Use the new async ReadAsync API with cancellation support
                    if (await jsonReader.ReadAsync(cancellationToken).ConfigureAwait(false))
                    {
                        userData = serializer.Deserialize<object>(jsonReader);
                    }
                    else
                    {
                        userData = null;
                    }
                }

                return Ok("User created successfully");
            }
            catch (OperationCanceledException)
            {
                _createUserCanceled(_logger, null);
                return StatusCode(499, "Client Closed Request");
            }
            catch (ArgumentNullException ex)
            {
                _createUserError(_logger, ex);
                throw;
            }
            catch (ArgumentException ex)
            {
                _createUserError(_logger, ex);
                throw;
            }
            catch (InvalidOperationException ex)
            {
                _createUserError(_logger, ex);
                throw;
            }
            finally
            {
                activity.Stop();
            }
        }
    }
}