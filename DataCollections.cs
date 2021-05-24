using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using F;
using Newtonsoft.Json;
using static F.Data;

// https://github.com/kofifus/F/wiki

#nullable enable

namespace F {
  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Lst<T> : IEnumerable<T>, IEnumerable {
    [FIgnore] readonly ImmutableList<T> Composed;
    [FIgnore] HashCode? HashCache;

    Lst(ImmutableList<T> composed, HashCode? hashCode = null) => (Composed, HashCache) = (composed, hashCode);

    public Lst() : this(ImmutableList<T>.Empty) { }
    public Lst(params T[] p) : this(new Lst<T>().AddRange(p)) { }
    public Lst(params IEnumerable<T>?[] p) : this(new Lst<T>().AddRange(p)) { }

    public override string ToString() => ToString(',');
    public string ToString(char separator) => Composed.IsEmpty ? "" : Composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : separator)}{next?.ToString() ?? ""}");

    public bool Equals(Lst<T>? obj) => obj is object && GetHashCode() == obj.GetHashCode() && Composed.SequenceEqual(obj.Composed);
    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new HashCode();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static Lst<T> operator +(Lst<T> o, T v) => o.Add(v);
    public static Lst<T> operator +(Lst<T> o, Lst<T> items) => o.AddRange(items);
    public static Lst<T> operator +(Lst<T> o, Set<T> items) => o.AddRange(items);
    public static Lst<T> operator +(Lst<T> o, Arr<T> items) => o.AddRange(items);
    public static Lst<T> operator +(Lst<T> o, Que<T> items) => o.AddRange(items);
    public static Lst<T> operator -(Lst<T> o, T v) => o.Remove(v);
    public static Lst<T> operator -(Lst<T> o, Lst<T> items) => o.RemoveRange(items);
    public static Lst<T> operator -(Lst<T> o, Set<T> items) => o.RemoveRange(items);
    public static Lst<T> operator -(Lst<T> o, Arr<T> items) => o.RemoveRange(items);
    public static Lst<T> operator -(Lst<T> o, Que<T> items) => o.RemoveRange(items);
    public static Lst<T> operator -(Lst<T> o, Predicate<T> match) => o.RemoveAll(match);

    // use this if T is a non-nullable reference type
    public T? this[int index] { get {
        if (index < 0 || index >= Count) return default;
        try { return Composed[index]; } catch { return default; }
      } }

    // use this if T is not a non-nullable reference type (ie long or Manager?)
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out T value) {
      if (index < 0 || index >= Count) { value = default; return false; };
      try { value = Composed[index]; return true; } catch { value = default; return false; }
    }

    // use this if T is a non-nullable reference type
    public T? Find(Predicate<T> match) => Composed.Find(match);

    // use this if T is nun a non-nullable reference type (ie long or Manager?)
    public bool Find(Predicate<T> match, [MaybeNullWhen(false)] out T value) {
      var index = Composed.FindIndex(match);
      value = index > -1 ? Composed[index] : default;
      return index > -1;
    }

    // use this if T is a non-nullable reference type
    public T? FindLast(Predicate<T> match) => Composed.FindLast(match);

    // use this if T is nut a non-nullable reference type (ie long or Manager?)
    public bool FindLast(Predicate<T> match, [MaybeNullWhen(false)] out T value) {
      var index = Composed.FindLastIndex(match);
      value = index > -1 ? Composed[index] : default;
      return index > -1;
    }


    // the rest of the methods just proxy to Composed

    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    public int Count { get { return Composed.Count; } }

    public Lst<T> Add(T value) {
      var res = new Lst<T>(Composed.Add(value), HashCache);
      if (res.HashCache is object) res.HashCache.Value.Add(value);
      return res;
    }

    public Lst<T> AddRange(IEnumerable<T>? items) {
      if (items is null) return this;
      var res = new Lst<T>(Composed.AddRange(items), HashCache);
      if (res.HashCache is object) foreach (var v in items) res.HashCache.Value.Add(v);
      return res;
    }

    public Lst<T> AddRange(params IEnumerable<T>?[] items) {
      var composed = Composed;
      var hash = HashCache;

      foreach (var item in items) {
        if (item is null) continue;
        composed = composed.AddRange(item);
        if (hash is object) foreach (var v in item) hash.Value.Add(v);
      }

      return new Lst<T>(composed, hash);
    }

    public int BinarySearch(T item) => Composed.BinarySearch(item);
    public int BinarySearch(T item, IComparer<T> comparer) => Composed.BinarySearch(item, comparer);
    public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => Composed.BinarySearch(index, count, item, comparer);
    public bool Contains(T value) => Composed.Contains(value);
    public bool Equals(IEnumerable<T> second, IEqualityComparer<T> comparer) => Composed.SequenceEqual(second, comparer);
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => Composed.CopyTo(index, array, arrayIndex, count);
    public void CopyTo(T[] array) => Composed.CopyTo(array);
    public void CopyTo(T[] array, int arrayIndex) => Composed.CopyTo(array, arrayIndex);
    public bool Exists(Predicate<T> match) => Composed.Exists(match);

    public Lst<T> FindAll(Predicate<T> match) => new(Composed.FindAll(match));
    public int FindIndex(Predicate<T> match) => Composed.FindIndex(match);
    public int FindIndex(int startIndex, Predicate<T> match) => Composed.FindIndex(startIndex, match);
    public int FindIndex(int startIndex, int count, Predicate<T> match) => Composed.FindIndex(startIndex, count, match);
    public int FindLastIndex(int startIndex, int count, Predicate<T> match) => Composed.FindLastIndex(startIndex, count, match);
    public int FindLastIndex(int startIndex, Predicate<T> match) => Composed.FindLastIndex(startIndex, match);
    public int FindLastIndex(Predicate<T> match) => Composed.FindLastIndex(match);
    public void ForEach(Action<T> action) => Composed.ForEach(action);
    public Lst<T> GetRange(int index, int count) => new(Composed.GetRange(index, count));
    public int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => Composed.IndexOf(item, index, count, equalityComparer);
    public int IndexOf(T value) => Composed.IndexOf(value);

    public Lst<T>? Insert(int index, T item) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.Insert(index, item)); } catch { return null; }
    }

    public Lst<T>? Insert(int index, IEnumerable<T> items) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.InsertRange(index, items)); } catch { return null; }
    }

    public ref readonly T ItemRef(int index) => ref Composed.ItemRef(index);
    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => Composed.LastIndexOf(item, index, count, equalityComparer);
    public Lst<T> Remove(T value) => new(Composed.Remove(value));
    public Lst<T> Remove(T value, IEqualityComparer<T> equalityComparer) => new(Composed.Remove(value, equalityComparer));
    public Lst<T> RemoveAll(Predicate<T> match) => new(Composed.RemoveAll(match));

    public Lst<T> RemoveAt(int index) {
      if (index < 0 || index >= Count) return this;
      try { return new(Composed.RemoveAt(index)); } catch { return this; }
    }

    public Lst<T> RemoveRange(IEnumerable<T> items) => new(Composed.RemoveRange(items));
    public Lst<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer) => new(Composed.RemoveRange(items, equalityComparer));

    public Lst<T> RemoveRange(int index, int count) {
      if (index < 0 || index >= Count) return this;
      try { return new(Composed.RemoveRange(index, count)); } catch { return this; }
    }

    public Lst<T> Replace(T oldValue, T newValue) => new(Composed.Replace(oldValue, newValue));
    public Lst<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) => new(Composed.Replace(oldValue, newValue, equalityComparer));

    public Lst<T>? Reverse(int index, int count) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.Reverse(index, count)); } catch { return null; }
    }

    public Lst<T> Reverse() => new(Composed.Reverse());

    public Lst<T>? SetItem(int index, T value) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.SetItem(index, value)); } catch { return null; }
    }

    public Lst<T> Sort() => new(Composed.Sort());
    public Lst<T> Sort(IComparer<T> comparer) => new(Composed.Sort(comparer));
    public Lst<T> Sort(Comparison<T> comparison) => new(Composed.Sort(comparison));

    public Lst<T>? Sort(int index, int count, IComparer<T> comparer) {
      if (index < 0 || index >= Count) return default;
      try { return new(Composed.Sort(index, count, comparer)); } catch { return null; }
    }

    public ImmutableList<T>.Builder ToBuilder() => Composed.ToBuilder();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (Composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (Composed as IEnumerable).GetEnumerator();
    public Lst<T> Remove(IEnumerable<T> items) => RemoveRange(items);
    public IEnumerator<T> GetEnumerator() => (this as IEnumerable<T>).GetEnumerator();
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Set<T> : IEnumerable<T>, IEnumerable {
    [FIgnore] readonly ImmutableHashSet<T> Composed;
    [FIgnore] HashCode? HashCache;

    Set(ImmutableHashSet<T> composed) => (Composed, HashCache) = (composed, null);

    public Set() : this(ImmutableHashSet<T>.Empty) { }
    public Set(params T[] p) : this(new Set<T>().Union(p)) { }
    public Set(params IEnumerable<T>?[] p) : this(new Set<T>().Union(p)) { }

    public override string ToString() => ToString(',');
    public string ToString(char separator) => Composed.IsEmpty ? "" : Composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : separator)}{next?.ToString() ?? ""}");

    public bool Equals(Set<T>? obj) => obj is not null && GetHashCode() == obj.GetHashCode() && Composed.SetEquals(((Set<T>)obj).Composed);
    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new HashCode();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public Set<T> RemoveAll(Predicate<T> match) => new(this.Where(v => !match(v)));

    public static Set<T> operator +(Set<T> o, T v) => o.Add(v);
    public static Set<T> operator +(Set<T> o, Set<T> other) => o.Union(other);
    public static Set<T> operator -(Set<T> o, T v) => o.Remove(v);
    public static Set<T> operator -(Set<T> o, IEnumerable<T> other) => o.Except(other);
    public static Set<T> operator -(Set<T> o, Predicate<T> match) => o.RemoveAll(match);

    public Set<T> Union(params IEnumerable<T>?[] items) {
      var res = Composed;
      foreach (var item in items) if (item is object) res = res.Union(item);
      return new(res);
    }

    // the rest of the methods just proxy to Composed

    public IEqualityComparer<T> KeyComparer { get { return Composed.KeyComparer; } }
    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }
    public int Count { get { return Composed.Count; } }

    public Set<T> Add(T item) => new(Composed.Add(item));
    public bool Contains(T item) => Composed.Contains(item);
    public Set<T> Except(IEnumerable<T> other) => new(Composed.Except(other));
    public Set<T> Intersect(IEnumerable<T> other) => new(Composed.Intersect(other));
    public bool IsProperSubsetOf(IEnumerable<T> other) => Composed.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<T> other) => Composed.IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<T> other) => Composed.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<T> other) => Composed.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<T> other) => Composed.Overlaps(other);
    public Set<T> Remove(T item) => new(Composed.Remove(item));
    public bool SetEquals(IEnumerable<T> other) => Composed.SetEquals(other);
    public Set<T> SymmetricExcept(IEnumerable<T> other) => new(Composed.SymmetricExcept(other));
    public Set<T> Union(IEnumerable<T> other) => new(Composed.Union(other));
    public ImmutableHashSet<T>.Builder ToBuilder() => Composed.ToBuilder();
    public IEnumerator<T> GetEnumerator() => Composed.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (Composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (Composed as IEnumerable).GetEnumerator();
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Map<TKey, TValue> : IEnumerable<(TKey, TValue)>, IEnumerable where TKey : notnull {
    [FIgnore] readonly ImmutableDictionary<TKey, TValue> Composed;
    [FIgnore] HashCode? HashCache;

    Map(ImmutableDictionary<TKey, TValue> composed) => (Composed, HashCache) = (composed, null);

    public Map() : this(ImmutableDictionary<TKey, TValue>.Empty) { }
    public Map(TKey key, TValue val) : this(new Map<TKey, TValue>().Add(key, val)) { }
    public Map(params (TKey key, TValue val)?[] items) : this(new Map<TKey, TValue>().SetItems(items)) { }
    public Map(params IEnumerable<(TKey key, TValue val)>?[] items) : this(new Map<TKey, TValue>().SetItems(items)) { }
    public Map(params IEnumerable<KeyValuePair<TKey, TValue>>?[] items) : this(new Map<TKey, TValue>().SetItems(items)) { }

    public override string ToString() => ToString(','); 
    public string ToString(char separator) => Composed.IsEmpty ? "" : Composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : separator)}{{{next.Key?.ToString() ?? ""},{next.Value?.ToString() ?? ""}}}");

    public bool Equals(Map<TKey, TValue>? obj) {
      if (obj is null || GetHashCode() != obj.GetHashCode()) return false;
      if (Count != obj.Count) return false;
      foreach (var (d1key, d1value) in Composed) {
        if (!obj.Composed.TryGetValue(d1key, out TValue? d2value)) return false;
        if (!object.Equals(d1value, d2value)) return false;
      }
      return true;
    }
    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new HashCode();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public Map<TKey, TValue> Remove(Func<TKey, TValue, bool> match) {
      bool pred((TKey, TValue) vt) => match(vt.Item1, vt.Item2);
      if (match is null) throw new ArgumentNullException(nameof(match));
      return new Map<TKey, TValue>(this.Where(kv => !pred(kv)));
    }

    public static Map<TKey, TValue> operator +(Map<TKey, TValue> o, (TKey key, TValue val) vt) => o.SetItem(vt.key, vt.val);
    public static Map<TKey, TValue> operator +(Map<TKey, TValue> o, Map<TKey, TValue> pairs) => o.SetItems(pairs);
    public static Map<TKey, TValue> operator -(Map<TKey, TValue> o, TKey key) => o.Remove(key);
    public static Map<TKey, TValue> operator -(Map<TKey, TValue> o, Set<TKey> keys) => new(o.Composed.RemoveRange(keys));
    public static Map<TKey, TValue> operator -(Map<TKey, TValue> o, Func<TKey, TValue, bool> match) => o.Remove(match);

    // use this if T is a non-nullable reference type
    public TValue? this[TKey key] => Composed.TryGetValue(key, out var value) ? value : default;

    public Map<TKey, TValue> SetItems(params (TKey key, TValue val)?[] items) {
      var res = Composed;
      foreach (var item in items) if (item is object) res = res.SetItem(item.Value.key, item.Value.val);
      return new(res);
    }

    public Map<TKey, TValue> SetItems(params IEnumerable<(TKey key, TValue val)>?[] items) {
      var res = Composed;
      foreach (var item in items) if (item is object) foreach (var (key, val) in item) res = res.SetItem(key, val);
      return new(res);
    }

    public Map<TKey, TValue> SetItems(params IEnumerable<KeyValuePair<TKey, TValue>>?[] items) {
      var res = Composed;
      foreach (var item in items) if (item is object) foreach (var kvp in item) res = res.SetItem(kvp.Key, kvp.Value);
      return new(res);
    }


    // the rest of the methods just proxy to Composed

    public IEqualityComparer<TKey> KeyComparer { get { return Composed.KeyComparer; } }
    public IEnumerable<TKey> Keys { get { return Composed.Keys; } }
    public IEqualityComparer<TValue> ValueComparer { get { return Composed.ValueComparer; } }
    public IEnumerable<TValue> Values { get { return Composed.Values; } }
    public int Count { get { return Composed.Count; } }

    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    public Map<TKey, TValue> Add(TKey key, TValue value) => new(Composed.Add(key, value));
    public Map<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>>? pairs) => pairs is null ? this : new(Composed.AddRange(pairs));

    public bool Contains(KeyValuePair<TKey, TValue> pair) => Composed.Contains(pair);
    public bool ContainsKey(TKey key) => Composed.ContainsKey(key);
    public bool ContainsValue(TValue value) => Composed.ContainsValue(value);

    public Map<TKey, TValue> Remove(TKey key) => new(Composed.Remove(key));
    public Map<TKey, TValue> RemoveRange(IEnumerable<TKey> keys) => new(Composed.RemoveRange(keys));

    public Map<TKey, TValue> SetItem(TKey key, TValue value) => new(Composed.SetItem(key, value));
    public Map<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items) => new(Composed.SetItems(items));

    public ImmutableDictionary<TKey, TValue>.Builder ToBuilder() => Composed.ToBuilder();

    IEnumerator<(TKey, TValue)> IEnumerable<(TKey, TValue)>.GetEnumerator() {
      var e = Composed.GetEnumerator();
      while (e.MoveNext()) yield return (e.Current.Key, e.Current.Value);
    }
    IEnumerator IEnumerable.GetEnumerator() => (Composed as IEnumerable).GetEnumerator();
    public IEnumerator<(TKey, TValue)> GetEnumerator() => (this as IEnumerable<(TKey, TValue)>).GetEnumerator();

    public bool TryGetKey(TKey equalKey, out TKey actualKey) => Composed.TryGetKey(equalKey, out actualKey);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => Composed.TryGetValue(key, out value);
  }

  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Que<T> : IEnumerable<T>, IEnumerable {
    [FIgnore] readonly ImmutableQueue<T> Composed;
    [FIgnore] HashCode? HashCache;

    Que(ImmutableQueue<T> composed, HashCode? hashCode = null) => (Composed, HashCache) = (composed, hashCode);

    public Que() : this(ImmutableQueue<T>.Empty) { }
    public Que(params T[] p) : this(new Que<T>().Enqueue(p)) { }
    public Que(params IEnumerable<T>?[] p) : this(new Que<T>().Enqueue(p)) { }

    public override string ToString() => ToString(',');
    public string ToString(char separator) => Composed.IsEmpty ? "" : Composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : separator)}{next?.ToString() ?? ""}");

    public bool Equals(Que<T>? obj) => obj is not null && GetHashCode() == obj.GetHashCode() && Composed.SequenceEqual(((Que<T>)obj).Composed);
    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new HashCode();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static Que<T> operator +(Que<T> o, T v) => o.Enqueue(v);

    public T? Peek() {
      if (IsEmpty) return default;
      try { return Composed.Peek(); } catch { return default; }
    }

    // use this if T is nut a non-nullable reference type (ie long or Manager?)
    public bool TryPeek([MaybeNullWhen(false)] out T value) {
      if (IsEmpty) { value = default; return false; }
      try { value = Composed.Peek(); return true; } catch { value = default; return false; }
    }

    public Que<T> Enqueue(params IEnumerable<T>?[] items) {
      var composed = Composed;
      var hash = HashCache;

      foreach (var item in items) {
        if (item is null) continue;
        composed = composed.Aggregate(ImmutableQueue<T>.Empty, (total, next) => total.Enqueue(next));
        if (hash is object) foreach (var v in item) hash.Value.Add(v);
      }

      return new Que<T>(composed, hash);
    }

    // the rest of the methods just proxy to Composed
    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    public int Count { get { return Composed.Count(); } } // todo cache

    public (Que<T>, T v) Dequeue() { var newq = Composed.Dequeue(out T v); return (new(newq), v); }
    public Que<T> Enqueue(T v) {
      var res = new Que<T>(Composed.Enqueue(v), HashCache);
      if (res.HashCache is object) res.HashCache.Value.Add(v);
      return res;
    }
    public Que<T> Enqueue(IEnumerable<T> v) => v.Aggregate(new Que<T>(), (total, next) => total.Enqueue(next));

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (Composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (Composed as IEnumerable).GetEnumerator();
  }

  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Arr<T> : IEnumerable<T>, IEnumerable {
    [FIgnore] readonly ImmutableArray<T> Composed;
    [FIgnore] HashCode? HashCache;

    Arr(ImmutableArray<T> composed, HashCode? hashCode = null) => (Composed, HashCache) = (composed, hashCode);

    public Arr() : this(ImmutableArray<T>.Empty) { }
    public Arr(params T[] p) : this(new Arr<T>().AddRange(p)) { }
    public Arr(params IEnumerable<T>?[] p) : this(new Arr<T>().AddRange(p)) { }

    public override string ToString() => ToString(',');
    public string ToString(char separator) => Composed.IsEmpty ? "" : Composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : separator)}{next?.ToString() ?? ""}");

    public bool Equals(Arr<T>? obj) => obj is not null && GetHashCode() == obj.GetHashCode() && Composed.SequenceEqual(((Arr<T>)obj).Composed);
    override public int GetHashCode() {
      if (HashCache is null) {
        HashCache = new HashCode();
        foreach (var v in Composed) HashCache.Value.Add(v);
      }
      return HashCache.Value.ToHashCode();
    }

    public static Arr<T> operator +(Arr<T> o, T v) => o.Add(v);
    public static Arr<T> operator +(Arr<T> o, Arr<T> items) => o.AddRange(items);
    public static Arr<T> operator -(Arr<T> o, T v) => o.Remove(v);
    public static Arr<T> operator -(Arr<T> o, Arr<T> items) => o.RemoveRange(items);
    public static Arr<T> operator -(Arr<T> o, Predicate<T> match) => o.RemoveAll(match);

    public T? this[int index] { get {
        if (index < 0 || index >= Count) return default;
        try { return Composed[index]; } catch { return default; }
      } }

    // use this if T is nut a non-nullable reference type (ie long or Manager?)
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out T value) {
      if (index < 0 || index >= Count) { value = default; return false; }
      try { value = Composed[index]; return true; } catch { value = default; return false; }
    }

    public int Count { get { return Composed.Length; } }

    public Arr<T>? Insert(int index, T item) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.Insert(index, item)); } catch { return null; }
    }

    public Arr<T>? RemoveAt(int index) {
      if (index < 0 || index >= Count) return this;
      try { return new(Composed.RemoveAt(index)); } catch { return null; }
    }

    public Arr<T>? InsertRange(int index, IEnumerable<T> items) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.InsertRange(index, items)); } catch { return null; }
    }

    public Arr<T>? InsertRange(int index, Arr<T> items) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.InsertRange(index, items.Composed)); } catch { return null; }
    }

    public Arr<T>? RemoveRange(int index, int count) {
      if (index < 0 || index >= Count) return this;
      try { return new(Composed.RemoveRange(index, count)); } catch { return null; }
    }

    public Arr<T>? SetItem(int index, T value) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.SetItem(index, value)); } catch { return null; }
    }

    public Arr<T>? Sort(int index, int count, IComparer<T> comparer) {
      if (index < 0 || index >= Count) return null;
      try { return new(Composed.Sort(index, count, comparer)); } catch { return null; }
    }

    public Arr<T> AddRange(params IEnumerable<T>?[] items) {
      var composed = Composed;
      var hash = HashCache;

      foreach (var item in items) {
        if (item is null) continue;
        composed = composed.AddRange(item);
        if (hash is object) foreach (var v in item) hash.Value.Add(v);
      }

      return new Arr<T>(composed, hash);
    }

    // the rest of the methods just proxy to Composed

    public bool IsEmpty { get { return Composed.IsEmpty; } }
    public bool NotEmpty { get { return !IsEmpty; } }

    public int Length { get { return Composed.Length; } }
    public bool IsDefaultOrEmpty { get { return Composed.IsDefaultOrEmpty; } }
    public bool IsDefault { get { return Composed.IsDefault; } }
    public Arr<T> Add(T value) {
      var res = new Arr<T>(Composed.Add(value), HashCache);
      if (res.HashCache is object) res.HashCache.Value.Add(value);
      return res;
    }
    public Arr<T> AddRange(IEnumerable<T>? items) {
      if (items is null) return this;
      var res = new Arr<T>(Composed.AddRange(items), HashCache);
      if (res.HashCache is object) foreach (var v in items) res.HashCache.Value.Add(v);
      return res;
    }
    public Arr<T> AddRange(Arr<T>? items) => items is null ? this : new(Composed.AddRange(items.Composed));
    public Arr<TOther> As<TOther>() where TOther : class => new(Composed.As<TOther>());
    public ReadOnlyMemory<T> AsMemory() => Composed.AsMemory();
    public ReadOnlySpan<T> AsSpan() => Composed.AsSpan();
    public int BinarySearch(T value) => Composed.BinarySearch(value);
    public int BinarySearch(T value, IComparer<T> comparer) => Composed.BinarySearch(value, comparer);
    public int BinarySearch(int index, int length, T value) => Composed.BinarySearch(index, length, value);
    public int BinarySearch(int index, int length, T value, IComparer<T> comparer) => Composed.BinarySearch(index, length, value, comparer);
    public Arr<TOther> CastArray<TOther>() where TOther : class => new(Composed.CastArray<TOther>());
    public bool Contains(T value) => Composed.Contains(value);
    public static Arr<T> CreateRange(IEnumerable<T> items) => new(ImmutableArray.CreateRange(items));
    public static Arr<TResult> CreateRange<TResult>(ImmutableArray<T> items, Func<T, TResult> selector) => new(ImmutableArray.CreateRange(items, selector));
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
    public Arr<T> Remove(T value) => new(Composed.Remove(value));
    public Arr<T> Remove(T value, IEqualityComparer<T> equalityComparer) => new(Composed.Remove(value, equalityComparer));
    public Arr<T> RemoveAll(Predicate<T> match) => new(Composed.RemoveAll(match));
    public Arr<T> RemoveRange(IEnumerable<T> items) => new(Composed.RemoveRange(items));
    public Arr<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer) => new(Composed.RemoveRange(items, equalityComparer));
    public Arr<T> RemoveRange(Arr<T> items) => new(Composed.RemoveRange(items.Composed));
    public Arr<T> RemoveRange(Arr<T> items, IEqualityComparer<T> equalityComparer) => new(Composed.RemoveRange(items.Composed, equalityComparer));
    public Arr<T> Replace(T oldValue, T newValue) => new(Composed.Replace(oldValue, newValue));
    public Arr<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) => new(Composed.Replace(oldValue, newValue, equalityComparer));
    public Arr<T> Sort(IComparer<T> comparer) => new(Composed.Sort(comparer));
    public Arr<T> Sort(Comparison<T> comparison) => new(Composed.Sort(comparison));
    public Arr<T> Sort() => new(Composed.Sort());
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (Composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (Composed as IEnumerable).GetEnumerator();
    public ImmutableArray<T>.Builder ToBuilder() => Composed.ToBuilder();
  }


  class JsonFCollectionConverter : JsonConverter {
    static FieldInfo GetCompositorField(Type? t) {
      while (t != null) {
        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.Name == "Composed");
        if (fields.Count() == 1) return fields.First();
        t = t.BaseType;
      }
      throw new JsonSerializationException();
    }

    public override bool CanConvert(Type? t) {
      var fields = t?.BaseType?.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.Name == "Composed") ?? Enumerable.Empty<FieldInfo>();
      return fields.Count() == 1;
    }

    // assumes Compositor<T> has either a constructor accepting T or an empty constructor
    public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer) {
      while (reader.TokenType == JsonToken.Comment && reader.Read()) { };
      if (reader.TokenType == JsonToken.Null) return null;
      var compositorField = GetCompositorField(objectType);
      var compositorType = compositorField.FieldType;
      var compositorValue = serializer.Deserialize(reader, compositorType);
      if (compositorValue is null) throw new JsonSerializationException();
      return Activator.CreateInstance(objectType, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, new object[] { compositorValue }, null);
    }

    public override void WriteJson(JsonWriter writer, object? o, JsonSerializer serializer) {
      if (o is null) return;
      var compositorField = GetCompositorField(o.GetType());
      var value = compositorField.GetValue(o);
      serializer.Serialize(writer, value);
    }
  }
}

namespace System.Linq {
  public static class CollectionsExtensionsMethods {

    public static Lst<TSource> ToLst<TSource>(this IEnumerable<TSource> source) => new(source);
    public static Lst<TSource> ToLst<TSource>(this (TSource, TSource) source) => new(source.Item1, source.Item2);
    public static Lst<TSource> ToLst<TSource>(this (TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3);
    public static Lst<TSource> ToLst<TSource>(this (TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4);
    public static Lst<TSource> ToLst<TSource>(this (TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5);
    public static Lst<TSource> ToLst<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6);
    public static Lst<TSource> ToLst<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7);

    public static Set<TSource> ToSet<TSource>(this IEnumerable<TSource> source) => new(source);
    public static Set<TSource> ToSet<TSource>(this (TSource, TSource) source) => new(source.Item1, source.Item2);
    public static Set<TSource> ToSet<TSource>(this (TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3);
    public static Set<TSource> ToSet<TSource>(this (TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4);
    public static Set<TSource> ToSet<TSource>(this (TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5);
    public static Set<TSource> ToSet<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6);
    public static Set<TSource> ToSet<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7);

    public static Map<TKey, TValue> ToMap<TKey, TValue>(this IEnumerable<(TKey key, TValue val)> source) where TKey : notnull => new(source);
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this IEnumerable<KeyValuePair<TKey, TValue>> source) where TKey : notnull => new(source);
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this ((TKey key, TValue val), (TKey key, TValue val)) source) where TKey : notnull => new(source.Item1, source.Item2);
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this ((TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val)) source) where TKey : notnull => new(source.Item1, source.Item2, source.Item3);
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this ((TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val)) source) where TKey : notnull => new(source.Item1, source.Item2, source.Item3, source.Item4);
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this ((TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val)) source) where TKey : notnull => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5);
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this ((TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val)) source) where TKey : notnull => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6);
    public static Map<TKey, TValue> ToMap<TKey, TValue>(this ((TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val), (TKey key, TValue val)) source) where TKey : notnull => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7);

    public static Que<TSource> ToQue<TSource>(this IEnumerable<TSource> source) => new(source);
    public static Que<TSource> ToQue<TSource>(this (TSource, TSource) source) => new(source.Item1, source.Item2);
    public static Que<TSource> ToQue<TSource>(this (TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3);
    public static Que<TSource> ToQue<TSource>(this (TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4);
    public static Que<TSource> ToQue<TSource>(this (TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5);
    public static Que<TSource> ToQue<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6);
    public static Que<TSource> ToQue<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7);

    public static Arr<TSource> ToArr<TSource>(this IEnumerable<TSource> source) => new(source);
    public static Arr<TSource> ToArr<TSource>(this (TSource, TSource) source) => new(source.Item1, source.Item2);
    public static Arr<TSource> ToArr<TSource>(this (TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3);
    public static Arr<TSource> ToArr<TSource>(this (TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4);
    public static Arr<TSource> ToArr<TSource>(this (TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5);
    public static Arr<TSource> ToArr<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6);
    public static Arr<TSource> ToArr<TSource>(this (TSource, TSource, TSource, TSource, TSource, TSource, TSource) source) => new(source.Item1, source.Item2, source.Item3, source.Item4, source.Item5, source.Item6, source.Item7);
  }
}



