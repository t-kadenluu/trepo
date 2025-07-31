using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Data.SqlClient;
using Microsoft.TestService.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
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
                var connectionString = GetPlatformCompatibleConnectionString(_connectionString);

                await using (var connection = new SqlConnection(connectionString))
                {
                    await connection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    await using (var command = new SqlCommand("SELECT Id, Name, Email FROM Users", connection))
                    await using (var reader = await command.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false))
                    {
                        while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
                        {
                            if (cancellationToken.IsCancellationRequested)
                            {
                                yield break;
                            }
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
            catch (SqlException ex) when (ex is not null)
            {
                _logger?.LogError(ex, "SQL error in GetDataAsync");
                throw;
            }
            catch (Exception ex) when (ex is not null)
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

            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (builder.IntegratedSecurity)
                {
                    builder.IntegratedSecurity = false;
                }
            }

            if (!builder.ContainsKey("Encrypt"))
            {
                builder.Encrypt = true;
            }
            if (!builder.ContainsKey("TrustServerCertificate"))
            {
                builder.TrustServerCertificate = true;
            }

            if (!builder.ContainsKey("Max Pool Size"))
            {
                builder.MaxPoolSize = 100;
            }

            return builder.ConnectionString;
        }

        public string SerializeData(object? data)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };
            return JsonSerializer.Serialize(data, options);
        }

        public async Task SerializeDataToFileAsync(object? data, string fileName, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            await using var stream = new FileStream(filePath, FileMode.Create, FileAccess.Write, FileShare.None, 4096, FileOptions.Asynchronous);
            await JsonSerializer.SerializeAsync(stream, data, data?.GetType() ?? typeof(object), options, cancellationToken).ConfigureAwait(false);
            await stream.FlushAsync(cancellationToken).ConfigureAwait(false);
        }

        public async Task<T?> DeserializeDataFromFileAsync<T>(string fileName, CancellationToken cancellationToken = default)
        {
            var options = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                WriteIndented = false
            };

            var filePath = Path.Combine(AppContext.BaseDirectory, fileName);
            await using var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.Asynchronous);
            return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);
        }
    }

    public record User
    {
        public int Id { get; init; }
        public string Name { get; init; } = string.Empty;
        public string Email { get; init; } = string.Empty;
    }
}