using Usenet.Nntp.Models;
using Usenet.Tests.TestHelpers;
using Xunit;

namespace Usenet.Tests.Nntp.Models;

public class NntpGroupsTests
{
    [Fact]
    internal void ConstrctWithEmptyStringShouldReturnEmptyString()
    {
        var groups = NntpGroups.Empty;
        Assert.Equal("", groups.ToString());
    }

    [Fact]
    internal void ConstructWithNullShouldReturnEmptyString()
    {
        var groups = new NntpGroups((string?)null!);
        Assert.Equal("", groups.ToString());
    }

    [Fact]
    internal void ConstructWithNullEnumerableShouldReturnEmptyString()
    {
        var groups = new NntpGroups((IEnumerable<string>?)null!);
        Assert.Equal("", groups.ToString());
    }

    [Fact]
    internal void ConstructWithMultipleGroupsShouldReturnMultipleGroupsString()
    {
        var groups = new NntpGroups(["group1", "group2"]);
        Assert.Equal("group1;group2", groups.ToString());
    }

    [Fact]
    internal void ConstructWithSameGroupsShouldReturnSingleGroupString()
    {
        var groups = new NntpGroups(["group1", "group1"]);
        Assert.Equal("group1", groups.ToString());
    }

    public static readonly IEnumerable<object[]> EqualsWithSameValues =
    [
        [
            new XSerializable<NntpGroups>(new NntpGroups("group1;group2")),
            new XSerializable<NntpGroups>(new NntpGroups("group1;group2"))
        ],
        [
            new XSerializable<NntpGroups>(new NntpGroups("group3;group4")),
            new XSerializable<NntpGroups>(new NntpGroups("group4;group3"))
        ],
        [
            new XSerializable<NntpGroups>(new NntpGroups("group5;group6")),
            new XSerializable<NntpGroups>(new NntpGroups("group6;group5;group5"))
        ]
    ];

    [Theory]
    [MemberData(nameof(EqualsWithSameValues))]
    internal void EqualsWithSameValuesShouldReturnTrue(XSerializable<NntpGroups> groups1, XSerializable<NntpGroups> groups2)
    {
        Assert.Equal(groups1.Object, groups2.Object);
    }
}