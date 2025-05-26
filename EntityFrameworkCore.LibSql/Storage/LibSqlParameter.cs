using System.Data;
using System.Data.Common;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlParameter : DbParameter
{
    private object? _value;
    private DbType _dbType = DbType.Object;
    private string _parameterName = string.Empty;

    public override DbType DbType 
    { 
        get => _dbType; 
        set => _dbType = value; 
    }

    public override ParameterDirection Direction { get; set; } = ParameterDirection.Input;
    public override bool IsNullable { get; set; } = true;
    
    public override string ParameterName 
    { 
        get => _parameterName; 
        set => _parameterName = value ?? string.Empty; 
    }

    public override int Size { get; set; }
    public override string SourceColumn { get; set; } = string.Empty;
    public override bool SourceColumnNullMapping { get; set; }
    public override DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

    public override object? Value 
    { 
        get => _value; 
        set 
        { 
            _value = value;
            if (_dbType == DbType.Object && value != null)
            {
                _dbType = InferDbType(value);
            }
        } 
    }

    public override void ResetDbType()
    {
        _dbType = DbType.Object;
    }

    private static DbType InferDbType(object value)
    {
        return value switch
        {
            string => DbType.String,
            int => DbType.Int32,
            long => DbType.Int64,
            double => DbType.Double,
            float => DbType.Single,
            bool => DbType.Boolean,
            DateTime => DbType.DateTime,
            byte[] => DbType.Binary,
            Guid => DbType.Guid,
            decimal => DbType.Decimal,
            _ => DbType.Object
        };
    }
}
