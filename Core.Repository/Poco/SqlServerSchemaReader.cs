using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using PluralizationService;
using PluralizeService.Core;
namespace Core.Repository.Poco;


abstract class SchemaReader
{
    public abstract Tables ReadSchema(DbConnection connection, DbProviderFactory factory, string schemaName);
    //public GeneratedTextTransformation outer;
    public void WriteLine(string o)
    {
        //outer.WriteLine(o);
    }

}

class SqlServerSchemaReader : SchemaReader
{
    // SchemaReader.ReadSchema
    public override Tables ReadSchema(DbConnection connection, DbProviderFactory factory, string schemaName)
    {
        var result = new Tables();

        _connection = connection;
        _factory = factory;

        var cmd = _factory.CreateCommand();
        cmd.Connection = connection;
        cmd.CommandText = GetTableSQL(schemaName);

        //pull the tables in a reader
        using (cmd)
        {

            using (var rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    Table tbl = new Table();
                    tbl.Name = rdr["TABLE_NAME"].ToString();
                    tbl.Schema = rdr["TABLE_SCHEMA"].ToString();
                    tbl.IsView = string.Compare(rdr["TABLE_TYPE"].ToString(), "View", true) == 0;
                    tbl.CleanName = CleanUp(tbl.Name);
                    if (tbl.CleanName.StartsWith("tbl_")) tbl.CleanName = tbl.CleanName.Replace("tbl_", "");
                    if (tbl.CleanName.StartsWith("tbl")) tbl.CleanName = tbl.CleanName.Replace("tbl", "");
                    tbl.CleanName = tbl.CleanName.Replace("_", "");
                    tbl.ClassName = Singularize(RemoveTablePrefixes(tbl.CleanName));

                    result.Add(tbl);
                }
            }
        }

        foreach (var tbl in result)
        {
            tbl.Columns = LoadColumns(tbl);

            // Mark the primary key
            string[] PrimaryKeys = GetPK(tbl.Name);

            foreach (string primaryKey in PrimaryKeys)
            {
                var pkColumn = tbl.Columns.SingleOrDefault(x => x.Name.ToLower().Trim() == primaryKey.ToLower().Trim());
                if (pkColumn != null)
                {
                    pkColumn.IsPrimaryKey = true;
                }
            }

            try
            {
                tbl.OuterKeys = LoadOuterKeys(tbl);
                tbl.InnerKeys = LoadInnerKeys(tbl);
            }
            catch (Exception x)
            {
                var error = x.Message.Replace("\r\n", "\n").Replace("\n", " ");
                WriteLine("");
                WriteLine("// -----------------------------------------------------------------------------------------");
                WriteLine(String.Format("// Failed to get relationships for `{0}` - {1}", tbl.Name, error));
                WriteLine("// -----------------------------------------------------------------------------------------");
                WriteLine("");
            }
        }


        return result;
    }

    DbConnection _connection;
    DbProviderFactory _factory;
    static Regex rxCleanUp = new Regex(@"[^\w\d_]", RegexOptions.Compiled);

    public Func<string, string> CleanUp = (str) =>
    {
        str = rxCleanUp.Replace(str, "_");
        if (char.IsDigit(str[0])) str = "_" + str;

        return str;
    };
    public string Singularize(string word)
    {
        string singularword = PluralizationProvider.Singularize(word);
        return singularword;
    }
    public string RemoveTablePrefixes(string word)
    {
        var cleanword = word;
        if (cleanword.StartsWith("tbl_")) cleanword = cleanword.Replace("tbl_", "");
        if (cleanword.StartsWith("tbl")) cleanword = cleanword.Replace("tbl", "");
        cleanword = cleanword.Replace("_", "");
        return cleanword;
    }
    List<Column> LoadColumns(Table tbl)
    {

        using (var cmd = _factory.CreateCommand())
        {
            cmd.Connection = _connection;
            const string COLUMN_SQL = @"
        SELECT 
		    TABLE_CATALOG AS `Database`,
		    TABLE_SCHEMA AS Owner, 
		    TABLE_NAME AS TableName, 
		    COLUMN_NAME AS ColumnName, 
		    COLUMN_TYPE as ColumnType,
		    COLUMN_DEFAULT as ColumnDefault,
		    ORDINAL_POSITION AS OrdinalPosition, 
		    COLUMN_DEFAULT AS DefaultSetting, 
		    IS_NULLABLE AS IsNullable, DATA_TYPE AS DataType, 
		    CHARACTER_MAXIMUM_LENGTH AS MaxLength, 
		    DATETIME_PRECISION AS DatePrecision,
		    EXTRA AS Extra
        FROM  INFORMATION_SCHEMA.COLUMNS
		WHERE TABLE_NAME=@tableName AND TABLE_SCHEMA=@schemaName
		ORDER BY OrdinalPosition ASC";
            cmd.CommandText = COLUMN_SQL;

            var p = cmd.CreateParameter();
            p.ParameterName = "@tableName";
            p.Value = tbl.Name;
            cmd.Parameters.Add(p);

            p = cmd.CreateParameter();
            p.ParameterName = "@schemaName";
            p.Value = tbl.Schema;
            cmd.Parameters.Add(p);

            var result = new List<Column>();
            using (IDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    Column col = new Column();
                    col.Name = rdr["ColumnName"].ToString();
                    col.ColumnName = rdr["ColumnName"].ToString();
                    col.ColumnType = rdr["ColumnType"].ToString();

                    col.ColumnDefault = rdr["ColumnDefault"].ToString();

                    col.PropertyName = CleanUp(col.Name);

                    col.IsNullable = rdr["IsNullable"].ToString() == "YES";
                    
                    col.Extra = rdr["Extra"].ToString();

                    col.IsAutoIncrement = col.Extra.Contains("auto_increment"); ;
                    if (long.TryParse(rdr["MaxLength"].ToString(), out var maxLength))
                    {
                        col.MaxLength = maxLength;
                    }
                    else
                    {
                        col.MaxLength = null;
                    }
                    col.DataType = GetPropertyType(rdr["DataType"].ToString(), col.MaxLength);
                   
                    result.Add(col);
                }
            }

            return result;
        }
    }

    List<Key> LoadOuterKeys(Table tbl)
    {
        using (var cmd = _factory.CreateCommand())
        {
            cmd.Connection = _connection;
            const string OUTER_KEYS_SQL = @" 
SELECT 
    TC.TABLE_NAME AS FKTable,
    FK.CONSTRAINT_NAME AS FKName,
    FK.COLUMN_NAME AS FKColumnName,
    REFERENCED_TBL.TABLE_NAME AS BYTableName,
    REFERENCED_COL.COLUMN_NAME AS BYColumnName
FROM INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS FK
INNER JOIN INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC
    ON FK.TABLE_SCHEMA = TC.TABLE_SCHEMA
    AND FK.TABLE_NAME = TC.TABLE_NAME
    AND FK.CONSTRAINT_NAME = TC.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS RC
    ON FK.TABLE_SCHEMA = RC.CONSTRAINT_SCHEMA
    AND FK.CONSTRAINT_NAME = RC.CONSTRAINT_NAME
INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS REFERENCED_COL
    ON FK.REFERENCED_TABLE_SCHEMA = REFERENCED_COL.TABLE_SCHEMA
    AND FK.REFERENCED_TABLE_NAME = REFERENCED_COL.TABLE_NAME
    AND FK.REFERENCED_COLUMN_NAME = REFERENCED_COL.COLUMN_NAME
INNER JOIN INFORMATION_SCHEMA.TABLES AS REFERENCED_TBL
    ON FK.REFERENCED_TABLE_SCHEMA = REFERENCED_TBL.TABLE_SCHEMA
    AND FK.REFERENCED_TABLE_NAME = REFERENCED_TBL.TABLE_NAME
WHERE FK.TABLE_SCHEMA = DATABASE() -- Replace DATABASE() with your specific database name if needed
  AND FK.TABLE_NAME = @tableName
  AND TC.CONSTRAINT_TYPE = 'FOREIGN KEY'
  AND REFERENCED_COL.CONSTRAINT_NAME != 'PRIMARY';
";
            cmd.CommandText = OUTER_KEYS_SQL;

            var p = cmd.CreateParameter();
            p.ParameterName = "@tableName";
            p.Value = tbl.Name;
            cmd.Parameters.Add(p);

            var result = new List<Key>();
            using (IDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var key = new Key();

                    key.FKName = rdr["FKName"].ToString();
                    key.FKTable = rdr["FKTable"].ToString();
                    key.FKTableType = Singularize(RemoveTablePrefixes(key.FKTable));

                    key.FKColumnName = rdr["FKColumnName"].ToString();

                    key.BYTableName = rdr["BYTableName"].ToString();
                    
                    key.CleanBYTableName = CleanUp(key.BYTableName);
                 
                    key.BYTableType = Singularize(RemoveTablePrefixes(key.CleanBYTableName));

                    key.BYColumnName = rdr["BYColumnName"].ToString();
                    result.Add(key);
                }
            }

            return result;
        }
    }

    List<Key> LoadInnerKeys(Table tbl)
    {
        using (var cmd = _factory.CreateCommand())
        {
            cmd.Connection = _connection;
            const string INNER_KEYS_SQL = @"  
SELECT
    FK.TABLE_NAME AS FKTable,
    FK.COLUMN_NAME AS FKColumnName,
    FK.CONSTRAINT_NAME AS FKName,
    FK.REFERENCED_TABLE_NAME AS BYTableName,
    FK.REFERENCED_COLUMN_NAME AS BYColumnName
FROM
    INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS FK

WHERE
    FK.REFERENCED_TABLE_SCHEMA = DATABASE() -- Replace DATABASE() with your specific database name if needed --
    AND FK.REFERENCED_TABLE_NAME = @tableName
    AND FK.POSITION_IN_UNIQUE_CONSTRAINT IS NOT NULL
";
            cmd.CommandText = INNER_KEYS_SQL;

            var p = cmd.CreateParameter();
            p.ParameterName = "@tableName";
            p.Value = tbl.Name;
            cmd.Parameters.Add(p);

            var result = new List<Key>();
            using (IDataReader rdr = cmd.ExecuteReader())
            {
                while (rdr.Read())
                {
                    var key = new Key();
                    key.FKName = rdr["FKName"].ToString();
                    key.FKTable = rdr["FKTable"].ToString();
                    key.FKTableType = Singularize(RemoveTablePrefixes(key.FKTable));

                    key.FKColumnName = rdr["FKColumnName"].ToString();
                    key.BYTableName = rdr["BYTableName"].ToString();
                    key.BYColumnName = rdr["BYColumnName"].ToString();
                    key.CleanBYTableName = CleanUp(key.BYTableName);
     
                    key.BYTableType = Singularize(RemoveTablePrefixes(key.CleanBYTableName));
                    result.Add(key);
                }
            }

            return result;
        }
    }

    string[] GetPK(string table)
    {

        //string sql = @"SELECT c.name AS ColumnName
        //        FROM sys.indexes AS i 
        //        INNER JOIN sys.index_columns AS ic ON i.object_id = ic.object_id AND i.index_id = ic.index_id 
        //        INNER JOIN sys.objects AS o ON i.object_id = o.object_id 
        //        LEFT OUTER JOIN sys.columns AS c ON ic.object_id = c.object_id AND c.column_id = ic.column_id
        //        WHERE (i.type = 1) AND (o.name = @tableName)";
        var sql = @"SELECT COLUMN_NAME AS ColumnName
FROM INFORMATION_SCHEMA.STATISTICS
WHERE TABLE_SCHEMA = DATABASE()
  AND TABLE_NAME = @tableName
  AND INDEX_NAME = 'PRIMARY';";
        List<string> primaryKeys = new List<string>();

        using (var cmd = _factory.CreateCommand())
        {
            cmd.Connection = _connection;
            cmd.CommandText = sql;

            var p = cmd.CreateParameter();
            p.ParameterName = "@tableName";
            p.Value = table;
            cmd.Parameters.Add(p);

            using (var result = cmd.ExecuteReader())
            {
                if (result != null && result.HasRows)
                {
                    while (result.Read())
                    {
                        primaryKeys.Add(result.GetString(0));
                    }
                }
            }
        }

        return primaryKeys.ToArray();
    }

    string GetPropertyType(string sqlType, long? maxLength)
    {
        string sysType = "string";
        switch (sqlType)
        {
            case "char":
                if (maxLength == 36)
                    sysType = "Guid";
                break;
            case "bigint":
                sysType = "long";
                break;
            case "smallint":
                sysType = "short";
                break;
            case "int":
                sysType = "int";
                break;
            case "uniqueidentifier":
                sysType = "Guid";
                break;
            case "smalldatetime":
            case "datetime":
            case "datetime2":
            case "date":
            case "time":
                sysType = "DateTime";
                break;
            case "float":
                sysType = "double";
                break;
            case "real":
                sysType = "float";
                break;
            case "numeric":
            case "smallmoney":
            case "decimal":
            case "money":
                sysType = "decimal";
                break;
            case "tinyint":
                sysType = "byte";
                break;
            case "bit":
                sysType = "bool";
                break;
            case "image":
            case "binary":
            case "varbinary":
            case "timestamp":
                sysType = "DateTime";
                break;
            case "geography":
                sysType = "Microsoft.SqlServer.Types.SqlGeography";
                break;
            case "geometry":
                sysType = "Microsoft.SqlServer.Types.SqlGeometry";
                break;
        }
        return sysType;
    }

    string GetTableSQL(string schemaName)
    {
        return $@"
SELECT
    *
FROM
    INFORMATION_SCHEMA.TABLES
WHERE
    (
        TABLE_TYPE = 'BASE TABLE'
        OR TABLE_TYPE = 'VIEW'
    )
    AND TABLE_SCHEMA = '{schemaName}'
";
    }

  //      const string COLUMN_SQL = @"SELECT 
		//	TABLE_CATALOG AS [Database],
		//	TABLE_SCHEMA AS Owner, 
		//	TABLE_NAME AS TableName, 
		//	COLUMN_NAME AS ColumnName, 
		//	ORDINAL_POSITION AS OrdinalPosition, 
		//	COLUMN_DEFAULT AS DefaultSetting, 
		//	IS_NULLABLE AS IsNullable, DATA_TYPE AS DataType, 
		//	CHARACTER_MAXIMUM_LENGTH AS MaxLength, 
		//	DATETIME_PRECISION AS DatePrecision,
		//	COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsIdentity') AS IsIdentity,
		//	COLUMNPROPERTY(object_id('[' + TABLE_SCHEMA + '].[' + TABLE_NAME + ']'), COLUMN_NAME, 'IsComputed') as IsComputed
		//FROM  INFORMATION_SCHEMA.COLUMNS
		//WHERE TABLE_NAME=@tableName AND TABLE_SCHEMA=@schemaName
		//ORDER BY OrdinalPosition ASC";

    
  //const string OUTER_KEYS_SQL = @"SELECT 
		//	FK = OBJECT_NAME(pt.constraint_object_id),
		//	Referenced_tbl = OBJECT_NAME(pt.referenced_object_id),
		//	Referencing_col = pc.name, 
		//	Referenced_col = rc.name
		//FROM sys.foreign_key_columns AS pt
		//INNER JOIN sys.columns AS pc
		//ON pt.parent_object_id = pc.[object_id]
		//AND pt.parent_column_id = pc.column_id
		//INNER JOIN sys.columns AS rc
		//ON pt.referenced_column_id = rc.column_id
		//AND pt.referenced_object_id = rc.[object_id]
		//WHERE pt.parent_object_id = OBJECT_ID(@tableName);";
    


//const string INNER_KEYS_SQL = @"SELECT 
//			[Schema] = OBJECT_SCHEMA_NAME(pt.parent_object_id),
//			Referencing_tbl = OBJECT_NAME(pt.parent_object_id),
//			FK = OBJECT_NAME(pt.constraint_object_id),
//			Referencing_col = pc.name, 
//			Referenced_col = rc.name
//		FROM sys.foreign_key_columns AS pt
//		INNER JOIN sys.columns AS pc
//		ON pt.parent_object_id = pc.[object_id]
//		AND pt.parent_column_id = pc.column_id
//		INNER JOIN sys.columns AS rc
//		ON pt.referenced_column_id = rc.column_id
//		AND pt.referenced_object_id = rc.[object_id]
//		WHERE pt.referenced_object_id = OBJECT_ID(@tableName);";
}

