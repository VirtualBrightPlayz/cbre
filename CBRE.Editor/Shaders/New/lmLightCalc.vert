// todo: maybe convert this and the fragment shader to a compute shader
#version 450

layout(location = 0) in vec4 Position;
layout(location = 1) in vec3 Normal;
layout(location = 2) in vec4 Color;
layout(location = 3) in vec2 LmCoord;

layout(location = 0) out vec4 worldPostion;
layout(location = 1) out vec3 fragNormal;
layout(location = 2) out vec4 fragColor;
layout(location = 3) out vec4 shadowMapPos0;
layout(location = 4) out vec4 shadowMapPos1;
layout(location = 5) out vec4 shadowMapPos2;
layout(location = 6) out vec4 shadowMapPos3;
layout(location = 7) out vec4 shadowMapPos4;
layout(location = 8) out vec4 shadowMapPos5;

layout(set = 0, binding = 0) uniform VertexUniformBuffer {
    mat4 lightProjView0;
    mat4 lightProjView1;
    mat4 lightProjView2;
    mat4 lightProjView3;
    mat4 lightProjView4;
    mat4 lightProjView5;
} ubo;

void main()
{
    gl_Position = vec4(
        (LmCoord.x * 2) - 1.0,
        -(LmCoord.y * 2) + 1.0,
        0.0,
        1.0
    );
    worldPostion = Position;
    fragNormal = Normal;
    fragColor = Color;

    shadowMapPos0 = ubo.lightProjView0 * Position;
    shadowMapPos1 = ubo.lightProjView1 * Position;
    shadowMapPos2 = ubo.lightProjView2 * Position;
    shadowMapPos3 = ubo.lightProjView3 * Position;
    shadowMapPos4 = ubo.lightProjView4 * Position;
    shadowMapPos5 = ubo.lightProjView5 * Position;
}