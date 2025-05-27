using System.Collections;
using System.Data.Common;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlParameterCollection : DbParameterCollection
{
    private readonly List<LibSqlParameter> _parameters = new();

    public override int Count => _parameters.Count;
    public override object SyncRoot => _parameters;

    // Add 'new' keyword to fix warning CS0108
    public new LibSqlParameter this[int index] => _parameters[index];

    public override int Add(object value)
    {
        if (value is LibSqlParameter param)
        {
            Console.WriteLine($"DEBUG LibSqlParameterCollection: Adding parameter {param.ParameterName} = {param.Value}");
            _parameters.Add(param);
            return _parameters.Count - 1;
        }
        throw new ArgumentException("Parameter must be a LibSqlParameter", nameof(value));
    }

    public override void AddRange(Array values)
    {
        foreach (var value in values)
        {
            Add(value);
        }
    }

    public override void Clear() 
    {
        Console.WriteLine($"DEBUG LibSqlParameterCollection: Clearing {_parameters.Count} parameters");
        _parameters.Clear();
    }

    public override bool Contains(object value) => _parameters.Contains(value);
    public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);

    public override void CopyTo(Array array, int index)
    {
        ((ICollection)_parameters).CopyTo(array, index);
    }
    
    public override IEnumerator GetEnumerator() => _parameters.GetEnumerator();

    public override int IndexOf(object value) => _parameters.IndexOf((LibSqlParameter)value);
    public override int IndexOf(string parameterName) => _parameters.FindIndex(p => p.ParameterName == parameterName);

    public override void Insert(int index, object value)
    {
        if (value is LibSqlParameter param)
        {
            Console.WriteLine($"DEBUG LibSqlParameterCollection: Inserting parameter {param.ParameterName} at index {index}");
            _parameters.Insert(index, param);
        }
        else
        {
            throw new ArgumentException("Parameter must be a LibSqlParameter", nameof(value));
        }
    }

    public override void Remove(object value)
    {
        if (value is LibSqlParameter param)
        {
            Console.WriteLine($"DEBUG LibSqlParameterCollection: Removing parameter {param.ParameterName}");
            _parameters.Remove(param);
        }
    }

    public override void RemoveAt(int index) 
    {
        Console.WriteLine($"DEBUG LibSqlParameterCollection: Removing parameter at index {index}");
        _parameters.RemoveAt(index);
    }
    
    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            RemoveAt(index);
        }
    }

    protected override DbParameter GetParameter(int index) => _parameters[index];
    protected override DbParameter GetParameter(string parameterName)
    {
        var param = _parameters.FirstOrDefault(p => p.ParameterName == parameterName);
        if (param == null)
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found");
        }
        return param;
    }

    protected override void SetParameter(int index, DbParameter value)
    {
        if (value is LibSqlParameter param)
        {
            Console.WriteLine($"DEBUG LibSqlParameterCollection: Setting parameter at index {index} to {param.ParameterName}");
            _parameters[index] = param;
        }
        else
        {
            throw new ArgumentException("Parameter must be a LibSqlParameter", nameof(value));
        }
    }

    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            SetParameter(index, value);
        }
        else
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found");
        }
    }
}