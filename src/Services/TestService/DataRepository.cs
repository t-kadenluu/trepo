using System;
using System.Collections.Generic;
using System.Linq;
using System.Data;
using Microsoft.Data.SqlClient;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using Microsoft.Extensions.Caching.Memory;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Microsoft.TestService.Data
{
    /// <summary>
    /// Data access layer using modern ADO.NET and cross-platform APIs
    /// </summary>
    public class DataRepository
    {
        private readonly string _connectionString;
        private readonly IMemoryCache _cache;
        private static readonly ActivitySource ActivitySource = new ActivitySource("Microsoft.TestService.Data.DataRepository");

        public DataRepository(string connectionString, IMemoryCache cache)
        {
            _connectionString = connectionString;
            _cache = cache;
        }

        public async Task<List<object>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("GetDataAsync");
            var cacheKey = "UsersData";
            if (_cache.TryGetValue(cacheKey, out List<object>? cachedResults))
            {
                return cachedResults!;
            }

            var results = new List<object>();

            using (var connection = new SqlConnection(_connectionString))
            {
                await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                using var command = new SqlCommand("SELECT * FROM Users", connection);
                using var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
                var columns = Enumerable.Range(0, reader.FieldCount).Select(reader.GetName).ToList();
                while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                {
                    var row = new Dictionary<string, object?>();
                    foreach (var column in columns)
                    {
                        row[column] = await reader.IsDBNullAsync(reader.GetOrdinal(column), cancellationToken).ConfigureAwait(false)
                            ? null
                            : reader.GetValue(reader.GetOrdinal(column));
                    }
                    results.Add(row);
                }
            }

            _cache.Set(cacheKey, results, TimeSpan.FromMinutes(5));
            return results;
        }

        public string SerializeData(object? data)
        {
            return JsonSerializer.Serialize(data);
        }
    }
}