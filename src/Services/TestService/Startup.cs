using System;
using System.Threading.Tasks;
using Microsoft.Owin;
using Microsoft.Owin.Security;
using Microsoft.Owin.Hosting;
using Owin;

namespace Microsoft.TestService.Startup
{
    /// <summary>
    /// OWIN Startup configuration
    /// </summary>
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            // Configure OWIN middleware
            app.Use<CustomMiddleware>();
        }
    }

    public class CustomMiddleware : OwinMiddleware
    {
        public CustomMiddleware(OwinMiddleware next) : base(next)
        {
        }

        public override async Task Invoke(IOwinContext context)
        {
            // Custom middleware logic
            await Next.Invoke(context);
        }
    }
}
