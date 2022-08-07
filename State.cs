
// https://github.com/kofifus/F/wiki

#nullable enable

using System;
using System.Threading;

namespace F.State {
  using F.Collections;

  // T is assumed to be Data (immutable with value semantics)
  public delegate void ActionRef<T>(ref T r1);
  public delegate RES FuncRef<T, RES>(ref T r1);


  // readonly access to val without initiating side effects
  public interface IStateVal<T> {
    T Val();

    IStateVal<TMember> Lens<TMember>(Func<T, TMember> lensGetter); // lens to a member of T
  }

  // read/write access to val with initiating side effects (ie locking)
  public interface IStateRef<T> {
    T Val();
    void Ref(ActionRef<T> f);
    TRES Ref<TRES>(FuncRef<T, TRES> f);

    IStateVal<T> ToIVal { get; }
    IStateVal<TMember> Lens<TMember>(Func<T, TMember> lensGetter) => ToIVal.Lens(lensGetter); // lens to a member of T
  }

  // T1, T2 etc are assumed to be Data (immutable with value semantics)
  public delegate void ActionRef<T1, T2>(ref T1 r1, ref T2 r2);
  public delegate RES FuncRef<T1, T2, RES>(ref T1 r1, ref T2 r2);

  public interface IStateVal<T1, T2> {
    (T1, T2) Val();
  }

  public interface IStateRef<T1, T2>  {
    void Ref(ActionRef<T1, T2> f);
    TRES Ref<TRES>(FuncRef<T1, T2, TRES> f);
  }

  abstract public class State<T> : IStateVal<T>, IStateRef<T>, IEquatable<State<T>> {
    T Value;

    virtual protected void PreVal() { }
    virtual protected void PostVal() { }
    virtual protected object? PreRef(in T preState) => null; // result will be passed to PostRef
    virtual protected void PostRef(in T preState, in T postState, object? PreData) { }

    public State(T value) => this.Value = value;

    public virtual T Val() {
      PreVal();
      var res = Value;
      PostVal();
      return res;
    }

    public virtual void Ref(ActionRef<T> f) {
      var preRefData = PreRef(Value);
      var preValue = Value;
      try { f(ref Value); }
      finally { PostRef(preValue, Value, preRefData); }
    }

    public virtual TRES Ref<TRES>(FuncRef<T, TRES> f) {
      var preRefData = PreRef(Value);
      var preValue = Value;
      try { return f(ref Value); }
      finally { PostRef(preValue, Value, preRefData); }
    }

    public IStateVal<T> ToIVal => this;
    public IStateRef<T> ToIRef => this;

    public bool Equals(State<T>? o) => o is not null && GetType() == o.GetType() && ReferenceEquals(Value, o.Value);
    public override bool Equals(object? obj) => Equals(obj as State<T>);
    public override int GetHashCode() => Value?.GetHashCode() ?? 0;


    // -----------  combined State

    public abstract class Combine<T2> : IStateVal<T, T2>, IStateRef<T, T2>, IEquatable<Combine<T2>> {
      readonly State<T> State1;
      readonly State<T2> State2;

      virtual protected void PreVal() {
        State1.PreVal();
        State2.PreVal();
      }

      virtual protected void PostVal() {
        State2.PostVal();
        State1.PostVal();
      }

      virtual protected object? PreRef(in T preState1, in T2 preState2) {
        var preData1 = State1.PreRef(preState1);
        var preData2 = State2.PreRef(preState2);
        return (preData1, preData2);
      }

      virtual protected void PostRef(in T preState1, in T postState1, in T2 preState2, in T2 postState2, object? preData) {
        var (preData1, preData2) = (ValueTuple<object?, object?>)preData!;
        State2.PostRef(preState2, postState2, preData2);
        State1.PostRef(preState1, postState1, preData1);
      }

      public Combine(State<T> vstate1, State<T2> vstate2) => (this.State1, this.State2) = (vstate1, vstate2);

      public (T, T2) Val() {
        PreVal();
        var res = (State1.Val(), State2.Val());
        PostVal();
        return res;
      }

      public void Ref(ActionRef<T, T2> f) {
        var preData = PreRef(State1.Value, State2.Value);
        var preState = (State1.Value, State2.Value);
        try { f(ref State1.Value, ref State2.Value); }
        finally { PostRef(preState.Item1, State1.Value, preState.Item2, State2.Value, preData); }
      }

      public TRES Ref<TRES>(FuncRef<T, T2, TRES> f) {
        var preData = PreRef(State1.Value, State2.Value);
        var preState = (State1.Value, State2.Value);
        try { return f(ref State1.Value, ref State2.Value); }
        finally { PostRef(preState.Item1, State1.Value, preState.Item2, State2.Value, preData); }
      }

      public IStateVal<T, T2> ToIVal => this;
      public IStateRef<T, T2> ToIRef => this;

      public bool Equals(Combine<T2>? o) => o is not null && o.State1.Equals(State1) && o.State2.Equals(State2);
      public override bool Equals(object? obj) => Equals(obj as State<T>);
      public override int GetHashCode() => HashCode.Combine(State1.GetHashCode(), State2.GetHashCode());
    }

    // -----------  lenses

    public IStateVal<TMember> Lens<TMember>(Func<T, TMember> lensGetter)
      => new LensValState<TMember>(this, lensGetter);

    // private helper State for a Val lens
    class LensValState<TMember> : IStateVal<TMember> {
      readonly IStateVal<T> TStateVal;
      readonly Func<T, TMember> LensGetter;

      public LensValState(IStateVal<T> tStateVal, Func<T, TMember> lensGetter)
        => (TStateVal, LensGetter) = (tStateVal, lensGetter);

      public TMember Val() => LensGetter(TStateVal.Val());

      public IStateVal<TMember1> Lens<TMember1>(Func<TMember, TMember1> lensGetter)
        => new State<TMember>.LensValState<TMember1>(this, lensGetter);
    }
  }


  // simplest state implementation, does nothing
  public class SimpleState<T> : State<T> {
    public SimpleState(T value) : base(value) { }
  }


  // lock during Ref
  public class LockedState<T> : State<T> {
    public LockedState(T value) : base(value) { }

    readonly object theLock = new();
    override protected void PreVal() => Monitor.Enter(theLock);
    override protected void PostVal() => Monitor.Exit(theLock);
    override protected object? PreRef(in T _) { Monitor.Enter(theLock); return null; }
    override protected void PostRef(in T preVal, in T postVal, object? PreRefData) => Monitor.Exit(theLock);

  }

  // lock during Ref around T1 then around T2
  public class LockedState<T1, T2> : State<T1>.Combine<T2> {
    public LockedState(State<T1> vstate1, State<T2> vstate2) : base(vstate1, vstate2) { }
  }


  // lock during Ref then store the change in the Journal
  public class JournalLockedState<T> : State<T> {
    public JournalLockedState(T value) : base(value) { }

    readonly object theLock = new();
    public Lst<T> Journal = new();

    override protected void PreVal() => Monitor.Enter(theLock);
    override protected void PostVal() => Monitor.Exit(theLock);

    override protected object? PreRef(in T preVal) {
      var wasEntered = Monitor.IsEntered(theLock);
      Monitor.Enter(theLock);
      if (Journal.IsEmpty) Journal += preVal;
      return wasEntered;
    }
    override protected void PostRef(in T _, in T postVal, object? PreRefData) {
      var wasEntered = (bool)PreRefData!;
      if (!wasEntered) Journal += postVal;
      Monitor.Exit(theLock);
    }
  }


  // allow registering events that get invoked before/after a Ref
    public class ObserverState<T> : State<T> {
    public record OserverStatePreEventArgs(T PreVal);
    public record OserverStatePostEventArgs(T PreVal, T PostVal);

    public ObserverState(T value) : base(value) { }

    public event EventHandler<OserverStatePreEventArgs>? PreEvent;
    public event EventHandler<OserverStatePostEventArgs>? PostEvent;

    override protected object? PreRef(in T preVal) { PreEvent?.Invoke(this, new(preVal)); return null; }
    override protected void PostRef(in T preVal, in T postVal, object? _) => PostEvent?.Invoke(this, new(preVal, postVal));
  }
}
