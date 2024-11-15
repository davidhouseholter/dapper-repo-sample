using Core.Repository.SqlGenerator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repository.Config;


public interface IMySqlJobStorageConfig
{
    string ConnectionString { get; }
    string? Prefix { get; }
}
public class MySqlJobStorageConfig : IMySqlJobStorageConfig
{
    public string ConnectionString { get; set; } =
        "server=localhost;database=test;uid=test;pwd=test";

    public string? Prefix { get; set; }
}

public interface IConnectionStringConfig
{
    string ConnectionString { get; }
    string? Prefix { get; }
}
public class ConnectionStringConfig: IConnectionStringConfig
{
    public string ConnectionString { get; set; }
    public string? Prefix { get; }
}
public class CoreRepositoryConfig
{
    public EntityMangerContext Context { get; set; }

    public EntityGeneratedConfig Generated { get; set; }
}
public class EntityMangerContext
{
    public string Name { get; set; }
    public SqlProvider SqlProvider { get; set; } = SqlProvider.MySQL;
}
public class EntityGeneratedConfig
{
    public string EntityPath { get; set; }
    public string TablePrefix { get; set; } = string.Empty;
    public bool UseQuotationMarks { get; set; }

}