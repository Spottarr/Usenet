using Usenet.Nntp.Models;

namespace Usenet.Tests.Nntp.Models;

internal sealed class NntpGroupsTests
{
    [Test]
    internal async Task ConstructWithEmptyStringShouldReturnEmptyString()
    {
        var groups = NntpGroups.Empty;
        await Assert.That(groups.ToString()).IsEqualTo("");
    }

    [Test]
    internal async Task ConstructWithNullShouldThrow()
    {
        await Assert
            .That(() => new NntpGroups((string?)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    internal async Task ConstructWithNullEnumerableShouldThrow()
    {
        await Assert
            .That(() => new NntpGroups((IEnumerable<string>?)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    internal async Task ConstructWithMultipleGroupsShouldReturnMultipleGroupsString()
    {
        var groups = new NntpGroups(["group1", "group2"]);
        await Assert.That(groups.ToString()).IsEqualTo("group1;group2");
    }

    [Test]
    internal async Task ConstructWithSameGroupsShouldReturnSingleGroupString()
    {
        var groups = new NntpGroups(["group1", "group1"]);
        await Assert.That(groups.ToString()).IsEqualTo("group1");
    }

    public static IEnumerable<(NntpGroups, NntpGroups)> EqualsWithSameValues()
    {
        yield return (new NntpGroups("group1;group2"), new NntpGroups("group1;group2"));
        yield return (new NntpGroups("group3;group4"), new NntpGroups("group4;group3"));
        yield return (new NntpGroups("group5;group6"), new NntpGroups("group6;group5;group5"));
    }

    [Test]
    [MethodDataSource(nameof(EqualsWithSameValues))]
    internal async Task EqualsWithSameValuesShouldReturnTrue(NntpGroups groups1, NntpGroups groups2)
    {
        await Assert.That(groups2).IsEqualTo(groups1);
    }
}
