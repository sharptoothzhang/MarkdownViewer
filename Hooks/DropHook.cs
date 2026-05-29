using System;
using System.Text;
using MarkdownViewer.Core;

namespace MarkdownViewer.Hooks
{
    class DropHook
    {
        static IntPtr hookId = IntPtr.Zero;
        static NativeMethods.HookProc hookProc;
        static Forms.MainForm form;

        public static void Install(Forms.MainForm f)
        {
            form = f;
            hookProc = new NativeMethods.HookProc(HookCallback);
            IntPtr moduleHandle = NativeMethods.GetModuleHandle(null);
            hookId = NativeMethods.SetWindowsHookEx(NativeMethods.WH_GETMESSAGE, hookProc, moduleHandle, 0);
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
                if (wParam == (IntPtr)NativeMethods.WM_DROPFILES)
                {
                    IntPtr hDrop = lParam;
                    uint fileCount = NativeMethods.DragQueryFile(hDrop, 0xFFFFFFFF, null, 0);
                    if (fileCount > 0)
                    {
                        StringBuilder sb = new StringBuilder(260);
                        NativeMethods.DragQueryFile(hDrop, 0, sb, (uint)sb.Capacity);
                        NativeMethods.DragFinish(hDrop);
                        if (form != null) form.OpenFile(sb.ToString());
                    }
                }
            }
            return NativeMethods.CallNextHookEx(hookId, nCode, wParam, lParam);
        }
    }
}
