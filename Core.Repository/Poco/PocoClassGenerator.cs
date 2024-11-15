

using Core.Repository.Shared;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;

namespace Core.Repository.Poco;



public static partial class PocoClassGenerator
{
    public static Tables GeneratePocoTables(this DbConnection connection, string providerName, string schemaName, bool includeViews)
    {

        DbProviderFactory _factory;
        try
        {
            _factory = DbProviderFactories.GetFactory(connection);
        }
        catch (Exception x)
        {
            var error = x.Message.Replace("\r\n", "\n").Replace("\n", " ");
            Console.WriteLine(string.Format("Failed to load provider `{0}` - {1}", providerName, error));

            return new Tables();
        }

        if (connection.State != ConnectionState.Open) connection.Open();

        SchemaReader reader = null;

        // Assume SQL Server
        reader = new SqlServerSchemaReader();
        Tables result;

        result = reader.ReadSchema(connection, _factory, schemaName);

        // Remove unrequired tables/views
        for (int i = result.Count - 1; i >= 0; i--)
        {
            if (schemaName != null && string.Compare(result[i].Schema, schemaName, true) != 0)
            {
                result.RemoveAt(i);
                continue;
            }
            if (!includeViews && result[i].IsView)
            {
                result.RemoveAt(i);
                continue;
            }
        }
        return result;

    }

 
}
public abstract class SqlStorageBase
{
    protected static readonly Task<bool> AlwaysFalse = Task.FromResult(false);
}


public abstract class SqlStorageBase<TConnection> : SqlStorageBase where TConnection : DbConnection
{
    private readonly SemaphoreSlim _migrationLock = new(1);
    private int _databaseReady;


    protected abstract Task<TConnection> CreateConnection();

    protected virtual Task OpenConnection(TConnection connection)
        => connection.OpenAsync();

    protected virtual void DisposeConnection(TConnection connection)
    => connection.Dispose();

    protected virtual async Task<ILease<TConnection>> Connect()
    {
        var lease = new Lease<TConnection>(await CreateConnection(), DisposeConnection);
        try
        {
            var connection = lease.Connection;
            if (connection.State == ConnectionState.Closed)
                await OpenConnection(connection);

            if (!DatabaseReady)
                await TryCreateDatabase(connection);

            return lease;
        }
        catch
        {
            try
            {
                lease.Dispose();
            }
            catch
            {
                // ignore
            }

            throw;
        }
    }

    protected abstract Task CreateDatabase(TConnection connection);

    protected async Task TryCreateDatabase(TConnection connection)
    {
        if (DatabaseReady)
            return;

        await _migrationLock.WaitAsync();
        try
        {
            // check again
            if (DatabaseReady)
                return;

            await CreateDatabase(connection);

            DatabaseReady = true;
        }
        finally
        {
            _migrationLock.Release();
        }
    }

    public bool DatabaseReady
    {
        get => Interlocked.CompareExchange(ref _databaseReady, 0, 0) != 0;
        set => Interlocked.Exchange(ref _databaseReady, value ? 1 : 0);
    }

    protected virtual async Task<T> Execute<T>(TConnection connection, Func<TConnection, Task<T>> action, CancellationToken token = default)
    {
        token.ThrowIfCancellationRequested();
        return await action(connection);
    }
    
    public async Task<Tables> GeneratePoco()
    {
        var lease = new Lease<TConnection>(await CreateConnection(), DisposeConnection);
        try
        {
            var connection = lease.Connection;
            if (connection.State == ConnectionState.Closed)
                await OpenConnection(connection);

            Tables result = connection.GeneratePocoTables("MySQL", "TestDB", false);

            Console.WriteLine(result);
            return result;
        }
        catch (Exception ex)
        {
            try
            {
                lease.Dispose();
            }
            catch
            {
                // ignore
            }
            Console.WriteLine(ex.Message);
            throw;
        }
    }
}
