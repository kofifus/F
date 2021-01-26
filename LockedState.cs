using System;
using System.Threading;

#nullable enable

namespace F {

  public class LockedState<T> : State<T>  {
    readonly object theLock = new();

    override public void PreAction(in T _) => Monitor.Enter(theLock); 
    override public void PostAction(in T preVal, in T postVal) => Monitor.Exit(theLock);
    
    public LockedState(T value, Action<T, T>? trigger=null) : base(value) {  }
    public static implicit operator T(LockedState<T> v) => v.Val;
  }

  public class LockedState<T1, T2> : State<T1>.Combine<T2> {
    public LockedState(State<T1> vstate1, State<T2> vstate2) : base(vstate1, vstate2) { }
  }

  public class JournalingState<T> : State<T> where T : notnull {
    readonly object theLock = new();
    public Seq<T> Journal = new();

    override public void PreAction(in T preVal) { Monitor.Enter(theLock); if (Journal.IsEmpty) Journal += preVal; }
    override public void PostAction(in T _, in T postVal) { Journal += postVal; Monitor.Exit(theLock); }

    public JournalingState(T value) : base(value) { }
  }
}
