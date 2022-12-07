#version 450

layout(location = 0) in vec4 Postion;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec4 Color;
layout(location = 3) in float Selected;

layout(location = 0) out vec3 fragNormal;
layout(location = 1) out vec4 fragColor;
layout(location = 2) out float fragSelected;

layout(set = 0, binding = 0) uniform VertexUniformBuffer {
    mat4 world;
    mat4 view;
    mat4 proj;
    mat4 selection;
} ubo;

void main()
{
    vec4 worldPostion = ubo.world * Postion;
    // todo: math may not be right here
    worldPostion = (1.0 - Selected) * worldPostion + Selected * (ubo.selection * worldPostion);
    gl_Position = ubo.proj * ubo.view * worldPostion;
    fragColor = Color;
    fragNormal = Normal;
    fragSelected = Selected;
}
