using System;
using System.Web.Http;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.TestService.Controllers;
using Newtonsoft.Json;

namespace Microsoft.TestService.Controllers
{
    /// <summary>
    /// Web API Controller for handling user operations
    /// </summary>
    public class UserController : ApiController
    {
        [HttpGet]
        public async Task<IHttpActionResult> GetUser(int id)
        {
            var user = new { Id = id, Name = "Test User" };
            return Json(user);
        }

        [HttpPost]
        public async Task<IHttpActionResult> CreateUser([FromBody] object userData)
        {
            // Simulate user creation
            return Ok("User created successfully");
        }
    }
}
