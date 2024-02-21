// https://github.com/kofifus/F/wiki

#nullable enable

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;


namespace F.Collections {

  [JsonConverter(typeof(JsonFCollectionConverter))]
  public abstract record LstBase<TDerived, T> : IEnumerable<T>, IEnumerable
    where TDerived : LstBase<TDerived, T>, new() {

    protected ImmutableList<T> Composed { get; init; }
    protected HashCode? HashCache;

    public LstBase() => Composed = [];
    public LstBase(params T?[] v) => Composed = [.. v.Where(x => x is object)!];
    public LstBase(Lst<T> v) => Composed = v.Composed;

    public static TDerived New(ImmutableList<T> v) => new() { Composed = v }; // also used for json deserialization
    static TDerived New(IEnumerable<T> v) => new() { Composed = v.ToImmutableList() };

    public sealed override string ToString() => ToString(", ");
    public string ToString(string separator) => "{" + string.Join(separator, Composed) + "}";
    public string ToString(char separator) => ToString(separator.ToString());

    public virtual bool Equals(LstBase<TDerived, T>? obj) => obj is object && Count == obj.Count && GetHashCode() == obj.GetHashCode() && Composed.SequenceEqual(obj.Composed);

    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static TDerived operator +(LstBase<TDerived, T> o, T v) => o.Add(v);
    public static TDerived operator +(LstBase<TDerived, T> o, Lst<T> v) => o.AddRange(v);
    public static TDerived operator +(LstBase<TDerived, T> o, TDerived v) => o.AddRange(v);
    public static TDerived operator -(LstBase<TDerived, T> o, T v) => o.RemoveAll(v);
    public static TDerived operator -(LstBase<TDerived, T> o, Lst<T> v) => o.RemoveRange(v);
    public static TDerived operator -(LstBase<TDerived, T> o, TDerived v) => o.RemoveRange(v);
    public static TDerived operator -(LstBase<TDerived, T> o, Predicate<T> match) => o.RemoveAll(match);

    // use this if T is a non-nullable reference type (ie a MyClass or string) (https://stackoverflow.com/questions/63857659/c-sharp-9-nullable-types-issues)
    public T? this[int index] { get {
        if (index < 0 || index >= Count) return default;
        try { return Composed[index]; } catch { return default; }
    } }

    // use this if T is a nullable reference type or value type (ie MyClass? or int)
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out T value) {
      if (index < 0 || index >= Count) { value = default; return false; };
      try { value = Composed[index]; return true; } catch { value = default; return false; }
    }

    // use this if T is a non-nullable reference type (ie a MyClass)
    public T? Find(Predicate<T> match) => Composed.Find(match);

    // use this if T a nullable reference type or primitive type (ie long or MyClass?)
    public bool TryFind(Predicate<T> match, [MaybeNullWhen(false)] out T value) {
      var index = Composed.FindIndex(match);
      value = index > -1 ? Composed[index] : default;
      return index > -1;
    }

    // use this if T is a non-nullable reference type (ie a MyClass)
    public T? FindLast(Predicate<T> match) => Composed.FindLast(match);

    // use this if T a nullable reference type or primitive type (ie long or MyClass?)
    public bool TryFindLast(Predicate<T> match, [MaybeNullWhen(false)] out T value) {
      var index = Composed.FindLastIndex(match);
      value = index > -1 ? Composed[index] : default;
      return index > -1;
    }

    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    public int Count { get { return Composed.Count; } }

    public TDerived Add(T value) => New(Composed.Add(value));

    public TDerived AddRange(Lst<T> items) => New(Composed.AddRange(items));
    public TDerived AddRange(TDerived items) => New(Composed.AddRange(items));

    public TDerived RemoveRange(Lst<T> items) => New(Composed.RemoveRange(items));
    public TDerived RemoveRange(TDerived items) => New(Composed.RemoveRange(items));
    public TDerived RemoveRange(TDerived items, IEqualityComparer<T> equalityComparer) => New(Composed.RemoveRange(items, equalityComparer));

    public int BinarySearch(T item) => Composed.BinarySearch(item);
    public int BinarySearch(T item, IComparer<T> comparer) => Composed.BinarySearch(item, comparer);
    public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => Composed.BinarySearch(index, count, item, comparer);
    public bool Contains(T value) => Composed.Contains(value);
    public bool Equals(LstBase<TDerived, T> second, IEqualityComparer<T> comparer) => Composed.SequenceEqual(second, comparer);
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => Composed.CopyTo(index, array, arrayIndex, count);
    public void CopyTo(T[] array) => Composed.CopyTo(array);
    public void CopyTo(T[] array, int arrayIndex) => Composed.CopyTo(array, arrayIndex);
    public bool Exists(Predicate<T> match) => Composed.Exists(match);

    public TDerived FindAll(Predicate<T> match) => New(Composed.FindAll(match));
    public int FindIndex(Predicate<T> match) => Composed.FindIndex(match);
    public int FindIndex(int startIndex, Predicate<T> match) => Composed.FindIndex(startIndex, match);
    public int FindIndex(int startIndex, int count, Predicate<T> match) => Composed.FindIndex(startIndex, count, match);
    public int FindLastIndex(int startIndex, int count, Predicate<T> match) => Composed.FindLastIndex(startIndex, count, match);
    public int FindLastIndex(int startIndex, Predicate<T> match) => Composed.FindLastIndex(startIndex, match);
    public int FindLastIndex(Predicate<T> match) => Composed.FindLastIndex(match);
    public void ForEach(Action<T> action) => Composed.ForEach(action);
    public TDerived GetRange(int index, int count) => New(Composed.GetRange(index, count));
    public int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => Composed.IndexOf(item, index, count, equalityComparer);
    public int IndexOf(T value) => Composed.IndexOf(value);

    public TDerived? Insert(int index, T item) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.Insert(index, item)); } catch { return null; }
    }

    public TDerived? Insert(int index, TDerived items) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.InsertRange(index, items)); } catch { return null; }
    }

    public TDerived? Insert(int index, Lst<T> items) => Insert(index, New(items.Composed));

    public ref readonly T ItemRef(int index) => ref Composed.ItemRef(index);
    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => Composed.LastIndexOf(item, index, count, equalityComparer);

    public TDerived Remove(T value) => New(Composed.Remove(value));
    public TDerived Remove(T value, IEqualityComparer<T> equalityComparer) => New(Composed.Remove(value, equalityComparer));
    public TDerived RemoveAll(Predicate<T> match) => New(Composed.RemoveAll(match));
    public TDerived RemoveAll(T v) => RemoveAll(t => Equals(t, v));

    public TDerived RemoveAt(int index) {
      if (index < 0 || index >= Count) return New(this);
      try { return New(Composed.RemoveAt(index)); } catch { return New(this); }
    }

    public TDerived RemoveRange(int index, int count) {
      if (index < 0 || index >= Count) return New(this);
      try { return New(Composed.RemoveRange(index, count)); } catch { return New(this); }
    }

    public TDerived Replace(T oldValue, T newValue) => New(Composed.Replace(oldValue, newValue));
    public TDerived Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) => New(Composed.Replace(oldValue, newValue, equalityComparer));

    public TDerived? Reverse(int index, int count) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.Reverse(index, count)); } catch { return null; }
    }

    public TDerived Reverse() => New(Composed.Reverse());

    public TDerived? SetItem(int index, T value) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.SetItem(index, value)); } catch { return null; }
    }

    public TDerived Sort() => New(Composed.Sort());
    public TDerived Sort(IComparer<T> comparer) => New(Composed.Sort(comparer));
    public TDerived Sort(Comparison<T> comparison) => New(Composed.Sort(comparison));

    public TDerived? Sort(int index, int count, IComparer<T> comparer) {
      if (index < 0 || index >= Count) return default;
      try { return New(Composed.Sort(index, count, comparer)); } catch { return null; }
    }

    public ImmutableList<T>.Builder ToBuilder() => Composed.ToBuilder();

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Composed).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Composed).GetEnumerator();
  }


  public sealed record Lst<T> : LstBase<Lst<T>, T> {
    public Lst() : base() { }
    public Lst(params T?[] v) : base(v) { }
  }


  // Base class for Set<T> and any user defined Set
  [JsonConverter(typeof(JsonFCollectionConverter))]
  public abstract record SetBase<TDerived, T> : IEnumerable<T>, IEnumerable
    where TDerived : SetBase<TDerived, T>, new() {

    protected ImmutableHashSet<T> Composed { get; init; }
    protected HashCode? HashCache;

    public SetBase() => Composed = [];
    public SetBase(params T?[] v) => Composed = ImmutableHashSet<T>.Empty.Union(v.Where(t => t is object)!);
    public SetBase(Set<T> v) => Composed = v.Composed;

    public static TDerived New(ImmutableHashSet<T> v) => new() { Composed = v }; // also used for json deserialization
    static TDerived New(IEnumerable<T> v) => new() { Composed = v.ToImmutableHashSet() };

    public sealed override string ToString() => ToString(", ");
    public string ToString(string separator) => "{" + string.Join(separator, Composed) + "}";
    public string ToString(char separator) => ToString(separator.ToString());

    public virtual bool Equals(SetBase<TDerived, T>? obj) => obj is not null && Count == obj.Count && GetHashCode() == obj.GetHashCode() && Composed.SetEquals(obj.Composed);

    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static TDerived operator +(SetBase<TDerived, T> o, T v) => o.Add(v);
    public static TDerived operator +(SetBase<TDerived, T> o, Set<T> v) => o.Union(v);
    public static TDerived operator +(SetBase<TDerived, T> o, TDerived v) => o.Union(v);
    public static TDerived operator -(SetBase<TDerived, T> o, T v) => o.Remove(v);
    public static TDerived operator -(SetBase<TDerived, T> o, Set<T> v) => o.Except(v);
    public static TDerived operator -(SetBase<TDerived, T> o, TDerived v) => o.Except(v);
    public static TDerived operator -(SetBase<TDerived, T> o, Predicate<T> match) => o.RemoveAll(match);

    public TDerived RemoveAll(Predicate<T> match) => New(this.Where(v => !match(v)));

    public TDerived Union(TDerived v) => New(Composed.Union(v.Composed));
    public TDerived Union(Set<T> v) => New(Composed.Union(v.Composed));

    public IEqualityComparer<T> KeyComparer { get { return Composed.KeyComparer; } }
    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }
    public int Count { get { return Composed.Count; } }

    public TDerived Add(T item) => New(Composed.Add(item));
    public bool Contains(T item) => Composed.Contains(item);

    public TDerived Except(TDerived v) => New(Composed.Except(v.Composed));
    public TDerived Except(Set<T> v) => New(Composed.Except(v.Composed));

    public TDerived Intersect(TDerived other) => New(Composed.Intersect(other));
    public TDerived Intersect(Set<T> other) => New(Composed.Intersect(other));
    public bool IsProperSubsetOf(TDerived other) => Composed.IsProperSubsetOf(other);
    public bool IsProperSubsetOf(Set<T> other) => Composed.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(TDerived other) => Composed.IsProperSupersetOf(other);
    public bool IsProperSupersetOf(Set<T> other) => Composed.IsProperSupersetOf(other);
    public bool IsSubsetOf(TDerived other) => Composed.IsSubsetOf(other);
    public bool IsSubsetOf(Set<T> other) => Composed.IsSubsetOf(other);
    public bool IsSupersetOf(TDerived other) => Composed.IsSupersetOf(other);
    public bool IsSupersetOf(Set<T> other) => Composed.IsSupersetOf(other);
    public bool Overlaps(TDerived other) => Composed.Overlaps(other);
    public bool Overlaps(Set<T> other) => Composed.Overlaps(other);
    public TDerived SymmetricExcept(TDerived other) => New(Composed.SymmetricExcept(other));
    public TDerived SymmetricExcept(Set<T> other) => New(Composed.SymmetricExcept(other));

    public TDerived Remove(T item) => New(Composed.Remove(item));
    public bool SetEquals(TDerived other) => Composed.SetEquals(other);

    public ImmutableHashSet<T>.Builder ToBuilder() => Composed.ToBuilder();

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Composed).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Composed).GetEnumerator();
  }

  // the default concrete SetBase
  public sealed record Set<T> : SetBase<Set<T>, T> {
    public Set() : base() { }
    public Set(params T?[] p) : base(p) { }
  }


  // Base class for Set<T> and any user defined Set
  [JsonConverter(typeof(JsonFCollectionConverter))]
  public abstract record OrderedSetBase<TDerived, T> : IEnumerable<T>, IEnumerable
    where TDerived : OrderedSetBase<TDerived, T>, new() {

    protected ImmutableSortedSet<T> Composed { get; init; }
    protected HashCode? HashCache;

    public OrderedSetBase() => Composed = [];
    public OrderedSetBase(params T?[] p) => Composed = ImmutableSortedSet<T>.Empty.Union(p.Where(t => t is object)!);
    public OrderedSetBase(OrderedSet<T> v) => Composed = v.Composed;

    public static TDerived New(ImmutableSortedSet<T> p) => new() { Composed = p }; // also used for json deserialization
    static TDerived New(IEnumerable<T> p) => new() { Composed = p.ToImmutableSortedSet() };

    public sealed override string ToString() => ToString(", ");
    public string ToString(string separator) => "{" + string.Join(separator, Composed) + "}";
    public string ToString(char separator) => ToString(separator.ToString());

    public virtual bool Equals(OrderedSetBase<TDerived, T>? obj) => obj is not null && Count == obj.Count && GetHashCode() == obj.GetHashCode() && Composed.SetEquals(obj.Composed);

    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static TDerived operator +(OrderedSetBase<TDerived, T> o, T v) => o.Add(v);
    public static TDerived operator +(OrderedSetBase<TDerived, T> o, OrderedSet<T> other) => o.Union(other);
    public static TDerived operator +(OrderedSetBase<TDerived, T> o, TDerived other) => o.Union(other);
    public static TDerived operator -(OrderedSetBase<TDerived, T> o, T v) => o.Remove(v);
    public static TDerived operator -(OrderedSetBase<TDerived, T> o, OrderedSet<T> other) => o.Except(other);
    public static TDerived operator -(OrderedSetBase<TDerived, T> o, TDerived other) => o.Except(other);
    public static TDerived operator -(OrderedSetBase<TDerived, T> o, Predicate<T> match) => o.RemoveAll(match);

    public TDerived RemoveAll(Predicate<T> match) => New(this.Where(v => !match(v)));

    public TDerived Union(TDerived v) => New(Composed.Union(v.Composed));
    public TDerived Union(OrderedSet<T> v) => New(Composed.Union(v.Composed));

    public IComparer<T> KeyComparer { get { return Composed.KeyComparer; } }
    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }
    public int Count { get { return Composed.Count; } }

    public TDerived Add(T item) => New(Composed.Add(item));
    public bool Contains(T item) => Composed.Contains(item);

    public TDerived Except(TDerived v) => New(Composed.Except(v.Composed));
    public TDerived Except(OrderedSet<T> v) => New(Composed.Except(v.Composed));

    public TDerived Intersect(TDerived other) => New(Composed.Intersect(other));
    public TDerived Intersect(OrderedSet<T> other) => New(Composed.Intersect(other));
    public bool IsProperSubsetOf(TDerived other) => Composed.IsProperSubsetOf(other);
    public bool IsProperSubsetOf(OrderedSet<T> other) => Composed.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(TDerived other) => Composed.IsProperSupersetOf(other);
    public bool IsProperSupersetOf(OrderedSet<T> other) => Composed.IsProperSupersetOf(other);
    public bool IsSubsetOf(TDerived other) => Composed.IsSubsetOf(other);
    public bool IsSubsetOf(OrderedSet<T> other) => Composed.IsSubsetOf(other);
    public bool IsSupersetOf(TDerived other) => Composed.IsSupersetOf(other);
    public bool IsSupersetOf(OrderedSet<T> other) => Composed.IsSupersetOf(other);
    public bool Overlaps(TDerived other) => Composed.Overlaps(other);
    public bool Overlaps(OrderedSet<T> other) => Composed.Overlaps(other);
    public TDerived SymmetricExcept(TDerived other) => New(Composed.SymmetricExcept(other));
    public TDerived SymmetricExcept(OrderedSet<T> other) => New(Composed.SymmetricExcept(other));

    public TDerived Remove(T item) => New(Composed.Remove(item));
    public bool SetEquals(OrderedSetBase<TDerived, T> other) => Composed.SetEquals(other);

    public ImmutableSortedSet<T>.Builder ToBuilder() => Composed.ToBuilder();
    public TDerived WithComparer(IComparer<T> comparer) => New(Composed.WithComparer(comparer));

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Composed).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Composed).GetEnumerator();
  }

  // the default concrete OrderedSetBase
  public sealed record OrderedSet<T> : OrderedSetBase<OrderedSet<T>, T> {
    public OrderedSet() : base() { }
    public OrderedSet(params T?[] p) : base(p) { }
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public abstract record MapBase<TDerived, TKey, TValue> : IEnumerable<(TKey, TValue)>, IEnumerable
    where TKey : notnull
    where TDerived : MapBase<TDerived, TKey, TValue>, new() {

    protected ImmutableDictionary<TKey, TValue> Composed { get; init; }
    protected HashCode? HashCache;

    public MapBase() => Composed = ImmutableDictionary<TKey, TValue>.Empty;
    public MapBase(TKey key, TValue val) => Composed = ImmutableDictionary<TKey, TValue>.Empty.Add(key, val);
    public MapBase(params (TKey key, TValue val)?[] ps) => Composed = ps.Aggregate(ImmutableDictionary<TKey, TValue>.Empty, (t, p) => p is null ? t : t.SetItem(p.Value.key, p.Value.val));
    public MapBase(Map<TKey, TValue> v) => Composed = v.Composed;

    public static TDerived New(ImmutableDictionary<TKey, TValue> v) => new() { Composed = v }; // also used for json deserialization
    static TDerived New(IEnumerable<(TKey key, TValue val)> v) => new() { Composed = v.ToImmutableDictionary(t => t.key, t => t.val) };

    public sealed override string ToString() => ToString(", ");
    public string ToString(string separator) => "{" +  string.Join(separator, Composed.Select(vt => $"({vt.Key}, {vt.Value?.ToString() ?? "null"})"))+"}";
    public string ToString(char separator) => ToString(separator.ToString());

    public virtual bool Equals(MapBase<TDerived, TKey, TValue>? obj) {
      if (obj is null || Count != obj.Count || GetHashCode() != obj.GetHashCode()) return false;
      foreach (var (d1key, d1value) in Composed) {
        if (!obj.Composed.TryGetValue(d1key, out TValue? d2value)) return false;
        if (!object.Equals(d1value, d2value)) return false;
      }
      return true;
    }

    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static TDerived operator +(MapBase<TDerived, TKey, TValue> o, (TKey key, TValue val) vt) => o.SetItem(vt.key, vt.val);
    public static TDerived operator +(MapBase<TDerived, TKey, TValue> o, TDerived v) => New(o.Composed.SetItems(v.Composed));
    public static TDerived operator +(MapBase<TDerived, TKey, TValue> o, Set<(TKey, TValue)> v) => o.SetItems(v);
    public static TDerived operator +(MapBase<TDerived, TKey, TValue> o, Set<KeyValuePair<TKey, TValue>> v) => o.SetItems(v);
    public static TDerived operator +(MapBase<TDerived, TKey, TValue> o, Map<TKey, TValue> v) => o.SetItems(v);
    public static TDerived operator -(MapBase<TDerived, TKey, TValue> o, TKey key) => o.Remove(key);
    public static TDerived operator -(MapBase<TDerived, TKey, TValue> o, Set<TKey> keys) => New(o.Composed.RemoveRange(keys));
    public static TDerived operator -(MapBase<TDerived, TKey, TValue> o, Func<TKey, TValue, bool> match) => o.Remove(match);

    // use this if T is a non-nullable reference type (ie a MyClass or string) (https://stackoverflow.com/questions/63857659/c-sharp-9-nullable-types-issues)
    public TValue? this[TKey key] => Composed.TryGetValue(key, out var value) ? value : default;

    // use this if T is a nullable reference type or value type (ie MyClass? or int)
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Composed.TryGetValue(key, out value);

    public bool TryGetKey(TKey equalKey, [MaybeNullWhen(false)] out TKey actualKey) => Composed.TryGetKey(equalKey, out actualKey);

    public TDerived SetItems(params (TKey key, TValue val)?[] v) => New(v.Aggregate(Composed, (t, p) => p is null ? t : t.SetItem(p.Value.key, p.Value.val)));
    public TDerived SetItems(Set<(TKey key, TValue val)> v) => New(Composed.SetItems(v.Select(vt => new KeyValuePair<TKey, TValue>(vt.key, vt.val))));
    public TDerived SetItems(Set<KeyValuePair<TKey, TValue>> v) => New(Composed.SetItems(v));
    public TDerived SetItems(Map<TKey, TValue> v) => New(Composed.SetItems(v.Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Item1, kvp.Item2))));

    public TDerived Remove(Func<TKey, TValue, bool> match) {
      bool pred((TKey, TValue) vt) => match(vt.Item1, vt.Item2);
      return match is null ? throw new ArgumentNullException(nameof(match)) : New(this.Where(kv => !pred(kv)));
    }

    public IEqualityComparer<TKey> KeyComparer { get { return Composed.KeyComparer; } }
    public Set<TKey> Keys { get { return Composed.Keys.ToSet(); } }
    public IEqualityComparer<TValue> ValueComparer { get { return Composed.ValueComparer; } }
    public Set<TValue> Values { get { return Composed.Values.ToSet(); } }
    public int Count { get { return Composed.Count; } }

    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    //public TDerived Add(TKey key, TValue value) => New(Composed.Add(key, value));
    //public TDerived AddRange(MapBase<TDerived, TKey, TValue> v) => New(v.Aggregate(Composed, (t, p) => t.SetItem(p.Item1, p.Item2)));

    public bool Contains(KeyValuePair<TKey, TValue> pair) => Composed.Contains(pair);
    public bool ContainsKey(TKey key) => Composed.ContainsKey(key);
    public bool ContainsValue(TValue value) => Composed.ContainsValue(value);

    public TDerived Remove(TKey key) => New(Composed.Remove(key));
    public TDerived RemoveRange(Set<TKey> keys) => New(Composed.RemoveRange(keys));

    public TDerived SetItem(TKey key, TValue value) => New(Composed.SetItem(key, value));

    public ImmutableDictionary<TKey, TValue>.Builder ToBuilder() => Composed.ToBuilder();

    IEnumerator<(TKey, TValue)> IEnumerable<(TKey, TValue)>.GetEnumerator() {
      var e = Composed.GetEnumerator();
      while (e.MoveNext()) yield return (e.Current.Key, e.Current.Value);
    }
    public IEnumerator<(TKey, TValue)> GetEnumerator() => (this as IEnumerable<(TKey, TValue)>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (Composed as IEnumerable).GetEnumerator();
  }


  public sealed record Map<TKey, TValue> : MapBase<Map<TKey, TValue>, TKey, TValue> 
  where TKey : notnull {

    public Map() : base() { }
    public Map(TKey k, TValue v) : base(k, v) { }
    public Map(params (TKey k, TValue v)?[] items) : base(items) { }
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public abstract record OrderedMapBase<TDerived, TKey, TValue> : IEnumerable<(TKey, TValue)>, IEnumerable
    where TKey : notnull 
    where TDerived : OrderedMapBase<TDerived, TKey, TValue>, new() {

    protected ImmutableSortedDictionary<TKey, TValue> Composed { get; init; }
    protected HashCode? HashCache;

    public OrderedMapBase() => Composed = ImmutableSortedDictionary<TKey, TValue>.Empty;
    public OrderedMapBase(TKey key, TValue val) => Composed = ImmutableSortedDictionary<TKey, TValue>.Empty.Add(key, val);
    public OrderedMapBase(params (TKey key, TValue val)?[] ps) => Composed = ps.Aggregate(ImmutableSortedDictionary<TKey, TValue>.Empty, (t, p) => p is null ? t : t.SetItem(p.Value.key, p.Value.val));
    public OrderedMapBase(OrderedMap<TKey, TValue> v) => Composed = v.Composed;

    public static TDerived New(ImmutableSortedDictionary<TKey, TValue> p) => new() { Composed = p }; // also used for json deserialization
    static TDerived New(IEnumerable<(TKey key, TValue val)> p) => new() { Composed = p.ToImmutableSortedDictionary(t => t.key, t => t.val) };

    public sealed override string ToString() => ToString(", ");
    public string ToString(string separator) => "{" + string.Join(separator, Composed.Select(vt => $"({vt.Key}, {vt.Value?.ToString() ?? "null"})")) + "}";
    public string ToString(char separator) => ToString(separator.ToString());

    public virtual bool Equals(OrderedMapBase<TDerived, TKey, TValue>? obj) {
      if (obj is null || Count != obj.Count || GetHashCode() != obj.GetHashCode()) return false;
      foreach (var (d1key, d1value) in Composed) {
        if (!obj.Composed.TryGetValue(d1key, out TValue? d2value)) return false;
        if (!object.Equals(d1value, d2value)) return false;
      }
      return true;
    }

    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static TDerived operator +(OrderedMapBase<TDerived, TKey, TValue> o, (TKey key, TValue val) vt) => o.SetItem(vt.key, vt.val);
    public static TDerived operator +(OrderedMapBase<TDerived, TKey, TValue> o, TDerived v) => New(o.Composed.SetItems(v.Composed));
    public static TDerived operator +(OrderedMapBase<TDerived, TKey, TValue> o, Set<(TKey, TValue)> v) => o.SetItems(v);
    public static TDerived operator +(OrderedMapBase<TDerived, TKey, TValue> o, Set<KeyValuePair<TKey, TValue>> v) => o.SetItems(v);
    public static TDerived operator +(OrderedMapBase<TDerived, TKey, TValue> o, OrderedMap<TKey, TValue> v) => o.SetItems(v);
    public static TDerived operator -(OrderedMapBase<TDerived, TKey, TValue> o, TKey key) => o.Remove(key);
    public static TDerived operator -(OrderedMapBase<TDerived, TKey, TValue> o, Set<TKey> keys) => New(o.Composed.RemoveRange(keys));
    public static TDerived operator -(OrderedMapBase<TDerived, TKey, TValue> o, Func<TKey, TValue, bool> match) => o.Remove(match);

    // use this if T is a non-nullable reference type (ie a MyClass or string) (https://stackoverflow.com/questions/63857659/c-sharp-9-nullable-types-issues)
    public TValue? this[TKey key] => Composed.TryGetValue(key, out var value) ? value : default;

    // use this if T is a nullable reference type or value type (ie MyClass? or int)
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Composed.TryGetValue(key, out value);

    public bool TryGetKey(TKey equalKey, [MaybeNullWhen(false)] out TKey actualKey) => Composed.TryGetKey(equalKey, out actualKey);

    public TDerived SetItems(params (TKey key, TValue val)?[] ps) => New(ps.Aggregate(Composed, (t, p) => p is null ? t : t.SetItem(p.Value.key, p.Value.val)));
    public TDerived SetItems(Set<(TKey key, TValue val)> v) => New(Composed.SetItems(v.Select(vt => new KeyValuePair<TKey, TValue>(vt.key, vt.val))));
    public TDerived SetItems(Set<KeyValuePair<TKey, TValue>> v) => New(Composed.SetItems(v));
    public TDerived SetItems(OrderedMap<TKey, TValue> v) => New(Composed.SetItems(v.Select(kvp => new KeyValuePair<TKey, TValue>(kvp.Item1, kvp.Item2))));

    public TDerived Remove(Func<TKey, TValue, bool> match) {
      bool pred((TKey, TValue) vt) => match(vt.Item1, vt.Item2);
      return match is null ? throw new ArgumentNullException(nameof(match)) : New(this.Where(kv => !pred(kv)));
    }

    public IComparer<TKey> KeyComparer { get { return Composed.KeyComparer; } }
    public Set<TKey> Keys { get { return Composed.Keys.ToSet(); } }
    public IEqualityComparer<TValue> ValueComparer { get { return Composed.ValueComparer; } }
    public Set<TValue> Values { get { return Composed.Values.ToSet(); } }
    public int Count { get { return Composed.Count; } }

    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    //public TDerived Add(TKey key, TValue value) => New(Composed.Add(key, value));
    //public TDerived AddRange(IEnumerable<KeyValuePair<TKey, TValue>>? pairs) => pairs is null ? New(this) : New(Composed.AddRange(pairs));

    public bool Contains(KeyValuePair<TKey, TValue> pair) => Composed.Contains(pair);
    public bool ContainsKey(TKey key) => Composed.ContainsKey(key);
    public bool ContainsValue(TValue value) => Composed.ContainsValue(value);

    public TDerived Remove(TKey key) => New(Composed.Remove(key));
    public TDerived RemoveRange(IEnumerable<TKey> keys) => New(Composed.RemoveRange(keys));

    public TDerived SetItem(TKey key, TValue value) => New(Composed.SetItem(key, value));
    public TDerived SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items) => New(Composed.SetItems(items));

    public ImmutableSortedDictionary<TKey, TValue>.Builder ToBuilder() => Composed.ToBuilder();

    IEnumerator<(TKey, TValue)> IEnumerable<(TKey, TValue)>.GetEnumerator() {
      var e = Composed.GetEnumerator();
      while (e.MoveNext()) yield return (e.Current.Key, e.Current.Value);
    }
    public IEnumerator<(TKey, TValue)> GetEnumerator() => (this as IEnumerable<(TKey, TValue)>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (Composed as IEnumerable).GetEnumerator();

    public TDerived WithComparer(IComparer<TKey> comparers) => New(Composed.WithComparers(comparers));
  }


  public sealed record OrderedMap<TKey, TValue> : OrderedMapBase<OrderedMap<TKey, TValue>, TKey, TValue> 
    where TKey : notnull {

    public OrderedMap() : base() { }
    public OrderedMap(TKey key, TValue val) : base(key, val) { }
    public OrderedMap(params (TKey key, TValue val)?[] items) : base(items) { }
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public abstract record QueBase<TDerived, T> : IEnumerable<T>, IEnumerable
    where TDerived : QueBase<TDerived, T>, new() {

    protected ImmutableQueue<T> Composed { get; init; }
    protected HashCode? HashCache;

    public QueBase() => Composed = [];
    public QueBase(params T?[] v) => Composed = v.Aggregate(ImmutableQueue<T>.Empty, (t, v) => v is null ? t : t.Enqueue(v));
    public QueBase(Que<T> v) => Composed = v.Composed;

    public static TDerived New(ImmutableQueue<T> v) => new() { Composed = v }; // also used for json deserialization
    //static TDerived New(IEnumerable<T> v) => new() { Composed = v.Aggregate(ImmutableQueue<T>.Empty, (t, v) => v is null ? t : t.Enqueue(v)) };

    public sealed override string ToString() => ToString(", ");
    public string ToString(string separator) => "{" + string.Join(separator, Composed) + "}";
    public string ToString(char separator) => ToString(separator.ToString());

    public virtual bool Equals(QueBase<TDerived, T>? obj) => obj is not null && Count==obj.Count && GetHashCode() == obj.GetHashCode() && Composed.SequenceEqual(((QueBase<TDerived, T>)obj).Composed);
    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static TDerived operator +(QueBase<TDerived, T> o, T v) => o.Enqueue(v);
    public static TDerived operator +(QueBase<TDerived, T> o, TDerived v) => o.Enqueue(v);
    public static TDerived operator +(QueBase<TDerived, T> o, Que<T> v) => o.Enqueue(v);

    // use this if T is a non-nullable reference type (ie a MyClass or string) (https://stackoverflow.com/questions/63857659/c-sharp-9-nullable-types-issues)
    public T? Peek() {
      if (IsEmpty) return default;
      try { return Composed.Peek(); } catch { return default; }
    }

    // use this if T is a nullable reference type or value type (ie MyClass? or int)
    public bool TryPeek([MaybeNullWhen(false)] out T value) {
      if (IsEmpty) { value = default; return false; }
      try { value = Composed.Peek(); return true; } catch { value = default; return false; }
    }

    // the rest of the methods just proxy to Composed
    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    public int Count { get { return Composed.Count(); } } // todo cache

    public TDerived Enqueue(T v) => New(Composed.Enqueue(v));
    public TDerived Enqueue(TDerived v) => New(v.Aggregate(ImmutableQueue<T>.Empty, (t, v) => t.Enqueue(v)));
    public TDerived Enqueue(Que<T> v) => New(v.Aggregate(ImmutableQueue<T>.Empty, (t, v) => t.Enqueue(v)));

    public (TDerived Que, T Value) Dequeue() { var newq = Composed.Dequeue(out T v); return (New(newq), v); }

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Composed).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Composed).GetEnumerator();
  }

  public sealed record Que<T> : QueBase<Que<T>, T> {
    public Que() : base() { }
    public Que(params T?[] v) : base(v) { }
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public abstract record ArrBase<TDerived, T> : IEnumerable<T>, IEnumerable
    where TDerived : ArrBase<TDerived, T>, new() {

    protected ImmutableArray<T> Composed { get; init; }
    protected HashCode? HashCache;

    public ArrBase() => Composed = []; 
    public ArrBase(params T?[] p) => Composed = [.. p.Where(x => x is object)!]; 
    public ArrBase(Arr<T> v) => Composed = v.Composed;

    public static TDerived New(ImmutableArray<T> p) => new() { Composed = p }; // also used for json deserialization
    static TDerived New(IEnumerable<T> p) => new() { Composed = p.ToImmutableArray() };

    public sealed override string ToString() => ToString(", ");
    public string ToString(string separator) => "{" + string.Join(separator, Composed) + "}";
    public string ToString(char separator) => ToString(separator.ToString());

    public virtual bool Equals(ArrBase<TDerived, T>? obj) => obj is not null && Count == obj.Count && GetHashCode() == obj.GetHashCode() && Composed.SequenceEqual(obj.Composed);
    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static TDerived operator +(ArrBase<TDerived, T> o, T v) => o.Add(v);
    public static TDerived operator +(ArrBase<TDerived, T> o, TDerived items) => o.AddRange(items);
    public static TDerived operator +(ArrBase<TDerived, T> o, Arr<T> items) => o.AddRange(items);
    public static TDerived operator -(ArrBase<TDerived, T> o, T v) => o.RemoveAll(v);
    public static TDerived operator -(ArrBase<TDerived, T> o, TDerived items) => o.RemoveRange(items);
    public static TDerived operator -(ArrBase<TDerived, T> o, Arr<T> items) => o.RemoveRange(items);
    public static TDerived operator -(ArrBase<TDerived, T> o, Predicate<T> match) => o.RemoveAll(match);

    // use this if T is a non-nullable reference type (ie a MyClass or string) (https://stackoverflow.com/questions/63857659/c-sharp-9-nullable-types-issues)
    public T? this[int index] { get {
        if (index < 0 || index >= Count) return default;
        try { return Composed[index]; } catch { return default; }
    } }

    // use this if T is a nullable reference type or value type (ie MyClass? or int)
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out T value) {
      if (index < 0 || index >= Count) { value = default; return false; }
      try { value = Composed[index]; return true; } catch { value = default; return false; }
    }

    public int Count { get { return Composed.Length; } }

    public TDerived? Insert(int index, T item) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.Insert(index, item)); } catch { return null; }
    }

    public TDerived? InsertRange(int index, TDerived items) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.InsertRange(index, items)); } catch { return null; }
    }

    public TDerived? InsertRange(int index, Arr<T> items) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.InsertRange(index, items)); } catch { return null; }
    }

    public TDerived? RemoveAt(int index) {
      if (index < 0 || index >= Count) return New(this);
      try { return New(Composed.RemoveAt(index)); } catch { return null; }
    }

    public TDerived? RemoveRange(int index, int count) {
      if (index < 0 || index >= Count) return New(this);
      try { return New(Composed.RemoveRange(index, count)); } catch { return null; }
    }

    public TDerived Remove(T v) => New(Composed.Remove(v));
    public TDerived Remove(T v, IEqualityComparer<T> equalityComparer) => New(Composed.Remove(v, equalityComparer));
    public TDerived RemoveAll(Predicate<T> match) => New(Composed.RemoveAll(match));
    public TDerived RemoveAll(T v) => RemoveAll(t => Equals(t, v));

    public TDerived RemoveRange(TDerived items) => New(Composed.RemoveRange(items.Composed));
    public TDerived RemoveRange(TDerived items, IEqualityComparer<T> equalityComparer) => New(Composed.RemoveRange(items.Composed, equalityComparer));
    public TDerived RemoveRange(Arr<T> items) => New(Composed.RemoveRange(items.Composed));
    public TDerived RemoveRange(Arr<T> items, IEqualityComparer<T> equalityComparer) => New(Composed.RemoveRange(items.Composed, equalityComparer));

    public TDerived? SetItem(int index, T value) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.SetItem(index, value)); } catch { return null; }
    }

    public TDerived? Sort(int index, int count, IComparer<T> comparer) {
      if (index < 0 || index >= Count) return null;
      try { return New(Composed.Sort(index, count, comparer)); } catch { return null; }
    }

    public TDerived Add(T value) => New(Composed.Add(value));

    public TDerived AddRange(TDerived items) => New(Composed.AddRange(items.Composed));
    public TDerived AddRange(Arr<T> items) => New(Composed.AddRange(items.Composed));

    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    public int Length { get { return Composed.Length; } }
    public bool IsDefaultOrEmpty { get { return Composed.IsDefaultOrEmpty; } }
    public bool IsDefault { get { return Composed.IsDefault; } }

    public Arr<TOther> As<TOther>() where TOther : class => Arr<TOther>.New(Composed.As<TOther>());
    public ReadOnlyMemory<T> AsMemory() => Composed.AsMemory();
    public ReadOnlySpan<T> AsSpan() => Composed.AsSpan();
    public int BinarySearch(T value) => Composed.BinarySearch(value);
    public int BinarySearch(T value, IComparer<T> comparer) => Composed.BinarySearch(value, comparer);
    public int BinarySearch(int index, int length, T value) => Composed.BinarySearch(index, length, value);
    public int BinarySearch(int index, int length, T value, IComparer<T> comparer) => Composed.BinarySearch(index, length, value, comparer);
    public Arr<TOther> CastArray<TOther>() where TOther : class => Arr<TOther>.New(Composed.CastArray<TOther>());
    public bool Contains(T value) => Composed.Contains(value);
    public static TDerived CreateRange(Arr<T> items) => New(ImmutableArray.CreateRange(items.Composed));
    public static Arr<TResult> CreateRange<TResult>(Arr<T> items, Func<T, TResult> selector) => Arr<TResult>.New(ImmutableArray.CreateRange(items.Composed, selector));
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => Composed.CopyTo(index, array, arrayIndex, count);
    public void CopyTo(T[] array) => Composed.CopyTo(array);
    public void CopyTo(T[] array, int arrayIndex) => Composed.CopyTo(array, arrayIndex);
    public bool Exists(Predicate<T> match) => Composed.Any(new Func<T, bool>(match));
    public void ForEach(Action<T> action) { foreach (var v in Composed) action(v); }
    public int IndexOf(T item, int startIndex, IEqualityComparer<T> equalityComparer) => Composed.IndexOf(item, startIndex, equalityComparer);
    public int IndexOf(T item, int startIndex, int count) => Composed.IndexOf(item, startIndex, count);
    public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => Composed.IndexOf(item, startIndex, count, equalityComparer);
    public int IndexOf(T item, int startIndex) => Composed.IndexOf(item, startIndex);
    public int IndexOf(T value) => Composed.IndexOf(value);
    public ref readonly T ItemRef(int index) => ref Composed.ItemRef(index);
    public int LastIndexOf(T item) => Composed.LastIndexOf(item);
    public int LastIndexOf(T item, int index) => Composed.LastIndexOf(item, index);
    public int LastIndexOf(T item, int index, int count) => Composed.LastIndexOf(item, index, count);
    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => Composed.LastIndexOf(item, index, count, equalityComparer);
    public IEnumerable<TResult> OfType<TResult>() => Composed.OfType<TResult>();
    public TDerived Replace(T oldValue, T newValue) => New(Composed.Replace(oldValue, newValue));
    public TDerived Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) => New(Composed.Replace(oldValue, newValue, equalityComparer));
    public TDerived Sort(IComparer<T> comparer) => New(Composed.Sort(comparer));
    public TDerived Sort(Comparison<T> comparison) => New(Composed.Sort(comparison));
    public TDerived Sort() => New(Composed.Sort());
    public ImmutableArray<T>.Builder ToBuilder() => Composed.ToBuilder();

    public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>)Composed).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable)Composed).GetEnumerator();
  }


  public sealed record Arr<T> : ArrBase<Arr<T>, T> {
    public Arr() : base() { }
    public Arr(params T?[] v) : base(v) { }
  }

  [FIgnore]
  class JsonFCollectionConverter : JsonConverter {
    static PropertyInfo GetCompositor(Type? t) {
      while (t != null) {
        var properties = t.GetProperties(BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.Name == "Composed");
        if (properties.Count() == 1) return properties.First();
        t = t.BaseType;
      }
      throw new JsonSerializationException();
    }

    public override bool CanConvert(Type? t) {
      var fields = t?.BaseType?.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.Name == "Composed") ?? [];
      return fields.Count() == 1;
    }

    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
      while (reader.TokenType == JsonToken.Comment && reader.Read()) { };
      if (reader.TokenType == JsonToken.Null) return null;
      var compositor = GetCompositor(objectType);
      var compositorType = compositor.PropertyType;
      var compositorValue = serializer.Deserialize(reader, compositorType) ?? throw new JsonSerializationException();
      try {
        return Activator.CreateInstance(objectType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, [compositorValue], null); // try ctor
      }
      catch (Exception) { // try empty ctor
        try {
          var instance = Activator.CreateInstance(objectType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, null, null);
          compositor.SetValue(instance, compositorValue);
          return instance;
        }
        catch (Exception) { // try calling New
          var factory = objectType.GetMethod("New", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy, [compositorType]);
          if (factory is null) throw;
          return factory.Invoke(null, [compositorValue]); // first try using New method if exists
        }
      }
    }

    public override void WriteJson(JsonWriter writer, object? o, JsonSerializer serializer) {
      if (o is null) return;
      var compositor = GetCompositor(o.GetType());
      var value = compositor.GetValue(o);
      serializer.Serialize(writer, value);
    }
  }
}

namespace System.Linq {
  using F.Collections;

  public static class CollectionsExtensionsMethods {
    public static Lst<T> ToLst<T>(this IEnumerable<T> source) => LstBase<Lst<T>, T>.New(source.ToImmutableList());

    public static Set<T> ToSet<T>(this IEnumerable<T> source) => SetBase<Set<T>, T>.New(source.ToImmutableHashSet());

    public static OrderedSet<T> ToOrderedSet<T>(this IEnumerable<T> source) => OrderedSetBase<OrderedSet<T>, T>.New(source.ToImmutableSortedSet());

    public static Map<TKey, TValue> ToMap<TKey, TValue>(this IEnumerable<(TKey key, TValue val)> source) where TKey : notnull => Map<TKey, TValue>.New(source.Aggregate(ImmutableDictionary<TKey, TValue>.Empty, (t, vt) => t.SetItem(vt.key, vt.val)));
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this IEnumerable<(TKey key, TValue val)?> source) where TKey : notnull => Map<TKey, TValue>.New(source.Aggregate(ImmutableDictionary<TKey, TValue>.Empty, (t, vt) => vt is null ? t : t.SetItem(vt.Value.key, vt.Value.val)));
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull => Map<TKey, TValue>.New(source.Aggregate(ImmutableDictionary<TKey, TValue>.Empty, (t, kvp) => t.SetItem(kvp.Key, kvp.Value)));
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>?> source) where TKey : notnull => Map<TKey, TValue>.New(source.Aggregate(ImmutableDictionary<TKey, TValue>.Empty, (t, kvp) => kvp is null ? t : t.SetItem(kvp.Value.Key, kvp.Value.Value)));
    public static Map<TKey, TValue> ToMap<TKey, TValue, TSource>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) where TKey : notnull => Map<TKey, TValue>.New(source.ToImmutableDictionary(keySelector, valueSelector));

    public static OrderedMap<TKey, TValue> ToOrderedMap<TKey, TValue>(this IEnumerable<(TKey key, TValue val)> source) where TKey : notnull => OrderedMap<TKey, TValue>.New(source.Aggregate(ImmutableSortedDictionary<TKey, TValue>.Empty, (t, vt) => t.SetItem(vt.key, vt.val)));
    public static OrderedMap<TKey, TValue> ToOrderedMap<TKey, TValue>(this IEnumerable<(TKey key, TValue val)?> source) where TKey : notnull => OrderedMap<TKey, TValue>.New(source.Aggregate(ImmutableSortedDictionary<TKey, TValue>.Empty, (t, vt) => vt is null ? t : t.SetItem(vt.Value.key, vt.Value.val)));
    public static OrderedMap<TKey, TValue> ToOrderedMap<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull => OrderedMap<TKey, TValue>.New(source.Aggregate(ImmutableSortedDictionary<TKey, TValue>.Empty, (t, kvp) => t.SetItem(kvp.Key, kvp.Value)));
    public static OrderedMap<TKey, TValue> ToOrderedMap<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>?> source) where TKey : notnull => OrderedMap<TKey, TValue>.New(source.Aggregate(ImmutableSortedDictionary<TKey, TValue>.Empty, (t, kvp) => kvp is null ? t : t.SetItem(kvp.Value.Key, kvp.Value.Value)));
    public static OrderedMap<TKey, TValue> ToOrderedMap<TKey, TValue, TSource>(this IEnumerable<TSource> source, Func<TSource, TKey> keySelector, Func<TSource, TValue> valueSelector) where TKey : notnull => OrderedMap<TKey, TValue>.New(source.ToImmutableSortedDictionary(keySelector, valueSelector));

    public static Que<T> ToQue<T>(this IEnumerable<T> source) => QueBase<Que<T>, T>.New(source.Aggregate(ImmutableQueue<T>.Empty, (t, v) => t.Enqueue(v)));

    public static Arr<T> ToArr<T>(this IEnumerable<T> source) => ArrBase<Arr<T>, T>.New(source.ToImmutableArray());
  }
}



