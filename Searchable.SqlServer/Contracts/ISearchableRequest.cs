namespace Searchable.SqlServer.Contracts;

/// <summary>
/// Represents a request that can supply a search term.
/// </summary>
public interface ISearchableRequest
{
    /// <summary>
    /// Gets the raw search term supplied by the caller.
    /// </summary>
    string? SearchTerm { get; }
}
