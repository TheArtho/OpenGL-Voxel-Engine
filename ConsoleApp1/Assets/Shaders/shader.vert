#version 460 core
layout (location = 0) in vec3 vPos;
layout (location = 1) in vec2 vUv;

uniform sampler3D chunk;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

out vec2 fUv;

void main()
{
    gl_Position = uProjection * uView * uModel * vec4(vPos, 1.0);
    fUv = vUv;
}