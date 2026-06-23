namespace StrangeAttractors
{
    // Все шейдеры в одном месте. Числа в правых частях — канонические
    // параметры соответствующих систем.
    internal static class Shaders
    {
        // Вершинный шейдер апдейта. Работает с включённым RASTERIZER_DISCARD:
        // читает позицию частицы, делает один шаг RK4 выбранной системы и пишет
        // результат в transform feedback. В w кладём модуль скорости (для цвета).
        public const string UpdateVs = @"#version 330 core
layout(location = 0) in vec4 inData;   // xyz — позиция, w не используется на входе

uniform int   uType;       // номер аттрактора
uniform float uDt;         // шаг интегрирования
uniform float uTime;       // время (для псевдослучайного респавна)
uniform float uMaxR;       // радиус вылета
uniform float uSeedScale;  // размер облака при перерождении
uniform vec3  uCenter;     // центр системы

out vec4 vOut;             // transform feedback varying

// Правая часть ОДУ для каждой системы.
vec3 deriv(int t, vec3 p)
{
    float x = p.x, y = p.y, z = p.z;
    if (t == 0) {              // Lorenz
        return vec3(10.0 * (y - x),
                    x * (28.0 - z) - y,
                    x * y - (8.0 / 3.0) * z);
    } else if (t == 1) {       // Aizawa
        float a = 0.95, b = 0.7, c = 0.6, d = 3.5, e = 0.25, f = 0.1;
        return vec3((z - b) * x - d * y,
                    d * x + (z - b) * y,
                    c + a * z - z * z * z / 3.0 - (x * x + y * y) * (1.0 + e * z) + f * z * x * x * x);
    } else if (t == 2) {       // Thomas
        float b = 0.208186;
        return vec3(sin(y) - b * x,
                    sin(z) - b * y,
                    sin(x) - b * z);
    } else if (t == 3) {       // Halvorsen
        float a = 1.89;
        return vec3(-a * x - 4.0 * y - 4.0 * z - y * y,
                    -a * y - 4.0 * z - 4.0 * x - z * z,
                    -a * z - 4.0 * x - 4.0 * y - x * x);
    } else if (t == 4) {       // Dadras
        float a = 3.0, b = 2.7, c = 1.7, d = 2.0, e = 9.0;
        return vec3(y - a * x + b * y * z,
                    c * y - x * z + z,
                    d * x * y - e * z);
    } else if (t == 5) {       // Chen-Lee
        float a = 5.0, b = -10.0, c = -0.38;
        return vec3(a * x - y * z,
                    b * y + x * z,
                    c * z + x * y / 3.0);
    } else if (t == 6) {       // Rossler
        float a = 0.2, b = 0.2, c = 5.7;
        return vec3(-y - z,
                    x + a * y,
                    b + z * (x - c));
    } else {                   // Lorenz-84
        float a = 0.25, b = 4.0, f = 8.0, g = 1.0;
        return vec3(-a * x - y * y - z * z + a * f,
                    -y + x * y - b * x * z + g,
                    -z + b * x * y + x * z);
    }
}

// Хеш для псевдослучайного перерождения частиц.
float hash(uint n)
{
    n = (n << 13U) ^ n;
    n = n * (n * n * 15731U + 789221U) + 1376312589U;
    return float(n & 0x7fffffffU) / float(0x7fffffff);
}

void main()
{
    vec3 p = inData.xyz;
    float dt = uDt;

    // Классический Рунге-Кутта 4-го порядка.
    vec3 k1 = deriv(uType, p);
    vec3 k2 = deriv(uType, p + 0.5 * dt * k1);
    vec3 k3 = deriv(uType, p + 0.5 * dt * k2);
    vec3 k4 = deriv(uType, p + dt * k3);
    vec3 np = p + (dt / 6.0) * (k1 + 2.0 * k2 + 2.0 * k3 + k4);

    float speed = length(np - p) / dt;

    // Вылет за пределы или NaN — перерождаем частицу около центра.
    if (!(length(np) < uMaxR))
    {
        uint id = uint(gl_VertexID);
        uint salt = uint(uTime * 60.0);
        float rx = hash(id * 3u + 0u + salt);
        float ry = hash(id * 3u + 1u + salt);
        float rz = hash(id * 3u + 2u + salt);
        np = uCenter + (vec3(rx, ry, rz) - 0.5) * uSeedScale;
        speed = 0.0;
    }

    vOut = vec4(np, speed);
    gl_Position = vec4(np, 1.0); // не используется (растеризация выключена)
}
";

        // Рендер: точка-спрайт, размер с учётом расстояния, цвет от скорости.
        public const string RenderVs = @"#version 330 core
layout(location = 0) in vec4 inData;   // xyz — позиция, w — скорость

uniform mat4  uMVP;
uniform float uPointSize;
uniform float uSpeedScale;

out float vT;

void main()
{
    vec4 clip = uMVP * vec4(inData.xyz, 1.0);
    gl_Position = clip;
    vT = clamp(inData.w * uSpeedScale, 0.0, 1.0);
    float d = max(clip.w, 0.001);
    gl_PointSize = clamp(uPointSize / d, 1.0, 2.5);
}
";

        public const string RenderFs = @"#version 330 core
in float vT;
out vec4 frag;

uniform float uIntensity;

// Палитра холодный -> горячий по скорости.
vec3 palette(float t)
{
    vec3 c0 = vec3(0.10, 0.20, 0.70);
    vec3 c1 = vec3(0.10, 0.75, 0.85);
    vec3 c2 = vec3(0.95, 0.85, 0.25);
    vec3 c3 = vec3(1.00, 0.30, 0.18);
    vec3 col = mix(c0, c1, smoothstep(0.0, 0.40, t));
    col = mix(col, c2, smoothstep(0.40, 0.72, t));
    col = mix(col, c3, smoothstep(0.72, 1.0, t));
    return col;
}

void main()
{
    vec2 d = gl_PointCoord * 2.0 - 1.0;
    float r2 = dot(d, d);
    if (r2 > 1.0) discard;
    float a = 1.0 - r2;
    a *= a;                       // мягкий гало-спад
    // Линейный HDR: копится аддитивно (ONE, ONE), тон-маппинг — отдельным проходом.
    frag = vec4(palette(vT) * a * uIntensity, 1.0);
}
";

        // --- Тон-маппинг: полноэкранный проход, ACES + гамма ---
        // Полноэкранный треугольник генерится из gl_VertexID (VBO не нужен).
        public const string TonemapVs = @"#version 330 core
out vec2 vUv;
void main()
{
    vec2 p = vec2(float((gl_VertexID << 1) & 2), float(gl_VertexID & 2));
    vUv = p;
    gl_Position = vec4(p * 2.0 - 1.0, 0.0, 1.0);
}
";

        public const string TonemapFs = @"#version 330 core
in vec2 vUv;
out vec4 frag;

uniform sampler2D uHdr;
uniform float uExposure;

// ACES filmic (аппроксимация Narkowicz): мягко сворачивает пересветы,
// сохраняя цвет в плотных областях вместо плоского белого.
vec3 aces(vec3 x)
{
    const float a = 2.51, b = 0.03, c = 2.43, d = 0.59, e = 0.14;
    return clamp((x * (a * x + b)) / (x * (c * x + d) + e), 0.0, 1.0);
}

void main()
{
    vec3 hdr = texture(uHdr, vUv).rgb;
    vec3 col = aces(hdr * uExposure);
    col = pow(col, vec3(1.0 / 2.2));   // в sRGB
    frag = vec4(col, 1.0);
}
";
    }
}
