sampler2D TextureSampler : register(s0);
float Intensity = 0.5;
float4 MainPS(float4 color : COLOR0, float2 uv : TEXCOORD0) : COLOR0
{
    float scan = sin(uv.y * 1200.0) * 0.04 * Intensity;
    float2 cc = uv - 0.5;
    uv += cc * dot(cc, cc) * 0.08;
    float4 tex = tex2D(TextureSampler, uv) * color;
    return float4(tex.rgb - scan, tex.a);
}
technique CRT { pass P0 { PixelShader = compile ps_4_0_level_9_1 MainPS(); } }
