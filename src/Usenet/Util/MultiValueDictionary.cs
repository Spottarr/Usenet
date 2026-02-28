namespace Usenet.Util;

/// <summary>
/// Represents a collection of keys with multiple values.
/// </summary>
/// <typeparam name="TKey">The type of the keys in the dictionary.</typeparam>
/// <typeparam name="TValue">The type of values in the dictionary.</typeparam>
internal class MultiValueDictionary<TKey, TValue>
    : Dictionary<TKey, ICollection<TValue>>,
        IEquatable<MultiValueDictionary<TKey, TValue>>
{
    private readonly Func<ICollection<TValue>> _collectionFactory;

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey,TValue}"/>
    /// that is empty and uses a <see cref="HashSet{TValue}"/> factor to create the internal collections.
    /// </summary>
    public MultiValueDictionary()
        : this(() => new HashSet<TValue>()) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey,TValue}"/>
    /// that is empty and uses a <see cref="HashSet{TValue}"/> factor to create the internal collections.
    /// </summary>
    /// <param name="keyComparer"></param>
    public MultiValueDictionary(IEqualityComparer<TKey> keyComparer)
        : this(() => new HashSet<TValue>(), keyComparer) { }

    /// <summary>
    /// Initializes a new instance of the <see cref="MultiValueDictionary{TKey,TValue}"/>
    /// that is empty and uses the specified <paramref name="collectionFactory"/> to create the internal collections.
    /// </summary>
    /// <param name="collectionFactory">The collection factory to use.</param>
    /// <param name="keyComparer"></param>
    public MultiValueDictionary(
        Func<ICollection<TValue>> collectionFactory,
        IEqualityComparer<TKey> keyComparer = null
    )
        : base(keyComparer)
    {
        _collectionFactory = collectionFactory;
    }

    /// <summary>
    /// Adds the specified key and value to the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to add.</param>
    /// <param name="value">The value of the element to add.</param>
    public virtual void Add(TKey key, TValue value)
    {
        if (!TryGetValue(key, out var values) || values == null)
        {
            values = _collectionFactory();
            Add(key, values);
        }

        values.Add(value);
    }

    /// <summary>
    /// Removes the specified key and value from the dictionary.
    /// </summary>
    /// <param name="key">The key of the element to remove.</param>
    /// <param name="value">The value of the element to remove.</param>
    /// <returns>true if the element is successfully found and removed; otherwise, false.</returns>
    public virtual bool Remove(TKey key, TValue value)
    {
        if (!TryGetValue(key, out var values) || values == null)
            return false;
        if (!values.Remove(value))
            return false;

        if (values.Count == 0)
            Remove(key);

        return true;
    }

    /// <summary>Gets the number of elements contained in the <see cref="MultiValueDictionary{TKey,TValue}" />.</summary>
    /// <returns>The number of elements contained in the <see cref="MultiValueDictionary{TKey,TValue}" />.</returns>
    public new int Count =>
        Values
            .Where(valueCollection => valueCollection != null)
            .Sum(valueCollection => valueCollection.Count);

    /// <summary>
    /// Represents an empty <see cref="MultiValueDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <returns>A new empty instance on every call of the <see cref="MultiValueDictionary{TKey,TValue}"/>
    /// that uses a <see cref="HashSet{TValue}"/> factory internally.</returns>
    public static MultiValueDictionary<TKey, TValue> Empty => new();

    /// <summary>
    /// Represents an empty, case-insensitive, <see cref="MultiValueDictionary{TKey,TValue}"/>.
    /// </summary>
    /// <returns>A new empty instance on every call of the <see cref="MultiValueDictionary{TKey,TValue}"/>
    /// that uses a <see cref="HashSet{TValue}"/> factory internally.</returns>
    public static MultiValueDictionary<string, TValue> EmptyIgnoreCase =>
        new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Returns the hash code for this instance.
    /// </summary>
    /// <returns>A 32-bit signed integer hash code.</returns>
    public override int GetHashCode() => HashCode.Start.Hash(this);

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="MultiValueDictionary{TKey,TValue}"/> value.
    /// </summary>
    /// <param name="other">A <see cref="MultiValueDictionary{TKey,TValue}"/> object to compare to this instance.</param>
    /// <returns>true if <paramref name="other" /> has the same value as this instance; otherwise, false.</returns>
    public bool Equals(MultiValueDictionary<TKey, TValue> other)
    {
        if (other is null || Count != other.Count)
            return false;

        var comp = MultiSetComparer<TValue>.Instance;
        foreach (var pair in this)
        {
            var thisValues = pair.Value;
            if (
                !other.TryGetValue(pair.Key, out var otherValues)
                || !comp.Equals(thisValues, otherValues)
            )
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Returns a value indicating whether this instance is equal to the specified <see cref="MultiValueDictionary{TKey,TValue}"/> value.
    /// </summary>
    /// <param name="obj">An <see cref="object"/> to compare to this instance.</param>
    /// <returns>true if <paramref name="obj" /> has the same value as this instance; otherwise, false.</returns>
    public override bool Equals(object obj) => Equals(obj as MultiValueDictionary<TKey, TValue>);

    /// <summary>
    /// Returns a value indicating whether the frst <see cref="MultiValueDictionary{TKey,TValue}"/>
    /// value is equal to the second <see cref="MultiValueDictionary{TKey,TValue}"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="MultiValueDictionary{TKey,TValue}"/>.</param>
    /// <param name="second">The second <see cref="MultiValueDictionary{TKey,TValue}"/>.</param>
    /// <returns>true if <paramref name="first"/> has the same value as <paramref name="second"/>; otherwise false.</returns>
    public static bool operator ==(
        MultiValueDictionary<TKey, TValue> first,
        MultiValueDictionary<TKey, TValue> second
    ) => first?.Equals(second) ?? second is null;

    /// <summary>
    /// Returns a value indicating whether the frst <see cref="MultiValueDictionary{TKey,TValue}"/>
    /// value is unequal to the second <see cref="MultiValueDictionary{TKey,TValue}"/> value.
    /// </summary>
    /// <param name="first">The first <see cref="MultiValueDictionary{TKey,TValue}"/>.</param>
    /// <param name="second">The second <see cref="MultiValueDictionary{TKey,TValue}"/>.</param>
    /// <returns>true if <paramref name="first"/> has a different value than <paramref name="second"/>; otherwise false.</returns>
    public static bool operator !=(
        MultiValueDictionary<TKey, TValue> first,
        MultiValueDictionary<TKey, TValue> second
    ) => !(first == second);
}
