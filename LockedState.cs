// https://github.com/kofifus/F/wiki

#nullable enable

namespace F
{

  public class LockedState<T> : State<T>
  {
    readonly object theLock = new();

    override protected object? PreAction(in T _) { Monitor.Enter(theLock); return null; }
    override protected void PostAction(in T preVal, in T postVal, object? preActionData) => Monitor.Exit(theLock);

    public LockedState(T value) : base(value) { }
  }

  public class LockedState<T1, T2> : State<T1>.Combine<T2>
  {
    public LockedState(State<T1> vstate1, State<T2> vstate2) : base(vstate1, vstate2) { }
  }

  public class JournalingState<T> : State<T>
  {
    readonly object theLock = new();
    public Lst<T> Journal = new();

    override protected object? PreAction(in T preVal)
    {
      var wasEntered = Monitor.IsEntered(theLock);
      Monitor.Enter(theLock);
      if (Journal.IsEmpty) Journal += preVal;
      return wasEntered;
    }
    override protected void PostAction(in T _, in T postVal, object? preActionData)
    {
      var wasEntered = (bool)preActionData!;
      if (!wasEntered) Journal += postVal;
      Monitor.Exit(theLock);
    }

    public JournalingState(T value) : base(value) { }
  }
}
