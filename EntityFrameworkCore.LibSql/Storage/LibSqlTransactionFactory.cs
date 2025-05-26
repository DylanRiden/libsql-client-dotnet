using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlTransactionFactory : IRelationalTransactionFactory
{
    private readonly ISqlGenerationHelper _sqlGenerationHelper;

    public LibSqlTransactionFactory(ISqlGenerationHelper sqlGenerationHelper)
    {
        _sqlGenerationHelper = sqlGenerationHelper;
    }

    public RelationalTransaction Create(
        IRelationalConnection connection,
        DbTransaction transaction,
        Guid transactionId,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger,
        bool transactionOwned)
    {
        return new RelationalTransaction(connection, transaction, transactionId, logger, transactionOwned, _sqlGenerationHelper);
    }
}
