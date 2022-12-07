#version 450

layout(location = 0) in vec3 Normal;
layout(location = 1) in vec4 Color;
layout(location = 2) in float Selected;

layout(location = 0) out vec4 fragColor;

void main()
{
    float lighting = dot(Normal, vec3(0.2672,0.8017,0.5345)) * 0.25 + 0.75;

    fragColor = vec4(lighting, lighting, lighting, 1.0) * (Selected * vec4(1.0,0.0,0.0,1.0) + (1.0 - Selected) * Color);
}