float shadowMapTexelSize;
int blurRadius;

Texture2D xTexture;
sampler TextureSampler : register (s0) = sampler_state { Texture = <xTexture>; };

struct VertexShaderInput {
    float4 Position : SV_POSITION;
    float2 TexCoord : TEXCOORD0;
};

struct VertexShaderOutput {
    float4 WorldPosition : POSITION0;
    float2 TexCoord : TEXCOORD0;
};

VertexShaderOutput VertexShaderF(VertexShaderInput input) {
    VertexShaderOutput output;

    output.WorldPosition = input.Position;
    output.TexCoord = input.TexCoord;

    return output;
}

float4 PixelShaderF(VertexShaderOutput input) : COLOR0 {
    float4 c = tex2D(TextureSampler, input.TexCoord.xy);
    float val = shadowMapTexelSize * blurRadius;
    [unroll(16)]
    for (float y = -val; y <= val; y+=shadowMapTexelSize) {
        [unroll(16)]
        for (float x = -val; x <= val; x+=shadowMapTexelSize) {
            c += tex2D(TextureSampler, input.TexCoord.xy + float2(x, y));
        }
    }
    c /= (blurRadius * blurRadius * 2 * 2) + 2 * 2;
    // c /= (blurRadius * blurRadius) * 2 * 2;

    c.a = 1.0f;
    c.rgb = saturate(c.rgb);

    return c;
}

technique Blur {
    pass Pass1 {
        VertexShader = compile vs_3_0 VertexShaderF();
        PixelShader = compile ps_3_0 PixelShaderF();
    }
}