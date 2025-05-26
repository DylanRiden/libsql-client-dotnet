namespace Libsql.Client.Ado.Exceptions;

public class LibsqlException : System.Data.Common.DbException // Inherit from DbException for ADO.NET context
{
    public LibsqlException(string message) : base(message) { }
    public LibsqlException(string message, Exception inner) : base(message, inner) { }
    // Add constructor for error code if needed
    public LibsqlException(string message, int errorCode) : base($"{message} (Code: {errorCode})") { /* Store errorCode */ }

}