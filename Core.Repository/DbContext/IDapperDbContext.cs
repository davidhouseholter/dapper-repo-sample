using System;
using System.Data;

namespace Core.Repository.DbContext;

public interface IDapperDbContext : IDisposable
{
    IDbConnection Connection { get; }

    void OpenConnection();

    IDbTransaction BeginTransaction();
}
