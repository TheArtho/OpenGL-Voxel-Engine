#version 460

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

struct VerticeData {
    vec4 position;
    vec3 uv;
};

layout (r8ui, binding = 0) uniform readonly uimage3D chunkData;

layout (std430, binding = 1) buffer vertice {
    VerticeData data[];
};

layout (std430, binding = 2) buffer DrawCommands {
    uint count;
    uint instanceCount;
    uint first;
    uint baseInstance;
};

uniform float isoLevel = 0;
uniform uint block_resolution = 16;
uniform uint atlasPadding = 2;
uniform uint atlasWidth = 5;
uniform uint atlasHeight = 1;

const ivec3 FRONT = ivec3(0,0,1);   // 0
const ivec3 BACK = ivec3(0,0,-1);   // 1
const ivec3 RIGHT = ivec3(1,0,0);   // 2
const ivec3 LEFT = ivec3(-1,0,0);   // 3
const ivec3 TOP = ivec3(0,1,0);     // 4
const ivec3 BOTTOM = ivec3(0,-1,0); // 5

const vec2 uvOffsets[6] = {vec2(0, 0), vec2(0, 1), vec2(1, 0), vec2(1, 0), vec2(0, 1), vec2(1, 1)};

// Fonction pour packer voxelValue et faceID (pas utilisée pour le moment)
int packVoxelData(uint voxelValue, uint faceID) 
{
    return (int(voxelValue) & 0xFF) | ((int(faceID) & 0x07) << 8);
}

uint GetVoxelValue(ivec3 pos) 
{
    return imageLoad(chunkData, pos).r;
}

vec2 recalculateUV(vec2 uv, vec2 atlasOffset, uint tileResolution, uint atlasWidth, uint atlasHeight, uint padding)
{
    // Calculate pixel size for one tile
    float pixelSizeX = 1.0 / (float(atlasWidth) * float(tileResolution + padding));
    float pixelSizeY = 1.0 / (float(atlasHeight) * float(tileResolution + padding));

    // Calculate UV offset to move 1 pixel inwards
    vec2 pixelOffset = vec2(pixelSizeX, pixelSizeY) * float(padding);

    // Apply atlas offset and scale, then add one pixel padding offset
    vec2 scaledUV = (uv / vec2(atlasWidth, atlasHeight)) + atlasOffset;
    return scaledUV + pixelOffset - pixelOffset * 2.0 * uv;  // adjust to move inward by one pixel on each side
}

//vec2 recalculateUV(vec2 uv, vec2 atlasOffset)
//{
//    return uv / vec2(atlasWidth, atlasHeight) + atlasOffset;
//}

void addFaceData(uint index, vec3 voxelPos, vec3 positions[6], uint faceID, float voxelValue, vec2 atlasOffset) {
    for (int i = 0; i < 6; i++) {
        data[index + i].uv.xy = uvOffsets[i];
        data[index + i].position.xyz = voxelPos + positions[i];
        data[index + i].position.w = voxelValue;
        data[index + i].uv.xy = recalculateUV(data[index + i].uv.xy, atlasOffset, block_resolution, atlasWidth, atlasHeight, atlasPadding);
        //data[index + i].uv = recalculateUV(data[index + i].uv, atlasOffset);
        data[index + i].uv.z = faceID;
    }
}

void checkAndProcessFace(bool condition, vec3 voxelPos, vec3 positions[6], float voxelValue, uint faceID, vec2 atlasOffset) {
    if (condition) {
        uint index = atomicAdd(count, 6);
        addFaceData(index, voxelPos, positions, faceID, voxelValue, atlasOffset);
    }
}

void main() 
{
    const ivec3 voxelPos = ivec3(gl_GlobalInvocationID.xyz);

    const uint voxelValue = GetVoxelValue(voxelPos);

    const float voxelFront = GetVoxelValue(voxelPos + FRONT);
    const float voxelBack = GetVoxelValue(voxelPos + BACK);
    const float voxelTop = GetVoxelValue(voxelPos + TOP);
    const float voxelBottom = GetVoxelValue(voxelPos + BOTTOM);
    const float voxelRight = GetVoxelValue(voxelPos + RIGHT);
    const float voxelLeft = GetVoxelValue(voxelPos + LEFT);
    
    // 8 vertices d'un cube
    const vec3 v0 = vec3(0, 1, 1) - vec3(0.5);
    const vec3 v1 = vec3(0, 0, 1) - vec3(0.5);
    const vec3 v2 = vec3(1, 0, 1) - vec3(0.5);
    const vec3 v3 = vec3(1, 1, 1) - vec3(0.5);
    const vec3 v4 = vec3(0, 1, 0) - vec3(0.5);
    const vec3 v5 = vec3(1, 1, 0) - vec3(0.5);
    const vec3 v6 = vec3(0, 0, 0) - vec3(0.5);
    const vec3 v7 = vec3(1, 0, 0) - vec3(0.5);

    barrier();

    const vec2 atlasOffset = vec2(
        float((voxelValue - 1) % (atlasWidth)) / float(atlasWidth),
        float((voxelValue - 1) / (atlasWidth)) / float(atlasHeight)
    );

    if (voxelValue > isoLevel) {
        vec3 frontPositions[6] = {v0, v1, v3, v3, v1, v2};
        checkAndProcessFace(voxelPos.z == 15 || voxelFront <= isoLevel, voxelPos, frontPositions, voxelValue, 0, atlasOffset);

        vec3 backPositions[6] = {v5, v7, v4, v4, v7, v6};
        checkAndProcessFace(voxelPos.z == 0 || voxelBack <= isoLevel, voxelPos, backPositions, voxelValue, 1, atlasOffset);

        vec3 rightPositions[6] = {v3, v2, v5, v5, v2, v7};
        checkAndProcessFace(voxelPos.x == 15 || voxelRight <= isoLevel, voxelPos, rightPositions, voxelValue, 2, atlasOffset);

        vec3 leftPositions[6] = {v4, v6, v0, v0, v6, v1};
        checkAndProcessFace(voxelPos.x == 0 || voxelLeft <= isoLevel, voxelPos, leftPositions, voxelValue, 3, atlasOffset);

        vec3 topPositions[6] = {v4, v0, v5, v5, v0, v3};
        checkAndProcessFace(voxelPos.y == 15 || voxelTop <= isoLevel, voxelPos, topPositions, voxelValue, 4, atlasOffset);

        vec3 bottomPositions[6] = {v1, v6, v2, v2, v6, v7};
        checkAndProcessFace(voxelPos.y == 0 || voxelBottom <= isoLevel, voxelPos, bottomPositions, voxelValue, 5, atlasOffset);
    }
}

