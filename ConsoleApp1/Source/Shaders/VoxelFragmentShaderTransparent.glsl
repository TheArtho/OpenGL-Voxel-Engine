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

struct PointLight {
    vec3 position;

    float constant;
    float linear;
    float quadratic;

    vec3 ambient;
    vec3 diffuse;
    vec3 specular;
};

uniform sampler2D uTexture0;

uniform DirectionalLight dirLight;
#define NR_POINT_LIGHTS 6
uniform PointLight pointLights[NR_POINT_LIGHTS];
uniform vec3 viewPos;
// uniform vec3 HorizonColor;

uniform float fogDistance = 100;

layout(location = 0) out vec4 FragColor;
layout(location = 1) out vec4 Brightness;
layout(location = 2) out vec4 Transparent;

const vec3 material_specular = vec3(0.1);

vec3 CalcDirLight(DirectionalLight light, vec3 normal, vec3 viewDir)
{
    vec3 lightDir = normalize(-light.direction);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 1);
    // combine results
    vec3 ambient  = light.ambient  * vec3(texture(uTexture0, TexCoords));
    vec3 diffuse  = light.diffuse  * diff * vec3(texture(uTexture0, TexCoords));
    vec3 specular = light.specular * spec * material_specular;
    return (ambient + diffuse + specular);
}

vec3 CalcPointLight(PointLight light, vec3 normal, vec3 fragPos, vec3 viewDir)
{
    vec3 lightDir = normalize(light.position - fragPos);
    // diffuse shading
    float diff = max(dot(normal, lightDir), 0.0);
    // specular shading
    vec3 reflectDir = reflect(-lightDir, normal);
    float spec = pow(max(dot(viewDir, reflectDir), 0.0), 1);
    // attenuation
    float distance    = length(light.position - fragPos);
    float attenuation = 1.0 / (light.constant + light.linear * distance +
    light.quadratic * (distance * distance));
    // combine results
    vec3 ambient  = light.ambient  * vec3(texture(uTexture0, TexCoords));
    vec3 diffuse  = light.diffuse  * diff * vec3(texture(uTexture0, TexCoords));
    vec3 specular = light.specular * spec * material_specular;
    ambient  *= attenuation;
    diffuse  *= attenuation;
    specular *= attenuation;
    return (ambient + diffuse + specular);
}

float getFogFactor(float fogCoordinate)
{
    float linearStart = 60;
    float linearEnd = 400;
    
    float fogLength = linearEnd - linearStart;
    float result = (linearEnd - fogCoordinate) / fogDistance;

    result = 1.0 - clamp(result, 0.0, 1.0);
    return result;
}

void main()
{
    // vec3 horizonColor = vec3(206, 226, 239) / 255;
    
    // properties
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - WorldPosition);

    // phase 1: Directional lighting
    vec3 result = CalcDirLight(dirLight, norm, viewDir);
    // phase 2: Point lights
    for(int i = 0; i < NR_POINT_LIGHTS; i++)
    {
        result += CalcPointLight(pointLights[i], norm, floor(WorldPosition * 8) / 8, viewDir);
    }
    // phase 3: Spot lightaz
    //result += CalcSpotLight(spotLight, norm, FragPos, viewDir);

    // Calcul de la distance en eye space
    float z = abs(gl_FragCoord.z / gl_FragCoord.w);

    // Calcul du facteur de brouillard
    float fogFactor = 0; // getFogFactor(z);
    
    Transparent = vec4(result, texture(uTexture0, TexCoords).a);; // vec4(mix(result, HorizonColor, fogFactor), texture(uTexture0, TexCoords).a);
    Brightness = vec4(0);
}

