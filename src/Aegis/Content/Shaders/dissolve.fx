sampler2D TextureSampler : register(s0);
float Progress = 0.5;
float Hash(float2 p) { return frac(sin(dot(p, float2(12.9898,78.233))) * 43758.5453); }
float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 tex = tex2D(TextureSampler, texCoord) * color;
    clip(Hash(texCoord * 512) - Progress);
    return tex;
}
technique Dissolve { pass P0 { PixelShader = compile ps_4_0_level_9_1 MainPS(); } }
