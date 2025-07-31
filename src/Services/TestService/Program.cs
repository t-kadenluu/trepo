using System;
using System.Threading.Tasks;

namespace Microsoft.TestService.Startup
{
    /// <summary>
    /// Application startup configuration
    /// </summary>
    public class Startup
    {
        public void Configure()
        {
            Console.WriteLine("Application configured successfully");
        }
    }
}

namespace Microsoft.TestService.Program
{
    /// <summary>
    /// Main program entry point
    /// </summary>
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Starting TestService...");
            
            // Initialize startup
            var startup = new Microsoft.TestService.Startup.Startup();
            startup.Configure();
            
            Console.WriteLine("TestService started successfully");
            Console.WriteLine("Press any key to exit...");
            Console.ReadLine();
        }
    }
}
