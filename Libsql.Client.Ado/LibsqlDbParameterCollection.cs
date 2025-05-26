using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;

namespace Libsql.Client.Ado
{
    public class LibsqlDbParameterCollection : DbParameterCollection
    {
        private readonly List<LibsqlDbParameter> _parameters = new List<LibsqlDbParameter>();

        // Required overrides for DbParameterCollection
        public override int Count => _parameters.Count;
        public override object SyncRoot => ((System.Collections.ICollection)_parameters).SyncRoot;

        public override int Add(object value)
        {
            if (!(value is LibsqlDbParameter param))
            {
                throw new ArgumentException("Value must be of type LibsqlDbParameter.", nameof(value));
            }
            _parameters.Add(param);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            foreach (var value in values)
            {
                Add(value);
            }
        }

        public override void Clear() => _parameters.Clear();
        public override bool Contains(object value) => IndexOf(value) != -1;
        public override bool Contains(string value) => IndexOf(value) != -1;

        public override void CopyTo(Array array, int index) => ((System.Collections.ICollection)_parameters).CopyTo(array, index);

        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();

        protected override DbParameter GetParameter(int index) => _parameters[index];
        protected override DbParameter GetParameter(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index < 0)
            {
                throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
            }
            return _parameters[index];
        }

        public override int IndexOf(object value)
        {
            if (!(value is LibsqlDbParameter param)) return -1;
            return _parameters.IndexOf(param);
        }
        public override int IndexOf(string parameterName)
        {
            for (int i = 0; i < _parameters.Count; i++)
            {
                // Support parameters with or without leading '@' or ':' or '$'
                var name = _parameters[i].ParameterName;
                if (string.Equals(name, parameterName, StringComparison.OrdinalIgnoreCase) ||
                    string.Equals(name.TrimStart('@', ':', '$'), parameterName.TrimStart('@', ':', '$'), StringComparison.OrdinalIgnoreCase))
                {
                    return i;
                }
            }
            return -1;
        }

        public override void Insert(int index, object value)
        {
            if (!(value is LibsqlDbParameter param))
            {
                throw new ArgumentException("Value must be of type LibsqlDbParameter.", nameof(value));
            }
            _parameters.Insert(index, param);
        }

        public override void Remove(object value)
        {
            if (!(value is LibsqlDbParameter param)) return;
            _parameters.Remove(param);
        }

        public override void RemoveAt(int index) => _parameters.RemoveAt(index);
        public override void RemoveAt(string parameterName)
        {
            var index = IndexOf(parameterName);
            if (index >= 0) RemoveAt(index);
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            if (!(value is LibsqlDbParameter param))
            {
                throw new ArgumentException("Value must be of type LibsqlDbParameter.", nameof(value));
            }
            _parameters[index] = param;
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            var index = IndexOf(parameterName);
            if (index < 0) throw new IndexOutOfRangeException($"Parameter '{parameterName}' not found.");
            SetParameter(index, value);
        }

        // Strongly typed Add
        public LibsqlDbParameter Add(LibsqlDbParameter value)
        {
            _parameters.Add(value);
            return value;
        }

        // Convenient AddWithValue
        public LibsqlDbParameter AddWithValue(string parameterName, object? value)
        {
            var param = new LibsqlDbParameter(parameterName, value);
            return Add(param);
        }
    }
}