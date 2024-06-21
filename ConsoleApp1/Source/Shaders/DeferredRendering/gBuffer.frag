#version 430

in vec2 TexCoords;
in vec3 FragPos;
in vec3 Normal;

layout(location = 0) out vec3 gPosition;
layout(location = 1) out vec3 gNormal;
layout(location = 2) out vec4 gAlbedoSpec;

uniform sampler2D texture_diffuse1;
uniform sampler2D texture_specular1;

void main() 
{
    // Store the fragment position vector in the first gBuffer texture;
    gPosition = FragPos;
    // Also store the per-fragment normals into the gBuffer
    gNormal = normalize(Normal);
    // And the diffure per-fragment color
    gAlbedoSpec.rgb = texture(texture_diffuse1, TexCoords).rgb;
    // Store specular intensity in gAlbedoSpec's alpha component
    gAlbedoSpec.a = texture(texture_specular1, TexCoords).r;
}
