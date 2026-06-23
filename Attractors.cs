namespace StrangeAttractors
{
    // Описание одного аттрактора.
    // Сами уравнения (правые части ОДУ) интегрируются на GPU — см. Shaders.UpdateVs,
    // где номеру Index соответствует ветка switch. Здесь только параметры,
    // нужные хосту: шаг dt, радиус «вылета» для респавна, размер облака сидов,
    // центр системы и радиус для подгонки камеры.
    internal sealed class Attractor
    {
        public int Index;
        public string Name;
        public float Dt;          // шаг интегрирования RK4
        public float MaxR;        // если |p| > MaxR — частица перерождается
        public float SeedScale;   // размер стартового облака
        public Vec3 Center;       // центр системы (цель камеры и центр сидов)
        public float ViewRadius;  // характерный радиус для начального зума

        public Attractor(int index, string name, float dt, float maxR,
                         float seedScale, Vec3 center, float viewRadius)
        {
            Index = index; Name = name; Dt = dt; MaxR = maxR;
            SeedScale = seedScale; Center = center; ViewRadius = viewRadius;
        }
    }

    internal static class Attractors
    {
        // Порядок строго совпадает со switch в апдейт-шейдере.
        public static readonly Attractor[] All =
        {
            new Attractor(0, "Lorenz",     0.0050f, 200f, 30f, new Vec3(0, 0, 25f), 38f),
            new Attractor(1, "Aizawa",     0.0100f,  20f, 1.5f, new Vec3(0, 0, 0.4f), 2.0f),
            new Attractor(2, "Thomas",     0.0200f,  60f,  6f, new Vec3(0, 0, 0),    7f),
            new Attractor(3, "Halvorsen",  0.0050f, 120f,  8f, new Vec3(-2, -2, -2), 16f),
            new Attractor(4, "Dadras",     0.0100f, 300f,  6f, new Vec3(0, 0, 0),    22f),
            new Attractor(5, "Chen-Lee",   0.0030f, 300f, 12f, new Vec3(0, 0, 0),    30f),
            new Attractor(6, "Rossler",    0.0120f, 300f, 12f, new Vec3(0, 0, 6f),   22f),
            new Attractor(7, "Lorenz-84",  0.0120f,  60f,  2f, new Vec3(1f, 0, 0),   4f),
        };
    }
}
