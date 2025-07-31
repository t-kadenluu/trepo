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
using System.Runtime.InteropServices;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;

namespace Microsoft.TestService.Data
{
    /// <summary>
    /// Data access layer using modern ADO.NET and cross-platform APIs
    /// </summary>
    public class DataRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DataRepository>? _logger;
        private static readonly ActivitySource ActivitySource = new ActivitySource("Microsoft.TestService.Data.DataRepository");

        public DataRepository(string connectionString, ILogger<DataRepository>? logger = null)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async IAsyncEnumerable<User> GetDataAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("GetDataAsync", ActivityKind.Internal);
            try
            {
                // Platform-specific connection string handling
                var connectionString = GetPlatformCompatibleConnectionString(_connectionString);

                await using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    await using (var command = new SqlCommand("SELECT Id, Name, Email FROM Users", connection))
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            var user = new User
                            {
                                Id = reader.GetInt32(0),
                                Name = reader.GetString(1),
                                Email = reader.GetString(2)
                            };
                            yield return user;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                _logger?.LogError(ex, "SQL error in GetDataAsync");
                throw;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error in GetDataAsync");
                throw;
            }
            finally
            {
                activity?.Stop();
                _logger?.LogInformation("GetDataAsync duration: {Duration}ms", activity?.Duration.TotalMilliseconds);
            }
        }

        // Helper to adjust connection string for cross-platform compatibility
        private static string GetPlatformCompatibleConnectionString(string baseConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(baseConnectionString);

            // Remove or adjust Windows-only authentication for non-Windows platforms
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (builder.IntegratedSecurity)
                {
                    // Integrated Security is not supported cross-platform; fallback to SQL authentication or throw
                    builder.IntegratedSecurity = false;
                    // Optionally: throw new PlatformNotSupportedException("Integrated Security is not supported on this platform.");
                }
                // Remove or adjust other Windows-specific settings as needed
            }

            // Ensure encryption and trust server certificate settings are compatible
            if (!builder.ContainsKey("Encrypt"))
            {
                builder.Encrypt = true;
            }
            if (!builder.ContainsKey("TrustServerCertificate"))
            {
                builder.TrustServerCertificate = true;
            }

            // Connection pooling configuration for .NET 9.0
            if (!builder.ContainsKey("Max Pool Size"))
            {
                builder.MaxPoolSize = 100;
            }

            return builder.ConnectionString;
        }

        public string SerializeData(object? data)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCaseNamingStrategyContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };
            return JsonConvert.SerializeObject(data, serializerSettings);
        }

        public async Task SerializeDataToFileAsync(object? data, string fileName, CancellationToken cancellationToken = default)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCaseNamingStrategyContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            using var streamWriter = new StreamWriter(stream, Encoding.UTF8);
            using var jsonWriter = new JsonTextWriter(streamWriter);
            var serializer = JsonSerializer.Create(serializerSettings);

            // Ensure cancellation is respected during serialization
            await serializer.SerializeAsync(jsonWriter, data, cancellationToken).ConfigureAwait(false);

            await jsonWriter.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<T?> DeserializeDataFromFileAsync<T>(string fileName, CancellationToken cancellationToken = default)
        {
            var serializerSettings = new JsonSerializerSettings
            {
                ContractResolver = new CamelCaseNamingStrategyContractResolver(),
                NullValueHandling = NullValueHandling.Ignore,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                DateTimeZoneHandling = DateTimeZoneHandling.Utc
            };

            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            using var streamReader = new StreamReader(stream, Encoding.UTF8);
            using var jsonReader = new JsonTextReader(streamReader);
            var serializer = JsonSerializer.Create(serializerSettings);

            // Ensure cancellation is respected during deserialization
            return await serializer.DeserializeAsync<T>(jsonReader, cancellationToken).ConfigureAwait(false);
        }
    }

    // Custom contract resolver using the latest IContractResolver and NamingStrategy extensibility
    public class CamelCaseNamingStrategyContractResolver : DefaultContractResolver
    {
        public CamelCaseNamingStrategyContractResolver()
        {
            NamingStrategy = new CamelCaseNamingStrategy();
        }
    }

    public record User
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}