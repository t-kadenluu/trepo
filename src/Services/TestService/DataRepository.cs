using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using System.Data.SqlClient;
using Microsoft.TestService.Data;
using Newtonsoft.Json;

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

        public async Task<List<object>> GetDataAsync()
        {
            var results = new List<object>();
            
            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync();
                var command = new SqlCommand("SELECT * FROM Users", connection);
                // Legacy data access code
            }

            return results;
        }

        public string SerializeData(object data)
        {
            return JsonConvert.SerializeObject(data);
        }
    }
}
