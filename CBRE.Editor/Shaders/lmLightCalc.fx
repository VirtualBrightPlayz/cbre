float lightRange;
float3 lightPos;
float4 lightColor;

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 LmCoord : TEXCOORD1;
};

struct VertexShaderOutput
{
    float4 DrawPosition : POSITION0;
    float4 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float4 Color : COLOR0;
};

VertexShaderOutput VertexShaderF(VertexShaderInput input)
{
    VertexShaderOutput output;

    output.DrawPosition = float4((input.LmCoord.x*2)-1.0, -(input.LmCoord.y*2)+1.0, 0.0, 1.0);
    output.WorldPosition = input.Position;
    output.Normal = input.Normal;
    output.Color = input.Color;

    return output;
}

float4 PixelShaderF(VertexShaderOutput input) : COLOR0
{
    float4 c = saturate((lightRange - distance(lightPos, input.WorldPosition.xyz)) / lightRange);
    c *= c;
    c *= saturate(dot(normalize(lightPos - input.WorldPosition.xyz), input.Normal));
    c *= lightColor;

    c.a = 1.0f;
    c.xyz = saturate(c.xyz);

    return c;
}

technique TexturedShaded
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderF();
        PixelShader = compile ps_3_0 PixelShaderF();
    }
}