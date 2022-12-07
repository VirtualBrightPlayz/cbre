#version 450

layout(location = 0) in vec4 worldPostion;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec4 Color;
layout(location = 3) in vec4 shadowMapPos0;
layout(location = 4) in vec4 shadowMapPos1;
layout(location = 5) in vec4 shadowMapPos2;
layout(location = 6) in vec4 shadowMapPos3;
layout(location = 7) in vec4 shadowMapPos4;
layout(location = 8) in vec4 shadowMapPos5;

layout(location = 0) out vec4 fragColor;

layout(set = 1, binding = 0) uniform FragmentUniformBuffer {
    vec4 lightColor;
    vec3 lightDirection;
    vec3 lightPosition;
    vec2 lightConeAngles;
    float lightRange;
    float shadowMapTexelSize;
    int lightType;
} ubo;

float shadowMapBlocked(sampler2D sTexture, vec4 positon) {
    if (positon.w < 0.0) return 0.0;
    vec2 uv = vec2(positon.x / positon.w, positon.y / positon.w);
    uv.y *= -1.0;
    uv.x += 1.0;
    uv.y += 1.0;
    uv.xy *= 0.5;
    if (uv.x < 0.0 ||  uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0) return 0.0;

    float sample00 = texture(sTexture, uv + vec2(-ubo.shadowMapTexelSize, -ubo.shadowMapTexelSize)).r * ubo.lightRange;
    float sample10 = texture(sTexture, uv + vec2(0.0, -ubo.shadowMapTexelSize)).r * ubo.lightRange;
    float sample20 = texture(sTexture, uv + vec2(ubo.shadowMapTexelSize, -ubo.shadowMapTexelSize)).r * ubo.lightRange;

    float sample01 = texture(sTexture, uv + vec2(-ubo.shadowMapTexelSize, 0.0)).r * ubo.lightRange;
    float sample11 = texture(sTexture, uv).r * ubo.lightRange;
    float sample21 = texture(sTexture, uv + vec2(ubo.shadowMapTexelSize, 0.0)).r * ubo.lightRange;

    float sample02 = texture(sTexture, uv + vec2(-ubo.shadowMapTexelSize, ubo.shadowMapTexelSize)).r * ubo.lightRange;
    float sample12 = texture(sTexture, uv + vec2(0.0, ubo.shadowMapTexelSize)).r * ubo.lightRange;
    float sample22 = texture(sTexture, uv + vec2(ubo.shadowMapTexelSize, ubo.shadowMapTexelSize)).r * ubo.lightRange;

    float dsample =
        max(sample00, max(
            sample10, max(
            sample20, max(
            sample01, max(
            sample11, max(
            sample21, max(
            sample02, max(
            sample12, sample22
    ))))))));

    return ((dsample + 10.0) > positon.z) ? 1.0 : 0.0;
}

void main()
{   
    // todo: the actual fragment shader    
}