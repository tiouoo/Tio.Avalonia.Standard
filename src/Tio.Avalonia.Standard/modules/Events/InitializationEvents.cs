namespace Tio.Avalonia.Standard.Modules.Events;

public static class InitializationEvents
{
    public static event Action? BeforeReadSettings;

    internal static void RaiseBeforeReadSettings()
    {
        BeforeReadSettings?.Invoke();
    }

    public static event Action? BeforeUiLoaded;

    internal static void RaiseBeforeUiLoaded()
    {
        BeforeUiLoaded?.Invoke();
    }

    public static event Action? AfterUiLoaded;

    internal static void RaiseAfterUiLoaded()
    {
        AfterUiLoaded?.Invoke();
    }
}