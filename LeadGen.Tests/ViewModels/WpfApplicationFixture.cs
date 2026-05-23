using System.Windows;

namespace LeadGen.Tests.ViewModels;

/// <summary>
/// Инициализация WPF Application для STA-тестов ViewModel.
/// </summary>
public sealed class WpfApplicationFixture : IDisposable
{
    private static int _initialized;

    public static WpfApplicationFixture EnsureApplication()
    {
        if (Interlocked.CompareExchange(ref _initialized, 1, 0) == 0)
        {
            if (Application.Current is null)
                new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown };
        }

        return new WpfApplicationFixture();
    }

    public void Dispose()
    {
        // Application живёт до конца процесса тестов
    }
}
