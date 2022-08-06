float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 Selection;

Texture2D xTexture;
sampler TextureSampler : register (s0) = sampler_state { Texture = <xTexture>; };

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float4 Color : COLOR0;
    float Selected : COLOR1;
};


struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float4 Color : COLOR0;
    float Selected : COLOR1;
};

VertexShaderOutput VertexShaderF(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float4 worldPosition = mul(input.Position, World);
    worldPosition = (1.0 - input.Selected) * worldPosition + input.Selected * mul(worldPosition, Selection);

    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.Color = input.Color;
    output.Selected = input.Selected;

    return output;
}

float4 PixelShaderF(VertexShaderOutput input) : COLOR0
{
    float4 c = (input.Selected * float4(1,0,0,1) + (1.0 - input.Selected) * input.Color);

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