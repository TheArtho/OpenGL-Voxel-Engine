#version 430

in vec3 TexCoords;
out vec4 FragColor;

uniform usampler3D voxelTexture;

void main() {
    uint blockID = texture(voxelTexture, TexCoords).r;
    vec4 color = vec4(blockID.rrr / 255.0, 1);
    FragColor = vec4(color.rgb * TexCoords, 1);
}
