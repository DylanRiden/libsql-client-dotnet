using System.Collections;
using System.Data;
using System.Data.Common;
using Libsql.Client;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlDataReader : DbDataReader
{
    private readonly ResultSet _resultSet;
    private readonly IEnumerator<Row> _rowEnumerator;
    private readonly string[] _columnNames;
    private bool _disposed;
    private bool _hasCurrentRow;

    public LibSqlDataReader(ResultSet resultSet)
    {
        _resultSet = resultSet ?? throw new ArgumentNullException(nameof(resultSet));
        _rowEnumerator = resultSet.Rows.GetEnumerator();
        _columnNames = resultSet.Columns.ToArray();
    }

    public override int FieldCount => _columnNames.Length;
    public override bool HasRows => _resultSet.Rows.Any();
    public override bool IsClosed => _disposed;
    public override int RecordsAffected => (int)(_resultSet.RowsAffected ?? 0);

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override int Depth => 0; // LibSQL doesn't support nested result sets

    public override bool Read()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(LibSqlDataReader));

        _hasCurrentRow = _rowEnumerator.MoveNext();
        return _hasCurrentRow;
    }

    public override async Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        // LibSQL client doesn't support async row reading currently
        // So we just call the sync version
        return Read();
    }

    public override bool NextResult()
    {
        // LibSQL doesn't support multiple result sets
        return false;
    }

    public override string GetName(int ordinal)
    {
        ValidateOrdinal(ordinal);
        return _columnNames[ordinal];
    }

    public override int GetOrdinal(string name)
    {
        for (int i = 0; i < _columnNames.Length; i++)
        {
            if (string.Equals(_columnNames[i], name, StringComparison.OrdinalIgnoreCase))
                return i;
        }
        throw new ArgumentException($"Column '{name}' not found", nameof(name));
    }

    public override Type GetFieldType(int ordinal)
    {
        ValidateOrdinal(ordinal);
        if (!_hasCurrentRow)
            return typeof(object);

        var value = GetValue(ordinal);
        return value?.GetType() ?? typeof(object);
    }

    public override string GetDataTypeName(int ordinal)
    {
        var type = GetFieldType(ordinal);
        return type.Name;
    }

    public override object GetValue(int ordinal)
    {
        ValidateOrdinal(ordinal);
        if (!_hasCurrentRow)
            throw new InvalidOperationException("No current row");

        var row = _rowEnumerator.Current;
        var values = row.ToArray();
        
        if (ordinal >= values.Length)
            return DBNull.Value;

        var value = values[ordinal];
        return ConvertLibSqlValue(value);
    }

    public override bool IsDBNull(int ordinal)
    {
        var value = GetValue(ordinal);
        return value == null || value == DBNull.Value;
    }

    public override async Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
    {
        return IsDBNull(ordinal);
    }

    public override bool GetBoolean(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            bool b => b,
            long l => l != 0,
            int i => i != 0,
            string s => bool.Parse(s),
            _ => Convert.ToBoolean(value)
        };
    }

    public override byte GetByte(int ordinal) => Convert.ToByte(GetValue(ordinal));
    public override char GetChar(int ordinal) => Convert.ToChar(GetValue(ordinal));
    
    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            DateTime dt => dt,
            string s => DateTime.Parse(s),
            _ => Convert.ToDateTime(value)
        };
    }

    public override decimal GetDecimal(int ordinal) => Convert.ToDecimal(GetValue(ordinal));
    public override double GetDouble(int ordinal) => Convert.ToDouble(GetValue(ordinal));
    public override float GetFloat(int ordinal) => Convert.ToSingle(GetValue(ordinal));
    
    public override Guid GetGuid(int ordinal)
    {
        var value = GetValue(ordinal);
        return value switch
        {
            Guid g => g,
            string s => Guid.Parse(s),
            _ => throw new InvalidCastException($"Cannot convert {value?.GetType()} to Guid")
        };
    }

    public override short GetInt16(int ordinal) => Convert.ToInt16(GetValue(ordinal));
    public override int GetInt32(int ordinal) => Convert.ToInt32(GetValue(ordinal));
    public override long GetInt64(int ordinal) => Convert.ToInt64(GetValue(ordinal));
    public override string GetString(int ordinal) => Convert.ToString(GetValue(ordinal)) ?? string.Empty;

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value is not byte[] bytes)
            throw new InvalidCastException("Column is not a byte array");

        if (buffer == null)
            return bytes.Length;

        var bytesToCopy = Math.Min(length, bytes.Length - (int)dataOffset);
        Array.Copy(bytes, dataOffset, buffer, bufferOffset, bytesToCopy);
        return bytesToCopy;
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var value = GetString(ordinal);
        if (buffer == null)
            return value.Length;

        var charsToCopy = Math.Min(length, value.Length - (int)dataOffset);
        value.CopyTo((int)dataOffset, buffer, bufferOffset, charsToCopy);
        return charsToCopy;
    }

    public override int GetValues(object[] values)
    {
        if (values == null)
            throw new ArgumentNullException(nameof(values));

        var count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }
        return count;
    }

    public override DataTable GetSchemaTable()
    {
        var schemaTable = new DataTable("SchemaTable");
        
        // Add standard schema columns
        schemaTable.Columns.Add("ColumnName", typeof(string));
        schemaTable.Columns.Add("ColumnOrdinal", typeof(int));
        schemaTable.Columns.Add("ColumnSize", typeof(int));
        schemaTable.Columns.Add("DataType", typeof(Type));
        schemaTable.Columns.Add("AllowDBNull", typeof(bool));
        schemaTable.Columns.Add("IsKey", typeof(bool));
        schemaTable.Columns.Add("IsUnique", typeof(bool));
        schemaTable.Columns.Add("IsReadOnly", typeof(bool));

        for (int i = 0; i < FieldCount; i++)
        {
            var row = schemaTable.NewRow();
            row["ColumnName"] = GetName(i);
            row["ColumnOrdinal"] = i;
            row["ColumnSize"] = -1; // Unknown size for LibSQL
            row["DataType"] = GetFieldType(i);
            row["AllowDBNull"] = true; // LibSQL allows nulls by default
            row["IsKey"] = false; // We don't have key information from ResultSet
            row["IsUnique"] = false; // We don't have unique information from ResultSet
            row["IsReadOnly"] = false;
            schemaTable.Rows.Add(row);
        }

        return schemaTable;
    }

    public override IEnumerator GetEnumerator()
    {
        return new DbEnumerator(this);
    }

    public override void Close()
    {
        if (!_disposed)
        {
            _rowEnumerator?.Dispose();
            _disposed = true;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed && disposing)
        {
            Close();
        }
        base.Dispose(disposing);
    }

    private void ValidateOrdinal(int ordinal)
    {
        if (ordinal < 0 || ordinal >= FieldCount)
            throw new ArgumentOutOfRangeException(nameof(ordinal), $"Ordinal {ordinal} is out of range. Valid range is 0 to {FieldCount - 1}");
    }

    private static object ConvertLibSqlValue(Value value)
    {
        return value switch
        {
            Integer integer => integer.Value,
            Real real => real.Value,
            Text text => text.Value,
            Blob blob => blob.Value,
            Null => DBNull.Value,
            _ => DBNull.Value
        };
    }
}