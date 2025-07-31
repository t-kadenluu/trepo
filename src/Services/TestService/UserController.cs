using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
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
        [HttpGet("{id}")]
        public async Task<ActionResult> GetUser(int id, CancellationToken cancellationToken)
        {
            var user = new { Id = id, Name = "Test User" };
            return new JsonResult(user);
        }

        [HttpPost]
        public async Task<ActionResult> CreateUser([FromBody] object userData, CancellationToken cancellationToken)
        {
            // Simulate user creation
            return Ok("User created successfully");
        }
    }
}