#version 460

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout (r8ui, binding = 0) uniform uimage3D voxelTexture;

uniform uint time = 0;
uniform vec3 offset = vec3(0,0,0);

// Voronoi 3D

const mat2 myt = mat2(.12121212, .13131313, -.13131313, .12121212);
const vec2 mys = vec2(1e4, 1e6);

vec2 rhash(vec2 uv) {
    uv *= myt;
    uv *= mys;
    return fract(fract(uv / mys) * uv);
}

vec3 hash(vec3 p) {
    return fract(
    sin(vec3(dot(p, vec3(1.0, 57.0, 113.0)), dot(p, vec3(57.0, 113.0, 1.0)),
    dot(p, vec3(113.0, 1.0, 57.0)))) *
    43758.5453);
}

vec3 voronoi3d(const in vec3 x) {
    vec3 p = floor(x);
    vec3 f = fract(x);

    float id = 0.0;
    vec2 res = vec2(100.0);
    for (int k = -1; k <= 1; k++) {
        for (int j = -1; j <= 1; j++) {
            for (int i = -1; i <= 1; i++) {
                vec3 b = vec3(float(i), float(j), float(k));
                vec3 r = vec3(b) - f + hash(p + b);
                float d = dot(r, r);

                float cond = max(sign(res.x - d), 0.0);
                float nCond = 1.0 - cond;

                float cond2 = nCond * max(sign(res.y - d), 0.0);
                float nCond2 = 1.0 - cond2;

                id = (dot(p + b, vec3(1.0, 57.0, 113.0)) * cond) + (id * nCond);
                res = vec2(d, res.x) * cond + res * nCond;

                res.y = cond2 * d + nCond2 * res.y;
            }
        }
    }

    return vec3(sqrt(res), abs(id));
}

// Rand

float rand(vec3 p) {
    return fract(sin(dot(p ,vec3(127.1, 311.7, 74.7))) * 43758.5453);
}

// Main Compute

void main() {
    ivec3 gid = ivec3(gl_GlobalInvocationID.xyz);
    vec3 pos = vec3(vec3(gid) + offset + time);
    uint height = uint(rand(pos) * 255);

    imageStore(voxelTexture, gid, height.xxxx);
}
