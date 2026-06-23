using System;
using System.Runtime.InteropServices;

namespace StrangeAttractors
{
    // Облако частиц интегрируется целиком на GPU: апдейт-программа читает
    // позиции из одного буфера и пишет в другой через transform feedback,
    // буферы пинг-понгуются.
    //
    // Рендер двухпроходный: точки копятся аддитивно в float-буфер (RGBA16F),
    // затем полноэкранный проход делает тон-маппинг ACES + гамму. Без этого
    // миллион аддитивных точек пересвечивает картинку в плоский белый.
    internal sealed unsafe class Renderer
    {
        readonly int _count;
        readonly uint[] _buf = new uint[2];
        readonly uint[] _vao = new uint[2];
        int _src, _dst;

        uint _update, _render, _tonemap;
        uint _emptyVao;

        // HDR-таргет.
        uint _fbo, _hdrTex;
        int _fbW, _fbH;

        // Униформы апдейта.
        int uType, uDt, uTime, uMaxR, uSeedScale, uCenter;
        // Униформы рендера точек.
        int uMVP, uPointSize, uSpeedScale, uIntensity;
        // Униформы тон-маппинга.
        int uHdr, uExposure;

        float[] _seed;

        public Renderer(int count)
        {
            _count = count;
            _seed = new float[count * 4];
        }

        public void Init()
        {
            _update = Gl.BuildProgram(Shaders.UpdateVs, FeedbackFs, "vOut");
            _render = Gl.BuildProgram(Shaders.RenderVs, Shaders.RenderFs, null);
            _tonemap = Gl.BuildProgram(Shaders.TonemapVs, Shaders.TonemapFs, null);

            uType = Gl.GetUniformLocation(_update, "uType");
            uDt = Gl.GetUniformLocation(_update, "uDt");
            uTime = Gl.GetUniformLocation(_update, "uTime");
            uMaxR = Gl.GetUniformLocation(_update, "uMaxR");
            uSeedScale = Gl.GetUniformLocation(_update, "uSeedScale");
            uCenter = Gl.GetUniformLocation(_update, "uCenter");

            uMVP = Gl.GetUniformLocation(_render, "uMVP");
            uPointSize = Gl.GetUniformLocation(_render, "uPointSize");
            uSpeedScale = Gl.GetUniformLocation(_render, "uSpeedScale");
            uIntensity = Gl.GetUniformLocation(_render, "uIntensity");

            uHdr = Gl.GetUniformLocation(_tonemap, "uHdr");
            uExposure = Gl.GetUniformLocation(_tonemap, "uExposure");

            _emptyVao = Gl.GenVertexArray();

            for (int i = 0; i < 2; i++)
            {
                _buf[i] = Gl.GenBuffer();
                Gl.BindBuffer(Gl.ARRAY_BUFFER, _buf[i]);
                Gl.BufferData(Gl.ARRAY_BUFFER, _count * 4 * sizeof(float), IntPtr.Zero, Gl.DYNAMIC_COPY);

                _vao[i] = Gl.GenVertexArray();
                Gl.BindVertexArray(_vao[i]);
                Gl.BindBuffer(Gl.ARRAY_BUFFER, _buf[i]);
                Gl.EnableVertexAttribArray(0);
                Gl.VertexAttribPointer(0, 4, Gl.FLOAT, false, 0, 0);
            }
            Gl.BindVertexArray(0);
            _src = 0; _dst = 1;
        }

        // Создаёт/пересоздаёт HDR-текстуру и FBO под текущий размер окна.
        void EnsureTarget(int w, int h)
        {
            if (w == _fbW && h == _fbH && _fbo != 0) return;
            _fbW = w; _fbH = h;

            if (_hdrTex != 0) Gl.DeleteTexture(_hdrTex);
            _hdrTex = Gl.GenTexture();
            Gl.glBindTexture(Gl.TEXTURE_2D, _hdrTex);
            Gl.glTexImage2D(Gl.TEXTURE_2D, 0, Gl.RGBA16F, w, h, 0, Gl.RGBA, Gl.FLOAT, IntPtr.Zero);
            Gl.glTexParameteri(Gl.TEXTURE_2D, Gl.TEXTURE_MIN_FILTER, Gl.LINEAR);
            Gl.glTexParameteri(Gl.TEXTURE_2D, Gl.TEXTURE_MAG_FILTER, Gl.LINEAR);
            Gl.glTexParameteri(Gl.TEXTURE_2D, Gl.TEXTURE_WRAP_S, Gl.CLAMP_TO_EDGE);
            Gl.glTexParameteri(Gl.TEXTURE_2D, Gl.TEXTURE_WRAP_T, Gl.CLAMP_TO_EDGE);

            if (_fbo == 0) _fbo = Gl.GenFramebuffer();
            Gl.BindFramebuffer(Gl.FRAMEBUFFER, _fbo);
            Gl.FramebufferTexture2D(Gl.FRAMEBUFFER, Gl.COLOR_ATTACHMENT0, Gl.TEXTURE_2D, _hdrTex, 0);
            Gl.BindFramebuffer(Gl.FRAMEBUFFER, 0);
        }

        // Заполняет оба буфера стартовым облаком вокруг центра системы.
        public void Reseed(Attractor a, Random rng)
        {
            for (int i = 0; i < _count; i++)
            {
                _seed[i * 4 + 0] = a.Center.X + ((float)rng.NextDouble() - 0.5f) * a.SeedScale;
                _seed[i * 4 + 1] = a.Center.Y + ((float)rng.NextDouble() - 0.5f) * a.SeedScale;
                _seed[i * 4 + 2] = a.Center.Z + ((float)rng.NextDouble() - 0.5f) * a.SeedScale;
                _seed[i * 4 + 3] = 0f;
            }
            fixed (float* p = _seed)
            {
                for (int i = 0; i < 2; i++)
                {
                    Gl.BindBuffer(Gl.ARRAY_BUFFER, _buf[i]);
                    Gl.BufferSubData(Gl.ARRAY_BUFFER, 0, _count * 4 * sizeof(float), (IntPtr)p);
                }
            }
            _src = 0; _dst = 1;
        }

        // Один подшаг интегрирования: src -> dst, затем меняем местами.
        public void Step(Attractor a, float time)
        {
            Gl.UseProgram(_update);
            Gl.Uniform1i(uType, a.Index);
            Gl.Uniform1f(uDt, a.Dt);
            Gl.Uniform1f(uTime, time);
            Gl.Uniform1f(uMaxR, a.MaxR);
            Gl.Uniform1f(uSeedScale, a.SeedScale);
            Gl.Uniform3f(uCenter, a.Center.X, a.Center.Y, a.Center.Z);

            Gl.glEnable(Gl.RASTERIZER_DISCARD);
            Gl.BindVertexArray(_vao[_src]);
            Gl.BindBufferBase(Gl.TRANSFORM_FEEDBACK_BUFFER, 0, _buf[_dst]);
            Gl.BeginTransformFeedback(Gl.POINTS);
            Gl.glDrawArrays(Gl.POINTS, 0, _count);
            Gl.EndTransformFeedback();
            Gl.glDisable(Gl.RASTERIZER_DISCARD);

            int t = _src; _src = _dst; _dst = t;
        }

        // Полный кадр: точки в HDR-буфер, затем тон-маппинг на экран.
        public void RenderFrame(float[] mvp, int w, int h,
                                float pointSize, float speedScale, float intensity, float exposure)
        {
            EnsureTarget(w, h);

            // --- проход 1: аддитивное накопление точек в float-буфер ---
            Gl.BindFramebuffer(Gl.FRAMEBUFFER, _fbo);
            Gl.glViewport(0, 0, w, h);
            Gl.glClearColor(0f, 0f, 0f, 1f);
            Gl.glClear(Gl.COLOR_BUFFER_BIT);

            Gl.UseProgram(_render);
            Gl.UniformMatrix4(uMVP, mvp);
            Gl.Uniform1f(uPointSize, pointSize);
            Gl.Uniform1f(uSpeedScale, speedScale);
            Gl.Uniform1f(uIntensity, intensity);

            Gl.glDisable(Gl.DEPTH_TEST);
            Gl.glDepthMask(0);
            Gl.glEnable(Gl.BLEND);
            Gl.glBlendFunc(Gl.ONE, Gl.ONE);

            Gl.BindVertexArray(_vao[_src]);
            Gl.glDrawArrays(Gl.POINTS, 0, _count);

            // --- проход 2: тон-маппинг HDR -> экран ---
            Gl.BindFramebuffer(Gl.FRAMEBUFFER, 0);
            Gl.glViewport(0, 0, w, h);
            Gl.glDisable(Gl.BLEND);
            Gl.glClear(Gl.COLOR_BUFFER_BIT);

            Gl.UseProgram(_tonemap);
            Gl.ActiveTexture(Gl.TEXTURE0);
            Gl.glBindTexture(Gl.TEXTURE_2D, _hdrTex);
            Gl.Uniform1i(uHdr, 0);
            Gl.Uniform1f(uExposure, exposure);

            Gl.BindVertexArray(_emptyVao);
            Gl.glDrawArrays(Gl.TRIANGLES, 0, 3);
            Gl.BindVertexArray(0);
        }

        // Тривиальный фрагментный шейдер для апдейт-программы (не выполняется).
        const string FeedbackFs = @"#version 330 core
out vec4 frag;
void main() { frag = vec4(0.0); }
";
    }
}
