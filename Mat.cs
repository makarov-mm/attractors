using System;

namespace StrangeAttractors
{
    // Минимальная векторно-матричная математика.
    // Матрицы хранятся как float[16] в column-major (как ждёт OpenGL).
    internal struct Vec3
    {
        public float X, Y, Z;
        public Vec3(float x, float y, float z) { X = x; Y = y; Z = z; }

        public static Vec3 operator +(Vec3 a, Vec3 b) => new Vec3(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        public static Vec3 operator -(Vec3 a, Vec3 b) => new Vec3(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        public static Vec3 operator *(Vec3 a, float s) => new Vec3(a.X * s, a.Y * s, a.Z * s);

        public float Length() => (float)Math.Sqrt(X * X + Y * Y + Z * Z);

        public Vec3 Normalized()
        {
            float l = Length();
            return l > 1e-8f ? new Vec3(X / l, Y / l, Z / l) : new Vec3(0, 0, 0);
        }

        public static Vec3 Cross(Vec3 a, Vec3 b) =>
            new Vec3(a.Y * b.Z - a.Z * b.Y,
                     a.Z * b.X - a.X * b.Z,
                     a.X * b.Y - a.Y * b.X);

        public static float Dot(Vec3 a, Vec3 b) => a.X * b.X + a.Y * b.Y + a.Z * b.Z;
    }

    internal static class Mat
    {
        // Произведение двух column-major матриц: r = a * b.
        public static float[] Mul(float[] a, float[] b)
        {
            float[] r = new float[16];
            for (int col = 0; col < 4; col++)
                for (int row = 0; row < 4; row++)
                {
                    float s = 0f;
                    for (int k = 0; k < 4; k++)
                        s += a[k * 4 + row] * b[col * 4 + k];
                    r[col * 4 + row] = s;
                }
            return r;
        }

        // Перспективная проекция (правосторонняя, как gluPerspective).
        public static float[] Perspective(float fovYrad, float aspect, float near, float far)
        {
            float f = 1f / (float)Math.Tan(fovYrad * 0.5f);
            float[] m = new float[16];
            m[0] = f / aspect;
            m[5] = f;
            m[10] = (far + near) / (near - far);
            m[11] = -1f;
            m[14] = (2f * far * near) / (near - far);
            return m;
        }

        // Камера, смотрящая из eye в target.
        public static float[] LookAt(Vec3 eye, Vec3 target, Vec3 up)
        {
            Vec3 f = (target - eye).Normalized();
            Vec3 s = Vec3.Cross(f, up).Normalized();
            Vec3 u = Vec3.Cross(s, f);

            float[] m = new float[16];
            m[0] = s.X; m[4] = s.Y; m[8] = s.Z;
            m[1] = u.X; m[5] = u.Y; m[9] = u.Z;
            m[2] = -f.X; m[6] = -f.Y; m[10] = -f.Z;
            m[12] = -Vec3.Dot(s, eye);
            m[13] = -Vec3.Dot(u, eye);
            m[14] = Vec3.Dot(f, eye);
            m[15] = 1f;
            return m;
        }
    }
}
