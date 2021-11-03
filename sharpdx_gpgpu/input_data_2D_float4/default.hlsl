Texture2D<float4> InputTexture : register(t0);
SamplerState TexSampler : register(s0);

float4 vs_main (float2 pos : POSITION, out float2 coords : TEXCOORD0) : SV_POSITION
{
    // -- positions [-1,1] so shift-scaling to texc [0,1]:
    coords = float2(pos.x * .5f + .5f, -pos.y * .5f + .5f);
    return float4(pos, 0.0f, 1.0f);
}
float2 ps_main (float4 pos : SV_POSITION, float2 coords : TEXCOORD0) : SV_Target{
    float4 data = InputTexture.SampleLevel(TexSampler, coords, 0).rgba;
    
    return float2(data.x + data.y, data.z + data.w);
}
