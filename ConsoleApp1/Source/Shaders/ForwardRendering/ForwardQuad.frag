#version 430

#define WEIGHTSIZE 8

in vec2 TexCoords;

layout(binding = 0) uniform sampler2D fbColor;
layout(binding = 1) uniform sampler2D fbBrightness;
layout(binding = 2) uniform sampler2D fbTransparent;
layout(binding = 3) uniform sampler2D fbNormal;
layout(binding = 4) uniform sampler2D fbPosition;
layout(binding = 5) uniform sampler2D ssaoColor;

uniform bool horizontal = false;
uniform float weight[WEIGHTSIZE] = float[] (0.227027, 0.1945946, 0.1216216, 0.1216216, 0.1216216, 0.1216216, 0.054054, 0.016216);

out vec4 FragColor;

vec4 GaussianBlur(sampler2D image, bool horizontal, float[WEIGHTSIZE] weight) 
{
    vec2 tex_offset = 4.0 / textureSize(image, 0); // gets size of single texel
    vec3 result = texture(image, TexCoords).rgb * weight[0]; // current fragment's contribution
    if(horizontal)
    {
        for(int i = 1; i < WEIGHTSIZE; ++i)
        {
            result += texture(image, TexCoords + vec2(tex_offset.x * i, 0.0)).rgb * weight[i];
            result += texture(image, TexCoords - vec2(tex_offset.x * i, 0.0)).rgb * weight[i];
        }
    }
    else
    {
        for(int i = 1; i < WEIGHTSIZE; ++i)
        {
            result += texture(image, TexCoords + vec2(0.0, tex_offset.y * i)).rgb * weight[i];
            result += texture(image, TexCoords - vec2(0.0, tex_offset.y * i)).rgb * weight[i];
        }
    }
    return vec4(result, 1.0);
}

void main()
{
    vec4 color = texture(fbColor, TexCoords);
    vec4 transparentColor = texture(fbTransparent, TexCoords);
    vec4 ssao = clamp(texture(ssaoColor, TexCoords).rrrr + 0.3, 0, 1);
    
    FragColor = mix(color * ssao, transparentColor, transparentColor.a)
    + GaussianBlur(fbBrightness, horizontal, weight)
    + GaussianBlur(fbBrightness, !horizontal, weight);
    
//    FragColor = ssao;
}
