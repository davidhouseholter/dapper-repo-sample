using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repository.Shared;

public interface ILease<out T> : IDisposable
{
    T Connection { get; }
}
public class Lease<T> : ILease<T>
{
    private readonly Action<T> _dispose;

    public Lease(T connection, Action<T> dispose)
    {
        _dispose = dispose;
        Connection = connection;
    }

    public T Connection { get; }

    public void Dispose() => _dispose(Connection);
}