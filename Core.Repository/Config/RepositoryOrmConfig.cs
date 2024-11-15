using Core.Repository.SqlGenerator;

namespace Core.Repository.Config;

/// <summary>
/// This class is used to support dependency injection
/// </summary>
public static class RepositoryOrmConfig
{
    public static CoreRepositoryConfig Config { get; set; }
    /// <summary>
    ///     Type Sql provider
    /// </summary>
    public static SqlProvider SqlProvider
    {
        get
        {
            return Config.Context.SqlProvider;
        }
    }

    /// <summary>
    ///     Use quotation marks for TableName and ColumnName
    /// </summary>
    public static bool UseQuotationMarks {
        get
        {
            return Config.Generated.UseQuotationMarks;
        }
    }

    /// <summary>
    ///     Prefix for tables
    /// </summary>
    public static string TablePrefix
    {
        get
        {
            return Config.Generated.TablePrefix;
        }
    }

    /// <summary>
    ///     Allow Key attribute as Identity if Identity is not set
    /// </summary>
    public static bool AllowKeyAsIdentity { get; set; }
}
