
using Core.Repository.Config;
using Core.Repository.Poco;
using Core.Repository.Shared;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using MySqlConnector;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlTypes;

namespace Core.Repository.Migrate
{
    public class SQLBase<TConnection> : SqlStorageBase<MySqlConnection>
    {
        private readonly Func<Task<MySqlConnection>> _connectionFactory;

        public SQLBase(IConnectionStringConfig config)
        {
            _connectionFactory = ConnectionFactory(config.ConnectionString);

        }

        private static Func<Task<MySqlConnection>> ConnectionFactory(string connectionString) => () => Task.FromResult(new MySqlConnection(connectionString));

        protected override Task<MySqlConnection> CreateConnection() => _connectionFactory();

        protected override Task CreateDatabase(MySqlConnection connection)
        {

            //var migrations = _resourceLoader.LoadMigrations(_tablePrefix);
            //var migrator = new MySqlMigrator(connection, _tablePrefix, migrations);
            //migrator.Install();
            return Task.CompletedTask;
        }

        public async Task<Tables> GeneratePoco()
        {
            var lease = new Lease<MySqlConnection>(await CreateConnection(), DisposeConnection);
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
    public class MigrateSchemaService

    {
        private readonly SQLBase<MySqlConnection> baseSql;
        private readonly IConfiguration _configuration;

        private readonly CoreRepositoryConfig _entityConfig;
        public MigrateSchemaService(IConfiguration configuration)
        {
            _configuration = configuration;

            baseSql = new SQLBase<MySqlConnection>(new ConnectionStringConfig() { 
             ConnectionString = "Server=10.1.1.8;Database=TestDB;Uid=tree;Pwd=WWW112233!;AllowUserVariables=true"
            });
            _entityConfig = _configuration.GetRequiredSection("EntityConfig").Get<CoreRepositoryConfig>();
            RepositoryOrmConfig.Config = _entityConfig;
        }

         public async Task Migrate(string[] args)
        {
            Tables res = await baseSql.GeneratePoco();
            var fil = GenerateCode("Core.Repository.Entity", res);
            Console.WriteLine(fil);
            // Get the absolute path to the executable or assembly
            string exePath = Assembly.GetExecutingAssembly().Location;
            string exeDirectory = Path.GetDirectoryName(exePath);


            string projectDirectory = Path.GetFullPath(Path.Combine(exeDirectory, @"..\..\..\..\SampleMVC"));
            string savPath = Path.Combine(projectDirectory, _entityConfig.Generated.EntityPath);
            Console.WriteLine(savPath);
            File.WriteAllText(Path.Combine(savPath, $"{_entityConfig.Context.Name}EntityContext.cs"), fil);
            File.WriteAllText(Path.Combine(savPath, $"{_entityConfig.Context.Name}EntityContext.json"), JsonConvert.SerializeObject(res));


            return;
        }
       
        public static CoreRepositoryConfig LoadJson()
        {
            try
            {
                // Read the file content
                string jsonContent = File.ReadAllText("repo.config.json");

                // Deserialize the JSON content to the MySettings object
                CoreRepositoryConfig settings = JsonConvert.DeserializeObject<CoreRepositoryConfig>(jsonContent);

                return settings;
            }
            catch (Exception ex)
            {
                // Handle exceptions (file not found, invalid JSON, etc.)
                Console.WriteLine("Error loading JSON file: " + ex.Message);
                return null;
            }
        }

        public static string GenerateCode(string namespaceName, Tables tables)
        {
            var sb = new StringBuilder();
            sb.AppendLine("using System;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
            sb.AppendLine("using System.ComponentModel.DataAnnotations.Schema;");
            sb.AppendLine("using System.Collections.Generic;");
            sb.AppendLine("using Core.Repository.Attributes;");
            sb.AppendLine("using Core.Repository.Attributes.Joins;");
            sb.AppendLine();
            sb.AppendLine($"namespace {namespaceName}");
            sb.AppendLine("{");

            foreach (var table in tables)
            {
                sb.AppendLine($"    [Table(\"{table.Name}\")]");
                sb.AppendLine($"    public partial class {table.ClassName}");
                sb.AppendLine("    {");

                foreach (var column in table.Columns)
                {
                    if (column.IsPrimaryKey)
                    {
                        sb.AppendLine("        [Key]");
                    }
                    if (column.IsAutoIncrement)
                    {
                        sb.AppendLine("        [Identity]");
                    }
                    sb.AppendLine($"        public {column.DataType}{(column.IsNullable ? "?" : "")} {column.PropertyName} {{ get; set; }}");
                }
                sb.AppendLine("// Inner Keys");

                foreach (var key in table.InnerKeys)
                {
                    //sb.AppendLine($"       // {JsonConvert.SerializeObject(key)}");
                    sb.AppendLine($"        [LeftJoin(\"{key.FKTable}\",\"{key.BYColumnName}\",\"{key.FKColumnName}\")]");

                    sb.AppendLine($"        public List<{key.FKTableType}> {key.FKTable} {{ get; set; }}");

                    //sb.AppendLine($"        public {key.BYTableName} {key.BYTableName} {{ get; set; }}");
                }
                sb.AppendLine("// OuterKeys Keys");

                foreach (var key in table.OuterKeys)
                {
                    //sb.AppendLine($"       // {JsonConvert.SerializeObject(key)}");

                    //sb.AppendLine($"        [InverseProperty(\"{key.BYTableType}\")]");
                    //        [LeftJoin("Jobs", "JobId", "Id")]

                    sb.AppendLine($"        [LeftJoin(\"{key.BYTableName}\",\"{key.FKColumnName}\",\"{key.BYColumnName}\")]");
                    sb.AppendLine($"        public {key.BYTableType} {key.BYTableType} {{ get; set; }}");
                }

                sb.AppendLine("    }");
            }

            sb.AppendLine("}");

            return sb.ToString();
        }

    }
}
