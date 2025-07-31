using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.TestService.Data;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.IO;
using System.Text;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace Microsoft.TestService.Data
{
    /// <summary>
    /// Data access layer using modern ADO.NET and cross-platform APIs
    /// </summary>
    public class DataRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DataRepository>? _logger;

        public DataRepository(string connectionString, ILogger<DataRepository>? logger = null)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async Task<List<User>> GetDataAsync(CancellationToken cancellationToken = default)
        {
            var results = new List<User>();
            var activity = new Activity("GetDataAsync");
            activity.Start();
            try
            {
                using (var connection = new SqlConnection(_connectionString))
                {
                    await connection.OpenAsync(cancellationToken);
                    var command = new SqlCommand("SELECT Id, Name, Email FROM Users", connection);

                    using (var reader = await command.ExecuteReaderAsync(cancellationToken))
                    {
                        while (await reader.ReadAsync(cancellationToken))
                        {
                            var user = new User
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2)
                            };
                            results.Add(user);
                        }
                    }
                }
                return results;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in GetDataAsync");
                throw;
            }
            finally
            {
                activity.Stop();
                _logger?.LogInformation("GetDataAsync duration: {Duration}ms", activity.Duration.TotalMilliseconds);
            }
        }

        public string SerializeData(object? data)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };
            return JsonConvert.SerializeObject(data, serializerSettings);
        }

        public async Task SerializeDataToFileAsync(object? data, string fileName, CancellationToken cancellationToken = default)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, true))
            using (var streamWriter = new StreamWriter(stream, Encoding.UTF8))
            using (var jsonWriter = new JsonTextWriter(streamWriter))
            {
                var serializer = JsonSerializer.Create(serializerSettings);
                await Task.Run(() => serializer.Serialize(jsonWriter, data), cancellationToken);
                await jsonWriter.FlushAsync(cancellationToken);
            }
        }

        public async Task<T?> DeserializeDataFromFileAsync<T>(string fileName, CancellationToken cancellationToken = default)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                NullValueHandling = NullValueHandling.Ignore
            };

            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, true))
            using (var streamReader = new StreamReader(stream, Encoding.UTF8))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                var serializer = JsonSerializer.Create(serializerSettings);
                return await Task.Run(() => serializer.Deserialize<T>(jsonReader), cancellationToken);
            }
        }
    }

    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }
}