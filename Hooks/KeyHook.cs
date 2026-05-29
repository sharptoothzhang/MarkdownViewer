using System;
using System.Runtime.InteropServices;
using MarkdownViewer.Core;

namespace MarkdownViewer.Hooks
{
    class KeyHook
    {
        static IntPtr hookId = IntPtr.Zero;
        static NativeMethods.HookProc hookProc;
        static Action<int> onKeyAction;

        public static void Install(Action<int> callback)
        {
            onKeyAction = callback;
            hookProc = new NativeMethods.HookProc(HookCallback);
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(null);
            hookId = NativeMethods.SetWindowsHookEx(13, hookProc, moduleHandle, 0); // WH_KEYBOARD = 13
        }

        public static void Uninstall()
        {
            if (hookId != IntPtr.Zero)
            {
                NativeMethods.UnhookWindowsHookEx(hookId);
                hookId = IntPtr.Zero;
            }
        }

        static IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode >= 0)
            {
                int msg = wParam.ToInt32();
                if (msg == 0x0100) // WM_KEYDOWN
                {
                    int vkCode = Marshal.ReadInt32(lParam);
                    bool ctrl = (NativeMethods.GetAsyncKeyState(0x11) & 0x8000) != 0;
                    if (ctrl && onKeyAction != null)
                    {
                        onKeyAction(vkCode);
                    }
                }
            }
            return NativeMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
}
