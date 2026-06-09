using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Searchable.SqlServer.Contracts;
using Searchable.SqlServer.Enums;

namespace Searchable.SqlServer.UnitTests;

public class SearchableExtensionsTests
{
    [Fact]
    public void SplitSearchTermIntoWords_ReturnsEmptyForBlankInput()
    {
        string[] result = SearchableExtensions.SplitSearchTermIntoWords("   ");

        Assert.Empty(result);
    }

    [Fact]
    public void SplitSearchTermIntoWords_TrimsAndCollapsesWhitespace()
    {
        string[] result = SearchableExtensions.SplitSearchTermIntoWords("  alpha   beta  gamma ");

        Assert.Equal(new[] { "alpha", "beta", "gamma" }, result);
    }

    [Fact]
    public void SplitSearchTermIntoWords_TruncatesLongValues()
    {
        string searchTerm = new string('a', 1005);

        string[] result = SearchableExtensions.SplitSearchTermIntoWords(searchTerm);

        Assert.Single(result);
        Assert.Equal(1000, result[0].Length);
    }

    [Theory]
    [InlineData("", "")]
    [InlineData("   ", "")]
    [InlineData("[50%_] result", "[[]50[%][_]] result")]
    public void CleanSearchTermForLike_CleansExpectedCharacters(string input, string expected)
    {
        string result = SearchableExtensions.CleanSearchTermForLike(input);

        Assert.Equal(expected, result);
    }

    [Fact]
    public void DynamicLikeSearch_RequestOverload_NullRequest_ReturnsOriginalQueryable()
    {
        using SearchableTestContext context = CreateContext();
        IQueryable<SamplePerson> query = context.People;

        IQueryable<SamplePerson> result = query.DynamicLikeSearch(
            (ISearchableRequest)null!,
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! });

        Assert.Same(query, result);
    }

    [Fact]
    public void DynamicLikeSearch_RequestOverload_BlankSearchTerm_ReturnsOriginalQueryable()
    {
        using SearchableTestContext context = CreateContext();
        IQueryable<SamplePerson> query = context.People;

        IQueryable<SamplePerson> result = query.DynamicLikeSearch(
            new SearchableRequest("   "),
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! });

        Assert.Same(query, result);
    }

    [Fact]
    public void DynamicLikeSearch_RequestOverload_WithTerms_ProducesExpectedSql()
    {
        using SearchableTestContext context = CreateContext();

        IQueryable<SamplePerson> query = context.People.DynamicLikeSearch(
            new SearchableRequest("al ex"),
            new Expression<Func<SamplePerson, string>>[]
            {
                person => person.FirstName!,
                person => person.LastName!
            },
            ILikeMatchModeEnum.Contains);

        string sql = query.ToQueryString();

        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AND", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OR", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Equal(4, CountOccurrences(sql, "LIKE"));
    }

    [Theory]
    [InlineData(ILikeMatchModeEnum.Contains, "%al%")]
    [InlineData(ILikeMatchModeEnum.StartsWith, "al%")]
    [InlineData(ILikeMatchModeEnum.EndsWith, "%al")]
    [InlineData(ILikeMatchModeEnum.Exact, "al")]
    public void DynamicLikeSearch_RequestOverload_UsesExpectedMatchModePattern(
        ILikeMatchModeEnum matchMode,
        string expectedPattern)
    {
        using SearchableTestContext context = CreateContext();

        IQueryable<SamplePerson> query = context.People.DynamicLikeSearch(
            new SearchableRequest("al"),
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! },
            matchMode);

        string sql = query.ToQueryString();

        Assert.Contains(expectedPattern, sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void DynamicLikeSearch_RawTermsOverload_EmptyTerms_ReturnsOriginalQueryable()
    {
        using SearchableTestContext context = CreateContext();
        IQueryable<SamplePerson> query = context.People;

        IQueryable<SamplePerson> result = query.DynamicLikeSearch(
            new[] { "   ", "" },
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! });

        Assert.Same(query, result);
    }

    [Fact]
    public void DynamicLikeSearch_RawTermsOverload_WithTerms_ProducesExpectedSql()
    {
        using SearchableTestContext context = CreateContext();

        IQueryable<SamplePerson> query = context.People.DynamicLikeSearch(
            new[] { "al", "ex" },
            new Expression<Func<SamplePerson, string>>[]
            {
                person => person.FirstName!,
                person => person.LastName!
            },
            ILikeMatchModeEnum.StartsWith,
            termLogic: false,
            propertyLogic: false);

        string sql = query.ToQueryString();

        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AND", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildDynamicSearchExpression_RequestOverload_NullRequest_ReturnsTrueExpression()
    {
        var expression = SearchableExtensions.BuildDynamicSearchExpression<SamplePerson>(
            (ISearchableRequest)null!,
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! });

        Assert.True(expression.Compile()(new SamplePerson()));
    }

    [Fact]
    public void BuildDynamicSearchExpression_RequestOverload_BlankSearchTerm_ReturnsTrueExpression()
    {
        var expression = SearchableExtensions.BuildDynamicSearchExpression<SamplePerson>(
            new SearchableRequest("   "),
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! });

        Assert.True(expression.Compile()(new SamplePerson()));
    }

    [Fact]
    public void BuildDynamicSearchExpression_RequestOverload_WithTerms_ProducesExpectedSql()
    {
        using SearchableTestContext context = CreateContext();

        var predicate = SearchableExtensions.BuildDynamicSearchExpression<SamplePerson>(
            new SearchableRequest("al ex"),
            new Expression<Func<SamplePerson, string>>[]
            {
                person => person.FirstName!,
                person => person.LastName!
            },
            ILikeMatchModeEnum.Contains);

        string sql = context.People.Where(predicate).ToQueryString();

        Assert.Contains("LIKE", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("AND", sql, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("OR", sql, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void BuildDynamicSearchExpression_RawTermsOverload_EmptyTerms_ReturnsTrueExpression()
    {
        var expression = SearchableExtensions.BuildDynamicSearchExpression<SamplePerson>(
            new[] { "", "   " },
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! });

        Assert.True(expression.Compile()(new SamplePerson()));
    }

    [Theory]
    [InlineData(ILikeMatchModeEnum.Contains, "%al%")]
    [InlineData(ILikeMatchModeEnum.StartsWith, "al%")]
    [InlineData(ILikeMatchModeEnum.EndsWith, "%al")]
    [InlineData(ILikeMatchModeEnum.Exact, "al")]
    public void BuildDynamicSearchExpression_RawTermsOverload_UsesExpectedMatchModePattern(
        ILikeMatchModeEnum matchMode,
        string expectedPattern)
    {
        using SearchableTestContext context = CreateContext();

        var predicate = SearchableExtensions.BuildDynamicSearchExpression<SamplePerson>(
            new[] { "al" },
            new Expression<Func<SamplePerson, string>>[] { person => person.FirstName! },
            matchMode);

        string sql = context.People.Where(predicate).ToQueryString();

        Assert.Contains(expectedPattern, sql, StringComparison.OrdinalIgnoreCase);
    }

    private static SearchableTestContext CreateContext()
    {
        DbContextOptions<SearchableTestContext> options = new DbContextOptionsBuilder<SearchableTestContext>()
            .UseSqlServer("Server=localhost;Database=SearchableTests;Trusted_Connection=True;TrustServerCertificate=True")
            .Options;

        return new SearchableTestContext(options);
    }

    private static int CountOccurrences(string input, string value)
    {
        int count = 0;
        int startIndex = 0;

        while (true)
        {
            int index = input.IndexOf(value, startIndex, StringComparison.OrdinalIgnoreCase);
            if (index < 0)
            {
                return count;
            }

            count++;
            startIndex = index + value.Length;
        }
    }

    private sealed class SearchableRequest : ISearchableRequest
    {
        public SearchableRequest(string? searchTerm)
        {
            SearchTerm = searchTerm;
        }

        public string? SearchTerm { get; }
    }

    private sealed class SearchableTestContext : DbContext
    {
        public SearchableTestContext(DbContextOptions<SearchableTestContext> options)
            : base(options)
        {
        }

        public DbSet<SamplePerson> People => Set<SamplePerson>();
    }

    private sealed class SamplePerson
    {
        public int Id { get; set; }

        public string? FirstName { get; set; }

        public string? LastName { get; set; }
    }
}
