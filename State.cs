using System;

// https://github.com/kofifus/F/wiki

#nullable enable

namespace F {

  // T is assumed to be Data (immutable with value semantics)
  public delegate void ActionRef<T>(ref T r1);
  public delegate RES FuncRef<T, RES>(ref T r1);

  // readonly access to val without initiating side effects
  public interface IStateVal<T> {
    T Val { get; }

    IStateVal<TMember> Lens<TMember>(Func<T, TMember> lensGetter); // Val lens to a member of T
  }

  // read/write access to val with initiating side effects (ie locking)
  public interface IStateRef<T>  {
    T Val { get; }
    void Ref(ActionRef<T> f);
    TRES Ref<TRES>(FuncRef<T, TRES> f);

    IStateVal<T> AsVal { get; }
  }

  // T1, T2 etc are assumed to be Data (immutable with value semantics)
  public delegate void ActionRef<T1, T2>(ref T1 r1, ref T2 r2);
  public delegate RES FuncRef<T1, T2, RES>(ref T1 r1, ref T2 r2);

  public interface IStateVal<T1, T2> {
    (T1, T2) Val { get; }
  }

  public interface IStateRef<T1, T2>  {
    void Ref(ActionRef<T1, T2> f);
    TRES Ref<TRES>(FuncRef<T1, T2, TRES> f);
  }

  abstract public class State<T> : IStateVal<T>, IStateRef<T>  {
    T Value;

    abstract protected object? PreAction(in T preState); // result will be passed to PostAction
    abstract protected void PostAction(in T preState, in T postState, object? PreData);

    public State(T value) => this.Value = value;

    public virtual T Val { get => Value; }
    
    public virtual void Ref(ActionRef<T> f) {
      var preActionData = PreAction(Value);
      var preValue = Value;
      try { f(ref Value); }
      finally { PostAction(preValue, Value, preActionData); }
    }

    public virtual TRES Ref<TRES>(FuncRef<T, TRES> f) {
      var preActionData = PreAction(Value);
      var preValue = Value;
      try { return f(ref Value); }
      finally { PostAction(preValue, Value, preActionData); }
    }

    public IStateVal<T> AsVal => this;
    public IStateRef<T> AsRef => this;


    // -----------  combined State
    
    public abstract class Combine<T2> : IStateVal<T, T2>, IStateRef<T, T2>  {
      readonly State<T> State1;
      readonly State<T2> State2;

      virtual public object? PreAction(in State<T> preState1, in State<T2> preState2) {
        var preData1 = preState1.PreAction(preState1.Val);
        var preData2 = preState2.PreAction(preState2.Val);
        return (preData1, preData2);
      }

      virtual public void PostAction(in State<T> preState1, in State<T> postState1, in State<T2> preState2, in State<T2> postState2, object? preData) {
        var (preData1, preData2) = (ValueTuple<object?, object?>)preData!;
        preState1.PostAction(preState1.Val, postState1.Val, preData1);
        preState2.PostAction(preState2.Val, postState2.Val, preData2);
      }

      public Combine(State<T> vstate1, State<T2> vstate2) => (this.State1, this.State2) = (vstate1, vstate2);

      public (T, T2) Val => (State1.Val, State2.Val);

      public void Ref(ActionRef<T, T2> f) {
        var preData = PreAction(State1, State2);
        var preState1 = State1;
        var preState2 = State2;
        try { f(ref State1.Value, ref State2.Value); }
        finally { PostAction(preState1, State1, preState2, State2, preData); }
      }

      public TRES Ref<TRES>(FuncRef<T, T2, TRES> f) {
        var preData = PreAction(State1, State2);
        var preState1 = State1;
        var preState2 = State2;
        try { return f(ref State1.Value, ref State2.Value); }
        finally { PostAction(preState1, State1, preState2, State2, preData); }
      }

      public IStateVal<T, T2> AsVal => this;
      public IStateRef<T, T2> AsRef => this;
    }

    // -----------  lenses

    public IStateVal<TMember> Lens<TMember>(Func<T, TMember> lensGetter)
      => new LensValState<TMember>(this, lensGetter);

    // private helper State for a Val lens
    record LensValState<TMember> : IStateVal<TMember> {
      readonly IStateVal<T> TStateVal;
      readonly Func<T, TMember> LensGetter;

      public LensValState(IStateVal<T> tStateVal, Func<T, TMember> lensGetter)
        => (TStateVal, LensGetter) = (tStateVal, lensGetter);

      public TMember Val => LensGetter(TStateVal.Val);

      public IStateVal<TMember1> Lens<TMember1>(Func<TMember, TMember1> lensGetter)
        => new State<TMember>.LensValState<TMember1>(this, lensGetter);
    }
  }
}
