#version 460 core

in vec2 fUv;

uniform sampler2D uTexture0;
uniform sampler2D uTexture1;

uniform sampler3D chunk;
uniform float time;

out vec4 FragColor;

void main()
{
    FragColor = texture(uTexture0, fUv);
}