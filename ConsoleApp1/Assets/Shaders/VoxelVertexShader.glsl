#version 430 core

layout(location = 0) in vec4 vPos;
layout(location = 1) in vec3 vTexCoord;
layout(location = 2) in uint vFaceID;

out vec2 TexCoords;
out vec3 Normal;
out vec3 WorldPosition;
flat out uint voxelValue;
// out uint faceID;

uniform mat4 uModel;
uniform mat4 uView;
uniform mat4 uProjection;

const vec3 normalFromFaceID[6] = {
    vec3(0, 0, 1),
    vec3(0, 0, -1),
    vec3(1, 0, 0),
    vec3(-1, 0, 0),
    vec3(0, 1, 0),
    vec3(0, -1, 0)
};

void main()
{
    uint faceID = uint(vTexCoord.z);
    
    vec3 position = vPos.xyz;
    vec3 normal = normalFromFaceID[faceID];
    
    gl_Position = uProjection * uView * uModel * vec4(position, 1.0);
    TexCoords = vTexCoord.xy;
    Normal = normal;
    WorldPosition = (uModel * vec4(position, 1.0)).xyz;
    voxelValue = uint(vPos.w);
    // faceID = vFaceID;
}

