using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;

namespace Usenet.Extensions;

/// <summary>
/// Dictionary extension methods.
/// Based on Kristian Hellang's yEnc project https://github.com/khellang/yEnc.
/// </summary>
internal static class DictionaryExtensions
{
    /// <summary>
    /// Gets a value from the string dictionary and converts it using the specified converter.
    /// </summary>
    /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to find.</param>
    /// <param name="converter">The converter function to use.</param>
    /// <returns>The value if the key was found. Otherwise the default value of type <typeparamref name="TValue"/>.</returns>
    public static TValue? GetAndConvert<TValue>(
        this IDictionary<string, string> dictionary,
        string key,
        Func<string, TValue> converter
    )
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(converter);

        return dictionary.TryGetValue(key, out var stringValue) ? converter(stringValue) : default;
    }

    /// <summary>
    /// Gets a value from the dictionary or a default value if the key was not found.
    /// </summary>
    /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
    /// <param name="dictionary">The dictionary to search.</param>
    /// <param name="key">The key to find.</param>
    /// <returns>The value if the key was found. Otherwise the default value of type <typeparamref name="TValue"/>.</returns>
    public static TValue? GetOrDefault<TValue>(
        this IDictionary<string, TValue> dictionary,
        string key
    )
    {
        ArgumentNullException.ThrowIfNull(dictionary);
        ArgumentException.ThrowIfNullOrEmpty(key);

        return dictionary.TryGetValue(key, out var value) ? value : default;
    }

    /// <summary>
    /// Merges the source dictionary into the target dictionary.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary key.</typeparam>
    /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
    /// <param name="target">The target dictionary.</param>
    /// <param name="source">The source dictionary.</param>
    /// <param name="overwriteExistingKeys">A value indicating whether existing keys should be overwritten or not.</param>
    /// <returns>The target dictionary.</returns>
    public static IDictionary<TKey, TValue> Merge<TKey, TValue>(
        this IDictionary<TKey, TValue> target,
        IDictionary<TKey, TValue> source,
        bool overwriteExistingKeys
    )
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(source);

        foreach (var item in source)
        {
            if (!overwriteExistingKeys && target.ContainsKey(item.Key))
            {
                continue;
            }
            target[item.Key] = item.Value;
        }
        return target;
    }

    /// <summary>
    /// Adds a value to the set of values associated with the given key, creating the backing
    /// <see cref="HashSet{TValue}"/> on first use. Duplicate values for a key are collapsed by the set.
    /// </summary>
    /// <typeparam name="TKey">Type of the dictionary key.</typeparam>
    /// <typeparam name="TValue">Type of the dictionary value.</typeparam>
    /// <param name="dictionary">The multi-value dictionary to add to.</param>
    /// <param name="key">The key to add the value under.</param>
    /// <param name="value">The value to add.</param>
    public static void AddValue<TKey, TValue>(
        this IDictionary<TKey, ICollection<TValue>> dictionary,
        TKey key,
        TValue value
    )
    {
        ArgumentNullException.ThrowIfNull(dictionary);

        if (!dictionary.TryGetValue(key, out var values))
        {
            values = new HashSet<TValue>();
            dictionary.Add(key, values);
        }

        values.Add(value);
    }

    /// <summary>
    /// Produces an immutable multi-value dictionary with immutable hashset collections containing the values.
    /// </summary>
    /// <returns>An immutable multi-value dictionary with immutable hashset collections containing the values.</returns>
    public static ImmutableDictionary<
        TKey,
        ImmutableHashSet<TValue>
    > ToImmutableDictionaryWithHashSets<TKey, TValue>(
        this IDictionary<TKey, ICollection<TValue>> multiValueDictionary
    )
        where TKey : notnull =>
        multiValueDictionary.ToImmutableDictionary(x => x.Key, x => x.Value.ToImmutableHashSet());
}
