float lightRange;
float3 lightPos;
float4 lightColor;
float shadowMapTexelSize;

Matrix lightProjView0;
Matrix lightProjView1;
Matrix lightProjView2;
Matrix lightProjView3;
Matrix lightProjView4;
Matrix lightProjView5;

Texture2D lightShadowMap0;
sampler lightShadowMapSampler0 : register (s0) = sampler_state { Texture = <lightShadowMap0>; };
Texture2D lightShadowMap1;
sampler lightShadowMapSampler1 : register (s1) = sampler_state { Texture = <lightShadowMap1>; };
Texture2D lightShadowMap2;
sampler lightShadowMapSampler2 : register (s2) = sampler_state { Texture = <lightShadowMap2>; };
Texture2D lightShadowMap3;
sampler lightShadowMapSampler3 : register (s3) = sampler_state { Texture = <lightShadowMap3>; };
Texture2D lightShadowMap4;
sampler lightShadowMapSampler4 : register (s4) = sampler_state { Texture = <lightShadowMap4>; };
Texture2D lightShadowMap5;
sampler lightShadowMapSampler5 : register (s5) = sampler_state { Texture = <lightShadowMap5>; };

struct VertexShaderInput {
    float4 Position : SV_POSITION;
    float3 Normal : NORMAL0;
    float4 Color : COLOR0;
    float2 LmCoord : TEXCOORD1;
};

struct VertexShaderOutput {
    float4 DrawPosition : POSITION0;
    float4 WorldPosition : TEXCOORD0;
    float3 Normal : TEXCOORD1;
    float4 Color : COLOR0;
    float4 ShadowMapPos0 : TEXCOORD2;
    float4 ShadowMapPos1 : TEXCOORD3;
    float4 ShadowMapPos2 : TEXCOORD4;
    float4 ShadowMapPos3 : TEXCOORD5;
    float4 ShadowMapPos4 : TEXCOORD6;
    float4 ShadowMapPos5 : TEXCOORD7;
};

VertexShaderOutput VertexShaderF(VertexShaderInput input) {
    VertexShaderOutput output;

    output.DrawPosition = float4((input.LmCoord.x*2)-1.0, -(input.LmCoord.y*2)+1.0, 0.0, 1.0);
    output.WorldPosition = input.Position;
    output.Normal = input.Normal;
    output.Color = input.Color;
    
    output.ShadowMapPos0 = mul(input.Position, lightProjView0);
    output.ShadowMapPos1 = mul(input.Position, lightProjView1);
    output.ShadowMapPos2 = mul(input.Position, lightProjView2);
    output.ShadowMapPos3 = mul(input.Position, lightProjView3);
    output.ShadowMapPos4 = mul(input.Position, lightProjView4);
    output.ShadowMapPos5 = mul(input.Position, lightProjView5);

    return output;
}

float shadowMapBlocked(sampler smp, float4 position) {
    if (position.w < 0.0) { return 0.0; }
    float2 uv = float2(position.x / position.w, position.y / position.w);
    uv.y *= -1.0;
    uv.x += 1.0; uv.y += 1.0;
    uv.xy *= 0.5;
    if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) { return 0.0; }
    
    float sample00 = tex2D(smp, uv+float2(-shadowMapTexelSize, -shadowMapTexelSize)).r * lightRange;
    float sample10 = tex2D(smp, uv+float2(0, -shadowMapTexelSize)).r * lightRange;
    float sample20 = tex2D(smp, uv+float2(shadowMapTexelSize, -shadowMapTexelSize)).r * lightRange;
    
    float sample01 = tex2D(smp, uv+float2(-shadowMapTexelSize, 0)).r * lightRange;
    float sample11 = tex2D(smp, uv).r * lightRange;
    float sample21 = tex2D(smp, uv+float2(shadowMapTexelSize, 0)).r * lightRange;
    
    float sample02 = tex2D(smp, uv+float2(-shadowMapTexelSize, shadowMapTexelSize)).r * lightRange;
    float sample12 = tex2D(smp, uv+float2(0, shadowMapTexelSize)).r * lightRange;
    float sample22 = tex2D(smp, uv+float2(shadowMapTexelSize, shadowMapTexelSize)).r * lightRange; 
    
    float sample =
        max(sample00, max(
            sample10, max(
            sample20, max(
            sample01, max(
            sample11, max(
            sample21, max(
            sample02, max(
            sample12, sample22
            ))))))));
    
    return ((sample+10.0) > position.z) ? 1.0 : 0.0;
}

float4 PixelShaderF(VertexShaderOutput input) : COLOR0 {
    float4 c = saturate((lightRange - distance(lightPos, input.WorldPosition.xyz)) / lightRange);
    c *= c;
    c *= saturate(dot(normalize(lightPos - input.WorldPosition.xyz), input.Normal));
    c *= lightColor;
    c.xyz *= min(1.0,
             shadowMapBlocked(lightShadowMapSampler0, input.ShadowMapPos0)
            +shadowMapBlocked(lightShadowMapSampler1, input.ShadowMapPos1)
            +shadowMapBlocked(lightShadowMapSampler2, input.ShadowMapPos2)
            +shadowMapBlocked(lightShadowMapSampler3, input.ShadowMapPos3)
            +shadowMapBlocked(lightShadowMapSampler4, input.ShadowMapPos4)
            +shadowMapBlocked(lightShadowMapSampler5, input.ShadowMapPos5));
    /*c.xyz *= 0.0001;
    float2 uv = float2(input.ShadowMapPos1.x / input.ShadowMapPos1.w, input.ShadowMapPos1.y / input.ShadowMapPos1.w);
    uv.y *= -1.0;
    uv.x += 1.0; uv.y += 1.0;
    uv.xy *= 0.5;
    if (uv.x > 1.0 || uv.y > 1.0 || uv.x < 0.0 || uv.y < 0.0) { uv = float2(0.0, 0.0); }
    else { uv = tex2D(lightShadowMapSampler1, uv).xy / 1024.0; }
    c.xy += uv;*/
    //c.xyz += uv.x;

    c.a = 1.0f;
    c.xyz = saturate(c.xyz);

    return c;
}

technique TexturedShaded {
    pass Pass1 {
        VertexShader = compile vs_3_0 VertexShaderF();
        PixelShader = compile ps_3_0 PixelShaderF();
    }
}