using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using Xunit;

namespace UsenetTests.Nntp.Parsers
{
    public class DateResponseParserTests
    {
        public static readonly IEnumerable<object[]> ParseData =
        [
            [111, "20170614110733", new DateTimeOffset(2017, 6, 14, 11, 7, 33, TimeSpan.Zero)],
            [111, "2017xxxxx10733", DateTimeOffset.MinValue],
            [111, "", DateTimeOffset.MinValue],
            [999, "20170614110733", DateTimeOffset.MinValue]
        ];

        [Theory]
        [MemberData(nameof(ParseData))]
        internal void ResponseShouldBeParsedCorrectly(
            int responseCode,
            string responseMessage,
            DateTimeOffset expectedDateTime)
        {
            var dateResponse = new DateResponseParser().Parse(responseCode, responseMessage);
            Assert.Equal(expectedDateTime, dateResponse.DateTime);
        }
    }
}
