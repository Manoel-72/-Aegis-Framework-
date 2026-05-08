sampler2D TextureSampler : register(s0);
float Intensity = 0.5;
float4 MainPS(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float4 tex = tex2D(TextureSampler, uv) * color;
    float2 d = abs(uv - 0.5) * 2.0;
    float vig = 1.0 - smoothstep(0.55, 1.0, length(d)) * Intensity;
    return float4(tex.rgb * vig, tex.a);
}
technique Vignette { pass P0 { PixelShader = compile ps_4_0_level_9_1 MainPS(); } }
