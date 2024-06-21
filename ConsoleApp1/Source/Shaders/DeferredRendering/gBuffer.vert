#version 430

layout(location = 0) in vec4 aPos;
layout(location = 1) in vec3 aTexCoords;
layout(location = 2) in uint aFaceID;

out vec3 FragPos;
out vec2 TexCoords;
out vec3 Normal;

uniform mat4 model;
uniform mat4 view;
uniform mat4 projection;

const vec3 normalFromFaceID[6] = {
vec3(0, 0, 1),
vec3(0, 0, -1),
vec3(1, 0, 0),
vec3(-1, 0, 0),
vec3(0, 1, 0),
vec3(0, -1, 0)
};

void main() {
    vec4 worldPos = model * vec4(aPos.xyz, 1.0);
    uint faceID = uint(aTexCoords.z);
    
    FragPos = worldPos.xyz;
    TexCoords = aTexCoords.xy;
    
    Normal = normalFromFaceID[faceID];
    
    gl_Position = projection * view * worldPos;
}
