using System;
using System.Data;
using System.Data.Common; // Changed namespace for base class
using System.Diagnostics.CodeAnalysis;
using System.Text;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using Libsql; // Assumed namespace for Bindings
using Libsql.Client.Ado.Exceptions; // Assumed namespace for LibsqlException

namespace Libsql.Client.Ado
{
    public class LibsqlDbConnection : DbConnection // Inherit from DbConnection
    {
        // Native handles
        private libsql_database_t _db;
        private libsql_connection_t _conn;

        private string? _connectionString;
        private ConnectionState _state = ConnectionState.Closed;
        private DatabaseClientOptions? _options; // Store parsed options
        private string? _serverVersion; // Cache server version

        private bool _disposed;

        // Constructors
        public LibsqlDbConnection() { }

        public LibsqlDbConnection(string connectionString)
        {
            ConnectionString = connectionString;
        }

        // Finalizer calls Dispose
        ~LibsqlDbConnection() => Dispose(false);

        // --- Abstract/Virtual Member Overrides from DbConnection ---

        [AllowNull]
        public override string ConnectionString
        {
            get => _connectionString ?? "";
            set
            {
                if (_state != ConnectionState.Closed)
                {
                    throw new InvalidOperationException("Cannot change ConnectionString while the connection is open or connecting.");
                }
                if (_connectionString != value)
                {
                    _connectionString = value;
                    _options = null; // Force re-parsing on next Open
                    _serverVersion = null; // Reset cache
                }
            }
        }

        public override int ConnectionTimeout => 15; // Default timeout

        public override string Database => GetDatabaseName(); // Delegate to helper

        public override ConnectionState State => _state;

        // DataSource should return the primary identifier (file path or URL)
        public override string DataSource => _options?.Url ?? ParseDataSourceFromConnectionString() ?? "";

        public override string ServerVersion
        {
            get
            {
                if (_disposed) throw new ObjectDisposedException(nameof(LibsqlDbConnection));
                if (_state != ConnectionState.Open)
                {
                    throw new InvalidOperationException($"Cannot retrieve ServerVersion when the connection state is {_state}.");
                }

                // Cache the version
                if (_serverVersion == null)
                {
                    try
                    {
                        using (var cmd = CreateDbCommand()) // Use the overridden method
                        {
                            cmd.CommandText = "SELECT sqlite_version();";
                            // This assumes ExecuteScalar works correctly
                            var result = cmd.ExecuteScalar();
                            _serverVersion = result?.ToString() ?? "N/A";
                        }
                    }
                    catch (Exception ex)
                    {
                        _serverVersion = $"Error: {ex.Message}"; // Or handle differently
                    }
                }
                return _serverVersion ?? "N/A";
            }
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
             if (_disposed) throw new ObjectDisposedException(nameof(LibsqlDbConnection));
             if (_state != ConnectionState.Open) throw new InvalidOperationException("Connection must be open to begin a transaction.");

             // Validate isolation level
             if (isolationLevel != IsolationLevel.Unspecified &&
                 isolationLevel != IsolationLevel.ReadCommitted &&
                 isolationLevel != IsolationLevel.Serializable /* Add others if mapped */)
             {
                 throw new ArgumentException($"IsolationLevel '{isolationLevel}' is not supported.", nameof(isolationLevel));
             }

            // When implemented:
            // 1. Execute "BEGIN" or appropriate command based on isolationLevel
            // 2. Create and return a new LibsqlDbTransaction(this, isolationLevel);
            throw new NotSupportedException("Transactions are not currently implemented.");
        }

        public override void ChangeDatabase(string databaseName)
        {
            // Still not supported
            throw new NotSupportedException("Changing the database after opening the connection is not supported.");
        }

        public override unsafe void Close()
        {
            if (_state != ConnectionState.Closed)
            {
                var previousState = _state;
                CloseNativeHandles();
                _state = ConnectionState.Closed;
                _options = null;
                _serverVersion = null;
                // Fire event *after* state change
                OnStateChange(new StateChangeEventArgs(previousState, _state));
            }
        }

        public override unsafe void Open()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(LibsqlDbConnection));
            if (_state != ConnectionState.Closed)
            {
                throw new InvalidOperationException($"Connection is already in state: {_state}.");
            }
            if (string.IsNullOrWhiteSpace(_connectionString))
            {
                throw new InvalidOperationException("ConnectionString must be set before opening.");
            }

            CloseNativeHandles(); // Ensure clean state
            var previousState = _state;
            _state = ConnectionState.Connecting;
            OnStateChange(new StateChangeEventArgs(previousState, _state)); // Fire Connecting event

            var error = new NativeError();
            int exitCode = 1;

            try
            {
                _options = ParseConnectionString(_connectionString);
                if (_options.Url == null) throw new InvalidOperationException("Parsed connection string did not yield a valid URL or Data Source.");

                // Step 1: Open Database (same logic as before)
                fixed (libsql_database_t* dbPtr = &_db)
                fixed (byte* urlPtr = Encoding.UTF8.GetBytes(_options.Url))
                {
                    bool isLocal = _options.Url == ":memory:" || (Uri.TryCreate(_options.Url, UriKind.Absolute, out var uri) && uri.IsFile);
                    if (isLocal)
                    {
                        exitCode = Bindings.libsql_open_ext(urlPtr, dbPtr, &error.Ptr);
                    }
                    else // Remote or Replica
                    {
                        string authToken = string.IsNullOrEmpty(_options.AuthToken) ? "\0" : _options.AuthToken;
                        fixed (byte* authTokenPtr = Encoding.UTF8.GetBytes(authToken))
                        {
                            if (string.IsNullOrEmpty(_options.ReplicaPath)) // Remote
                            {
                                exitCode = Bindings.libsql_open_remote(urlPtr, authTokenPtr, dbPtr, &error.Ptr);
                            }
                            else // Replica
                            {
                                fixed (byte* replicaPathPtr = Encoding.UTF8.GetBytes(_options.ReplicaPath))
                                {
                                    exitCode = Bindings.libsql_open_sync(replicaPathPtr, urlPtr, authTokenPtr, 1, null, dbPtr, &error.Ptr);
                                }
                            }
                        }
                    }
                }
                error.ThrowIfFailed(exitCode, $"Failed to open database '{_options.Url}'");

                // Step 2: Connect (same logic as before)
                fixed (libsql_connection_t* connPtr = &_conn)
                {
                    exitCode = Bindings.libsql_connect(_db, connPtr, &error.Ptr);
                }
                error.ThrowIfFailed(exitCode, "Failed to connect to database");

                previousState = _state;
                _state = ConnectionState.Open;
                OnStateChange(new StateChangeEventArgs(previousState, _state)); // Fire Open event
            }
            catch (Exception ex)
            {
                CloseNativeHandles(); // Cleanup on failure
                _options = null;
                previousState = _state;
                _state = ConnectionState.Closed;
                OnStateChange(new StateChangeEventArgs(previousState, _state)); // Fire Closed event
                throw new LibsqlException($"Failed to open connection: {ex.Message}", ex); // Wrap exception
            }
        }

        // Must override CreateDbCommand
        protected override unsafe DbCommand CreateDbCommand()
        {
             if (_disposed) throw new ObjectDisposedException(nameof(LibsqlDbConnection));
             if (!IsOpen()) // Use internal helper which checks state and handle
             {
                 throw new InvalidOperationException("Cannot create command when connection is not open.");
             }
            // Return the specific implementation, cast to base type DbCommand
            return new LibsqlDbCommand(this, _conn);
        }

        // --- Dispose Pattern ---

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;

            if (disposing)
            {
                // Release managed resources (if any added later)
                _options = null;
                _connectionString = null;
                 // Example: _managedResource?.Dispose();
            }

            // Always release unmanaged resources & update state
            Close(); // Close handles native cleanup and state update

            _disposed = true;
            // Call base class dispose *after* our logic
            base.Dispose(disposing);
        }


        #region Helper Methods (identical to previous implementation, keep private/internal)

        private unsafe void CloseNativeHandles()
        {
             if (_conn.ptr != null)
             {
                 Bindings.libsql_disconnect(_conn);
                 _conn.ptr = null;
             }
             if (_db.ptr != null)
             {
                 Bindings.libsql_close(_db);
                 _db.ptr = null;
             }
        }

        private DatabaseClientOptions ParseConnectionString(string connectionString)
        {
            var opts = new DatabaseClientOptions("");
            var builder = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var pairs = connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var pair in pairs)
            {
                var kvp = pair.Split(new[] { '=' }, 2);
                if (kvp.Length == 2) builder[kvp[0].Trim()] = kvp[1].Trim();
            }
            if (builder.TryGetValue("Data Source", out var dataSource)) opts.Url = dataSource;
            else if (builder.TryGetValue("Url", out var url)) opts.Url = url;
            if (builder.TryGetValue("Auth Token", out var authToken)) opts.AuthToken = authToken;
            if (builder.TryGetValue("Replica Path", out var replicaPath)) opts.ReplicaPath = replicaPath;
            if (builder.TryGetValue("Use Https", out var useHttpsStr) && bool.TryParse(useHttpsStr, out var useHttpsVal)) opts.UseHttps = useHttpsVal;
            if (string.IsNullOrWhiteSpace(opts.Url)) throw new ArgumentException("Connection string must contain a valid 'Data Source' or 'Url'.", nameof(connectionString));
            if (opts.Url == "") opts.Url = ":memory:";
            return opts;
        }

        private string GetDatabaseName()
        {
             if (_options?.Url != null)
             {
                 if (_options.Url == ":memory:") return ":memory:";
                 try
                 {
                     if (Uri.TryCreate(_options.Url, UriKind.Absolute, out var uri))
                     {
                         if (uri.IsFile) return System.IO.Path.GetFileName(uri.LocalPath);
                         if (uri.Scheme.StartsWith("http") || uri.Scheme.StartsWith("ws")) return uri.Host;
                     }
                     return System.IO.Path.GetFileName(_options.Url);
                 }
                 catch { }
                 return _options.Url;
             }
             return "";
        }

        private string? ParseDataSourceFromConnectionString()
        {
             if (string.IsNullOrWhiteSpace(_connectionString)) return null;
             try
             {
                 var pairs = _connectionString.Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries);
                 foreach(var pair in pairs)
                 {
                     var kvp = pair.Split(new[] { '=' }, 2);
                     if (kvp.Length == 2)
                     {
                          var key = kvp[0].Trim();
                          if (string.Equals(key, "Data Source", StringComparison.OrdinalIgnoreCase) || string.Equals(key, "Url", StringComparison.OrdinalIgnoreCase)) return kvp[1].Trim();
                     }
                 }
             } catch { }
             return null;
         }

         internal unsafe libsql_connection_t GetNativeConnection()
         {
             if (_disposed) throw new ObjectDisposedException(nameof(LibsqlDbConnection));
             if (!IsOpen()) throw new InvalidOperationException("Connection is not open or native handle is invalid.");
             return _conn;
         }

         internal unsafe bool IsOpen() => !_disposed && _state == ConnectionState.Open && _conn.ptr != null;

         #endregion // Helper Methods
    }
}