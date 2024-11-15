using System.Data;
using Core.Repository.Repo;
using Core.Repository.SqlGenerator;

namespace Core.Repository;

/// <summary>
///     Base Repository
/// </summary>
public partial class DapperRepository<TEntity> : ReadOnlyDapperRepository<TEntity>, IDapperRepository<TEntity>
    where TEntity : class
{
    /// <summary>
    ///     Constructor
    /// </summary>
    public DapperRepository(IDbConnection connection)
        : base(connection)
    {
    }

    /// <summary>
    ///     Constructor
    /// </summary>
    public DapperRepository(IDbConnection connection, ISqlGenerator<TEntity> sqlGenerator)
        : base(connection, sqlGenerator)
    {
    }
}
