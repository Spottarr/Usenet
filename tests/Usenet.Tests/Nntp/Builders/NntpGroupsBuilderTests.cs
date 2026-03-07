using Usenet.Nntp.Builders;
using Xunit;

namespace Usenet.Tests.Nntp.Builders;

public class NntpGroupsBuilderTests
{
    [Fact]
    public void AddNullShouldThrow()
    {
        var builder = new NntpGroupsBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.Add((string?)null!));
    }

    [Fact]
    public void AddNullEnumerableShouldThrow()
    {
        var builder = new NntpGroupsBuilder();
        Assert.Throws<ArgumentNullException>(() => builder.Add((IEnumerable<string>?)null!));
    }

    [Fact]
    public void AddSingleGroupShouldResultInSingleGroupString()
    {
        var builder = new NntpGroupsBuilder().Add("group1");
        Assert.Equal(["group1"], builder.Groups);
    }

    [Fact]
    public void AddMultipleGroupsShouldResultInMultipleGroupsString()
    {
        var builder = new NntpGroupsBuilder().Add(["group1", "group2"]).Add("group3");
        Assert.Equal(["group1", "group2", "group3"], builder.Groups);
    }

    [Fact]
    public void EqualsWithDifferentOrderShouldReturnTrue()
    {
        var builder1 = new NntpGroupsBuilder().Add("group1").Add("group2");
        var builder2 = new NntpGroupsBuilder().Add("group2").Add("group1");
        Assert.Equal(builder1.Build(), builder2.Build());
    }

    [Fact]
    public void EqualsOperatorWithDifferentOrderShouldReturnTrue()
    {
        var builder1 = new NntpGroupsBuilder().Add("group1").Add("group2");
        var builder2 = new NntpGroupsBuilder().Add("group2").Add("group1");
        Assert.True(builder1.Build() == builder2.Build());
    }
}
