float4x4 World;
float4x4 ProjectionView;
float maxDepth;

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL0;
};

struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Position_VisibleToFragShader : TEXCOORD0;
    float3 Normal : TEXCOORD1;
};

VertexShaderOutput VertexShaderF(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float4 worldPosition = mul(input.Position, World);
    output.Position = mul(worldPosition, ProjectionView);
    output.Position_VisibleToFragShader = output.Position;
    output.Normal = input.Normal;

    return output;
}

float4 PixelShaderF(VertexShaderOutput input) : COLOR0
{
    float4 c = float4(
        saturate(input.Position_VisibleToFragShader.z / maxDepth),
        0.0,
        0.0,
        1.0);
    //c *= 0.001;
    //c += float4((input.Normal.x + 0.5) / 2.0, (input.Normal.y + 0.5) / 2.0, (input.Normal.z + 0.5) / 2.0, 1.0);

    return c;
}

technique Depth
{
    pass Pass1
    {
        VertexShader = compile vs_3_0 VertexShaderF();
        PixelShader = compile ps_3_0 PixelShaderF();
    }
}