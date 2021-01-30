using System;
using System.Diagnostics.CodeAnalysis;

// https://github.com/kofifus/F/wiki

#nullable enable

namespace F {

  public delegate void ActionRef<T>(ref T r1);
  public delegate void ActionIn<T>(in T r1);
  public delegate RES FuncRef<T, RES>(ref T r1);
  public delegate RES FuncIn<T, RES>(in T r1);
  public delegate void ActionRef<T1, T2>(ref T1 r1, ref T2 r2);
  public delegate void ActionIn<T1, T2>(in T1 r1, in T2 r2);
  public delegate RES FuncRef<T1, T2, RES>(ref T1 r1, ref T2 r2);
  public delegate RES FuncIn<T1, T2, RES>(in T1 r1, in T2 r2);
  public delegate void ActionRef<T1, T2, T3>(ref T1 r1, ref T2 r2, ref T3 r3);
  public delegate void ActionIn<T1, T2, T3>(in T1 r1, in T2 r2, in T3 r3);
  public delegate RES FuncRef<T1, T2, T3, RES>(ref T1 r1, ref T2 r2, ref T3 r3);
  public delegate RES FuncIn<T1, T2, T3, RES>(in T1 r1, in T2 r2, in T3 r3);

  // T, T1, T2 etc are assumed to be Data (immutable with value semantics)

  // readonly access to val without initiating side effects
  public interface IStateVal<T> {
    T Val { get; }
  }

  // readonly access to val with initiating side effects (ie locking)
  public interface IStateIn<T> : IStateVal<T> {
    void In(ActionIn<T> f);
    TRES In<TRES>(FuncIn<T, TRES> f);
  }

  // read/write access to val with initiating side effects (ie locking)
  public interface IStateRef<T> : IStateIn<T>  {
    void Ref(ActionRef<T> f);
    TRES Ref<TRES>(FuncRef<T, TRES> f);
  }

  public interface IStateVal<T1, T2> {
    (T1, T2) Val { get; }
  }
  
  public interface IStateIn<T1, T2> : IStateVal<T1, T2> {
    void In(ActionIn<T1, T2> f);
    TRES In<TRES>(FuncIn<T1, T2, TRES> f);
  }

  public interface IStateRef<T1, T2> : IStateIn<T1, T2>  {
    void Ref(ActionRef<T1, T2> f);
    TRES Ref<TRES>(FuncRef<T1, T2, TRES> f);
  }

  abstract public class State<T> : IStateRef<T>  {
    T Value;

    abstract public void PreAction(in T preState);
    abstract public void PostAction(in T preState, in T postState);

    public State(T value) => this.Value = value;

    public T Val { get => Value; }
    
    public void In(ActionIn<T> f) {
      PreAction(Value);
      try { f(in Value); }
      finally { PostAction(Value, Value); }
    }

    public TRES In<TRES>(FuncIn<T, TRES> f) {
      PreAction(Value);
      try { return f(in Value); }
      finally { PostAction(Value, Value); }
    }

    public void Ref(ActionRef<T> f) {
      PreAction(Value);
      var preValue = Value;
      try { f(ref Value); }
      finally { PostAction(preValue, Value); }
    }

    public TRES Ref<TRES>(FuncRef<T, TRES> f) {
      PreAction(Value);
      var preValue = Value;
      try { return f(ref Value); }
      finally { PostAction(preValue, Value); }
    }

 
    public abstract class Combine<T2> : IStateRef<T, T2>  {
      readonly State<T> State1;
      readonly State<T2> State2;

      virtual public bool PreAction(in State<T> preState1, in State<T2> preState2) {
        preState1.PreAction(preState1.Val);
        preState2.PreAction(preState2.Val);
        return true;
      }

     virtual public void PostAction(in State<T> preState1, in State<T> postState1, in State<T2> preState2, in State<T2> postState2) {
        preState1.PostAction(preState1.Val, postState1.Val);
        preState2.PostAction(preState2.Val, postState2.Val);
      }

      public Combine(State<T> vstate1, State<T2> vstate2) => (this.State1, this.State2) = (vstate1, vstate2);

      public (T, T2) Val => (State1.Val, State2.Val);

      public void In(ActionIn<T, T2> f) {
        PreAction(State1, State2);
        try { f(in State1.Value, in State2.Value); }
        finally { PostAction(State1, State1, State2, State2); }
      }

      public TRES In<TRES>(FuncIn<T, T2, TRES> f) {
        PreAction(State1, State2);
        try { return f(in State1.Value, in State2.Value); }
        finally { PostAction(State1, State1, State2, State2); }
      }

      public void Ref(ActionRef<T, T2> f) {
        PreAction(State1, State2);
        var preState1 = State1;
        var preState2 = State2;
        try { f(ref State1.Value, ref State2.Value); }
        finally { PostAction(preState1, State1, preState2, State2); }
      }

      public TRES Ref<TRES>(FuncRef<T, T2, TRES> f) {
        PreAction(State1, State2);
        var preState1 = State1;
        var preState2 = State2;
        try { return f(ref State1.Value, ref State2.Value); }
        finally { PostAction(preState1, State1, preState2, State2); }
      }
    }
  }
}
