using System;
using System.Data;
using System.Data.Common;
using System.Text;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Libsql; // For Bindings.*
using Libsql.Client.Ado.Exceptions; // For LibsqlException

namespace Libsql.Client.Ado
{
    public sealed class LibsqlDbCommand : DbCommand
    {
        private LibsqlDbConnection? _connection;
        private libsql_connection_t _nativeConnection;
        private string _commandText = "";
        private int _commandTimeout = 30; // Default ADO.NET command timeout
        private CommandType _commandType = CommandType.Text;
        private readonly LibsqlDbParameterCollection _parameters = new LibsqlDbParameterCollection();
        private DbTransaction? _transaction;
        private UpdateRowSource _updatedRowSource = UpdateRowSource.None;

        private libsql_stmt_t _preparedStatement;
        private bool _isPrepared = false; // Tracks if Prepare() was explicitly called
        private bool _disposed = false;

        // Constants
        private const int LIBSQL_OK = 0; // Assuming 0 is success

        internal LibsqlDbCommand(LibsqlDbConnection connection, libsql_connection_t nativeConnection)
        {
            _connection = connection;
            _nativeConnection = nativeConnection;
            if (_connection.IsOpen() && nativeConnection.ptr == null)
            {
                Console.Error.WriteLine("Warning: LibsqlDbCommand created with an open connection but a null native handle.");
            }
        }

        ~LibsqlDbCommand() => Dispose(false);

        // --- Public Properties ---
        [AllowNull] public override string CommandText { get => _commandText; set { if (_commandText == value) return; if (_isPrepared) ResetPreparedState(); _commandText = value ?? string.Empty; } }
        public override int CommandTimeout { get => _commandTimeout; set => _commandTimeout = value; }
        public override CommandType CommandType { get => _commandType; set { if (value == _commandType) return; if (value != CommandType.Text) throw new NotSupportedException($"CommandType '{value}' is not supported. Only Text is valid."); _commandType = value; ResetPreparedState(); } }
        protected override unsafe DbConnection? DbConnection { get => _connection; set { if (_isPrepared) throw new InvalidOperationException("Cannot change Connection while command is prepared."); if (value == _connection) return; if (value is LibsqlDbConnection newConn) { _connection = newConn; _nativeConnection = _connection.IsOpen() ? _connection.GetNativeConnection() : default; } else if (value == null) { _connection = null; _nativeConnection.ptr = null; } else throw new ArgumentException("Connection must be of type LibsqlDbConnection.", nameof(value)); } }
        protected override DbParameterCollection DbParameterCollection => _parameters;
        protected override DbTransaction? DbTransaction { get => _transaction; set { if (_transaction == value) return; if (_isPrepared) throw new InvalidOperationException("Cannot change Transaction while command is prepared."); if (value != null) { if (value.Connection != _connection) throw new ArgumentException("Transaction is not associated with the command's Connection.", nameof(value)); throw new NotSupportedException("Transactions are not currently implemented."); /* TODO */ } _transaction = value; } }
        public override bool DesignTimeVisible { get; set; } = false;
        public override UpdateRowSource UpdatedRowSource { get => _updatedRowSource; set => _updatedRowSource = value; }

        // --- Methods ---
        public override void Cancel() => throw new NotSupportedException("Cancel is not supported.");
        protected override DbParameter CreateDbParameter() => new LibsqlDbParameter();

        public override unsafe int ExecuteNonQuery()
        {
            ValidateStateForExecute();
            var errorHelper = new NativeError(); // Helper, not using IDisposable here
            byte* errorPtr = null; // Pointer passed to native calls
            int rowsAffected = 0;
            bool statementWasExplicitlyPrepared = _isPrepared;
            bool statementPreparedNow = false;

            try
            {
                statementPreparedNow = EnsurePreparedAndBound(ref errorPtr, errorHelper);

                int executeResult = Bindings.libsql_execute_stmt(_preparedStatement, &errorPtr);
                errorHelper.ThrowIfFailed(executeResult, errorPtr, "Failed to execute statement");
                // If ThrowIfFailed returns, errorPtr was handled (freed or null)

                rowsAffected = (int)Bindings.libsql_changes(_nativeConnection);

                ResetStatement(ref errorPtr, errorHelper);
            }
            catch (Exception)
            {
                if (statementPreparedNow && !statementWasExplicitlyPrepared) DisposePreparedStatement();
                throw;
            }
            finally
            {
                // Ensure ephemeral statement is cleaned up if not caught above
                if (statementPreparedNow && !statementWasExplicitlyPrepared && _preparedStatement.ptr != null) DisposePreparedStatement();
                // Ensure errorPtr is null if ThrowIfFailed wasn't called or didn't throw (should already be handled)
                 if (errorPtr != null)
                 {
                      Console.Error.WriteLine($"Warning: ExecuteNonQuery finally block found non-null error pointer {(IntPtr)errorPtr}. Attempting free.");
                      Bindings.libsql_free_string(errorPtr); // Attempt cleanup just in case
                 }
            }
            return rowsAffected;
        }

        protected override unsafe DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            ValidateStateForExecute();
            var errorHelper = new NativeError();
            byte* errorPtr = null;
            libsql_rows_t rowsResult = default;
            bool statementPreparedNow = false;

            try
            {
                statementPreparedNow = EnsurePreparedAndBound(ref errorPtr, errorHelper);

                int queryResult = Bindings.libsql_query_stmt(_preparedStatement, &rowsResult, &errorPtr);
                errorHelper.ThrowIfFailed(queryResult, errorPtr, "Failed to query statement");
                // If ThrowIfFailed returns, errorPtr was handled (freed or null)

                // Statement execution is complete, reset it for potential reuse if prepared.
                ResetStatement(ref errorPtr, errorHelper);

                // If statement was ephemeral, dispose it now that query is done.
                if (statementPreparedNow && !_isPrepared && _preparedStatement.ptr != null) DisposePreparedStatement();

                // TODO: Requires LibsqlDbDataReader implementation that works with libsql_rows_t
                var reader = new LibsqlDbDataReader(this, rowsResult, behavior);
                rowsResult.ptr = null; // Reader now owns the handle

                return reader;
            }
            catch (Exception)
            {
                // Cleanup on failure
                if (rowsResult.ptr != null) Bindings.libsql_free_rows(rowsResult); // Free rows handle if created
                if (statementPreparedNow && !_isPrepared) DisposePreparedStatement(); // Free ephemeral statement
                // Ensure errorPtr is handled if ThrowIfFailed didn't run/throw
                if (errorPtr != null) Bindings.libsql_free_string(errorPtr);
                throw;
            }
        }

        public override object? ExecuteScalar()
        {
            object? result = null;
            using (var reader = ExecuteDbDataReader(CommandBehavior.SingleResult | CommandBehavior.SingleRow))
            {
                if (reader.Read() && reader.FieldCount > 0)
                {
                    result = reader.GetValue(0);
                }
            }
            return result;
        }

        public override unsafe void Prepare()
        {
            ValidateStateForPrepare();
            var errorHelper = new NativeError();
            byte* errorPtr = null;
            try
            {
                DisposePreparedStatement(); // Dispose any existing handle first

                fixed (byte* sqlPtr = Encoding.UTF8.GetBytes(_commandText))
                fixed (libsql_stmt_t* stmtPtr = &_preparedStatement)
                {
                    int prepareResult = Bindings.libsql_prepare(_nativeConnection, sqlPtr, stmtPtr, &errorPtr);
                    errorHelper.ThrowIfFailed(prepareResult, errorPtr, $"Failed to prepare SQL: {_commandText}");
                }
                _isPrepared = true; // Mark as explicitly prepared
            }
            catch (Exception)
            {
                 // Ensure errorPtr is handled if ThrowIfFailed didn't run/throw
                 if (errorPtr != null) Bindings.libsql_free_string(errorPtr);
                ResetPreparedState(); // Frees handle and sets _isPrepared = false
                throw;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (_disposed) return;
            if (disposing)
            {
                _connection = null;
                _transaction = null;
            }
            DisposePreparedStatement();
            _nativeConnection.ptr = null;
            _disposed = true;
            base.Dispose(disposing);
        }

        // --- Helper Methods ---
        private unsafe void ValidateConnectionForExecution()
        {
            if (_connection == null) throw new InvalidOperationException("Connection property must be set.");
            if (_nativeConnection.ptr == null)
            {
                if (_connection.IsOpen()) { _nativeConnection = _connection.GetNativeConnection(); if (_nativeConnection.ptr == null) throw new InvalidOperationException("Connection is open, but failed to retrieve a valid native handle."); }
                else throw new InvalidOperationException("Connection must be open.");
            }
            if (!_connection.IsOpen()) throw new InvalidOperationException("Connection must be open.");
        }
        private void ValidateStateForExecute() { if (_disposed) throw new ObjectDisposedException(nameof(LibsqlDbCommand)); ValidateConnectionForExecution(); if (string.IsNullOrWhiteSpace(CommandText)) throw new InvalidOperationException("CommandText must be set."); }
        private void ValidateStateForPrepare() { if (_disposed) throw new ObjectDisposedException(nameof(LibsqlDbCommand)); ValidateConnectionForExecution(); if (string.IsNullOrWhiteSpace(CommandText)) throw new InvalidOperationException("CommandText must be set."); }

        /// <summary> Ensures statement is prepared and parameters are bound. Returns true if prepared ephemerally. </summary>
        private unsafe bool EnsurePreparedAndBound(ref byte* errorPtr, NativeError errorHelper)
        {
            bool preparedNow = false;
            if (_preparedStatement.ptr == null)
            {
                if (_isPrepared) throw new InvalidOperationException("Command is marked prepared, but native handle is missing.");
                fixed (byte* sqlPtr = Encoding.UTF8.GetBytes(_commandText))
                fixed (libsql_stmt_t* stmtPtr = &_preparedStatement)
                {
                    int prepareResult = Bindings.libsql_prepare(_nativeConnection, sqlPtr, stmtPtr, &errorPtr);
                    errorHelper.ThrowIfFailed(prepareResult, errorPtr, $"Failed to prepare SQL for execution: {_commandText}");
                }
                preparedNow = true;
            }
            else if (_isPrepared)
            {
                 ResetStatement(ref errorPtr, errorHelper);
            }
            BindParameters(ref errorPtr, errorHelper);
            return preparedNow;
        }

        private unsafe void BindParameters(ref byte* errorPtr, NativeError errorHelper)
        {
            // Cannot check parameter count - libsql_bind_parameter_count missing
            int providedParamCount = _parameters.Count;
            if (providedParamCount == 0) return;

            for (int i = 0; i < providedParamCount; i++)
            {
                var dbParam = _parameters[i]; if (dbParam == null) continue;
                int nativeIndex = i + 1;
                int bindResult = LIBSQL_OK;
                object? val = dbParam.Value;
                string paramNameForError = string.IsNullOrEmpty(dbParam.ParameterName) ? $"#{nativeIndex}" : dbParam.ParameterName;

                errorPtr = null; // Reset error pointer before each bind call
                try
                {
                    if (val == null || val == DBNull.Value) { bindResult = Bindings.libsql_bind_null(_preparedStatement, nativeIndex, &errorPtr); }
                    else switch (val)
                    {
                        case int v: bindResult = Bindings.libsql_bind_int(_preparedStatement, nativeIndex, v, &errorPtr); break; // Use long overload
                        case long v: bindResult = Bindings.libsql_bind_int(_preparedStatement, nativeIndex, v, &errorPtr); break;
                        case double v: bindResult = Bindings.libsql_bind_float(_preparedStatement, nativeIndex, v, &errorPtr); break;
                        case float v: bindResult = Bindings.libsql_bind_float(_preparedStatement, nativeIndex, v, &errorPtr); break; // Promote
                        case decimal v: bindResult = Bindings.libsql_bind_float(_preparedStatement, nativeIndex, (double)v, &errorPtr); break; // Convert
                        case string v: fixed (byte* p = Encoding.UTF8.GetBytes(v)) { bindResult = Bindings.libsql_bind_string(_preparedStatement, nativeIndex, p, &errorPtr); } break;
                        case byte[] v: fixed (byte* p = v) { bindResult = Bindings.libsql_bind_blob(_preparedStatement, nativeIndex, p, v.Length, &errorPtr); } break;
                        case bool v: bindResult = Bindings.libsql_bind_int(_preparedStatement, nativeIndex, v ? 1L : 0L, &errorPtr); break; // Use long overload
                        case DateTime v: var s1 = v.ToString("yyyy-MM-dd HH:mm:ss.fff"); fixed (byte* p = Encoding.UTF8.GetBytes(s1)) { bindResult = Bindings.libsql_bind_string(_preparedStatement, nativeIndex, p, &errorPtr); } break;
                        case DateTimeOffset v: var s2 = v.ToString("o"); fixed (byte* p = Encoding.UTF8.GetBytes(s2)) { bindResult = Bindings.libsql_bind_string(_preparedStatement, nativeIndex, p, &errorPtr); } break;
                        case Guid v: var s3 = v.ToString(); fixed (byte* p = Encoding.UTF8.GetBytes(s3)) { bindResult = Bindings.libsql_bind_string(_preparedStatement, nativeIndex, p, &errorPtr); } break;
                        default: throw new NotSupportedException($"Binding type {val.GetType().Name} for parameter '{paramNameForError}' is not supported.");
                    }
                    errorHelper.ThrowIfFailed(bindResult, errorPtr, $"Failed to bind parameter '{paramNameForError}' (Index: {nativeIndex})");
                    // errorPtr handled by ThrowIfFailed
                }
                catch (Exception ex)
                {
                     // Ensure errorPtr is handled if ThrowIfFailed didn't run/throw
                    if (errorPtr != null) Bindings.libsql_free_string(errorPtr);
                    throw new InvalidOperationException($"Error binding parameter '{paramNameForError}' (Index: {nativeIndex}): {ex.Message}", ex);
                }
            }
        }

        /// <summary> Resets the prepared statement using libsql_reset_stmt. </summary>
        private unsafe void ResetStatement(ref byte* errorPtr, NativeError errorHelper)
        {
            if (_preparedStatement.ptr != null)
            {
                errorPtr = null; // Reset before call
                int resetResult = Bindings.libsql_reset_stmt(_preparedStatement, &errorPtr);
                errorHelper.ThrowIfFailed(resetResult, errorPtr, "Failed to reset the prepared statement.");
                // errorPtr handled by ThrowIfFailed
            }
        }

        /// <summary> Frees the native prepared statement handle. </summary>
        private unsafe void DisposePreparedStatement()
        {
            if (_preparedStatement.ptr != null) { Bindings.libsql_free_stmt(_preparedStatement); _preparedStatement.ptr = null; }
        }

        /// <summary> Resets prepared state flag and ensures native statement handle is disposed. </summary>
        private void ResetPreparedState() { DisposePreparedStatement(); _isPrepared = false; }

        // Internal accessors
        internal unsafe libsql_connection_t NativeConnection => _nativeConnection;
        internal LibsqlDbConnection? GetConnection() => _connection;
    }

    // TODO: Implement LibsqlDbDataReader
    // Needs to:
    // 1. Accept libsql_rows_t in constructor.
    // 2. Call libsql_free_rows in Dispose().
    // 3. Implement DbDataReader methods using:
    //    - libsql_column_count
    //    - libsql_column_name
    //    - libsql_next_row (advances internal row state)
    //    - libsql_column_type (needs current row)
    //    - libsql_get_int, _get_float, _get_string, _get_blob (needs current row)
    //    - Manage freeing strings/blobs returned by get methods (e.g., using NativeError pattern or similar)
    public sealed class LibsqlDbDataReader : DbDataReader // SKELETON
    {
        private LibsqlDbCommand _command;
        private libsql_rows_t _rows;
        private CommandBehavior _behavior;
        private bool _disposed;
        private int _fieldCount = -1; // Cache field count
        private libsql_row_t _currentRow; // Handle for the current row after calling next_row
        private bool _hasRows; // Indicates if there was at least one row initially
        private bool _currentRowIsValid; // Tracks if _currentRow is valid after a call to Read()

        internal LibsqlDbDataReader(LibsqlDbCommand command, libsql_rows_t rows, CommandBehavior behavior)
        {
            _command = command;
            _rows = rows; // Takes ownership
            _behavior = behavior;
            _hasRows = true; // Assume initially true, Read will confirm
             _currentRowIsValid = false;
             // Determine field count immediately?
             _fieldCount = GetFieldCount();
        }

         private unsafe int GetFieldCount()
         {
             if (_fieldCount == -1 && _rows.ptr != null)
             {
                 _fieldCount = Bindings.libsql_column_count(_rows);
             }
             return _fieldCount < 0 ? 0 : _fieldCount;
         }

        public override unsafe bool Read()
        {
            if (_disposed || _rows.ptr == null) return false;

            // Free the previous row handle if it was valid
            if (_currentRow.ptr != null)
            {
                Bindings.libsql_free_row(_currentRow);
                _currentRow.ptr = null;
            }
             _currentRowIsValid = false;

            var errorHelper = new NativeError();
            byte* errorPtr = null;
            int result = Bindings.libsql_next_row(_rows, &_currentRow, &errorPtr); // Get next row

            if (result == LIBSQL_OK && _currentRow.ptr != null) // Success, got a row
            {
                 errorHelper.ThrowIfFailed(result, errorPtr, "Unexpected error during successful row fetch"); // Should be OK if ptr non-null
                 _currentRowIsValid = true;
                return true;
            }
            else if (result == LIBSQL_OK && _currentRow.ptr == null) // Success, end of rows
            {
                 errorHelper.ThrowIfFailed(result, errorPtr, "Unexpected error during end-of-rows"); // Should be OK
                return false;
            }
            else // Error
            {
                errorHelper.ThrowIfFailed(result, errorPtr, "Failed to get next row");
                return false; // Unreachable due to throw, but compiler requires it
            }
        }


        protected override unsafe void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Release managed resources if any
                }

                // Free the current row handle if valid
                if (_currentRow.ptr != null)
                {
                    Bindings.libsql_free_row(_currentRow);
                    _currentRow.ptr = null;
                }

                // Free the main rows handle
                if (_rows.ptr != null)
                {
                    Bindings.libsql_free_rows(_rows);
                    _rows.ptr = null;
                }

                _command = null; // Break reference
                _disposed = true;
            }
            base.Dispose(disposing);
        }

        // --- Minimal implementation of other abstract members ---
        // Replace these with actual logic using _currentRow and Bindings.libsql_get_* / Bindings.libsql_column_*
        public override object GetValue(int ordinal) { /* TODO: Implement using Bindings.libsql_get_* based on type */ throw new NotImplementedException(); }
        public override int FieldCount => GetFieldCount();
        public override bool IsDBNull(int ordinal) { /* TODO: Implement using Bindings.libsql_column_type */ throw new NotImplementedException(); }
        public override string GetName(int ordinal) { /* TODO: Implement using Bindings.libsql_column_name */ throw new NotImplementedException(); }
        // ... other methods like GetInt32, GetString, GetBoolean etc. need full implementation ...
        public override int GetInt32(int ordinal) => throw new NotImplementedException();
        public override long GetInt64(int ordinal) => throw new NotImplementedException();
        public override string GetString(int ordinal) => throw new NotImplementedException();
        public override bool GetBoolean(int ordinal) => throw new NotImplementedException();
        public override decimal GetDecimal(int ordinal) => throw new NotImplementedException();
        public override double GetDouble(int ordinal) => throw new NotImplementedException();
        public override float GetFloat(int ordinal) => throw new NotImplementedException();
        public override DateTime GetDateTime(int ordinal) => throw new NotImplementedException();
        public override Guid GetGuid(int ordinal) => throw new NotImplementedException();
        public override DataTable GetSchemaTable() => throw new NotSupportedException(); // Or implement if needed
        public override bool NextResult() => false; // Assuming single result set
        public override int Depth => 0; // Not hierarchical
        public override bool IsClosed => _disposed;
        public override int RecordsAffected => -1; // Typically for non-query commands
        public override object this[int ordinal] => GetValue(ordinal);
        public override object this[string name] => GetValue(GetOrdinal(name));
        public override int GetOrdinal(string name) { /* TODO: Find ordinal based on name */ throw new NotImplementedException(); }
        // Add other necessary overrides...
        public override bool HasRows => _hasRows; // Needs better logic, check if first Read() succeeds?
        public override System.Collections.IEnumerator GetEnumerator() => new DbEnumerator(this); // Standard implementation

        public override Type GetFieldType(int ordinal) => throw new NotImplementedException(); // TODO use column_type
        public override long GetBytes(int ordinal, long dataOffset, byte[] buffer, int bufferOffset, int length) => throw new NotImplementedException(); // TODO use get_blob
        public override long GetChars(int ordinal, long dataOffset, char[] buffer, int bufferOffset, int length) => throw new NotSupportedException(); // Usually not implemented directly
        // ... etc
    }
}