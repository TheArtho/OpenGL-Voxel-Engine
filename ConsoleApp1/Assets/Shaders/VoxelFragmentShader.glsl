#version 460

in vec2 TexCoords;
in vec3 Normal;
in vec3 WorldPosition;
flat in uint voxelValue;
// in uint faceID;

struct DirectionalLight
{
    vec3 direction;
    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform sampler2D uTexture0;
uniform DirectionalLight dirLight;
uniform vec3 viewPos;

out vec4 FragColor;

void main() 
{
    // Ambient -------------------------------------------------------------------------
    vec3 ambient = dirLight.ambient * vec3(texture(uTexture0, TexCoords));

    // Diffuse -------------------------------------------------------------------------
    vec3 normal = normalize(Normal);
    vec3 lightDir = normalize(-dirLight.direction);
    float NdotL = max(dot(normal, lightDir), 0.0);
    vec3 diffuse = dirLight.diffuse * NdotL * vec3(texture(uTexture0, TexCoords));

    // Specular - Blinn-Phong ----------------------------------------------------------
    vec3 viewDir = normalize(viewPos - WorldPosition);
    vec3 halfDir = normalize(lightDir + viewDir);
    float NDotH = max(dot(normal, halfDir), 0.0);
    vec3 specular = dirLight.specular * 1 * pow(NDotH, 100.0f);

    FragColor = vec4(ambient + diffuse + specular, 1.0);
}

