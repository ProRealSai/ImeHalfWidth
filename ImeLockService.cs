using System.Reflection;
using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace ImeHalfWidth;

internal sealed class ImeLockService
{
    public const int PollMs = 200;
    private const uint WmImeControl = 0x0283;
    private const uint ImcGetConversionMode = 0x0001;
    private const uint ImcSetConversionMode = 0x0002;
    private const int ImeCmodeFullShape = 0x0008;
    private const int ImeCmodeSymbol = 0x0400;
    private const string RunKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
    private const string RunName = "ImeHalfWidth";

    public void Apply(AppSettings settings)
    {
        if (!settings.Enabled)
            return;

        var hwnd = GetForegroundWindow();
        var imeWnd = hwnd == IntPtr.Zero ? IntPtr.Zero : ImmGetDefaultIMEWnd(hwnd);
        if (imeWnd == IntPtr.Zero)
            imeWnd = hwnd;

        if (imeWnd != IntPtr.Zero)
        {
            var current = unchecked((int)SendMessage(imeWnd, WmImeControl, (IntPtr)ImcGetConversionMode, IntPtr.Zero));
            if (current < 0)
                current = 0;
            var desired = WithLockFlags(current, settings);
            if (desired != current)
                SendMessage(imeWnd, WmImeControl, (IntPtr)ImcSetConversionMode, (IntPtr)desired);
        }

        if (hwnd == IntPtr.Zero)
            return;

        var himc = ImmGetContext(hwnd);
        if (himc == IntPtr.Zero)
            return;

        try
        {
            if (!ImmGetConversionStatus(himc, out var conv, out var sentence))
                return;
            var next = (uint)WithLockFlags(unchecked((int)conv), settings);
            if (next != conv)
                ImmSetConversionStatus(himc, next, sentence);
        }
        finally
        {
            ImmReleaseContext(hwnd, himc);
        }
    }

    public static bool IsAutoStartEnabled()
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey);
        return !string.IsNullOrEmpty(key?.GetValue(RunName) as string);
    }

    public static void SetAutoStart(bool enabled)
    {
        using var key = Registry.CurrentUser.OpenSubKey(RunKey, true);
        if (key is null)
            return;

        var exe = Assembly.GetExecutingAssembly().Location;
        if (enabled && !string.IsNullOrEmpty(exe))
            key.SetValue(RunName, $"\"{exe}\"");
        else
            key.DeleteValue(RunName, false);
    }

    private static int WithLockFlags(int mode, AppSettings settings)
    {
        mode = settings.HalfWidth
            ? mode & ~ImeCmodeFullShape
            : mode | ImeCmodeFullShape;
        return settings.EnglishPunctuation
            ? mode & ~ImeCmodeSymbol
            : mode | ImeCmodeSymbol;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    private static extern IntPtr SendMessage(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetDefaultIMEWnd(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern IntPtr ImmGetContext(IntPtr hWnd);

    [DllImport("imm32.dll")]
    private static extern bool ImmReleaseContext(IntPtr hWnd, IntPtr hImc);

    [DllImport("imm32.dll")]
    private static extern bool ImmGetConversionStatus(IntPtr hImc, out uint conversion, out uint sentence);

    [DllImport("imm32.dll")]
    private static extern bool ImmSetConversionStatus(IntPtr hImc, uint conversion, uint sentence);
}
