using Usenet.Nntp.Builders;

namespace Usenet.Tests.Nntp.Builders;

internal sealed class NntpGroupsBuilderTests
{
    private static readonly string[] SingleGroup = ["group1"];
    private static readonly string[] ThreeGroups = ["group1", "group2", "group3"];

    [Test]
    public async Task AddNullShouldThrow()
    {
        var builder = new NntpGroupsBuilder();
        await Assert.That(() => builder.Add((string?)null!)).ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AddNullEnumerableShouldThrow()
    {
        var builder = new NntpGroupsBuilder();
        await Assert
            .That(() => builder.Add((IEnumerable<string>?)null!))
            .ThrowsExactly<ArgumentNullException>();
    }

    [Test]
    public async Task AddSingleGroupShouldResultInSingleGroupString()
    {
        var builder = new NntpGroupsBuilder().Add("group1");
        await Assert.That(builder.Groups).IsEquivalentTo(SingleGroup);
    }

    [Test]
    public async Task AddMultipleGroupsShouldResultInMultipleGroupsString()
    {
        var builder = new NntpGroupsBuilder().Add(["group1", "group2"]).Add("group3");
        await Assert.That(builder.Groups).IsEquivalentTo(ThreeGroups);
    }

    [Test]
    public async Task EqualsWithDifferentOrderShouldReturnTrue()
    {
        var builder1 = new NntpGroupsBuilder().Add("group1").Add("group2");
        var builder2 = new NntpGroupsBuilder().Add("group2").Add("group1");
        await Assert.That(builder2.Build()).IsEqualTo(builder1.Build());
    }

    [Test]
    public async Task EqualsOperatorWithDifferentOrderShouldReturnTrue()
    {
        var builder1 = new NntpGroupsBuilder().Add("group1").Add("group2");
        var builder2 = new NntpGroupsBuilder().Add("group2").Add("group1");
        await Assert.That(builder1.Build() == builder2.Build()).IsTrue();
    }
}
