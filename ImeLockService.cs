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

    public ImeStatus Apply(AppSettings settings)
    {
        var hwnd = GetForegroundWindow();
        var imeWnd = hwnd == IntPtr.Zero ? IntPtr.Zero : ImmGetDefaultIMEWnd(hwnd);
        if (imeWnd == IntPtr.Zero)
            imeWnd = hwnd;

        var current = imeWnd == IntPtr.Zero
            ? 0
            : unchecked((int)SendMessage(imeWnd, WmImeControl, (IntPtr)ImcGetConversionMode, IntPtr.Zero));
        if (current < 0)
            current = 0;

        var desired = current;
        if (settings.Enabled)
        {
            desired = settings.HalfWidth
                ? desired & ~ImeCmodeFullShape
                : desired | ImeCmodeFullShape;
            desired = settings.EnglishPunctuation
                ? desired & ~ImeCmodeSymbol
                : desired | ImeCmodeSymbol;

            if (desired != current && imeWnd != IntPtr.Zero)
                SendMessage(imeWnd, WmImeControl, (IntPtr)ImcSetConversionMode, (IntPtr)desired);

            if (hwnd != IntPtr.Zero)
            {
                var himc = ImmGetContext(hwnd);
                if (himc != IntPtr.Zero)
                {
                    if (ImmGetConversionStatus(himc, out var conv, out var sentence))
                    {
                        var next = conv;
                        next = settings.HalfWidth
                            ? next & ~(uint)ImeCmodeFullShape
                            : next | (uint)ImeCmodeFullShape;
                        next = settings.EnglishPunctuation
                            ? next & ~(uint)ImeCmodeSymbol
                            : next | (uint)ImeCmodeSymbol;
                        if (next != conv)
                            ImmSetConversionStatus(himc, next, sentence);
                    }

                    ImmReleaseContext(hwnd, himc);
                }
            }
        }

        return new ImeStatus(
            settings.Enabled,
            (desired & ImeCmodeFullShape) == 0,
            (desired & ImeCmodeSymbol) == 0,
            DateTime.Now);
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

        var exe = Environment.ProcessPath;
        if (enabled && !string.IsNullOrEmpty(exe))
            key.SetValue(RunName, $"\"{exe}\"");
        else
            key.DeleteValue(RunName, false);
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

internal readonly record struct ImeStatus(
    bool Enabled,
    bool HalfWidth,
    bool EnglishPunctuation,
    DateTime UpdatedAt);
