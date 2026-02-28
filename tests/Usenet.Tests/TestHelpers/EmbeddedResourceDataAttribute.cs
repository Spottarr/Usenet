using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Xunit;
using Xunit.Internal;
using Xunit.Sdk;
using Xunit.v3;

namespace Usenet.Tests.TestHelpers;

internal sealed class EmbeddedResourceDataAttribute : DataAttribute
{
    private readonly string[] _fileNames;

    private readonly EmbeddedFileProvider _fileProvider = new(
        typeof(EmbeddedResourceDataAttribute).Assembly,
        "Usenet.Tests.testdata"
    );

    public EmbeddedResourceDataAttribute(params string[] fileNames) => _fileNames = fileNames;

    public object[] AdditionalData { get; init; } = [];
    public string[] FileNames => _fileNames;

    public override ValueTask<IReadOnlyCollection<ITheoryDataRow>> GetData(
        MethodInfo testMethod,
        DisposalTracker disposalTracker
    )
    {
        var result = new List<object>(_fileNames.Length + AdditionalData.Length);

        foreach (var fileName in _fileNames)
            result.Add(_fileProvider.GetFileInfo(fileName));

        foreach (var additionalData in AdditionalData)
            result.Add(additionalData);

        List<ITheoryDataRow> rows = [ConvertDataRow(result.ToArray())];
        return ValueTask.FromResult(rows.CastOrToReadOnlyCollection());
    }

    public override bool SupportsDiscoveryEnumeration() => true;
}
