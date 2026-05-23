using System.Windows;
using System.Windows.Threading;

namespace LeadGen.Services;

/// <summary>
/// Безопасный доступ к UI-потоку — предотвращает NullReference при отсутствии Application.
/// </summary>
public static class UiDispatcher
{
    public static void RunOnUiThread(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return;
        }

        dispatcher.Invoke(action);
    }

    public static Task RunOnUiThreadAsync(Action action)
    {
        var dispatcher = Application.Current?.Dispatcher;
        if (dispatcher is null || dispatcher.CheckAccess())
        {
            action();
            return Task.CompletedTask;
        }

        dispatcher.Invoke(action);
        return Task.CompletedTask;
    }

    public static Dispatcher? Current => Application.Current?.Dispatcher;
}
