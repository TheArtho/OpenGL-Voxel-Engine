#version 430

in vec3 WorldPosition;
in vec2 TexCoords;

layout(location = 0) out vec3 FragColor;
layout(location = 1) out vec4 Brightness;
layout(location = 4) out vec3 fbPosition;

uniform vec3 CameraPosition;
uniform vec3 SkyColor;
uniform vec3 HorizonColor;

void main() 
{
    vec3 skyColor = SkyColor; // vec3(140, 199, 254) / 255;
    vec3 horizonColor = HorizonColor; vec3(206, 226, 239) / 255;
    
    vec3 Gradient = WorldPosition.yyy - CameraPosition.yyy;
    
    Gradient *= 20;

    Gradient = clamp(Gradient, 0, 1);

    fbPosition = vec3(0,0,-100000);
    
    FragColor = mix(horizonColor, skyColor, Gradient);
    Brightness = vec4(0);
}
