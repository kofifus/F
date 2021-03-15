using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Net.WebSockets;
using System.Reflection;
using Newtonsoft.Json;
using static F.Data;

// https://github.com/kofifus/F/wiki

#nullable enable

namespace F {

  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Seq<T> : IEnumerable<T>, IEnumerable where T : notnull {
    [FIgnore] readonly ImmutableList<T> composed;
    [FIgnore] HashCode? hashCache;

    Seq(ImmutableList<T> composed, HashCode? hashCode=null) => (this.composed, this.hashCache) = (composed, hashCode);
    Seq(Seq<T> seq) => this.composed = seq.composed;

    public Seq() : this(ImmutableList<T>.Empty) { }
    public Seq(IEnumerable<T> t) : this(new Seq<T>().Add(t)) { }
    public Seq(params T[] p) : this(new Seq<T>().Add(p)) { }

    public override string ToString() => composed.IsEmpty ? "" : composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : ",")}{next?.ToString() ?? ""}");

    public bool Equals(Seq<T>? obj) => obj is not null && GetHashCode()==obj.GetHashCode() && composed.SequenceEqual(obj.composed);
    override public int GetHashCode() {
      if (hashCache is null) {
        hashCache = new HashCode();
        foreach (var v in composed) hashCache.Value.Add(v);
      }
      return hashCache.Value.ToHashCode();
    }

    static public Seq<T> operator +(Seq<T> o, T v) => o.Add(v);
    static public Seq<T> operator +(Seq<T> o, IEnumerable<T> items) => o.Add(items);
    static public Seq<T> operator -(Seq<T> o, T v) => o.Remove(v);
    static public Seq<T> operator -(Seq<T> o, IEnumerable<T> items) => o.RemoveRange(items);
    static public Seq<T> operator -(Seq<T> o, Predicate<T> match) => o.RemoveAll(match);

    // use this if T is a non-nullable reference type
    public T? this[int index] { get { 
        if (index < 0 || index >= Count) return default; 
        try { return composed[index]; } catch { return default; } 
    } }

    // use this if T is not a non-nullable reference type (ie long or Manager?)
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out T value) {
      if (index < 0 || index >= Count) { value = default; return false; };
      try { value = composed[index]; return true; } catch { value = default; return false; }
    }

    // use this if T is a non-nullable reference type
    public T? Find(Predicate<T> match) => composed.Find(match);

    // use this if T is nun a non-nullable reference type (ie long or Manager?)
    public bool Find(Predicate<T> match, [MaybeNullWhen(false)] out T value) {
      var index = composed.FindIndex(match);
      value = index > -1 ? composed[index] : default;
      return index > -1;
    }

    // use this if T is a non-nullable reference type
    public T? FindLast(Predicate<T> match) => composed.FindLast(match);

    // use this if T is nut a non-nullable reference type (ie long or Manager?)
    public bool FindLast(Predicate<T> match, [MaybeNullWhen(false)] out T value) {
      var index = composed.FindLastIndex(match);
      value = index > -1 ? composed[index] : default;
      return index > -1;
    }


    // the rest of the methods just proxy to composed

    public bool IsEmpty { get { return composed.IsEmpty; } }
    public int Count { get { return composed.Count; } }

    public Seq<T> Add(T value) { 
      var res = new Seq<T>(composed.Add(value), hashCache); 
      if (res.hashCache is object) res.hashCache.Value.Add(value); 
      return res; 
    }
    public Seq<T> Add(IEnumerable<T> items) { 
      var res = new Seq<T>(composed.AddRange(items), hashCache);
      if (res.hashCache is object) foreach (var v in items) res.hashCache.Value.Add(v); 
      return res;
    }
    public int BinarySearch(T item) => composed.BinarySearch(item);
    public int BinarySearch(T item, IComparer<T> comparer) => composed.BinarySearch(item, comparer);
    public int BinarySearch(int index, int count, T item, IComparer<T> comparer) => composed.BinarySearch(index, count, item, comparer);
    public bool Contains(T value) => composed.Contains(value);
    public bool Equals(IEnumerable<T> second, IEqualityComparer<T> comparer) => composed.SequenceEqual(second, comparer);
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => composed.CopyTo(index, array, arrayIndex, count);
    public void CopyTo(T[] array) => composed.CopyTo(array);
    public void CopyTo(T[] array, int arrayIndex) => composed.CopyTo(array, arrayIndex);
    public bool Exists(Predicate<T> match) => composed.Exists(match);

    public Seq<T> FindAll(Predicate<T> match) => new(composed.FindAll(match));
    public int FindIndex(Predicate<T> match) => composed.FindIndex(match);
    public int FindIndex(int startIndex, Predicate<T> match) => composed.FindIndex(startIndex, match);
    public int FindIndex(int startIndex, int count, Predicate<T> match) => composed.FindIndex(startIndex, count, match);
    public int FindLastIndex(int startIndex, int count, Predicate<T> match) => composed.FindLastIndex(startIndex, count, match);
    public int FindLastIndex(int startIndex, Predicate<T> match) => composed.FindLastIndex(startIndex, match);
    public int FindLastIndex(Predicate<T> match) => composed.FindLastIndex(match);
    public void ForEach(Action<T> action) => composed.ForEach(action);
    public Seq<T> GetRange(int index, int count) => new(composed.GetRange(index, count));
    public int IndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => composed.IndexOf(item, index, count, equalityComparer);
    public int IndexOf(T value) => composed.IndexOf(value);

    public Seq<T>? Insert(int index, T item) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.Insert(index, item)); } catch { return null; }
    }
    
    public Seq<T>? Insert(int index, IEnumerable<T> items) {
      if (index < 0 || index >= Count) return null; 
      try { return new(composed.InsertRange(index, items)); } catch { return null; }
    }

    public ref readonly T ItemRef(int index) => ref composed.ItemRef(index);
    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => composed.LastIndexOf(item, index, count, equalityComparer);
    public Seq<T> Remove(T value) => new(composed.Remove(value));
    public Seq<T> Remove(T value, IEqualityComparer<T> equalityComparer) => new(composed.Remove(value, equalityComparer));
    public Seq<T> RemoveAll(Predicate<T> match) => new(composed.RemoveAll(match));

    public Seq<T> RemoveAt(int index) {
      if (index < 0 || index >= Count) return this;
      try { return new(composed.RemoveAt(index)); } catch { return this; } 
    }

    public Seq<T> RemoveRange(IEnumerable<T> items) => new(composed.RemoveRange(items));
    public Seq<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer) => new(composed.RemoveRange(items, equalityComparer));
    
    public Seq<T> RemoveRange(int index, int count) {
      if (index < 0 || index >= Count) return this;
      try { return new(composed.RemoveRange(index, count)); } catch { return this; } 
    }

    public Seq<T> Replace(T oldValue, T newValue) => new(composed.Replace(oldValue, newValue));
    public Seq<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) => new(composed.Replace(oldValue, newValue, equalityComparer));
    
    public Seq<T>? Reverse(int index, int count) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.Reverse(index, count)); } catch { return null; } 
    }

    public Seq<T> Reverse() => new(composed.Reverse());
    
    public Seq<T>? SetItem(int index, T value) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.SetItem(index, value)); } catch { return null; } 
    }
    
    public Seq<T> Sort(IComparer<T> comparer) => new(composed.Sort(comparer));
    public Seq<T> Sort(Comparison<T> comparison) => new(composed.Sort(comparison));
    
    public Seq<T>? Sort(int index, int count, IComparer<T> comparer) {
      if (index < 0 || index >= Count) return default;
      try { return new(composed.Sort(index, count, comparer)); } catch { return null; } 
    }
    public Seq<T> Sort() => new(composed.Sort());
    public ImmutableList<T>.Builder ToBuilder() => composed.ToBuilder();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (composed as IEnumerable).GetEnumerator();
    public Seq<T> Remove(IEnumerable<T> items) => RemoveRange(items);
    public IEnumerator<T> GetEnumerator() => (this as IEnumerable<T>).GetEnumerator();
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Set<T> : IEnumerable<T>, IEnumerable {
    [FIgnore] readonly ImmutableHashSet<T> composed;
    [FIgnore] HashCode? hashCache;

    Set(ImmutableHashSet<T> composed) => this.composed = composed;
    Set(Set<T> vset) => this.composed = vset.composed;

    public Set() : this(ImmutableHashSet<T>.Empty) { }
    public Set(IEnumerable<T> t) : this(new Set<T>().Union(t)) { }
    public Set(params T[] p) : this(new Set<T>().Union(p)) { }

    public override string ToString() => composed.IsEmpty ? "" : composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : ",")}{next?.ToString() ?? ""}");

    public bool Equals(Set<T>? obj) => obj is not null && GetHashCode() == obj.GetHashCode() && composed.SetEquals(((Set<T>)obj).composed);
    override public int GetHashCode() {
      if (hashCache is null) {
        hashCache = new HashCode();
        foreach (var v in composed) hashCache.Value.Add(v);
      }
      return hashCache.Value.ToHashCode();
    }

    public Set<T> RemoveAll(Predicate<T> match) => new(this.Where(v => !match(v)));

    static public Set<T> operator +(Set<T> o, T v) => o.Add(v);
    static public Set<T> operator +(Set<T> o, IEnumerable<T> other) => o.Union(other);
    static public Set<T> operator -(Set<T> o, T v) => o.Remove(v);
    static public Set<T> operator -(Set<T> o, IEnumerable<T> other) => o.Except(other);
    static public Set<T> operator -(Set<T> o, Predicate<T> match) => o.RemoveAll(match);

    // the rest of the methods just proxy to composed

    public IEqualityComparer<T> KeyComparer { get { return composed.KeyComparer; } }
    public bool IsEmpty { get { return composed.IsEmpty; } }
    public int Count { get { return composed.Count; } }

    public Set<T> Add(T item) => new(composed.Add(item));
    public bool Contains(T item) => composed.Contains(item);
    public Set<T> Except(IEnumerable<T> other) => new(composed.Except(other));
    public Set<T> Intersect(IEnumerable<T> other) => new(composed.Intersect(other));
    public bool IsProperSubsetOf(IEnumerable<T> other) => composed.IsProperSubsetOf(other);
    public bool IsProperSupersetOf(IEnumerable<T> other) => composed.IsProperSupersetOf(other);
    public bool IsSubsetOf(IEnumerable<T> other) => composed.IsSubsetOf(other);
    public bool IsSupersetOf(IEnumerable<T> other) => composed.IsSupersetOf(other);
    public bool Overlaps(IEnumerable<T> other) => composed.Overlaps(other);
    public Set<T> Remove(T item) => new(composed.Remove(item));
    public bool SetEquals(IEnumerable<T> other) => composed.SetEquals(other);
    public Set<T> SymmetricExcept(IEnumerable<T> other) => new(composed.SymmetricExcept(other));
    public Set<T> Union(IEnumerable<T> other) => new(composed.Union(other));
    public ImmutableHashSet<T>.Builder ToBuilder() => composed.ToBuilder();
    public IEnumerator<T> GetEnumerator() => composed.GetEnumerator();
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (composed as IEnumerable).GetEnumerator();
  }


  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Map<TKey, TValue> : IEnumerable<(TKey, TValue)>, IEnumerable where TKey : notnull {
    [FIgnore] readonly ImmutableDictionary<TKey, TValue> composed;
    [FIgnore] HashCode? hashCache;

    Map(ImmutableDictionary<TKey, TValue> composed) => this.composed = composed;
    Map(Map<TKey, TValue> map) => this.composed = map.composed;

    public Map() : this(ImmutableDictionary<TKey, TValue>.Empty) { }
    public Map(IEnumerable<(TKey key, TValue val)> pairs) : this(new Map<TKey, TValue>() + pairs) { }
    public Map(IEnumerable<KeyValuePair<TKey, TValue>> pairs) : this(new Map<TKey, TValue>().SetItems(pairs)) { }
    public Map(params (TKey key, TValue val)[] pairs) : this(new Map<TKey, TValue>() + pairs) { }

    public override string ToString() => composed.IsEmpty ? "" : composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : ",")}{{{next.Key?.ToString() ?? ""},{next.Value?.ToString() ?? ""}}}");

    public bool Equals(Map<TKey, TValue>? obj) {
      if (obj is null || GetHashCode() != obj.GetHashCode()) return false;
      if (Count != obj.Count) return false;
      foreach (var (d1key, d1value) in composed) {
        if (!obj.composed.TryGetValue(d1key, out TValue? d2value)) return false;
        if (!object.Equals(d1value, d2value)) return false;
      }
      return true;
    }
    override public int GetHashCode() {
      if (hashCache is null) {
        hashCache = new HashCode();
        foreach (var v in composed) hashCache.Value.Add(v);
      }
      return hashCache.Value.ToHashCode();
    }

    public Map<TKey, TValue> Remove(Func<TKey, TValue, bool> match) {
      bool pred((TKey, TValue) vt) => match(vt.Item1, vt.Item2);
      if (match is null) throw new ArgumentNullException(nameof(match));
      return new Map<TKey, TValue>(this.Where(kv => !pred(kv)));
    }

    static public Map<TKey, TValue> operator +(Map<TKey, TValue> o, (TKey key, TValue val) vt) => o.SetItem(vt.key, vt.val);
    static public Map<TKey, TValue> operator +(Map<TKey, TValue> o, IEnumerable<ValueTuple<TKey, TValue>> pairs) => o.SetItems(pairs.Select(vt => KeyValuePair.Create(vt.Item1, vt.Item2)));
    static public Map<TKey, TValue> operator -(Map<TKey, TValue> o, TKey key) => o.Remove(key);
    static public Map<TKey, TValue> operator -(Map<TKey, TValue> o, IEnumerable<TKey> keys) => new(o.composed.RemoveRange(keys));
    static public Map<TKey, TValue> operator -(Map<TKey, TValue> o, Func<TKey, TValue, bool> match) => o.Remove(match);

    // use this if T is a non-nullable reference type
    public TValue? this[TKey key] => composed.TryGetValue(key, out var value) ? value : default;

    // the rest of the methods just proxy to composed

    public IEqualityComparer<TKey> KeyComparer { get { return composed.KeyComparer; } }
    public IEnumerable<TKey> Keys { get { return composed.Keys; } }
    public IEqualityComparer<TValue> ValueComparer { get { return composed.ValueComparer; } }
    public IEnumerable<TValue> Values { get { return composed.Values; } }
    public int Count { get { return composed.Count; } }
    public bool IsEmpty { get { return composed.IsEmpty; } }

    public Map<TKey, TValue> Add(TKey key, TValue value) => new(composed.Add(key, value));
    public Map<TKey, TValue> AddRange(IEnumerable<KeyValuePair<TKey, TValue>> pairs) => new(composed.AddRange(pairs));

    public bool Contains(KeyValuePair<TKey, TValue> pair) => composed.Contains(pair);
    public bool ContainsKey(TKey key) => composed.ContainsKey(key);
    public bool ContainsValue(TValue value) => composed.ContainsValue(value);

    public Map<TKey, TValue> Remove(TKey key) => new(composed.Remove(key));
    public Map<TKey, TValue> RemoveRange(IEnumerable<TKey> keys) => new(composed.RemoveRange(keys));

    public Map<TKey, TValue> SetItem(TKey key, TValue value) => new(composed.SetItem(key, value));
    public Map<TKey, TValue> SetItems(IEnumerable<KeyValuePair<TKey, TValue>> items) => new(composed.SetItems(items));

    public ImmutableDictionary<TKey, TValue>.Builder ToBuilder() => composed.ToBuilder();

    IEnumerator<(TKey, TValue)> IEnumerable<(TKey, TValue)>.GetEnumerator() {
      var e = composed.GetEnumerator();
      while (e.MoveNext()) yield return (e.Current.Key, e.Current.Value);
    }
    IEnumerator IEnumerable.GetEnumerator() => (composed as IEnumerable).GetEnumerator();
    public IEnumerator<(TKey, TValue)> GetEnumerator() => (this as IEnumerable<(TKey, TValue)>).GetEnumerator();

    public bool TryGetKey(TKey equalKey, out TKey actualKey) => composed.TryGetKey(equalKey, out actualKey);
    public bool TryGetValue(TKey key, [MaybeNullWhen(false)] out TValue value) => composed.TryGetValue(key, out value);
  }

  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Que<T> : IEnumerable<T>, IEnumerable
  {
    [FIgnore] readonly ImmutableQueue<T> composed;
    [FIgnore] HashCode? hashCache;

    Que(ImmutableQueue<T> composed, HashCode? hashCode = null) => (this.composed, this.hashCache) = (composed, hashCode);
    Que(Que<T> vqueue) => this.composed = vqueue.composed;

    public Que() : this(ImmutableQueue<T>.Empty) { }
    public Que(IEnumerable<T> t) : this(new Que<T>().Enqueue(t)) { }
    public Que(params T[] p) : this(new Que<T>().Enqueue(p)) { }

    public override string ToString() => composed.IsEmpty ? "" : composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : ",")}{next?.ToString() ?? ""}");

    public bool Equals(Que<T>? obj) => obj is not null && GetHashCode() == obj.GetHashCode() && composed.SequenceEqual(((Que<T>)obj).composed);
    override public int GetHashCode() {
      if (hashCache is null) {
        hashCache = new HashCode();
        foreach (var v in composed) hashCache.Value.Add(v);
      }
      return hashCache.Value.ToHashCode();
    }

    static public Que<T> operator +(Que<T> o, T v) => o.Enqueue(v);

    public T? Peek() {
      if (IsEmpty) return default;
      try { return composed.Peek(); } catch { return default; } 
    }

    // use this if T is nut a non-nullable reference type (ie long or Manager?)
    public bool TryPeek([MaybeNullWhen(false)] out T value) { 
      if (IsEmpty) { value = default; return false; }
      try { value = composed.Peek(); return true;  } catch { value = default; return false; } 
    }

    // the rest of the methods just proxy to composed
    public bool IsEmpty { get { return composed.IsEmpty; } }
    public int Count { get { return composed.Count(); } } // todo cache

    public (Que<T>, T v) Dequeue() { var newq = composed.Dequeue(out T v); return (new(newq), v); }
    public Que<T> Enqueue(T v) {
      var res = new Que<T>(composed.Enqueue(v), hashCache);
      if (res.hashCache is object) res.hashCache.Value.Add(v);
      return res;
    }
    public Que<T> Enqueue(IEnumerable<T> v) => v.Aggregate(new Que<T>(), (total, next) => total.Enqueue(next));

    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (composed as IEnumerable).GetEnumerator();
  }

  [JsonConverter(typeof(JsonFCollectionConverter))]
  public sealed record Arr<T> : IEnumerable<T>, IEnumerable {
    [FIgnore] readonly ImmutableArray<T> composed;
    [FIgnore] HashCode? hashCache;

    Arr(ImmutableArray<T> composed, HashCode? hashCode = null) => (this.composed, this.hashCache) = (composed, hashCode);
    Arr(Arr<T> varr) => this.composed = varr.composed;

    public Arr() : this(ImmutableArray<T>.Empty) { }
    public Arr(IEnumerable<T> t) : this(new Arr<T>().AddRange(t)) { }
    public Arr(params T[] p) : this(new Arr<T>().AddRange(p)) { }

    public override string ToString() => composed.IsEmpty ? "" : composed.Aggregate("", (total, next) => $"{total}{(total == "" ? "" : ",")}{next?.ToString() ?? ""}");

    public bool Equals(Arr<T>? obj) => obj is not null && GetHashCode() == obj.GetHashCode() && composed.SequenceEqual(((Arr<T>)obj).composed);
    override public int GetHashCode() {
      if (hashCache is null) {
        hashCache = new HashCode();
        foreach (var v in composed) hashCache.Value.Add(v);
      }
      return hashCache.Value.ToHashCode();
    }

    static public Arr<T> operator +(Arr<T> o, T v) => o.Add(v);
    static public Arr<T> operator +(Arr<T> o, IEnumerable<T> items) => o.AddRange(items);
    static public Arr<T> operator -(Arr<T> o, T v) => o.Remove(v);
    static public Arr<T> operator -(Arr<T> o, IEnumerable<T> items) => o.RemoveRange(items);
    static public Arr<T> operator -(Arr<T> o, Predicate<T> match) => o.RemoveAll(match);

    public T? this[int index] { get {
        if (index < 0 || index >= Count) return default;
        try { return composed[index]; } catch { return default; }
    } }

    // use this if T is nut a non-nullable reference type (ie long or Manager?)
    public bool TryGetValue(int index, [MaybeNullWhen(false)] out T value) {
      if (index < 0 || index >= Count) { value = default; return false; }
      try { value = composed[index]; return true; } catch { value = default; return false; }
    }

    public int Count { get { return composed.Length; } }

    public Arr<T>? Insert(int index, T item) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.Insert(index, item)); } catch { return null; }
    }

    public Arr<T>? RemoveAt(int index) {
      if (index < 0 || index >= Count) return this;
      try { return new(composed.RemoveAt(index)); } catch { return null; }
    }

    public Arr<T>? InsertRange(int index, IEnumerable<T> items) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.InsertRange(index, items)); } catch { return null; }
    }

    public Arr<T>? InsertRange(int index, Arr<T> items) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.InsertRange(index, items.composed)); } catch { return null; }
    }

    public Arr<T>? RemoveRange(int index, int count) {
      if (index < 0 || index >= Count) return this;
      try { return new(composed.RemoveRange(index, count)); } catch { return null; }
    }

    public Arr<T>? SetItem(int index, T value) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.SetItem(index, value)); } catch { return null; }
    }

    public Arr<T>? Sort(int index, int count, IComparer<T> comparer) {
      if (index < 0 || index >= Count) return null;
      try { return new(composed.Sort(index, count, comparer)); } catch { return null; }
    }

    // the rest of the methods just proxy to composed

    public bool IsEmpty { get { return composed.IsEmpty; } }
    public int Length { get { return composed.Length; } }
    public bool IsDefaultOrEmpty { get { return composed.IsDefaultOrEmpty; } }
    public bool IsDefault { get { return composed.IsDefault; } }
    public Arr<T> Add(T value) {
      var res = new Arr<T>(composed.Add(value), hashCache);
      if (res.hashCache is object) res.hashCache.Value.Add(value);
      return res;
    }
    public Arr<T> AddRange(IEnumerable<T> items) {
      var res = new Arr<T>(composed.AddRange(items), hashCache);
      if (res.hashCache is object) foreach (var v in items) res.hashCache.Value.Add(v);
      return res;
    }
    public Arr<T> AddRange(Arr<T> items) => new(composed.AddRange(items.composed));
    public Arr<TOther> As<TOther>() where TOther : class => new(composed.As<TOther>());
    public ReadOnlyMemory<T> AsMemory() => composed.AsMemory();
    public ReadOnlySpan<T> AsSpan() => composed.AsSpan();
    public int BinarySearch(T value) => composed.BinarySearch(value);
    public int BinarySearch(T value, IComparer<T> comparer) => composed.BinarySearch(value, comparer);
    public int BinarySearch(int index, int length, T value) => composed.BinarySearch(index, length, value);
    public int BinarySearch(int index, int length, T value, IComparer<T> comparer) => composed.BinarySearch(index, length, value, comparer);
    public Arr<TOther> CastArray<TOther>() where TOther : class => new(composed.CastArray<TOther>());
    public bool Contains(T value) => composed.Contains(value);
    static public Arr<T> CreateRange(IEnumerable<T> items) => new(ImmutableArray.CreateRange(items));
    static public Arr<TResult> CreateRange<TResult>(ImmutableArray<T> items, Func<T, TResult> selector) => new(ImmutableArray.CreateRange(items, selector));
    public void CopyTo(int index, T[] array, int arrayIndex, int count) => composed.CopyTo(index, array, arrayIndex, count);
    public void CopyTo(T[] array) => composed.CopyTo(array);
    public void CopyTo(T[] array, int arrayIndex) => composed.CopyTo(array, arrayIndex);
    public bool Exists(Predicate<T> match) => composed.Any(new Func<T, bool>(match));
    public void ForEach(Action<T> action) { foreach (var v in composed) action(v); }
    public int IndexOf(T item, int startIndex, IEqualityComparer<T> equalityComparer) => composed.IndexOf(item, startIndex, equalityComparer);
    public int IndexOf(T item, int startIndex, int count) => composed.IndexOf(item, startIndex, count);
    public int IndexOf(T item, int startIndex, int count, IEqualityComparer<T>? equalityComparer) => composed.IndexOf(item, startIndex, count, equalityComparer);
    public int IndexOf(T item, int startIndex) => composed.IndexOf(item, startIndex);
    public int IndexOf(T value) => composed.IndexOf(value);
    public ref readonly T ItemRef(int index) => ref composed.ItemRef(index);
    public int LastIndexOf(T item) => composed.LastIndexOf(item);
    public int LastIndexOf(T item, int index) => composed.LastIndexOf(item, index);
    public int LastIndexOf(T item, int index, int count) => composed.LastIndexOf(item, index, count);
    public int LastIndexOf(T item, int index, int count, IEqualityComparer<T> equalityComparer) => composed.LastIndexOf(item, index, count, equalityComparer);
    public IEnumerable<TResult> OfType<TResult>() => composed.OfType<TResult>();
    public Arr<T> Remove(T value) => new(composed.Remove(value));
    public Arr<T> Remove(T value, IEqualityComparer<T> equalityComparer) => new(composed.Remove(value, equalityComparer));
    public Arr<T> RemoveAll(Predicate<T> match) => new(composed.RemoveAll(match));
    public Arr<T> RemoveRange(IEnumerable<T> items) => new(composed.RemoveRange(items));
    public Arr<T> RemoveRange(IEnumerable<T> items, IEqualityComparer<T> equalityComparer) => new(composed.RemoveRange(items, equalityComparer));
    public Arr<T> RemoveRange(Arr<T> items) => new(composed.RemoveRange(items.composed));
    public Arr<T> RemoveRange(Arr<T> items, IEqualityComparer<T> equalityComparer) => new(composed.RemoveRange(items.composed, equalityComparer));
    public Arr<T> Replace(T oldValue, T newValue) => new(composed.Replace(oldValue, newValue));
    public Arr<T> Replace(T oldValue, T newValue, IEqualityComparer<T> equalityComparer) => new(composed.Replace(oldValue, newValue, equalityComparer));
    public Arr<T> Sort(IComparer<T> comparer) => new(composed.Sort(comparer));
    public Arr<T> Sort(Comparison<T> comparison) => new(composed.Sort(comparison));
    public Arr<T> Sort() => new(composed.Sort());
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => (composed as IEnumerable<T>).GetEnumerator();
    IEnumerator IEnumerable.GetEnumerator() => (composed as IEnumerable).GetEnumerator();
    public ImmutableArray<T>.Builder ToBuilder() => composed.ToBuilder();
  }

/*
  static public class CollectionsExtensionsMethods {
    static public (bool, Arr<T>) With<T, TVal>(this Arr<T> o, int index, Expression<Func<T, TVal>> expression, TVal withVal) where T : FRecord<T> {
      var (ok, value) = o[index];
      return ok ? o.SetItem(index, value.With(expression, withVal)) : default; 
    }

    static public (bool, FList<T>) With<T, TVal>(this FList<T> o, int index, Expression<Func<T, TVal>> expression, TVal withVal) where T : FRecord<T> {
      var (ok, value) = o[index];
      return ok ? o.SetItem(index, value.With(expression, withVal)) : default;
    }

   
    static public (bool, Map<TKey, TValue>) With<TKey, TValue, TVal>(this Map<TKey, TValue> o, TKey index, Expression<Func<TValue, TVal>> expression, TVal withVal) where TKey : notnull where TValue : FRecord<TValue> {
      var (ok, value) = o[index];
      return ok ? (true, o.SetItem(index, value.With(expression, withVal))) : default;
    }

    static public (bool, Map<TKey, TValue>) With<TKey, TValue, TVal>(this Map<TKey, TValue> o, TKey index, Expression<Func<TValue, TVal>> expression, Func<TVal, TVal> func) where TKey : notnull where TValue : FRecord<TValue> {
      var (ok, value) = o[index];
      return ok ? (true, o.SetItem(index, value.With(expression, func))) : default;
    }
  }
*/

  class JsonFCollectionConverter : JsonConverter {
    static FieldInfo GetCompositorField(Type? t) {
      while (t != null) {
        var fields = t.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(p => p.Name == "composed");
        if (fields.Count() == 1) return fields.First();
        t = t.BaseType;
      }
      throw new JsonSerializationException();
    }

    public override bool CanConvert(Type? t) {
      var fields = t?.BaseType?.GetFields(BindingFlags.Instance | BindingFlags.NonPublic).Where(fi => fi.Name == "composed") ?? Enumerable.Empty<FieldInfo>();
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



