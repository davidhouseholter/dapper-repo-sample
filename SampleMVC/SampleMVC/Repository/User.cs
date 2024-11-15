namespace SampleMVC.Repository;

using Core.Repository;
using Core.Repository.Entity;
using Core.Repository.SqlGenerator;
using System.Data;

public class UserRepository : DapperRepository<User>
{
    public UserRepository(IDbConnection connection, ISqlGenerator<User> sqlGenerator)
   : base(connection, sqlGenerator)
    {

    }
}
