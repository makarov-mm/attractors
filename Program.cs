using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace StrangeAttractors
{
    internal static class Program
    {
        // Состояние приложения (статическое — WndProc тоже статический).
        static bool _running = true;
        static int _w = 1280, _h = 800;

        static Renderer _renderer;
        static int _current;
        static readonly Random _rng = new Random(1234);

        // Камера.
        static float _yaw = 0.7f, _pitch = 0.35f, _dist;
        static Vec3 _target;
        static bool _dragging;
        static int _lastX, _lastY;
        static bool _autoRotate = true;

        // Параметры рендера.
        static bool _paused;
        static float _exposure = 0.45f;
        const int SubSteps = 2;
        const int ParticleCount = 1 << 20; // ~1.05M частиц

        static Win32.WndProc _wndProcRef; // держим делегат, чтобы не собрал GC
        static IntPtr _hwnd, _hdc;

        [STAThread]
        static void Main()
        {
            const string cls = "StrangeAttractorsWindow";
            IntPtr inst = Win32.GetModuleHandle(null);

            _wndProcRef = WndProc;
            var wc = new Win32.WNDCLASSEX
            {
                cbSize = (uint)Marshal.SizeOf<Win32.WNDCLASSEX>(),
                style = 0x0023, // CS_HREDRAW|CS_VREDRAW|CS_OWNDC
                lpfnWndProc = _wndProcRef,
                hInstance = inst,
                hCursor = Win32.LoadCursor(IntPtr.Zero, Win32.IDC_ARROW),
                lpszClassName = cls
            };
            Win32.RegisterClassEx(ref wc);

            _hwnd = Win32.CreateWindowEx(0, cls, "Strange Attractors",
                Win32.WS_OVERLAPPEDWINDOW | Win32.WS_VISIBLE,
                Win32.CW_USEDEFAULT, Win32.CW_USEDEFAULT, _w, _h,
                IntPtr.Zero, IntPtr.Zero, inst, IntPtr.Zero);

            _hdc = Win32.GetDC(_hwnd);
            SetupPixelFormat(_hdc);

            // Временный legacy-контекст нужен только чтобы добраться до
            // wglCreateContextAttribsARB, затем создаём core 3.3.
            IntPtr legacy = Win32.wglCreateContext(_hdc);
            Win32.wglMakeCurrent(_hdc, legacy);

            IntPtr core = Gl.CreateCoreContext(_hdc);
            Win32.wglMakeCurrent(_hdc, core);
            Win32.wglDeleteContext(legacy);

            Gl.LoadAll();
            Gl.SwapInterval(1); // vsync

            Gl.glEnable(Gl.PROGRAM_POINT_SIZE);
            Gl.glClearColor(0.015f, 0.015f, 0.022f, 1f);

            _renderer = new Renderer(ParticleCount);
            _renderer.Init();
            SelectAttractor(0);

            Win32.GetClientRect(_hwnd, out var rc);
            _w = Math.Max(1, rc.Right - rc.Left);
            _h = Math.Max(1, rc.Bottom - rc.Top);

            var sw = Stopwatch.StartNew();

            // Главный цикл: качаем сообщения, шагаем интегрирование, рисуем.
            while (_running)
            {
                while (Win32.PeekMessage(out var msg, IntPtr.Zero, 0, 0, Win32.PM_REMOVE))
                {
                    Win32.TranslateMessage(ref msg);
                    Win32.DispatchMessage(ref msg);
                }
                if (!_running) break;

                float time = (float)sw.Elapsed.TotalSeconds;
                var a = Attractors.All[_current];

                if (!_paused)
                    for (int s = 0; s < SubSteps; s++)
                        _renderer.Step(a, time);

                if (_autoRotate && !_dragging)
                    _yaw += 0.0035f;

                Render(a);
                Win32.SwapBuffers(_hdc);
            }

            Win32.wglMakeCurrent(IntPtr.Zero, IntPtr.Zero);
            Win32.wglDeleteContext(core);
            Win32.ReleaseDC(_hwnd, _hdc);
            Win32.DestroyWindow(_hwnd);
        }

        static void Render(Attractor a)
        {
            float aspect = (float)_w / _h;
            var proj = Mat.Perspective(50f * (float)Math.PI / 180f, aspect, a.ViewRadius * 0.02f, a.ViewRadius * 40f);

            Vec3 eye = _target + new Vec3(
                (float)(Math.Cos(_pitch) * Math.Sin(_yaw)),
                (float)Math.Sin(_pitch),
                (float)(Math.Cos(_pitch) * Math.Cos(_yaw))) * _dist;

            var view = Mat.LookAt(eye, _target, new Vec3(0, 1, 0));
            var mvp = Mat.Mul(proj, view);

            // Масштаб скорости для палитры подбираем под характерный размер системы.
            float speedScale = 1f / (a.ViewRadius * 0.6f);
            _renderer.RenderFrame(mvp, _w, _h, 80f, speedScale, 0.10f, _exposure);
        }

        static void SelectAttractor(int index)
        {
            _current = (index % Attractors.All.Length + Attractors.All.Length) % Attractors.All.Length;
            var a = Attractors.All[_current];
            _renderer.Reseed(a, _rng);
            _target = a.Center;
            _dist = a.ViewRadius * 2.4f;
            Win32.SetWindowText(_hwnd,
                $"Strange Attractors — [{_current + 1}/{Attractors.All.Length}] {a.Name}   " +
                "(Space next, 1-8 select, R reseed, A auto-rot, P pause, +/- exposure, drag/wheel camera)");
        }

        static IntPtr WndProc(IntPtr hWnd, uint msg, IntPtr wParam, IntPtr lParam)
        {
            switch (msg)
            {
                case Win32.WM_CLOSE:
                case Win32.WM_DESTROY:
                    _running = false;
                    Win32.PostQuitMessage(0);
                    return IntPtr.Zero;

                case Win32.WM_SIZE:
                    _w = Math.Max(1, Win32.LoWord(lParam));
                    _h = Math.Max(1, Win32.HiWord(lParam));
                    return IntPtr.Zero;

                case Win32.WM_LBUTTONDOWN:
                    _dragging = true;
                    _lastX = Win32.LoWord(lParam);
                    _lastY = Win32.HiWord(lParam);
                    return IntPtr.Zero;

                case Win32.WM_LBUTTONUP:
                    _dragging = false;
                    return IntPtr.Zero;

                case Win32.WM_MOUSEMOVE:
                    if (_dragging)
                    {
                        int x = Win32.LoWord(lParam);
                        int y = Win32.HiWord(lParam);
                        _yaw += (x - _lastX) * 0.005f;
                        _pitch += (y - _lastY) * 0.005f;
                        _pitch = Math.Max(-1.5f, Math.Min(1.5f, _pitch));
                        _lastX = x; _lastY = y;
                    }
                    return IntPtr.Zero;

                case Win32.WM_MOUSEWHEEL:
                    {
                        int delta = Win32.WheelDelta(wParam);
                        _dist *= (float)Math.Pow(0.9, delta / 120.0);
                        _dist = Math.Max(0.05f, _dist);
                    }
                    return IntPtr.Zero;

                case Win32.WM_KEYDOWN:
                    OnKey((int)(long)wParam);
                    return IntPtr.Zero;
            }
            return Win32.DefWindowProc(hWnd, msg, wParam, lParam);
        }

        static void OnKey(int vk)
        {
            switch (vk)
            {
                case Win32.VK_ESCAPE: _running = false; Win32.PostQuitMessage(0); break;
                case Win32.VK_SPACE: SelectAttractor(_current + 1); break;
                case Win32.VK_BACK: SelectAttractor(_current - 1); break;
                case 0x52: SelectAttractor(_current); break;          // R — reseed
                case 0x41: _autoRotate = !_autoRotate; break;          // A
                case 0x50: _paused = !_paused; break;                  // P
                case 0xBB: case 0x6B: _exposure = Math.Min(8f, _exposure * 1.15f); break; // + / =
                case 0xBD: case 0x6D: _exposure = Math.Max(0.05f, _exposure / 1.15f); break; // - / _
                default:
                    if (vk >= 0x31 && vk <= 0x38) // клавиши 1..8
                        SelectAttractor(vk - 0x31);
                    break;
            }
        }

        static void SetupPixelFormat(IntPtr hdc)
        {
            var pfd = new Win32.PIXELFORMATDESCRIPTOR
            {
                nSize = (ushort)Marshal.SizeOf<Win32.PIXELFORMATDESCRIPTOR>(),
                nVersion = 1,
                dwFlags = Win32.PFD_DRAW_TO_WINDOW | Win32.PFD_SUPPORT_OPENGL | Win32.PFD_DOUBLEBUFFER,
                iPixelType = Win32.PFD_TYPE_RGBA,
                cColorBits = 32,
                cDepthBits = 24,
                iLayerType = Win32.PFD_MAIN_PLANE
            };
            int fmt = Win32.ChoosePixelFormat(hdc, ref pfd);
            Win32.SetPixelFormat(hdc, fmt, ref pfd);
        }
    }
}
