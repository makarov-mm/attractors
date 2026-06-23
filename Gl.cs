using System;
using System.Runtime.InteropServices;
using System.Text;

namespace StrangeAttractors
{
    // Базовые функции (GL 1.1) экспортируются opengl32.dll напрямую,
    // всё новее грузим через wglGetProcAddress (с откатом на GetProcAddress).
    internal static unsafe class Gl
    {
        public const uint COLOR_BUFFER_BIT = 0x4000;
        public const uint DEPTH_BUFFER_BIT = 0x0100;
        public const uint FLOAT = 0x1406;
        public const uint ARRAY_BUFFER = 0x8892;
        public const uint TRANSFORM_FEEDBACK_BUFFER = 0x8C8E;
        public const uint DYNAMIC_COPY = 0x88EA;
        public const uint DYNAMIC_DRAW = 0x88E8;
        public const uint POINTS = 0x0000;
        public const uint VERTEX_SHADER = 0x8B31;
        public const uint FRAGMENT_SHADER = 0x8B30;
        public const uint COMPILE_STATUS = 0x8B81;
        public const uint LINK_STATUS = 0x8B82;
        public const uint INFO_LOG_LENGTH = 0x8B84;
        public const uint INTERLEAVED_ATTRIBS = 0x8C8C;
        public const uint RASTERIZER_DISCARD = 0x8C89;
        public const uint BLEND = 0x0BE2;
        public const uint ONE = 1;
        public const uint DEPTH_TEST = 0x0B71;
        public const uint PROGRAM_POINT_SIZE = 0x8642;
        public const uint MULTISAMPLE = 0x809D;
        public const uint VERSION = 0x1F02;
        public const uint RENDERER = 0x1F01;
        public const uint TRUE = 1;
        public const uint FALSE = 0;

        // FBO / текстуры для HDR-конвейера.
        public const uint TRIANGLES = 0x0004;
        public const uint FRAMEBUFFER = 0x8D40;
        public const uint COLOR_ATTACHMENT0 = 0x8CE0;
        public const uint FRAMEBUFFER_COMPLETE = 0x8CD5;
        public const uint TEXTURE_2D = 0x0DE1;
        public const uint TEXTURE0 = 0x84C0;
        public const int RGBA16F = 0x881A;
        public const uint RGBA = 0x1908;
        public const uint TEXTURE_MIN_FILTER = 0x2801;
        public const uint TEXTURE_MAG_FILTER = 0x2800;
        public const uint TEXTURE_WRAP_S = 0x2802;
        public const uint TEXTURE_WRAP_T = 0x2803;
        public const int LINEAR = 0x2601;
        public const int CLAMP_TO_EDGE = 0x812F;

        // Атрибуты для wglCreateContextAttribsARB.
        public const int WGL_CONTEXT_MAJOR_VERSION_ARB = 0x2091;
        public const int WGL_CONTEXT_MINOR_VERSION_ARB = 0x2092;
        public const int WGL_CONTEXT_PROFILE_MASK_ARB = 0x9126;
        public const int WGL_CONTEXT_CORE_PROFILE_BIT_ARB = 0x00000001;

        // --- GL 1.1 напрямую ---
        [DllImport("opengl32.dll")] public static extern void glClear(uint mask);
        [DllImport("opengl32.dll")] public static extern void glClearColor(float r, float g, float b, float a);
        [DllImport("opengl32.dll")] public static extern void glViewport(int x, int y, int w, int h);
        [DllImport("opengl32.dll")] public static extern void glEnable(uint cap);
        [DllImport("opengl32.dll")] public static extern void glDisable(uint cap);
        [DllImport("opengl32.dll")] public static extern void glBlendFunc(uint s, uint d);
        [DllImport("opengl32.dll")] public static extern void glDrawArrays(uint mode, int first, int count);
        [DllImport("opengl32.dll")] public static extern uint glGetError();
        [DllImport("opengl32.dll")] public static extern IntPtr glGetString(uint name);
        [DllImport("opengl32.dll")] public static extern void glDepthMask(byte flag);
        [DllImport("opengl32.dll")] public static extern void glBindTexture(uint target, uint tex);
        [DllImport("opengl32.dll")] public static extern void glTexParameteri(uint target, uint pname, int param);
        [DllImport("opengl32.dll")] public static extern void glTexImage2D(uint target, int level, int iformat, int w, int h, int border, uint format, uint type, IntPtr pixels);
        [DllImport("opengl32.dll")] public static extern void glGenTextures(int n, uint* textures);
        [DllImport("opengl32.dll")] public static extern void glDeleteTextures(int n, uint* textures);

        // --- делегаты современных функций (StdCall = APIENTRY на Windows) ---
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DGenBuffers(int n, uint* buffers);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DBindBuffer(uint target, uint buf);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DBufferData(uint target, IntPtr size, IntPtr data, uint usage);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DBufferSubData(uint target, IntPtr offset, IntPtr size, IntPtr data);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DGenVertexArrays(int n, uint* arrays);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DBindVertexArray(uint a);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DEnableVertexAttribArray(uint index);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DVertexAttribPointer(uint index, int size, uint type, byte norm, int stride, IntPtr ptr);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint DCreateShader(uint type);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DShaderSource(uint shader, int count, IntPtr str, IntPtr len);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DCompileShader(uint shader);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DGetShaderiv(uint shader, uint pname, out int p);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DGetShaderInfoLog(uint shader, int max, out int len, byte[] log);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint DCreateProgram();
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DAttachShader(uint prog, uint shader);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DLinkProgram(uint prog);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DGetProgramiv(uint prog, uint pname, out int p);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DGetProgramInfoLog(uint prog, int max, out int len, byte[] log);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DUseProgram(uint prog);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DDeleteShader(uint shader);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int DGetUniformLocation(uint prog, [MarshalAs(UnmanagedType.LPStr)] string name);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DUniform1i(int loc, int v);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DUniform1f(int loc, float v);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DUniform3f(int loc, float a, float b, float c);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DUniformMatrix4fv(int loc, int count, byte transpose, float[] value);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DTransformFeedbackVaryings(uint prog, int count, IntPtr varyings, uint mode);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DBindBufferBase(uint target, uint index, uint buffer);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DBeginTransformFeedback(uint mode);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DEndTransformFeedback();
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate IntPtr DCreateContextAttribsARB(IntPtr hdc, IntPtr share, int* attribs);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate int DSwapIntervalEXT(int interval);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DActiveTexture(uint texture);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DGenFramebuffers(int n, uint* ids);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DBindFramebuffer(uint target, uint fb);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate void DFramebufferTexture2D(uint target, uint attach, uint textarget, uint tex, int level);
        [UnmanagedFunctionPointer(CallingConvention.StdCall)] delegate uint DCheckFramebufferStatus(uint target);

        static DGenBuffers _genBuffers;
        static DBindBuffer _bindBuffer;
        static DBufferData _bufferData;
        static DBufferSubData _bufferSubData;
        static DGenVertexArrays _genVertexArrays;
        static DBindVertexArray _bindVertexArray;
        static DEnableVertexAttribArray _enableVAA;
        static DVertexAttribPointer _vertexAttribPointer;
        static DCreateShader _createShader;
        static DShaderSource _shaderSource;
        static DCompileShader _compileShader;
        static DGetShaderiv _getShaderiv;
        static DGetShaderInfoLog _getShaderInfoLog;
        static DCreateProgram _createProgram;
        static DAttachShader _attachShader;
        static DLinkProgram _linkProgram;
        static DGetProgramiv _getProgramiv;
        static DGetProgramInfoLog _getProgramInfoLog;
        static DUseProgram _useProgram;
        static DDeleteShader _deleteShader;
        static DGetUniformLocation _getUniformLocation;
        static DUniform1i _uniform1i;
        static DUniform1f _uniform1f;
        static DUniform3f _uniform3f;
        static DUniformMatrix4fv _uniformMatrix4fv;
        static DTransformFeedbackVaryings _transformFeedbackVaryings;
        static DBindBufferBase _bindBufferBase;
        static DBeginTransformFeedback _beginTF;
        static DEndTransformFeedback _endTF;
        static DSwapIntervalEXT _swapInterval;
        static DActiveTexture _activeTexture;
        static DGenFramebuffers _genFramebuffers;
        static DBindFramebuffer _bindFramebuffer;
        static DFramebufferTexture2D _framebufferTexture2D;
        static DCheckFramebufferStatus _checkFramebufferStatus;

        static IntPtr _opengl32;

        static IntPtr Resolve(string name)
        {
            IntPtr p = Win32.wglGetProcAddress(name);
            long v = (long)p;
            if (v == 0 || v == 1 || v == 2 || v == 3 || v == -1)
            {
                if (_opengl32 == IntPtr.Zero)
                    _opengl32 = Win32.LoadLibrary("opengl32.dll");
                p = Win32.GetProcAddress(_opengl32, name);
            }
            return p;
        }

        static T Load<T>(string name) where T : Delegate
        {
            IntPtr p = Resolve(name);
            if (p == IntPtr.Zero)
                throw new Exception("Не найдена функция OpenGL: " + name);
            return Marshal.GetDelegateForFunctionPointer<T>(p);
        }

        // Создание core-контекста 3.3 поверх временного legacy-контекста.
        public static IntPtr CreateCoreContext(IntPtr hdc)
        {
            var fp = Resolve("wglCreateContextAttribsARB");
            if (fp == IntPtr.Zero)
                throw new Exception("wglCreateContextAttribsARB недоступна");
            var create = Marshal.GetDelegateForFunctionPointer<DCreateContextAttribsARB>(fp);

            int[] attribs =
            {
                WGL_CONTEXT_MAJOR_VERSION_ARB, 3,
                WGL_CONTEXT_MINOR_VERSION_ARB, 3,
                WGL_CONTEXT_PROFILE_MASK_ARB, WGL_CONTEXT_CORE_PROFILE_BIT_ARB,
                0
            };
            fixed (int* a = attribs)
                return create(hdc, IntPtr.Zero, a);
        }

        // Должно вызываться после wglMakeCurrent на валидном контексте.
        public static void LoadAll()
        {
            _genBuffers = Load<DGenBuffers>("glGenBuffers");
            _bindBuffer = Load<DBindBuffer>("glBindBuffer");
            _bufferData = Load<DBufferData>("glBufferData");
            _bufferSubData = Load<DBufferSubData>("glBufferSubData");
            _genVertexArrays = Load<DGenVertexArrays>("glGenVertexArrays");
            _bindVertexArray = Load<DBindVertexArray>("glBindVertexArray");
            _enableVAA = Load<DEnableVertexAttribArray>("glEnableVertexAttribArray");
            _vertexAttribPointer = Load<DVertexAttribPointer>("glVertexAttribPointer");
            _createShader = Load<DCreateShader>("glCreateShader");
            _shaderSource = Load<DShaderSource>("glShaderSource");
            _compileShader = Load<DCompileShader>("glCompileShader");
            _getShaderiv = Load<DGetShaderiv>("glGetShaderiv");
            _getShaderInfoLog = Load<DGetShaderInfoLog>("glGetShaderInfoLog");
            _createProgram = Load<DCreateProgram>("glCreateProgram");
            _attachShader = Load<DAttachShader>("glAttachShader");
            _linkProgram = Load<DLinkProgram>("glLinkProgram");
            _getProgramiv = Load<DGetProgramiv>("glGetProgramiv");
            _getProgramInfoLog = Load<DGetProgramInfoLog>("glGetProgramInfoLog");
            _useProgram = Load<DUseProgram>("glUseProgram");
            _deleteShader = Load<DDeleteShader>("glDeleteShader");
            _getUniformLocation = Load<DGetUniformLocation>("glGetUniformLocation");
            _uniform1i = Load<DUniform1i>("glUniform1i");
            _uniform1f = Load<DUniform1f>("glUniform1f");
            _uniform3f = Load<DUniform3f>("glUniform3f");
            _uniformMatrix4fv = Load<DUniformMatrix4fv>("glUniformMatrix4fv");
            _transformFeedbackVaryings = Load<DTransformFeedbackVaryings>("glTransformFeedbackVaryings");
            _bindBufferBase = Load<DBindBufferBase>("glBindBufferBase");
            _beginTF = Load<DBeginTransformFeedback>("glBeginTransformFeedback");
            _endTF = Load<DEndTransformFeedback>("glEndTransformFeedback");
            try { _swapInterval = Load<DSwapIntervalEXT>("wglSwapIntervalEXT"); } catch { _swapInterval = null; }
            _activeTexture = Load<DActiveTexture>("glActiveTexture");
            _genFramebuffers = Load<DGenFramebuffers>("glGenFramebuffers");
            _bindFramebuffer = Load<DBindFramebuffer>("glBindFramebuffer");
            _framebufferTexture2D = Load<DFramebufferTexture2D>("glFramebufferTexture2D");
            _checkFramebufferStatus = Load<DCheckFramebufferStatus>("glCheckFramebufferStatus");
        }

        // --- управляемые обёртки ---
        public static uint GenBuffer()
        {
            uint b;
            _genBuffers(1, &b);
            return b;
        }

        public static void BindBuffer(uint target, uint buf) => _bindBuffer(target, buf);

        public static void BufferData(uint target, int bytes, IntPtr data, uint usage) =>
            _bufferData(target, new IntPtr(bytes), data, usage);

        public static void BufferSubData(uint target, int offset, int bytes, IntPtr data) =>
            _bufferSubData(target, new IntPtr(offset), new IntPtr(bytes), data);

        public static uint GenVertexArray()
        {
            uint a;
            _genVertexArrays(1, &a);
            return a;
        }

        public static void BindVertexArray(uint a) => _bindVertexArray(a);
        public static void EnableVertexAttribArray(uint i) => _enableVAA(i);
        public static void VertexAttribPointer(uint i, int size, uint type, bool norm, int stride, int offset) =>
            _vertexAttribPointer(i, size, type, (byte)(norm ? 1 : 0), stride, new IntPtr(offset));

        public static void UseProgram(uint p) => _useProgram(p);
        public static int GetUniformLocation(uint p, string name) => _getUniformLocation(p, name);
        public static void Uniform1i(int loc, int v) => _uniform1i(loc, v);
        public static void Uniform1f(int loc, float v) => _uniform1f(loc, v);
        public static void Uniform3f(int loc, float a, float b, float c) => _uniform3f(loc, a, b, c);
        public static void UniformMatrix4(int loc, float[] m) => _uniformMatrix4fv(loc, 1, 0, m);

        public static void BindBufferBase(uint target, uint index, uint buffer) => _bindBufferBase(target, index, buffer);
        public static void BeginTransformFeedback(uint mode) => _beginTF(mode);
        public static void EndTransformFeedback() => _endTF();
        public static void SwapInterval(int i) { if (_swapInterval != null) _swapInterval(i); }

        public static void ActiveTexture(uint t) => _activeTexture(t);

        public static uint GenTexture()
        {
            uint t;
            glGenTextures(1, &t);
            return t;
        }

        public static void DeleteTexture(uint t) => glDeleteTextures(1, &t);

        public static uint GenFramebuffer()
        {
            uint f;
            _genFramebuffers(1, &f);
            return f;
        }

        public static void BindFramebuffer(uint target, uint fb) => _bindFramebuffer(target, fb);
        public static void FramebufferTexture2D(uint target, uint attach, uint textarget, uint tex, int level) =>
            _framebufferTexture2D(target, attach, textarget, tex, level);
        public static uint CheckFramebufferStatus(uint target) => _checkFramebufferStatus(target);

        public static string GetString(uint name)
        {
            IntPtr p = glGetString(name);
            return p == IntPtr.Zero ? "" : Marshal.PtrToStringAnsi(p);
        }

        // Компиляция шейдера с проверкой лога.
        static uint CompileShader(uint type, string src)
        {
            uint sh = _createShader(type);
            byte[] bytes = Encoding.UTF8.GetBytes(src);
            fixed (byte* pb = bytes)
            {
                IntPtr strPtr = (IntPtr)pb;
                int len = bytes.Length;
                _shaderSource(sh, 1, (IntPtr)(&strPtr), (IntPtr)(&len));
            }
            _compileShader(sh);
            _getShaderiv(sh, COMPILE_STATUS, out int ok);
            if (ok == 0)
            {
                byte[] log = new byte[4096];
                _getShaderInfoLog(sh, log.Length, out int n, log);
                throw new Exception("Ошибка компиляции шейдера:\n" + Encoding.UTF8.GetString(log, 0, Math.Max(0, n)));
            }
            return sh;
        }

        // Сборка программы. feedbackVarying != null -> регистрируем varying до линковки.
        public static uint BuildProgram(string vs, string fs, string feedbackVarying)
        {
            uint v = CompileShader(VERTEX_SHADER, vs);
            uint f = CompileShader(FRAGMENT_SHADER, fs);
            uint prog = _createProgram();
            _attachShader(prog, v);
            _attachShader(prog, f);

            if (feedbackVarying != null)
            {
                byte[] nameBytes = Encoding.ASCII.GetBytes(feedbackVarying + "\0");
                fixed (byte* pn = nameBytes)
                {
                    IntPtr namePtr = (IntPtr)pn;
                    _transformFeedbackVaryings(prog, 1, (IntPtr)(&namePtr), INTERLEAVED_ATTRIBS);
                }
            }

            _linkProgram(prog);
            _getProgramiv(prog, LINK_STATUS, out int ok);
            if (ok == 0)
            {
                byte[] log = new byte[4096];
                _getProgramInfoLog(prog, log.Length, out int n, log);
                throw new Exception("Ошибка линковки программы:\n" + Encoding.UTF8.GetString(log, 0, Math.Max(0, n)));
            }
            _deleteShader(v);
            _deleteShader(f);
            return prog;
        }
    }
}
