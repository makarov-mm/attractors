using System;
using System.Runtime.InteropServices;

namespace StrangeAttractors
{
    // Тонкая обёртка над Win32/WGL. Никаких WinForms — окно создаём вручную.
    internal static class Win32
    {
        public const uint WS_OVERLAPPEDWINDOW = 0x00CF0000;
        public const uint WS_VISIBLE = 0x10000000;
        public const int CW_USEDEFAULT = unchecked((int)0x80000000);

        public const uint WM_DESTROY = 0x0002;
        public const uint WM_CLOSE = 0x0010;
        public const uint WM_SIZE = 0x0005;
        public const uint WM_KEYDOWN = 0x0100;
        public const uint WM_MOUSEMOVE = 0x0200;
        public const uint WM_LBUTTONDOWN = 0x0201;
        public const uint WM_LBUTTONUP = 0x0202;
        public const uint WM_RBUTTONDOWN = 0x0204;
        public const uint WM_RBUTTONUP = 0x0205;
        public const uint WM_MOUSEWHEEL = 0x020A;

        public const uint PM_REMOVE = 0x0001;

        public const int SW_SHOW = 5;

        // Виртуальные коды клавиш.
        public const int VK_ESCAPE = 0x1B;
        public const int VK_SPACE = 0x20;
        public const int VK_BACK = 0x08;

        public const uint PFD_DRAW_TO_WINDOW = 0x00000004;
        public const uint PFD_SUPPORT_OPENGL = 0x00000020;
        public const uint PFD_DOUBLEBUFFER = 0x00000001;
        public const byte PFD_TYPE_RGBA = 0;
        public const byte PFD_MAIN_PLANE = 0;

        public delegate IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        public struct WNDCLASSEX
        {
            public uint cbSize;
            public uint style;
            public WndProc lpfnWndProc;
            public int cbClsExtra;
            public int cbWndExtra;
            public IntPtr hInstance;
            public IntPtr hIcon;
            public IntPtr hCursor;
            public IntPtr hbrBackground;
            public string lpszMenuName;
            public string lpszClassName;
            public IntPtr hIconSm;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct MSG
        {
            public IntPtr hwnd;
            public uint message;
            public IntPtr wParam;
            public IntPtr lParam;
            public uint time;
            public POINT pt;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct POINT { public int X, Y; }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT { public int Left, Top, Right, Bottom; }

        [StructLayout(LayoutKind.Sequential)]
        public struct PIXELFORMATDESCRIPTOR
        {
            public ushort nSize;
            public ushort nVersion;
            public uint dwFlags;
            public byte iPixelType;
            public byte cColorBits;
            public byte cRedBits, cRedShift, cGreenBits, cGreenShift, cBlueBits, cBlueShift;
            public byte cAlphaBits, cAlphaShift;
            public byte cAccumBits, cAccumRedBits, cAccumGreenBits, cAccumBlueBits, cAccumAlphaBits;
            public byte cDepthBits, cStencilBits, cAuxBuffers;
            public byte iLayerType, bReserved;
            public uint dwLayerMask, dwVisibleMask, dwDamageMask;
        }

        [DllImport("kernel32.dll")] public static extern IntPtr GetModuleHandle(string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] public static extern IntPtr LoadLibrary(string name);
        [DllImport("kernel32.dll", CharSet = CharSet.Ansi)] public static extern IntPtr GetProcAddress(IntPtr module, string name);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern ushort RegisterClassEx(ref WNDCLASSEX wc);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern IntPtr CreateWindowEx(uint exStyle, string cls, string title, uint style,
            int x, int y, int w, int h, IntPtr parent, IntPtr menu, IntPtr inst, IntPtr param);
        [DllImport("user32.dll")] public static extern IntPtr DefWindowProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam);
        [DllImport("user32.dll")] public static extern bool ShowWindow(IntPtr hWnd, int cmd);
        [DllImport("user32.dll")] public static extern bool UpdateWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern bool DestroyWindow(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern void PostQuitMessage(int code);
        [DllImport("user32.dll")] public static extern bool PeekMessage(out MSG msg, IntPtr hWnd, uint min, uint max, uint remove);
        [DllImport("user32.dll")] public static extern bool TranslateMessage(ref MSG msg);
        [DllImport("user32.dll")] public static extern IntPtr DispatchMessage(ref MSG msg);
        [DllImport("user32.dll")] public static extern IntPtr GetDC(IntPtr hWnd);
        [DllImport("user32.dll")] public static extern int ReleaseDC(IntPtr hWnd, IntPtr hdc);
        [DllImport("user32.dll")] public static extern bool GetClientRect(IntPtr hWnd, out RECT r);
        [DllImport("user32.dll", CharSet = CharSet.Unicode)] public static extern bool SetWindowText(IntPtr hWnd, string text);
        [DllImport("user32.dll")] public static extern IntPtr LoadCursor(IntPtr inst, int cursor);

        [DllImport("gdi32.dll")] public static extern int ChoosePixelFormat(IntPtr hdc, ref PIXELFORMATDESCRIPTOR pfd);
        [DllImport("gdi32.dll")] public static extern bool SetPixelFormat(IntPtr hdc, int format, ref PIXELFORMATDESCRIPTOR pfd);
        [DllImport("gdi32.dll")] public static extern bool SwapBuffers(IntPtr hdc);

        [DllImport("opengl32.dll")] public static extern IntPtr wglCreateContext(IntPtr hdc);
        [DllImport("opengl32.dll")] public static extern bool wglMakeCurrent(IntPtr hdc, IntPtr hglrc);
        [DllImport("opengl32.dll")] public static extern bool wglDeleteContext(IntPtr hglrc);
        [DllImport("opengl32.dll", CharSet = CharSet.Ansi)] public static extern IntPtr wglGetProcAddress(string name);

        public const int IDC_ARROW = 32512;

        // Распаковка координат/значений из lParam/wParam.
        public static int LoWord(IntPtr v) => (short)((long)v & 0xFFFF);
        public static int HiWord(IntPtr v) => (short)(((long)v >> 16) & 0xFFFF);
        public static int WheelDelta(IntPtr wParam) => (short)(((long)wParam >> 16) & 0xFFFF);
    }
}
