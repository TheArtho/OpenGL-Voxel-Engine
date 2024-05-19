using Minecraft;

namespace ConsoleApp1.Source.Graphics;

public class Material
{
    public Shader shader;
    public Texture texture;

    public Material(Shader shader, Texture texture)
    {
        this.shader = shader;
        this.texture = texture;
    }
}