#version 330 core

in vec2 TexCoords;
in vec3 Normal;
in vec3 viewNormal;
in vec3 viewPosition;

uniform sampler2D uTexture0;

layout(location = 0) out vec3 FragColor;
layout(location = 1) out vec4 Brightness;
layout(location = 2) out vec4 Transparent;
layout(location = 3) out vec3 fbNormal;
layout(location = 4) out vec3 fbPosition;

void main()
{
    FragColor = result;
    Brightness = vec4(0);
    fbNormal = normalize(viewNormal);
    fbPosition = viewPosition;
}