sampler2D TextureSampler : register(s0);
float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 tex = tex2D(TextureSampler, texCoord) * color;
    float g = dot(tex.rgb, float3(0.299, 0.587, 0.114));
    return float4(g, g, g, tex.a);
}
technique Grayscale { pass P0 { PixelShader = compile ps_4_0_level_9_1 MainPS(); } }
