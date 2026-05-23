namespace LeadGen.Helpers;

/// <summary>
/// Глобальный флаг анимаций UI (читается из config.json).
/// </summary>
public static class AnimationSettings
{
    public static bool IsEnabled { get; private set; } = true;

    public static event EventHandler? Changed;

    public static void Initialize(bool enabled)
    {
        IsEnabled = enabled;
        Changed?.Invoke(null, EventArgs.Empty);
    }

    public static void SetEnabled(bool enabled)
    {
        if (IsEnabled == enabled)
            return;

        IsEnabled = enabled;
        Changed?.Invoke(null, EventArgs.Empty);
    }
}
