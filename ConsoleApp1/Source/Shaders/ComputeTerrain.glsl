#version 460

layout (local_size_x = 4, local_size_y = 4, local_size_z = 4) in;

layout (r8ui, binding = 0) uniform uimage3D voxelTexture;

uniform float time;
uniform vec3 offset;

// Rand

#define MAGIC 43758.5453123

float random (vec2 st) {
    float s = dot(st, vec2(0.400,0.230));
    return -1. + 2. * fract(sin(s) * MAGIC);
}

vec2 random2(vec2 st){
    vec2 s = vec2(
    dot(st, vec2(127.1,311.7)),
    dot(st, vec2(269.5,183.3))
    );
    return -1. + 2. * fract(sin(s) * MAGIC);
}

vec2 scale (vec2 p, float s) {
    return p * s;
}

float interpolate (float t) {
    //return t;
    // return t * t * (3. - 2. * t); // smoothstep
    return t * t * t * (10. + t * (6. * t - 15.)); // smootherstep
}

vec4 valueNoise (vec2 p) {
    vec2 i = floor(p);

    float f11 = random(i + vec2(0., 0.));
    float f12 = random(i + vec2(0., 1.));
    float f21 = random(i + vec2(1., 0.));
    float f22 = random(i + vec2(1., 1.));

    return vec4(f11, f12, f21, f22);
}

vec4 gradientNoise (vec2 p) {
    vec2 i = floor(p);
    vec2 f = fract(p);

    float f11 = dot(random2(i + vec2(0., 0.)), f - vec2(0., 0.));
    float f12 = dot(random2(i + vec2(0., 1.)), f - vec2(0., 1.));
    float f21 = dot(random2(i + vec2(1., 0.)), f - vec2(1., 0.));
    float f22 = dot(random2(i + vec2(1., 1.)), f - vec2(1., 1.));

    return vec4(f11, f12, f21, f22);
}

float noise (vec2 p) {
    vec4 v = gradientNoise(p);
    //vec4 v = valueNoise(p);

    vec2 f = fract(p);
    float t = interpolate(f.x);
    float u = interpolate(f.y);

    // linear interpolation on t and u,
    // the returned value belongs to [0, 1]
    return clamp(
        mix(
            mix(v.x, v.z, t),
            mix(v.y, v.w, t),
            u
        ) * .5 + .5, 
    0, 1);
}

// Main Compute

void main() {
    ivec3 gid = ivec3(gl_GlobalInvocationID.xyz);
    vec3 pos = vec3(vec3(gid) + offset);
    uint height = uint(noise(pos.xz / 255.0 * 40.0 + time) * noise((pos.xz - time*5) / 255.0 * 20) * 32);    // noise between 0 and 255

    if (pos.y < 1)  // Bedrock
    {
        imageStore(voxelTexture, gid, uvec4(5));
    }
    else if (pos.y < height)    // Cobble
    {
        imageStore(voxelTexture, gid, uvec4(1));
    }
    else if (pos.y == height)   // Dirt
    {
        imageStore(voxelTexture, gid, uvec4(3));
    }
    else    // Air
    {
        imageStore(voxelTexture, gid, uvec4(0));
    }
}
