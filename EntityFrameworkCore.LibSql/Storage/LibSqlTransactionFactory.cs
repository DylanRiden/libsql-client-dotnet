﻿using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Storage;

namespace EntityFrameworkCore.LibSql.Storage;

public class LibSqlTransactionFactory : IRelationalTransactionFactory
{
    private readonly RelationalTransactionFactoryDependencies _dependencies;

    public LibSqlTransactionFactory(RelationalTransactionFactoryDependencies dependencies)
    {
        _dependencies = dependencies;
    }

    public RelationalTransaction Create(
        IRelationalConnection connection,
        DbTransaction transaction,
        Guid transactionId,
        IDiagnosticsLogger<DbLoggerCategory.Database.Transaction> logger,
        bool transactionOwned)
    {
        return new RelationalTransaction(connection, transaction, transactionId, logger, transactionOwned, _dependencies.SqlGenerationHelper);
    }
}