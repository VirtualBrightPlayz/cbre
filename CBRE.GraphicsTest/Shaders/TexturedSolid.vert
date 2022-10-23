#version 450

layout(location = 0) in vec3 Position;
layout(location = 1) in vec2 TextureCoordinate;
layout(location = 2) in vec4 Color;

layout(location = 0) out vec2 frag_TexCoord;
layout(location = 1) out vec4 frag_Color;

void main()
{
    gl_Position = vec4(Position, 1);
    frag_TexCoord = TextureCoordinate;
    frag_Color = Color;
}