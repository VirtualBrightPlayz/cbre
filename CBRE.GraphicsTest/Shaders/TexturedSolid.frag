#version 450

layout(location = 0) in vec2 frag_TexCoord;
layout(location = 1) in vec4 frag_Color;

layout(location = 0) out vec4 fragOut_Color;

layout(set = 0, binding = 0) uniform texture2D MainTexture;
layout(set = 0, binding = 1) uniform sampler MainTextureSampler;

void main()
{
    fragOut_Color = frag_Color * texture(sampler2D(MainTexture, MainTextureSampler), frag_TexCoord);
}