using Newtonsoft.Json;
using Xunit.Sdk;

namespace UsenetTests.TestHelpers;

internal sealed class XSerializable<T> : IXunitSerializable
{
    public T Object { get; private set; }

    public XSerializable()
    {
        Object = default!;
    }

    public XSerializable(T objectToSerialize)
    {
        Object = objectToSerialize;
    }

    public void Deserialize(IXunitSerializationInfo info)
    {
        Object = JsonConvert.DeserializeObject<T>(info.GetValue<string>("objValue")!)!;
    }

    public void Serialize(IXunitSerializationInfo info)
    {
        var json = JsonConvert.SerializeObject(Object);
        info.AddValue("objValue", json);
    }
}
