using Minecraft;

namespace ConsoleApp1.Source.Graphics;

public class Material
{
    public Shader shader;
    public Texture2D Texture2D;

    public Material(Shader shader, Texture2D texture2D)
    {
        this.shader = shader;
        this.Texture2D = texture2D;
    }
}