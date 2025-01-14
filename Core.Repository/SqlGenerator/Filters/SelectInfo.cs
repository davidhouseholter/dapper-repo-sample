using System.Collections.Generic;

namespace Core.Repository.SqlGenerator.Filters;

/// <summary>
///  Select Info
/// </summary>
public class SelectInfo
{
    /// <summary>
    /// The constructor
    /// </summary>
    public SelectInfo()
    {
        Columns = new List<string>();
    }

    /// <summary>
    /// Columns
    /// </summary>
    public List<string> Columns { get; set; }

    /// <summary>
    /// If true, will be used for all queries
    /// </summary>
    public bool Permanent { get; set; }
}
