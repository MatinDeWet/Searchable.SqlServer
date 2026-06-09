using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Searchable.SqlServer.Contracts;
using Searchable.SqlServer.Enums;

namespace Searchable.SqlServer;

/// <summary>
/// Extension methods for building SQL Server-backed search queries.
/// </summary>
public static class SearchableExtensions
{
    /// <summary>
    /// Dynamically searches entities using SQL Server LIKE pattern matching across multiple properties.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="queryable">The queryable to filter.</param>
    /// <param name="request">The searchable request containing the search term.</param>
    /// <param name="searchProperties">Array of expressions to select properties to search on.</param>
    /// <param name="matchMode">The pattern matching mode (contains, starts with, ends with, exact).</param>
    /// <param name="termLogic">Logic to use between different search terms (true = AND, false = OR). Default is AND.</param>
    /// <param name="propertyLogic">Logic to use between different properties for each term (true = OR, false = AND). Default is OR.</param>
    /// <returns>Filtered queryable with entities matching the search criteria.</returns>
    public static IQueryable<T> DynamicLikeSearch<T>(
        this IQueryable<T> queryable,
        ISearchableRequest request,
        Expression<Func<T, string>>[] searchProperties,
        ILikeMatchModeEnum matchMode = ILikeMatchModeEnum.StartsWith,
        bool termLogic = true,
        bool propertyLogic = true)
        where T : class
    {
        if (request == null || searchProperties == null || searchProperties.Length == 0)
        {
            return queryable;
        }

        string? searchTerm = request.SearchTerm;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return queryable;
        }

        string[] terms = SplitSearchTermIntoWords(searchTerm);

        if (terms.Length == 0)
        {
            return queryable;
        }

        return DynamicLikeSearchInternal(queryable, terms, searchProperties, matchMode, termLogic, propertyLogic);
    }

    /// <summary>
    /// Builds a dynamic search expression that works with ISearchableRequest.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="request">The searchable request containing the search term.</param>
    /// <param name="searchProperties">Array of expressions to select properties to search on.</param>
    /// <param name="matchMode">The pattern matching mode (contains, starts with, ends with, exact).</param>
    /// <param name="termLogic">Logic to use between different search terms (true = AND, false = OR). Default is AND.</param>
    /// <param name="propertyLogic">Logic to use between different properties for each term (true = OR, false = AND). Default is OR.</param>
    /// <returns>An expression that can be used with Where clause.</returns>
    public static Expression<Func<T, bool>> BuildDynamicSearchExpression<T>(
        ISearchableRequest request,
        Expression<Func<T, string>>[] searchProperties,
        ILikeMatchModeEnum matchMode = ILikeMatchModeEnum.StartsWith,
        bool termLogic = true,
        bool propertyLogic = true)
        where T : class
    {
        if (request == null || searchProperties == null || searchProperties.Length == 0)
        {
            return _ => true;
        }

        string? searchTerm = request.SearchTerm;

        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return _ => true;
        }

        string[] terms = SplitSearchTermIntoWords(searchTerm);

        if (terms.Length == 0)
        {
            return _ => true;
        }

        return BuildDynamicSearchExpressionInternal<T>(terms, searchProperties, matchMode, termLogic, propertyLogic);
    }

    /// <summary>
    /// Splits and cleans a search term into individual words.
    /// </summary>
    /// <param name="searchTerm">The raw search term.</param>
    /// <returns>Array of cleaned individual words.</returns>
    internal static string[] SplitSearchTermIntoWords(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return [];
        }

        searchTerm = searchTerm.Trim();

        while (searchTerm.Contains("  "))
        {
            searchTerm = searchTerm.Replace("  ", " ");
        }

        const int maxSearchTermLength = 1000;
        if (searchTerm.Length > maxSearchTermLength)
        {
            searchTerm = searchTerm[..maxSearchTermLength].TrimEnd();
        }

        string[] words = searchTerm.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return [.. words
            .Select(CleanSearchTermForLike)
            .Where(w => !string.IsNullOrWhiteSpace(w))];
    }

    /// <summary>
    /// Dynamically searches entities using SQL Server LIKE pattern matching across multiple properties and terms.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="queryable">The queryable to filter.</param>
    /// <param name="searchTerms">Collection of search terms to match.</param>
    /// <param name="searchProperties">Array of expressions to select properties to search on.</param>
    /// <param name="matchMode">The pattern matching mode (contains, starts with, ends with, exact).</param>
    /// <param name="termLogic">Logic to use between different search terms (AND/OR). Default is AND.</param>
    /// <param name="propertyLogic">Logic to use between different properties for each term (AND/OR). Default is OR.</param>
    /// <returns>Filtered queryable with entities matching the search criteria.</returns>
    public static IQueryable<T> DynamicLikeSearch<T>(
        this IQueryable<T> queryable,
        ICollection<string> searchTerms,
        Expression<Func<T, string>>[] searchProperties,
        ILikeMatchModeEnum matchMode = ILikeMatchModeEnum.StartsWith,
        bool termLogic = true,
        bool propertyLogic = true)
        where T : class
    {
        if (searchTerms == null || searchTerms.Count == 0 || searchProperties == null || searchProperties.Length == 0)
        {
            return queryable;
        }

        string[] validTerms = [.. searchTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(CleanSearchTermForLike)
            .Where(t => !string.IsNullOrWhiteSpace(t))];

        if (validTerms.Length == 0)
        {
            return queryable;
        }

        return DynamicLikeSearchInternal(queryable, validTerms, searchProperties, matchMode, termLogic, propertyLogic);
    }

    /// <summary>
    /// Builds a dynamic search expression similar to PredicateBuilder pattern.
    /// </summary>
    /// <typeparam name="T">Entity type.</typeparam>
    /// <param name="searchTerms">Collection of search terms to match.</param>
    /// <param name="searchProperties">Array of expressions to select properties to search on.</param>
    /// <param name="matchMode">The pattern matching mode (contains, starts with, ends with, exact).</param>
    /// <param name="termLogic">Logic to use between different search terms (true = AND, false = OR). Default is AND.</param>
    /// <param name="propertyLogic">Logic to use between different properties for each term (true = OR, false = AND). Default is OR.</param>
    /// <returns>An expression that can be used with Where clause.</returns>
    public static Expression<Func<T, bool>> BuildDynamicSearchExpression<T>(
        ICollection<string> searchTerms,
        Expression<Func<T, string>>[] searchProperties,
        ILikeMatchModeEnum matchMode = ILikeMatchModeEnum.StartsWith,
        bool termLogic = true,
        bool propertyLogic = true)
        where T : class
    {
        if (searchTerms == null || searchTerms.Count == 0 || searchProperties == null || searchProperties.Length == 0)
        {
            return _ => true;
        }

        string[] validTerms = [.. searchTerms
            .Where(t => !string.IsNullOrWhiteSpace(t))
            .Select(CleanSearchTermForLike)
            .Where(t => !string.IsNullOrWhiteSpace(t))];

        if (validTerms.Length == 0)
        {
            return _ => true;
        }

        return BuildDynamicSearchExpressionInternal<T>(validTerms, searchProperties, matchMode, termLogic, propertyLogic);
    }

    /// <summary>
    /// Internal implementation for dynamic LIKE search with cleaned terms.
    /// </summary>
    private static IQueryable<T> DynamicLikeSearchInternal<T>(
        IQueryable<T> queryable,
        string[] searchTerms,
        Expression<Func<T, string>>[] searchProperties,
        ILikeMatchModeEnum matchMode,
        bool termLogic,
        bool propertyLogic)
        where T : class
    {
        if (searchTerms.Length == 0 || searchProperties.Length == 0)
        {
            return queryable;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T), "e");
        ConstantExpression efFunctionsConstant = Expression.Constant(EF.Functions);

        MethodInfo? likeMethod = typeof(DbFunctionsExtensions)
            .GetMethod(nameof(DbFunctionsExtensions.Like), [typeof(DbFunctions), typeof(string), typeof(string)])
            ?? throw new InvalidOperationException("Could not find Like method. Ensure Microsoft.EntityFrameworkCore is referenced.");

        var termExpressions = new List<Expression>();

        foreach (string? term in searchTerms)
        {
            string pattern = matchMode switch
            {
                ILikeMatchModeEnum.StartsWith => $"{term}%",
                ILikeMatchModeEnum.EndsWith => $"%{term}",
                ILikeMatchModeEnum.Exact => term,
                ILikeMatchModeEnum.Contains => $"%{term}%",
                _ => $"%{term}%"
            };

            ConstantExpression patternConstant = Expression.Constant(pattern);

            var propertyConditions = searchProperties.Select(prop =>
            {
                InvocationExpression propertyAccess = Expression.Invoke(prop, parameter);
                return (Expression)Expression.Call(
                    likeMethod,
                    efFunctionsConstant,
                    propertyAccess,
                    patternConstant);
            }).ToList();

            Expression combinedPropertyCondition = propertyLogic
                ? propertyConditions.Aggregate((left, right) => Expression.OrElse(left, right))
                : propertyConditions.Aggregate((left, right) => Expression.AndAlso(left, right));

            termExpressions.Add(combinedPropertyCondition);
        }

        Expression finalCondition = termLogic
            ? termExpressions.Aggregate((left, right) => Expression.AndAlso(left, right))
            : termExpressions.Aggregate((left, right) => Expression.OrElse(left, right));

        var lambda = Expression.Lambda<Func<T, bool>>(finalCondition, parameter);
        return queryable.Where(lambda);
    }

    /// <summary>
    /// Internal implementation for building dynamic search expression with cleaned terms.
    /// </summary>
    private static Expression<Func<T, bool>> BuildDynamicSearchExpressionInternal<T>(
        string[] searchTerms,
        Expression<Func<T, string>>[] searchProperties,
        ILikeMatchModeEnum matchMode,
        bool termLogic,
        bool propertyLogic)
        where T : class
    {
        if (searchTerms.Length == 0 || searchProperties.Length == 0)
        {
            return _ => true;
        }

        ParameterExpression parameter = Expression.Parameter(typeof(T), "e");
        ConstantExpression efFunctionsConstant = Expression.Constant(EF.Functions);

        MethodInfo? likeMethod = typeof(DbFunctionsExtensions)
            .GetMethod(nameof(DbFunctionsExtensions.Like), [typeof(DbFunctions), typeof(string), typeof(string)])
            ?? throw new InvalidOperationException("Could not find Like method. Ensure Microsoft.EntityFrameworkCore is referenced.");

        var termExpressions = new List<Expression>();

        foreach (string? term in searchTerms)
        {
            string pattern = matchMode switch
            {
                ILikeMatchModeEnum.StartsWith => $"{term}%",
                ILikeMatchModeEnum.EndsWith => $"%{term}",
                ILikeMatchModeEnum.Exact => term,
                ILikeMatchModeEnum.Contains => $"%{term}%",
                _ => $"%{term}%"
            };

            ConstantExpression patternConstant = Expression.Constant(pattern);

            var propertyConditions = searchProperties.Select(prop =>
            {
                InvocationExpression propertyAccess = Expression.Invoke(prop, parameter);
                return (Expression)Expression.Call(
                    likeMethod,
                    efFunctionsConstant,
                    propertyAccess,
                    patternConstant);
            }).ToList();

            Expression combinedPropertyCondition = propertyLogic
                ? propertyConditions.Aggregate((left, right) => Expression.OrElse(left, right))
                : propertyConditions.Aggregate((left, right) => Expression.AndAlso(left, right));

            termExpressions.Add(combinedPropertyCondition);
        }

        Expression finalCondition = termLogic
            ? termExpressions.Aggregate((left, right) => Expression.AndAlso(left, right))
            : termExpressions.Aggregate((left, right) => Expression.OrElse(left, right));

        return Expression.Lambda<Func<T, bool>>(finalCondition, parameter);
    }

    /// <summary>
    /// Cleans the search term for safe use with SQL Server LIKE.
    /// </summary>
    /// <param name="searchTerm">The raw search term.</param>
    /// <returns>A cleaned search term safe for LIKE usage.</returns>
    internal static string CleanSearchTermForLike(string searchTerm)
    {
        if (string.IsNullOrWhiteSpace(searchTerm))
        {
            return string.Empty;
        }

        searchTerm = searchTerm.Trim();

        if (string.IsNullOrEmpty(searchTerm))
        {
            return string.Empty;
        }

        searchTerm = searchTerm.Replace("[", "[[]");
        searchTerm = searchTerm.Replace("%", "[%]");
        searchTerm = searchTerm.Replace("_", "[_]");

        const int maxSearchTermLength = 1000;
        if (searchTerm.Length > maxSearchTermLength)
        {
            searchTerm = searchTerm[..maxSearchTermLength].TrimEnd();
        }

        return searchTerm;
    }
}
