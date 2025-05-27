using System.Collections;
using System.Data;
using System.Data.Common;
using EntityFrameworkCore.LibSql.Http;

namespace EntityFrameworkCore.LibSql.Storage;

public class HttpLibSqlDataReader : DbDataReader
{
    private readonly LibSqlResult _result;
    private readonly string[] _columnNames;
    private readonly object[][] _rows;
    private int _currentRowIndex = -1;
    private bool _disposed;

    public HttpLibSqlDataReader(LibSqlResult result)
    {
        _result = result ?? throw new ArgumentNullException(nameof(result));
        _columnNames = result.Columns;
        _rows = result.Rows.ToArray();
        
        Console.WriteLine($"DEBUG HTTP LibSqlDataReader: Created with {_rows.Length} rows and {_columnNames.Length} columns");
        for (int i = 0; i < _columnNames.Length; i++)
        {
            Console.WriteLine($"DEBUG HTTP LibSqlDataReader: Column[{i}] = {_columnNames[i]}");
        }
    }
    
    public override int FieldCount => _columnNames.Length;
    public override bool HasRows => _rows.Length > 0;
    public override bool IsClosed => _disposed;
    public override int RecordsAffected => _result.RowsAffected;

    public override object this[int ordinal] => GetValue(ordinal);
    public override object this[string name] => GetValue(GetOrdinal(name));

    public override int Depth => 0;

    public override bool Read()
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(HttpLibSqlDataReader));

        _currentRowIndex++;
        var hasNext = _currentRowIndex < _rows.Length;
        
        if (hasNext)
        {
            Console.WriteLine($"DEBUG HTTP LibSqlDataReader: Read() - now at row {_currentRowIndex}");
        }
        else
        {
            Console.WriteLine($"DEBUG HTTP LibSqlDataReader: Read() - no more rows");
        }
        
        return hasNext;
    }

    public override Task<bool> ReadAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(Read());
    }

    public override bool NextResult()
    {
        return false; // HTTP LibSQL typically returns single result sets
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
        if (_currentRowIndex < 0 || _currentRowIndex >= _rows.Length)
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
        if (_currentRowIndex < 0 || _currentRowIndex >= _rows.Length)
            throw new InvalidOperationException("No current row");

        var value = _rows[_currentRowIndex][ordinal];
        Console.WriteLine($"DEBUG HTTP LibSqlDataReader: GetValue({ordinal}) = {value} (Type: {value?.GetType().Name ?? "null"})");
        
        return value ?? DBNull.Value;
    }

    public override bool IsDBNull(int ordinal)
    {
        var value = GetValue(ordinal);
        return value == null || value == DBNull.Value;
    }

    public override Task<bool> IsDBNullAsync(int ordinal, CancellationToken cancellationToken)
    {
        return Task.FromResult(IsDBNull(ordinal));
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
            row["ColumnSize"] = -1;
            row["DataType"] = GetFieldType(i);
            row["AllowDBNull"] = true;
            row["IsKey"] = false;
            row["IsUnique"] = false;
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
}