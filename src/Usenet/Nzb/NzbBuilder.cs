using System.Globalization;
using JetBrains.Annotations;
using Microsoft.Extensions.FileProviders;
using Usenet.Extensions;
using Usenet.Nntp.Builders;
using Usenet.Nntp.Models;

namespace Usenet.Nzb;

/// <summary>
/// Represents a mutable <see cref="NzbDocument"/>.
/// </summary>
[PublicAPI]
public class NzbBuilder
{
    private readonly List<File> _files = [];
    private readonly NntpGroupsBuilder _groupsBuilder = new();
    private readonly Dictionary<string, ICollection<string>> _metaData = new();
    private string _messageBase = "unknown.com";
    private string _documentPoster = "Anonymous <anonymous@unknown.com>";
    private long _partSize = 384_000;

    /// <summary>
    /// Creates a new instance of the <see cref="NzbBuilder"/> class.
    /// </summary>
    public NzbBuilder() { }

    /// <summary>
    /// Sets the NZB document's default poster. Will be used for every file.
    /// </summary>
    /// <param name="value">The poster value. Usually an e-mail address.</param>
    /// <returns>The <see cref="NzbBuilder"/> so that additional calls can be chained.</returns>
    public NzbBuilder SetPoster(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _documentPoster = value;
        return this;
    }

    /// <summary>
    /// Sets the NZB document's part size. Will be used for every segment.
    /// </summary>
    /// <param name="value">The segment size in bytes.</param>
    /// <returns>The <see cref="NzbBuilder"/> so that additional calls can be chained.</returns>
    public NzbBuilder SetPartSize(long value)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value);
        _partSize = value;
        return this;
    }

    /// <summary>
    /// Sets the NZB document's message base. Will be used in every segment's message id.
    /// Message id's format: part{partNr}of{totalPart}.{guid}@{messageBase}.
    /// For example: part1of10.4c7cb35fd4004511b981a69500697aac@random.local.
    /// </summary>
    /// <param name="value">The message base to use.</param>
    /// <returns>The <see cref="NzbBuilder"/> so that additional calls can be chained.</returns>
    public NzbBuilder SetMessageBase(string value)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value);
        _messageBase = value;
        return this;
    }

    /// <summary>
    /// Adds a file to the NZB document. Additional newsgroups may be provided.
    /// Optionally the default poster van be overriden.
    /// </summary>
    /// <param name="fileInfo">The file to add.</param>
    /// <param name="groups">The newsgroups to post the file in.</param>
    /// <param name="poster">Can be used to override the default poster.</param>
    /// <returns>The <see cref="NzbBuilder"/> so that additional calls can be chained.</returns>
    public NzbBuilder AddFile(IFileInfo fileInfo, NntpGroups? groups = null, string? poster = null)
    {
        ArgumentNullException.ThrowIfNull(fileInfo);
        _files.Add(new File(fileInfo, groups ?? NntpGroups.Empty, poster));
        return this;
    }

    /// <summary>
    /// Adds default newsgroups to the NZB document. All files will be posted to these newsgroups.
    /// </summary>
    /// <param name="groups">The newsgroups to post all the files in.</param>
    /// <returns>The <see cref="NzbBuilder"/> so that additional calls can be chained.</returns>
    public NzbBuilder AddGroups(params NntpGroups[] groups)
    {
        ArgumentNullException.ThrowIfNull(groups);
        foreach (var group in groups)
        {
            _groupsBuilder.Add(group);
        }

        return this;
    }

    /// <summary>
    /// Adds metadata to the NZB document.
    /// </summary>
    /// <param name="key">The metadata key.</param>
    /// <param name="value">The metadata value.</param>
    /// <returns>The <see cref="NzbBuilder"/> so that additional calls can be chained.</returns>
    public NzbBuilder AddMetaData(string key, string value)
    {
        _metaData.AddValue(key, value);
        return this;
    }

    /// <summary>
    /// Creates a <see cref="NzbDocument"/> with al the properties from the <see cref="NzbBuilder"/>.
    /// </summary>
    /// <returns>The <see cref="NzbDocument"/>.</returns>
    // NzbDocument materializes its own immutable copy of the metadata, so the builder can hand over
    // its live dictionary without a defensive copy.
    public NzbDocument Build() => new(_metaData, GetFiles());

    private List<NzbFile> GetFiles()
    {
        var date = DateTimeOffset.UtcNow;
        return _files
            .Select(f => new NzbFile(
                f.Poster ?? _documentPoster,
                GetSubject(f.FileInfo),
                f.FileInfo.Name,
                date,
                new NntpGroupsBuilder().Add(f.Groups).Add(_groupsBuilder.Groups).Build(),
                GetSegments(f.FileInfo)
            ))
            .ToList();
    }

    private string GetSubject(IFileInfo fileInfo)
    {
        var segmentCount = GetSegmentCount(fileInfo).ToString(CultureInfo.InvariantCulture);
        var one = "1".PadLeft(segmentCount.Length, '0');
        return $"\"{fileInfo.Name}\" yEnc ({one}/{segmentCount})";
    }

    private List<NzbSegment> GetSegments(IFileInfo fileInfo)
    {
        var fileGuid = Guid.NewGuid();
        var segmentCount = GetSegmentCount(fileInfo);
        var segments = new List<NzbSegment>(segmentCount);
        var offset = 0L;
        for (var number = 1; number <= segmentCount; number++)
        {
            var messageId = $"part{number}of{segmentCount}.{fileGuid:n}@{_messageBase}";
            var size = number < segmentCount ? _partSize : fileInfo.Length - offset;
            segments.Add(new NzbSegment(number, offset, size, messageId));
            offset += size;
        }

        return segments;
    }

    private int GetSegmentCount(IFileInfo fileInfo)
    {
        var count = (int)(fileInfo.Length / _partSize);
        if (count * _partSize < fileInfo.Length)
        {
            count++;
        }

        return count;
    }

    private class File
    {
        public IFileInfo FileInfo { get; }
        public NntpGroups Groups { get; }
        public string? Poster { get; }

        public File(IFileInfo fileInfo, NntpGroups groups, string? poster)
        {
            FileInfo = fileInfo;
            Groups = groups;
            Poster = poster;
        }
    }
}
