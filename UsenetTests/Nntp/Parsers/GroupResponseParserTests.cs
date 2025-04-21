﻿using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Nntp.Responses;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Nntp.Parsers
{
    public class GroupResponseParserTests
    {
        public static readonly IEnumerable<object[]> ParseData =
        [
            [
                211, "1234 3000234 3002322 misc.test",
                new XSerializable<NntpGroup>(new NntpGroup("misc.test", 1234, 3000234, 3002322, NntpPostingStatus.Unknown, string.Empty, new List<long>(0)))
            ],
            [
                411, "example.is.sob.bradner.or.barber is unknown",
                new XSerializable<NntpGroup>(new NntpGroup("", 0, 0, 0, NntpPostingStatus.Unknown, string.Empty, new List<long>(0)))
            ]
        ];

        [Theory]
        [MemberData(nameof(ParseData))]
        internal void ResponseShouldBeParsedCorrectly(
            int responseCode, 
            string responseMessage,
            XSerializable<NntpGroup> expectedGroup)
        {
            NntpGroupResponse groupResponse = new GroupResponseParser().Parse(responseCode, responseMessage);            
            Assert.Equal(expectedGroup.Object, groupResponse.Group);
        }
    }
}
