#version 430

in vec2 TexCoords;

layout(binding = 0) uniform sampler2D gPosition;
layout(binding = 1) uniform sampler2D gNormal;
layout(binding = 2) uniform sampler2D gAlbedoSpec;

out vec4 FragColor;

void main()
{
    vec3 position = texture(gPosition, TexCoords).xyz;
    vec3 normal = texture(gNormal, TexCoords).xyz;
    vec3 albedo = texture(gAlbedoSpec, TexCoords).xyz;
    float specular = texture(gAlbedoSpec, TexCoords).w;
    FragColor = vec4(albedo, 1);
}
