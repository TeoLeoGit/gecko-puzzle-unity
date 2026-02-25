using System;

public static class GameEvents
{
    public static event Action<BaseExit> OnExitCreated;

    public static void RaiseExitCreated(BaseExit exit)
    {
        OnExitCreated?.Invoke(exit);
    }
}