using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlTransactionFactory : IRelationalTransactionFactory
{
    public RelationalTransaction Create(
        IRelationalConnection connection,
        System.Data.Common.DbTransaction transaction,
        Guid transactionId,
        IDiagnosticsLogger<IDbContextTransaction> logger,
        bool transactionOwned)
    {
        return new RelationalTransaction(connection, transaction, transactionId, logger, transactionOwned);
    }
}