using Usenet.Nntp.Models;
using Usenet.Nntp.Parsers;
using Usenet.Util;

namespace Usenet.Nntp.Builders;

/// <summary>
/// Represents a mutable <see cref="NntpGroups"/>.
/// </summary>
public class NntpGroupsBuilder
{
    private readonly List<string> _groups = [];

    /// <summary>
    /// The raw groups collection.
    /// </summary>
    public IList<string> Groups => _groups;

    /// <summary>
    /// Creates a new instance of the <see cref="NntpGroupsBuilder"/> class.
    /// </summary>
    public NntpGroupsBuilder()
    {
    }

    /// <summary>
    /// Gets a value that indicates whether this list is empty.
    /// </summary>
    public bool IsEmpty => _groups.Count == 0;

    /// <summary>
    /// Adds a new value to the <see cref="NntpGroups"/> object.
    /// </summary>
    /// <param name="value">One or more NNTP newsgroup names seperated by the ';' character.</param>
    /// <returns>The <see cref="NntpGroups"/> object so that additional calls can be chained.</returns>
    public NntpGroupsBuilder Add(string value)
    {
        AddGroups(GroupsParser.Parse(value));
        return this;
    }

    /// <summary>
    /// Adds new values to the <see cref="NntpGroups"/> object.
    /// </summary>
    /// <param name="values">One or more NNTP newsgroup names seperated by the ';' character.</param>
    /// <returns>The <see cref="NntpGroups"/> object so that additional calls can be chained.</returns>
    public NntpGroupsBuilder Add(IEnumerable<string> values)
    {
        Guard.ThrowIfNull(values);
        AddGroups(GroupsParser.Parse(values));
        return this;
    }

    /// <summary>
    /// Removes a new value from the <see cref="NntpGroups"/> object.
    /// </summary>
    /// <param name="value">One or more NNTP newsgroup names seperated by the ';' character.</param>
    /// <returns>The <see cref="NntpGroups"/> object so that additional calls can be chained.</returns>
    public NntpGroupsBuilder Remove(string value)
    {
        Guard.ThrowIfNull(value);
        RemoveGroups(GroupsParser.Parse(value));
        return this;
    }

    /// <summary>
    /// Removes values from the <see cref="NntpGroups"/> object.
    /// </summary>
    /// <param name="values">One or more NNTP newsgroup names seperated by the ';' character.</param>
    /// <returns>The <see cref="NntpGroups"/> object so that additional calls can be chained.</returns>
    public NntpGroupsBuilder Remove(IEnumerable<string> values)
    {
        Guard.ThrowIfNull(values);
        RemoveGroups(GroupsParser.Parse(values));
        return this;
    }

    /// <summary>
    /// Creates a <see cref="NntpGroups"/> with al the properties from the <see cref="NntpGroupsBuilder"/>.
    /// </summary>
    /// <returns>The <see cref="NntpGroups"/>.</returns>
    public NntpGroups Build() => new(_groups, false);

    private void AddGroups(IEnumerable<string> values)
    {
        if (values == null)
        {
            return;
        }

        foreach (var group in values)
        {
            if (!_groups.Contains(group))
            {
                _groups.Add(group);
            }
        }
    }

    private void RemoveGroups(IEnumerable<string> values)
    {
        if (values == null)
        {
            return;
        }

        foreach (var group in values)
        {
            _groups.RemoveAll(g => g == group);
        }
    }
}
