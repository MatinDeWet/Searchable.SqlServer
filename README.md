# Searchable.SqlServer

SQL Server-specific dynamic search helpers for Entity Framework Core.

## Package

```bash
dotnet add package MatinDeWet.Searchable.SqlServer
```

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

## Repository

This repository is packaged as a NuGet library and includes GitHub Actions workflows for CI and release publishing.
