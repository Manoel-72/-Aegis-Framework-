// Aegis flash shader placeholder. Compile with mgfxc per platform.
#if OPENGL
#define SV_POSITION POSITION
#define VS_SHADERMODEL vs_3_0
#define PS_SHADERMODEL ps_3_0
#else
#define VS_SHADERMODEL vs_4_0_level_9_1
#define PS_SHADERMODEL ps_4_0_level_9_1
#endif
sampler2D TextureSampler : register(s0);
float4 FlashColor = float4(1,0.2,0.2,1);
float4 MainPS(float4 color : COLOR0, float2 texCoord : TEXCOORD0) : COLOR0
{
    float4 tex = tex2D(TextureSampler, texCoord) * color;
    return float4(FlashColor.rgb, tex.a);
}
technique Flash { pass P0 { PixelShader = compile PS_SHADERMODEL MainPS(); } }
