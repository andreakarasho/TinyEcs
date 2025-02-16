
using System.Collections.Frozen;

namespace TinyEcs.Tests;

public class FilterMatchTests
{
    private readonly FrozenSet<ulong> _archetypeIds;

    public FilterMatchTests()
    {
        // Initialize the FrozenSet with some ulong values
        _archetypeIds = new HashSet<ulong>([1, 2]).ToFrozenSet();
    }

    [Fact]
    public void Match_WithTerm_Found()
    {
        var terms = new IQueryTerm[]
        {
            new WithTerm(1) // Using ulong directly
        };

        var result = FilterMatch.Match(_archetypeIds, terms);
        Assert.Equal(ArchetypeSearchResult.Found, result);
    }

    [Fact]
    public void Match_WithTerm_Continue()
    {
        var terms = new IQueryTerm[]
        {
            new WithTerm(3) // Not in the set
        };

        var result = FilterMatch.Match(_archetypeIds, terms);
        Assert.Equal(ArchetypeSearchResult.Continue, result);
    }

    [Fact]
    public void Match_WithoutTerm_Stop()
    {
        var terms = new IQueryTerm[]
        {
            new WithoutTerm(1) // Present in the set
        };

        var result = FilterMatch.Match(_archetypeIds, terms);
        Assert.Equal(ArchetypeSearchResult.Stop, result);
    }

    [Fact]
    public void Match_WithoutTerm_Continue()
    {
        var terms = new IQueryTerm[]
        {
            new WithoutTerm(3) // Not present in the set
        };

        var result = FilterMatch.Match(_archetypeIds, terms);
        Assert.Equal(ArchetypeSearchResult.Found, result);
    }

    [Fact]
    public void Match_OptionalTerm_Found()
    {
        var terms = new IQueryTerm[]
        {
            new OptionalTerm(1) // Always returns Found
        };

        var result = FilterMatch.Match(_archetypeIds, terms);
        Assert.Equal(ArchetypeSearchResult.Found, result);
    }

    [Fact]
    public void Match_MultipleTerms_Combination()
    {
        var terms = new IQueryTerm[]
        {
            new WithoutTerm(1), // Present in the set, should stop
            new OptionalTerm(2), // Should not be reached
            new WithTerm(3), // Not in the set, should continue
        };

        var result = FilterMatch.Match(_archetypeIds, terms);
        Assert.Equal(ArchetypeSearchResult.Stop, result);
    }

    [Fact]
    public void Match_MultipleTerms_AllContinue()
    {
        var terms = new IQueryTerm[]
        {
            new WithTerm(3), // Not in the set, should continue
            new WithoutTerm(4), // Not in the set, should continue
            new OptionalTerm(5) // Should not be reached
        };

        var result = FilterMatch.Match(_archetypeIds, terms);
        Assert.Equal(ArchetypeSearchResult.Continue, result);
    }
}
