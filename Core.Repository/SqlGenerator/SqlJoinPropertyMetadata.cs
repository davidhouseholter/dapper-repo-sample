﻿using System.Reflection;
using Core.Repository.Attributes.Joins;

namespace Core.Repository.SqlGenerator;

/// <summary>
/// </summary>
public class SqlJoinPropertyMetadata : SqlPropertyMetadata
{

    /// <summary>
    ///     Metadata for join property info
    /// </summary>
    /// <param name="joinPropertyInfo">Table property info</param>
    /// <param name="propertyInfo">Table column property info</param>
    public SqlJoinPropertyMetadata(PropertyInfo joinPropertyInfo, PropertyInfo propertyInfo)
        : base(propertyInfo)
    {
        var joinAtttribute = joinPropertyInfo.GetCustomAttribute<JoinAttributeBase>();
        JoinPropertyInfo = joinPropertyInfo;

        if (joinAtttribute == null) return;

        TableSchema = joinAtttribute.TableSchema;
        TableName = joinAtttribute.TableName;
        TableAlias = joinAtttribute.TableAlias;
    }

    /// <summary>
    ///     Table name
    /// </summary>
    public string? TableName { get; set; }

    /// <summary>
    ///     Table alias
    /// </summary>
    public string? TableAlias { get; set; }

    /// <summary>
    ///     Schema name
    /// </summary>
    public string? TableSchema { get; set; }

    /// <summary>
    ///     Original join property info
    /// </summary>
    public PropertyInfo JoinPropertyInfo { get; set; }

    /// <summary>
    ///     Join property name
    /// </summary>
    public string JoinPropertyName => JoinPropertyInfo.Name;

    /// <summary>
    ///     Full property name
    /// </summary>
    public override string PropertyName => JoinPropertyName + base.PropertyName;
}