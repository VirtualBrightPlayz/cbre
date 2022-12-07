#version 450

layout(location = 0) in vec4 VPostion;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec4 Color;
layout(location = 3) in float Selected;
layout(location = 4) in vec2 TexCoord;
layout(location = 5) in vec2 LmCoord;

layout(location = 0) out vec4 fragColor;

layout(set = 1, binding = 0) uniform texture2D xTexture;
layout(set = 1, binding = 1) uniform sampler sTexture;

void main()
{
    float lighting = dot(Normal, vec3(0.2672,0.8017,0.5345)) * 0.25 + 0.75;

    fragColor = texture(sampler2D(xTexture, sTexture), TexCoord) * vec4(lighting, lighting, lighting, 1.0) * vec4(1.0, 1.0 - Selected, 1.0 - Selected, 1.0);
}