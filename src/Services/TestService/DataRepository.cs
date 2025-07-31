using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
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
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using System.Collections.Frozen;
using System.Buffers;

namespace Microsoft.TestService.Data
{
    /// <summary>
    /// Data access layer using modern ADO.NET and cross-platform APIs
    /// </summary>
    public class DataRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<DataRepository>? _logger;
        private static readonly ActivitySource ActivitySource = new("Microsoft.TestService.Data.DataRepository");

        // Metrics: Use Meter, Counter, Histogram for cross-platform telemetry
        private static readonly Meter Meter = new("Microsoft.TestService.Data.DataRepository", "9.0.0");
        private static readonly Counter<long> GetDataAsyncCallCount = Meter.CreateCounter<long>("getdataasync_calls", description: "Number of GetDataAsync calls");
        private static readonly Histogram<double> GetDataAsyncDuration = Meter.CreateHistogram<double>("getdataasync_duration_ms", unit: "ms", description: "Duration of GetDataAsync in milliseconds");

        // Predefined log messages for performance
        private static readonly Action<ILogger, Exception?> LogSqlError =
            LoggerMessage.Define(LogLevel.Error, new EventId(1, nameof(LogSqlError)), "SQL error in GetDataAsync");
        private static readonly Action<ILogger, Exception?> LogGeneralError =
            LoggerMessage.Define(LogLevel.Error, new EventId(2, nameof(LogGeneralError)), "Error in GetDataAsync");
        private static readonly Action<ILogger, double?, Exception?> LogGetDataAsyncDuration =
            LoggerMessage.Define<double?>(LogLevel.Information, new EventId(3, nameof(LogGetDataAsyncDuration)), "GetDataAsync duration: {Duration}ms");

        public DataRepository(string connectionString, ILogger<DataRepository>? logger = null)
        {
            _connectionString = connectionString;
            _logger = logger;
        }

        public async IAsyncEnumerable<User> GetDataAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var activity = ActivitySource.StartActivity("GetDataAsync", ActivityKind.Internal);
            var startTimestamp = Stopwatch.GetTimestamp();
            GetDataAsyncCallCount.Add(1, new KeyValuePair<string, object?>("operation", "GetDataAsync"));
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
                            cancellationToken.ThrowIfCancellationRequested();
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
            catch (Microsoft.Data.SqlClient.SqlException ex) when (ex is not null)
            {
                if (_logger is not null)
                {
                    LogSqlError(_logger, ex);
                }
                throw;
            }
            catch (SqlException ex) when (ex is not null)
            {
                if (_logger is not null)
                {
                    LogSqlError(_logger, ex);
                }
                throw;
            }
            catch (OperationCanceledException ex) when (ex is not null)
            {
                // Cancellation requested, do not log as error
                throw;
            }
            catch (Exception ex) when (ex is not null)
            {
                if (_logger is not null)
                {
                    LogGeneralError(_logger, ex);
                }
                throw;
            }
            finally
            {
                var durationMs = (Stopwatch.GetTimestamp() - startTimestamp) * 1000.0 / Stopwatch.Frequency;
                GetDataAsyncDuration.Record(durationMs, new KeyValuePair<string, object?>("operation", "GetDataAsync"));
                activity?.Stop();
                if (_logger is not null)
                {
                    LogGetDataAsyncDuration(_logger, durationMs, null);
                }
            }
        }

        // Helper to adjust connection string for cross-platform compatibility
        private static string GetPlatformCompatibleConnectionString(string baseConnectionString)
        {
            var builder = new SqlConnectionStringBuilder(baseConnectionString);

            if (!OperatingSystem.IsWindows())
            {
                if (builder.IntegratedSecurity)
                {
                    builder.IntegratedSecurity = false;
                    // For cross-platform, recommend using Azure AD authentication if needed
                    // builder.Authentication = "Active Directory Interactive";
                }
            }

            // Remove deprecated or Windows-only options if present
            if (builder.ContainsKey("AttachDbFilename"))
            {
                builder.Remove("AttachDbFilename");
            }
            if (builder.ContainsKey("User Instance"))
            {
                builder.Remove("User Instance");
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
            return JsonSerializer.Serialize(data, data?.GetType() ?? typeof(object), options);
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
            var fileStreamOptions = new FileStreamOptions
            {
                Access = FileAccess.Write,
                Mode = FileMode.Create,
                Share = FileShare.None,
                BufferSize = 4096,
                Options = FileOptions.Asynchronous
            };
            await using var stream = new FileStream(filePath, fileStreamOptions);
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
            var fileStreamOptions = new FileStreamOptions
            {
                Access = FileAccess.Read,
                Mode = FileMode.Open,
                Share = FileShare.Read,
                BufferSize = 4096,
                Options = FileOptions.Asynchronous
            };
            await using var stream = new FileStream(filePath, fileStreamOptions);
            var result = await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);
            return result;
        }
    }

    public record User
    {
        [JsonPropertyName("id")]
        public int Id { get; init; }
        [JsonPropertyName("name")]
        public string Name { get; init; } = string.Empty;
        [JsonPropertyName("email")]
        public string Email { get; init; } = string.Empty;
    }
}