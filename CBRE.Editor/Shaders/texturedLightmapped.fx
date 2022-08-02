float4x4 World;
float4x4 View;
float4x4 Projection;
float4x4 Selection;

Texture2D diffTexture;
Texture2D lmTexture;
sampler diffSampler : register (s0) = sampler_state { Texture = <diffTexture>; };
sampler lmSampler : register (s1) = sampler_state { Texture = <lmTexture>; };

struct VertexShaderInput
{
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL0;
    float4 Color : COLOR0;
    float Selected : COLOR1;
    float2 TexCoord : TEXCOORD0;
    float2 LmCoord : TEXCOORD1;
};


struct VertexShaderOutput
{
    float4 Position : POSITION0;
    float3 Normal : TEXCOORD2;
    float4 Color : COLOR0;
    float Selected : COLOR1;
    float2 TexCoord : TEXCOORD0;
    float2 LmCoord : TEXCOORD1;
};

VertexShaderOutput VertexShaderF(VertexShaderInput input)
{
    VertexShaderOutput output;
    
    float4 worldPosition = mul(input.Position, World);
    worldPosition = (1.0 - input.Selected) * worldPosition + input.Selected * mul(worldPosition, Selection);

    float4 viewPosition = mul(worldPosition, View);
    output.Position = mul(viewPosition, Projection);

    output.Normal = input.Normal;

    output.Color = input.Color;
    output.Selected = input.Selected;

    output.TexCoord = input.TexCoord;
    output.LmCoord = input.LmCoord;

    return output;
}

float4 PixelShaderF(VertexShaderOutput input) : COLOR0
{
    float4 lighting;
    /*if ((input.LmCoord.x > 1.0 || input.LmCoord.x < 0.0) && (input.LmCoord.y > 1.0 || input.LmCoord.y < 0.0)) {
        float lighting1 = dot(input.Normal, float3(0.2672,0.8017,0.5345)) * 0.25 + 0.75;
        lighting = float4(lighting1, lighting1, lighting1, 1.0);
    } else {*/
        lighting = tex2D(lmSampler, input.LmCoord);
    // }

    float4 c = (tex2D(diffSampler, input.TexCoord) * lighting) * float4(1.0, 1.0 - input.Selected, 1.0 - input.Selected, 1.0);

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