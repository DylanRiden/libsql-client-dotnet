using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Libsql.Client.Ado;

public class LibsqlDbParameter : DbParameter
{
    private string _parameterName = "";
    private object? _value;
    private DbType _dbType;
    private ParameterDirection _direction = ParameterDirection.Input;
    private bool _isNullable;
    private int _size;
    private string _sourceColumn = "";
    private bool _sourceColumnNullMapping;
    private DataRowVersion _sourceVersion = DataRowVersion.Current;

    public LibsqlDbParameter() { }

    public LibsqlDbParameter(string name, object? value)
    {
        ParameterName = name;
        Value = value;
    }

    public LibsqlDbParameter(string name, DbType type, object? value)
    {
        ParameterName = name;
        DbType = type;
        Value = value;
    }

    public override void ResetDbType()
    {
        // Infer type from value again if needed, or reset to default
        _dbType = InferDbType(Value);
    }

    // Infer DbType from value - basic implementation
    private static DbType InferDbType(object? value)
    {
        return value switch
        {
            null => DbType.Object, // Or String? SQLite is flexible
            int _ => DbType.Int64, // Use Int64 for SQLite INTEGER affinity
            long _ => DbType.Int64,
            float _ => DbType.Double, // Use Double for SQLite REAL affinity
            double _ => DbType.Double,
            decimal _ => DbType.Double, // Map decimal to REAL
            string _ => DbType.String,
            byte[] _ => DbType.Binary,
            bool b => DbType.Int64, // Store bools as 0/1
            DateTime dt => DbType.String, // Store dates as ISO8601 strings typically
            DateTimeOffset dto => DbType.String, // Store as ISO8601 strings
            _ => DbType.Object // Fallback
        };
    }

    public override DbType DbType
    {
        get => _dbType;
        set => _dbType = value;
    }

    public override ParameterDirection Direction
    {
        get => _direction;
        set
        {
            if (value != ParameterDirection.Input)
            {
                throw new NotSupportedException("Only Input parameters are supported by Libsql.");
            }
            _direction = value;
        }
    }

    public override bool IsNullable
    {
        get => _isNullable;
        set => _isNullable = value;
    }

    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = value ?? string.Empty;
    }

    [AllowNull]
    public override string SourceColumn
    {
        get => _sourceColumn;
        set => _sourceColumn = value ?? string.Empty;
    }

    public override bool SourceColumnNullMapping
    {
        get => _sourceColumnNullMapping;
        set => _sourceColumnNullMapping = value;
    }

    public override DataRowVersion SourceVersion
    {
        get => _sourceVersion;
        set => _sourceVersion = value;
    }

    public override object? Value
    {
        get => _value;
        set
        {
            _value = value;
            // Optionally infer DbType when value changes
            _dbType = InferDbType(value);
        }
    }

    public override int Size
    {
        get => _size;
        set => _size = value;
    }
}