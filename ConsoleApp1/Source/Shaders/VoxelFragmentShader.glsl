#version 460

in vec2 TexCoords;
in vec3 Normal;
in vec3 viewNormal;
in vec3 WorldPosition;
in vec3 viewPosition;

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

layout(location = 0) out vec3 FragColor;
layout(location = 1) out vec4 Brightness;
layout(location = 2) out vec4 Transparent;
layout(location = 3) out vec3 fbNormal;
layout(location = 4) out vec3 fbPosition;

const vec3 material_specular = vec3(0.1);

vec3 CalcGlobalLight(DirectionalLight light, vec3 normal)
{
    ivec3 inormal = ivec3(round(normal));
    float diff = 1;
    
    if (inormal == vec3(0, -1, 0)) {
        diff *= 0;
    }
    else if (inormal == vec3(1, 0, 0) || inormal == vec3(-1, 0, 0)) {
        diff *= 0.25;
    }
    else if (inormal == vec3(0, 0, 1) || inormal == vec3(0, 0, -1)) {
        diff *= 0.5;
    }
    
    // combine results
    vec3 ambient  = light.ambient * vec3(texture(uTexture0, TexCoords));
    vec3 diffuse  = light.diffuse * diff * vec3(texture(uTexture0, TexCoords));
    return (ambient + diffuse);
}

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
    vec4 tex = texture(uTexture0, TexCoords);
    
    if (tex.a < 0.5) 
    {
        discard;    
    }
    
    // vec3 horizonColor = vec3(206, 226, 239) / 255;
    
    // properties
    vec3 norm = normalize(Normal);
    vec3 viewDir = normalize(viewPos - WorldPosition);

    // phase 1: Directional lighting
    //vec3 result = CalcDirLight(dirLight, norm, viewDir);
    vec3 result = CalcGlobalLight(dirLight, norm);
    // phase 2: Point lights
    for(int i = 0; i < NR_POINT_LIGHTS; i++)
    {
        result += CalcPointLight(pointLights[i], norm, floor(WorldPosition * 8) / 8, viewDir);
    }
    // phase 3: Spot lightaz
    //result += CalcSpotLight(spotLight, norm, FragPos, viewDir);

    // Calcul de la distance en eye space
    float z = abs(distance(WorldPosition, viewPos));

    // Calcul du facteur de brouillard
    float fogFactor = 0; // getFogFactor(z);

    // FragColor = mix(result, horizonColor, fogFactor);
    
    FragColor = result; //mix(result, HorizonColor, fogFactor);
    Brightness = vec4(0);
    fbNormal = normalize(viewNormal);
    fbPosition = viewPosition;
}

