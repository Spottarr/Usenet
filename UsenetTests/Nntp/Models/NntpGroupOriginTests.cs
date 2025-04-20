using System;
using System.Collections.Generic;
using Usenet.Nntp.Models;
using UsenetTests.TestHelpers;
using Xunit;

namespace UsenetTests.Nntp.Models
{
    public class NntpGroupOriginTests
    {
        public static readonly IEnumerable<object[]> EqualsWithSameValues =
        [
            [
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group1", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero), "me")),
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group1", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero), "me"))
            ]
        ];

        [Theory]
        [MemberData(nameof(EqualsWithSameValues))]
        internal void EqualsWithSameValuesShouldReturnTrue(XSerializable<NntpGroupOrigin> group1, XSerializable<NntpGroupOrigin> group2)
        {
            Assert.Equal(group1.Object, group2.Object);
        }

        public static readonly IEnumerable<object[]> EqualsWithDifferentValues =
        [
            [
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group1", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero), "me")),
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group2", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero), "me"))
            ],
            [
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group1", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero), "me")),
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group1", new DateTimeOffset(2017, 5, 24, 15, 32, 11, TimeSpan.Zero), "me"))
            ],
            [
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group1", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero), "me")),
                new XSerializable<NntpGroupOrigin>(new NntpGroupOrigin("group1", new DateTimeOffset(2017, 5, 23, 15, 32, 11, TimeSpan.Zero), "not me"))
            ]
        ];

        [Theory]
        [MemberData(nameof(EqualsWithDifferentValues))]
        internal void EqualsWithDifferentValuesShouldReturnFalse(XSerializable<NntpGroupOrigin> group1, XSerializable<NntpGroupOrigin> group2)
        {
            Assert.NotEqual(group1.Object, group2.Object);
        }
    }
}
