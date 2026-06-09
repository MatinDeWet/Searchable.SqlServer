namespace Searchable.SqlServer.Enums;

/// <summary>
/// Describes how the search term should be matched in SQL Server LIKE queries.
/// </summary>
public enum ILikeMatchModeEnum
{
    /// <summary>
    /// Matches any term that contains the search value.
    /// </summary>
    Contains,

    /// <summary>
    /// Matches any term that starts with the search value.
    /// </summary>
    StartsWith,

    /// <summary>
    /// Matches any term that ends with the search value.
    /// </summary>
    EndsWith,

    /// <summary>
    /// Matches any term that is exactly equal to the search value.
    /// </summary>
    Exact
}
