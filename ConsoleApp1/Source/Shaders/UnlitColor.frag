#version 430

in vec2 TexCoords;

uniform vec3 color;

layout(location = 0) out vec3 FragColor;
layout(location = 1) out vec4 Brightness;

void main()
{
    FragColor = color;
    Brightness = vec4(color, 1);
}