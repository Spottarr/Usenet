using System.Text.Json;
using Usenet.Util;

// ReSharper disable DuplicateKeyCollectionInitialization

namespace Usenet.Tests.Util;

internal sealed class MultiValueDictionaryTests
{
    [Test]
    public async Task MultipleValuesWithSameKeyShouldBeAdded()
    {
        var dict = new MultiValueDictionary<int, string>(() => new HashSet<string>())
        {
            { 1, "one" },
            { 1, "een" },
            { 2, "two" },
            { 2, "twee" },
            { 2, "deux" },
        };

        await Assert.That(dict.Count).IsEqualTo(5);
        await Assert.That(dict[1].Count).IsEqualTo(2);
        await Assert.That(dict[2].Count).IsEqualTo(3);

        await Assert.That(new HashSet<string> { "one", "een" }.SetEquals(dict[1])).IsTrue();
        await Assert
            .That(new HashSet<string> { "two", "twee", "deux" }.SetEquals(dict[2]))
            .IsTrue();
    }

    [Test]
    public async Task MultipleValuesWithSameKeyShouldBeAddedIgnoreCase()
    {
        var dict = new MultiValueDictionary<string, string>(
            () => new HashSet<string>(),
            StringComparer.OrdinalIgnoreCase
        )
        {
            { "A", "one" },
            { "a", "een" },
            { "B", "two" },
            { "b", "twee" },
            { "b", "deux" },
        };

        await Assert.That(dict.Count).IsEqualTo(5);
        await Assert.That(dict["a"].Count).IsEqualTo(2);
        await Assert.That(dict["b"].Count).IsEqualTo(3);

        await Assert.That(new HashSet<string> { "one", "een" }.SetEquals(dict["a"])).IsTrue();
        await Assert
            .That(new HashSet<string> { "two", "twee", "deux" }.SetEquals(dict["b"]))
            .IsTrue();
    }

    [Test]
    public async Task SameValueWithSameKeyShouldNotBeAddedWhenUsingHashSet()
    {
        var dict = new MultiValueDictionary<int, string>(() => new HashSet<string>())
        {
            { 1, "one" },
            { 1, "one" },
        };

        await Assert.That(dict.Count).IsEqualTo(1);
        await Assert.That(dict[1].Count).IsEqualTo(1);

        await Assert.That(new HashSet<string> { "one" }.SetEquals(dict[1])).IsTrue();
    }

    [Test]
    public async Task SameValueWithSameKeyShouldBeAddedWhenUsingList()
    {
        var dict = new MultiValueDictionary<int, string>(() => new List<string>())
        {
            { 1, "one" },
            { 1, "one" },
        };

        await Assert.That(dict.Count).IsEqualTo(2);
        await Assert.That(dict[1].Count).IsEqualTo(2);

        await Assert.That(dict[1]).IsEquivalentTo(new List<string> { "one", "one" });
    }

    [Test]
    public async Task RemovingItemsShouldDecreaseCount()
    {
        var dict = new MultiValueDictionary<int, string>(() => new HashSet<string>())
        {
            { 1, "one" },
            { 1, "een" },
            { 2, "two" },
            { 2, "twee" },
            { 2, "deux" },
        };
        dict.Remove(2, "twee");
        dict.Remove(1);

        await Assert.That(dict.Count).IsEqualTo(2);
        await Assert.That(dict[2].Count).IsEqualTo(2);
    }

    [Test]
    public async Task ClearingDictionaryShouldResultInCountZero()
    {
        var dict = new MultiValueDictionary<int, string>(() => new HashSet<string>())
        {
            { 1, "one" },
            { 1, "een" },
            { 2, "two" },
            { 2, "twee" },
            { 2, "deux" },
        };
        dict.Clear();

        await Assert.That(dict.Count).IsEqualTo(0);
    }

    [Test]
    public async Task DictionariesShouldBeEqualIndependentOfOrder()
    {
        var dict1 = new MultiValueDictionary<int, string>(() => new HashSet<string>())
        {
            { 2, "twee" },
            { 2, "two" },
            { 1, "een" },
            { 2, "deux" },
            { 1, "one" },
        };

        var dict2 = new MultiValueDictionary<int, string>(() => new HashSet<string>())
        {
            { 1, "one" },
            { 1, "een" },
            { 2, "two" },
            { 2, "twee" },
            { 2, "deux" },
        };

        await Assert.That(dict2).IsEqualTo(dict1);
        await Assert.That(dict1 == dict2).IsTrue();
        await Assert.That(dict1.Equals(dict2)).IsTrue();
    }

    [Test]
    public async Task DictionaryCanBeSerializedToJsonAndDeserializedBackToDictionary()
    {
        var expected = new MultiValueDictionary<int, string>(() => new HashSet<string>())
        {
            { 2, "twee" },
            { 2, "two" },
            { 1, "een" },
            { 2, "deux" },
            { 1, "one" },
        };

        var json = JsonSerializer.Serialize(expected);
        var actual = JsonSerializer.Deserialize<MultiValueDictionary<int, string>>(json);
        await Assert.That(actual).IsEqualTo(expected);
    }
}
