// https://github.com/kofifus/F/wiki

#nullable enable

using System;
using System.Threading;

namespace F.State {
  using F.Collections;

  // T is assumed to be Data (immutable with value semantics)
  
  // readonly access to val without initiating side effects
  public interface IReadOnlyState<T> {
    T Val();

    IReadOnlyState<TMember> Lens<TMember>(Func<T, TMember> lensGetter); // lens to a member of T
  }

  // read/write access to val with initiating side effects (ie locking)
  public interface IState<T> {
    public delegate void ActionRef<U>(ref U r1);
    public delegate RES FuncRef<U, RES>(ref U r1);

    T Val();
    void Val(ActionRef<T> f);
    TRES Val<TRES>(FuncRef<T, TRES> f);

    IReadOnlyState<T> ToIReadOnlyState { get; }
    IReadOnlyState<TMember> Lens<TMember>(Func<T, TMember> lensGetter) => ToIReadOnlyState.Lens(lensGetter); // lens to a member of T
  }

  // T1, T2 etc are assumed to be Data (immutable with value semantics)

  public interface IReadOnlyState<T1, T2> {
    (T1, T2) Val();
  }

  public interface IState<T1, T2>  {
    public delegate void ActionRef<U1, U2>(ref U1 r1, ref U2 r2);
    public delegate RES FuncRef<U1, U2, RES>(ref U1 r1, ref U2 r2);

    void Val(ActionRef<T1, T2> f);
    TRES Val<TRES>(FuncRef<T1, T2, TRES> f);
  }

  abstract public class State<T>(T value) : IReadOnlyState<T>, IState<T>, IEquatable<State<T>> {
    T Value = value;

    virtual protected void PreGet() { }
    virtual protected void PostGet() { }
    virtual protected object? PreSet(in T preState) => null; // result will be passed to PostRef
    virtual protected void PostSet(in T preState, in T postState, object? PreData) { }

    public virtual T Val() {
      PreGet();
      var res = Value;
      PostGet();
      return res;
    }

    public virtual void Val(IState<T>.ActionRef<T> f) {
      var preRefData = PreSet(Value);
      var preValue = Value;
      try { f(ref Value); }
      finally { PostSet(preValue, Value, preRefData); }
    }

    public virtual TRES Val<TRES>(IState<T>.FuncRef<T, TRES> f) {
      var preRefData = PreSet(Value);
      var preValue = Value;
      try { return f(ref Value); }
      finally { PostSet(preValue, Value, preRefData); }
    }

    public IReadOnlyState<T> ToIReadOnlyState => this;
    public IState<T> ToIState => this;

    public bool Equals(State<T>? o) => o is not null && GetType() == o.GetType() && ReferenceEquals(Value, o.Value);
    public override bool Equals(object? obj) => Equals(obj as State<T>);
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;


    // -----------  combined State

    public abstract class Combine<T2> : IReadOnlyState<T, T2>, IState<T, T2>, IEquatable<Combine<T2>> {
      readonly State<T> State1;
      readonly State<T2> State2;

      public Combine(State<T> vstate1, State<T2> vstate2) => (this.State1, this.State2) = (vstate1, vstate2);

      virtual protected void PreGet() {
        State1.PreGet();
        State2.PreGet();
      }

      virtual protected void PostGet() {
        State2.PostGet();
        State1.PostGet();
      }

      virtual protected object? PreSet(in T preState1, in T2 preState2) {
        var preData1 = State1.PreSet(preState1);
        var preData2 = State2.PreSet(preState2);
        return (preData1, preData2);
      }

      virtual protected void PostSet(in T preState1, in T postState1, in T2 preState2, in T2 postState2, object? preData) {
        var (preData1, preData2) = (ValueTuple<object?, object?>)preData!;
        State2.PostSet(preState2, postState2, preData2);
        State1.PostSet(preState1, postState1, preData1);
      }

      public (T, T2) Val() {
        PreGet();
        var res = (State1.Val(), State2.Val());
        PostGet();
        return res;
      }

      public void Val(IState<T, T2>.ActionRef<T, T2> f) {
        var preData = PreSet(State1.Value, State2.Value);
        var preState = (State1.Value, State2.Value);
        try { f(ref State1.Value, ref State2.Value); }
        finally { PostSet(preState.Item1, State1.Value, preState.Item2, State2.Value, preData); }
      }

      public TRES Val<TRES>(IState<T, T2>.FuncRef<T, T2, TRES> f) {
        var preData = PreSet(State1.Value, State2.Value);
        var preState = (State1.Value, State2.Value);
        try { return f(ref State1.Value, ref State2.Value); }
        finally { PostSet(preState.Item1, State1.Value, preState.Item2, State2.Value, preData); }
      }

      public IReadOnlyState<T, T2> ToIReadOnlyState => this;
      public IState<T, T2> ToIState => this;

      public bool Equals(Combine<T2>? o) => o is not null && o.State1.Equals(State1) && o.State2.Equals(State2);
      public override bool Equals(object? obj) => Equals(obj as State<T>);
      public override int GetHashCode() => HashCode.Combine(State1.GetHashCode(), State2.GetHashCode());
    }

    // -----------  lenses

    public IReadOnlyState<TMember> Lens<TMember>(Func<T, TMember> lensGetter)
      => new LensValState<TMember>(this, lensGetter);

    // private helper State for a Val lens
    class LensValState<TMember> : IReadOnlyState<TMember> {
      readonly IReadOnlyState<T> TStateVal;
      readonly Func<T, TMember> LensGetter;

      public LensValState(IReadOnlyState<T> tStateVal, Func<T, TMember> lensGetter)
        => (TStateVal, LensGetter) = (tStateVal, lensGetter);

      public TMember Val() => LensGetter(TStateVal.Val());

      public IReadOnlyState<TMember1> Lens<TMember1>(Func<TMember, TMember1> lensGetter)
        => new State<TMember>.LensValState<TMember1>(this, lensGetter);
    }
  }

  // simplest state implementation, does nothing
  public class SimpleState<T>(T value) : State<T>(value) {
  }


  // lock during Ref
  public class LockedState<T>(T value) : State<T>(value) {
    readonly object theLock = new();
    override protected void PreGet() => Monitor.Enter(theLock);
    override protected void PostGet() => Monitor.Exit(theLock);
    override protected object? PreSet(in T _) { Monitor.Enter(theLock); return null; }
    override protected void PostSet(in T preVal, in T postVal, object? PreRefData) => Monitor.Exit(theLock);

  }

  public class LockedState<T1, T2>(State<T1> vstate1, State<T2> vstate2) : State<T1>.Combine<T2>(vstate1, vstate2) {
  }


  // lock during Ref then store the change in the Journal
  public class JournalLockedState<T>(T value) : State<T>(value) {
    readonly object theLock = new();
    public Lst<T> Journal = new();

    override protected void PreGet() => Monitor.Enter(theLock);
    override protected void PostGet() => Monitor.Exit(theLock);

    override protected object? PreSet(in T preVal) {
      var wasEntered = Monitor.IsEntered(theLock);
      Monitor.Enter(theLock);
      if (Journal.IsEmpty) Journal += preVal;
      return wasEntered;
    }
    override protected void PostSet(in T _, in T postVal, object? PreRefData) {
      var wasEntered = (bool)PreRefData!;
      if (!wasEntered) Journal += postVal;
      Monitor.Exit(theLock);
    }
  }

  public class JournalLockedState<T1, T2>(State<T1> vstate1, State<T2> vstate2) : State<T1>.Combine<T2>(vstate1, vstate2) {
  }

  // allow registering events that get invoked before/after a Ref
  public class ObserverState<T>(T value) : State<T>(value) {
    public event Action<T>? PreValEvent;
    public event Action<T>? PostValEvent;
    public event Action<T>? PreRefEvent;
    public event Action<T, T>? PostRefEvent;

    override protected void PreGet() => PreValEvent?.Invoke(Val());
    override protected void PostGet() => PostValEvent?.Invoke(Val());
    override protected object? PreSet(in T preVal) { PreRefEvent?.Invoke(preVal); return null; }
    override protected void PostSet(in T preVal, in T postVal, object? _) => PostRefEvent?.Invoke(preVal, postVal);
  }

  public class ObserverState<T1, T2>(State<T1> vstate1, State<T2> vstate2) : State<T1>.Combine<T2>(vstate1, vstate2) {
  }

}
