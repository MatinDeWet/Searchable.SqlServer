using Searchable.SqlServer.Contracts;

namespace Searchable.SqlServer.UnitTests;

public class SearchableExtensionsTests
{
    [Fact]
    public void SplitSearchTermIntoWords_TrimsAndCollapsesWhitespace()
    {
        string[] result = SearchableExtensions.SplitSearchTermIntoWords("  alpha   beta  gamma ");

        Assert.Equal(["alpha", "beta", "gamma"], result);
    }

    [Fact]
    public void CleanSearchTermForLike_EscapesLikeWildcards()
    {
        string result = SearchableExtensions.CleanSearchTermForLike("[50%_] result");

        Assert.Equal("[[]50[%][_]] result", result);
    }

    [Fact]
    public void BuildDynamicSearchExpression_ReturnsAlwaysTrueForBlankSearchTerm()
    {
        var expression = SearchableExtensions.BuildDynamicSearchExpression<SampleEntity>(
            new SearchableRequest("   "),
            [entity => entity.Name]);

        Assert.True(expression.Compile()(new SampleEntity { Name = "anything" }));
    }

    private sealed class SearchableRequest : ISearchableRequest
    {
        public SearchableRequest(string? searchTerm)
        {
            SearchTerm = searchTerm;
        }

        public string? SearchTerm { get; }
    }

    private sealed class SampleEntity
    {
        public string? Name { get; set; }
    }
}
