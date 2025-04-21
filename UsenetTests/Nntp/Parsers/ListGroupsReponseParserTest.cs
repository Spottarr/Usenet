using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Nntp.Parsers
{
    public class ListGroupsReponseParserTest
    {
        public static readonly IEnumerable<object[]> MultiLineParseData =
        [
            [
                211, "1234 3000234 3002322 misc.test list follows", Array.Empty<string>(),
                new XSerializable<NntpGroup>(new NntpGroup("misc.test", 1234, 3000234, 3002322, NntpPostingStatus.Unknown, string.Empty, new List<long>(0)))
            ],
            [
                211, "1234 3000234 3000236 misc.test list follows", new [] {"3000234", "3000235", "3000236"},
                new XSerializable<NntpGroup>(new NntpGroup("misc.test", 1234, 3000234, 3000236, NntpPostingStatus.Unknown, string.Empty,
                    [3000234L, 3000235L, 3000236L]))
            ],
            [
                411, "example.is.sob.bradner.or.barber is unknown", Array.Empty<string>(),
                new XSerializable<NntpGroup>(new NntpGroup("", 0, 0, 0, NntpPostingStatus.Unknown, string.Empty, new List<long>(0)))
            ],
            [
                412, "no newsgroup selected", Array.Empty<string>(),
                new XSerializable<NntpGroup>(new NntpGroup("", 0, 0, 0, NntpPostingStatus.Unknown, string.Empty, new List<long>(0)))
            ]
        ];

        [Theory]
        [MemberData(nameof(MultiLineParseData))]
        internal void MultiLineResponseShouldBeParsedCorrectly(
            int responseCode,
            string responseMessage,
            string[] lines,
            XSerializable<NntpGroup> expectedGroup)
        {
            NntpGroupResponse groupResponse = new ListGroupResponseParser().Parse(responseCode, responseMessage, lines.ToList());
            Assert.Equal(expectedGroup.Object, groupResponse.Group);
        }
    }
}
