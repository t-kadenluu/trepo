using System;
using System.Collections.Generic;
using System.Configuration;

namespace Microsoft.TestService.Auth
{
    /// <summary>
    /// Legacy authentication helper
    /// </summary>
    public static class AuthHelper
    {
        public static bool ValidateUser(string username, string password)
        {
            // Legacy authentication logic
            var configValue = ConfigurationManager.AppSettings["AuthEnabled"];
            return !string.IsNullOrEmpty(username);
        }

        public static string GetCurrentUser()
        {
            // Simplified for demo - would normally check HttpContext
            return "DemoUser";
        }
    }
}

namespace Microsoft.TestService.Data
{
    /// <summary>
    /// Data access layer using legacy ADO.NET
    /// </summary>
    public class DataRepository
    {
        private readonly string _connectionString;

        public DataRepository(string connectionString)
        {
            _connectionString = connectionString;
        }

        public List<object> GetData()
        {
            var results = new List<object>();
            
            // Simulate data access
            results.Add(new { Id = 1, Name = "Sample User" });
            
            return results;
        }

        public string SerializeData(object data)
        {
            return data?.ToString() ?? string.Empty;
        }
    }
}

namespace Microsoft.TestService.Services
{
    /// <summary>
    /// Service for legacy operations
    /// </summary>
    public interface ILegacyService
    {
        string GetLegacyData(string id);
    }

    public class LegacyService : ILegacyService
    {
        public string GetLegacyData(string id)
        {
            return $"Legacy data for ID: {id}";
        }
    }
}
