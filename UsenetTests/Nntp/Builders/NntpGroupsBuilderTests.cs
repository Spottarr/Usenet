using Usenet.Nntp.Builders;
using Xunit;

namespace UsenetTests.Nntp.Builders
{
    public class NntpGroupsBuilderTests
    {
        [Fact]
        public void AddNullShouldResultInEmptyCollection()
        {
            var builder = new NntpGroupsBuilder().Add((string?)null);
            Assert.Equal(Array.Empty<string>(), builder.Groups);
        }

        [Fact]
        public void AddNullEnumerableShouldResultInEmptyCollection()
        {
            var builder = new NntpGroupsBuilder().Add((IEnumerable<string>?)null);
            Assert.Equal(Array.Empty<string>(), builder.Groups);
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
            Assert.Equal(["group1","group2","group3"], builder.Groups);
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
}
