sampler2D Input : register(s0);
float4 SizeInfo : register(c0); // x=texW px, y=texH px, z=unused, w=corner radius px
float4 Optics   : register(c1); // x=refraction, y=bezel px, z=frost px, w=chromatic
float4 Tint     : register(c2); // rgb (0..1), a=amount
float4 Light    : register(c3); // x=specular, y=edge shade

float sdRoundBox(float2 p, float2 h, float r) {
    float2 q = abs(p) - h + r;
    return length(max(q, 0.0)) + min(max(q.x, q.y), 0.0) - r;
}
float3 samp(float2 uv, float2 offPx, float ca, float2 tex) {
    float2 o = offPx / tex;
    float2 lo = 0.5 / tex; float2 hi = 1.0 - lo;
    float r = tex2D(Input, clamp(uv + o * (1.0 + ca), lo, hi)).r;
    float g = tex2D(Input, clamp(uv + o, lo, hi)).g;
    float b = tex2D(Input, clamp(uv + o * (1.0 - ca), lo, hi)).b;
    return float3(r, g, b);
}
float4 main(float2 uv : TEXCOORD) : COLOR {
    float2 tex = SizeInfo.xy;
    float2 p = uv * tex - tex * 0.5;
    float2 h = tex * 0.5;
    float r = min(SizeInfo.w, min(h.x, h.y));
    float d = sdRoundBox(p, h, r);
    float2 g = float2(sdRoundBox(p + float2(1, 0), h, r) - sdRoundBox(p - float2(1, 0), h, r),
                      sdRoundBox(p + float2(0, 1), h, r) - sdRoundBox(p - float2(0, 1), h, r));
    float2 n = normalize(g + 1e-5);
    float bezel = max(Optics.y, 1.0);
    float t = saturate(1.0 + d / bezel);
    float2 off = -n * (Optics.x * bezel * t * t);
    float3 col = 0;
    [unroll] for (int i = -1; i <= 1; i++)
    [unroll] for (int j = -1; j <= 1; j++)
        col += samp(uv, off + float2(i, j) * Optics.z, Optics.w, tex);
    col /= 9.0;
    col *= 1.0 - Light.y * smoothstep(-bezel, 0.0, d) * 0.5;
    float spec = pow(saturate(dot(n, normalize(float2(-0.5, -0.85)))), 4.0) * smoothstep(-3.0, 0.0, d) * Light.x;
    col = lerp(col, Tint.rgb, Tint.a) + spec;
    float a = 1.0 - smoothstep(-0.75, 0.75, d);
    return float4(col * a, a);
}
