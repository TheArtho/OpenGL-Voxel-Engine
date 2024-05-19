#version 330 core
in vec2 fUv;

uniform sampler2D uTexture0;
uniform sampler2D uTexture1;

uniform float time;

out vec4 FragColor;

void main()
{
    FragColor = mix(texture(uTexture0, fUv), texture(uTexture1, fUv), sin(time * 8) / 2 + 0.5f);
}