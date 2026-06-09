# Searchable.SqlServer

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
- Keeps the API small so it can stay provider-specific and easy to mirror later for PostgreSQL.

## Usage

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

## API Surface

- `Searchable.SqlServer.SearchableExtensions`
- `Searchable.SqlServer.Contracts.ISearchableRequest`
- `Searchable.SqlServer.Enums.ILikeMatchModeEnum`

## Package Notes

- Package ID: `MatinDeWet.Searchable.SqlServer`
- Target framework: `net10.0`
- License: `GPL-3.0-only`
- Symbols: published as `.snupkg`

## Repository

This repository includes GitHub Actions workflows for CI and NuGet publishing.

The package and repository use the GPL-3.0-only license, and GitHub Sponsors support is configured through `.github/FUNDING.yml`.
