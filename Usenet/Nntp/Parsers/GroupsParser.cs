using Usenet.Extensions;
using Usenet.Nntp.Models;

namespace Usenet.Nntp.Parsers
{
    internal static class GroupsParser
    {
        public static IEnumerable<string> Parse(string value)
        {
            var groups = new List<string>();
            AddGroups(groups, value);
            return groups;
        }

        public static IEnumerable<string> Parse(IEnumerable<string> values)
        {
            var groups = new List<string>();
            if (values == null)
            {
                return groups;
            }
            foreach (var value in values)
            {
                AddGroups(groups, value);
            }
            return groups;
        }

        private static void AddGroups(List<string> groups, string value)
        {
            if (value == null)
            {
                return;
            }
            foreach (var group in value.Split(new[] { NntpGroups.GroupSeperator }, StringSplitOptions.RemoveEmptyEntries))
            {
                var packed = group.Pack();
                if (packed.Length > 0 && !groups.Contains(packed))
                {
                    groups.Add(packed);
                }
            }
        }
    }
}
