using System.Reflection;
using Microsoft.Extensions.FileProviders;
using Xunit.Sdk;

namespace UsenetTests.TestHelpers;

internal sealed class EmbeddedResourceDataAttribute : DataAttribute
{
    private readonly string[] _fileNames;

    private readonly EmbeddedFileProvider _fileProvider = new(typeof(EmbeddedResourceDataAttribute).Assembly, "UsenetTests.testdata");

    public EmbeddedResourceDataAttribute(params string[] fileNames) => _fileNames = fileNames;

    public object[] AdditionalData { get; init; } = [];
    public string[] FileNames => _fileNames;

    public override IEnumerable<object[]> GetData(MethodInfo testMethod)
    {
        var result = new List<object>(_fileNames.Length + AdditionalData.Length);

        foreach (var fileName in _fileNames)
            result.Add(_fileProvider.GetFileInfo(fileName));

        foreach (var additionalData in AdditionalData)
            result.Add(additionalData);

        return [result.ToArray()];
    }
}
