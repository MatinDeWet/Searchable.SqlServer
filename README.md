# Searchable.SqlServer

[![NuGet Version](https://img.shields.io/nuget/v/MatinDeWet.Searchable.SqlServer)](https://www.nuget.org/packages/MatinDeWet.Searchable.SqlServer)
[![CI Status](https://img.shields.io/github/actions/workflow/status/MatinDeWet/Searchable.SqlServer/CI.yml?branch=master)](https://github.com/MatinDeWet/Searchable.SqlServer/actions/workflows/CI.yml)
[![Publish Status](https://img.shields.io/github/actions/workflow/status/MatinDeWet/Searchable.SqlServer/nuget-publish.yml?branch=master)](https://github.com/MatinDeWet/Searchable.SqlServer/actions/workflows/nuget-publish.yml)

SQL Server-specific dynamic search helpers for Entity Framework Core.

This package provides a small, focused API for building `LIKE`-based search filters over `IQueryable<T>` using SQL Server semantics. It is intended to be consumed as a NuGet package and versioned like one.

## Package

```bash
dotnet add package MatinDeWet.Searchable.SqlServer
```

## What It Does

- Builds dynamic search expressions from a request object or raw search terms.
- Supports `Contains`, `StartsWith`, `EndsWith`, and `Exact` matching modes.
- Escapes SQL Server wildcard characters before composing the query.
- Lets you combine multiple properties and multiple search terms with `AND` / `OR` logic.
- Keeps the API small so it can stay provider-specific and easy to mirror later for PostgreSQL.

## Usages

### 1. Request-based search over a queryable

Use this when your request object already carries a search term.

```csharp
using Searchable.SqlServer;
using Searchable.SqlServer.Contracts;
using Searchable.SqlServer.Enums;

IQueryable<Person> query = dbContext.People;
ISearchableRequest request = new SearchableRequest("al ex");

query = query.DynamicLikeSearch(
    request,
    [person => person.FirstName, person => person.LastName],
    ILikeMatchModeEnum.Contains);
```

`DynamicLikeSearch` removes empty input handling from the calling code and returns the original query when the request or search term is blank.

### 2. Search a queryable with raw terms

Use this when you already have a collection of terms and do not want to wrap them in a request object.

```csharp
ICollection<string> terms = ["al", "ex"];

query = query.DynamicLikeSearch(
    terms,
    [person => person.FirstName, person => person.LastName],
    ILikeMatchModeEnum.StartsWith,
    termLogic: true,
    propertyLogic: true);
```

In this example, both terms must match because `termLogic: true` uses `AND`, while either property may match because `propertyLogic: true` uses `OR`.

### 3. Build a reusable expression from a request

Use this when you want to build an `Expression<Func<T, bool>>` and pass it into a LINQ `Where` later.

```csharp
Expression<Func<Person, bool>> predicate = SearchableExtensions.BuildDynamicSearchExpression<Person>(
    request,
    [person => person.FirstName, person => person.LastName],
    ILikeMatchModeEnum.Contains);

query = query.Where(predicate);
```

### 4. Build a reusable expression from raw terms

Use this when the search terms are already in memory and you want to compose a predicate first.

```csharp
Expression<Func<Person, bool>> predicate = SearchableExtensions.BuildDynamicSearchExpression<Person>(
    ["al", "ex"],
    [person => person.FirstName, person => person.LastName],
    ILikeMatchModeEnum.Contains,
    termLogic: false,
    propertyLogic: true);

query = query.Where(predicate);
```

### 5. Match modes

`ILikeMatchModeEnum` controls the generated SQL Server `LIKE` pattern.

- `Contains` matches anywhere in the string.
- `StartsWith` matches values that begin with the term.
- `EndsWith` matches values that end with the term.
- `Exact` matches the value exactly.

### 6. Search term normalization

Search terms are trimmed, split on whitespace, and escaped before being used in the query. That means input like `"  al   ex  "` becomes two search terms, and special SQL Server wildcard characters are safely escaped.

## API Surface

- `Searchable.SqlServer.SearchableExtensions`
- `Searchable.SqlServer.Contracts.ISearchableRequest`
- `Searchable.SqlServer.Enums.ILikeMatchModeEnum`

The package uses the GPL-3.0-only license, and GitHub Sponsors support is configured through `.github/FUNDING.yml`.
