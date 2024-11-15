using System.Data;

namespace Core.Repository.DbContext;

public class DapperDbContext : IDapperDbContext
{
    protected readonly IDbConnection InnerConnection;

    protected DapperDbContext(IDbConnection connection)
    {
        InnerConnection = connection;
    }

    public virtual IDbConnection Connection
    {
        get
        {
            OpenConnection();
            return InnerConnection;
        }
    }

    public void OpenConnection()
    {
        if (InnerConnection.State != ConnectionState.Open && InnerConnection.State != ConnectionState.Connecting)
            InnerConnection.Open();
    }

    public virtual IDbTransaction BeginTransaction()
    {
        return Connection.BeginTransaction();
    }

    public void Dispose()
    {
        if (InnerConnection.State != ConnectionState.Closed)
            InnerConnection.Close();
    }
}
