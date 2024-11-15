using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Core.Repository.Poco;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

public class Table
{
    public List<Column> Columns;
    public List<Key> InnerKeys = new List<Key>();
    public List<Key> OuterKeys = new List<Key>();
    public string Name = string.Empty;
    public string Schema = string.Empty;
    public bool IsView;
    public string CleanName = string.Empty;
    public string ClassName = string.Empty;
    public string SequenceName = string.Empty;
    public bool Ignore;
    public bool IsPrimaryKeyColumn(string columnName)
    {
        //return Columns.Single(x=>string.Compare(x.Name, columnName, true)==0).IsPK;
        var found = Columns.FirstOrDefault(x => string.Compare(x.Name, columnName, true) == 0);
        return found == null ? false : found.IsPrimaryKey;
    }

    public Column GetColumn(string columnName)
    {
        return Columns.Single(x => string.Compare(x.Name, columnName, true) == 0);
    }

    public Column this[string columnName]
    {
        get
        {
            return GetColumn(columnName);
        }
    }

}

public class Column
{
    public string Name = string.Empty;
    public string DataType = string.Empty;
    public string ColumnName = string.Empty;
    public string ColumnType = string.Empty;
    public string PropertyName = string.Empty;

    public string ColumnDefault = string.Empty;
    public string Extra = string.Empty;
    public bool IsPrimaryKey;
    public bool IsNullable;
    public bool IsAutoIncrement;
    public bool Ignore;
    public long? MaxLength { get; set; }

}

public class Key
{
    public string FKName = string.Empty;
    public string ReferencedTableName = string.Empty;
    public string BYTableName = string.Empty;
    public string FKTable = string.Empty;
    public string BYColumnName = string.Empty;

    public string BYTableType { get; set; } = string.Empty;
    public string CleanBYTableName { get; set; } = string.Empty;
    public string FKColumnName { get; set; } = string.Empty;
    public string FKTableType { get; set; }
}

public class Tables : List<Table>
{
    public Tables()
    {
    }

    public Table GetTable(string tableName)
    {
        return this.Single(x => string.Compare(x.Name, tableName, true) == 0);
    }

    public Table this[string tableName]
    {
        get
        {
            return GetTable(tableName);
        }
    }

}