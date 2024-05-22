using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using System;
using System.IO;
using System.Numerics;
using Minecraft;
using Minecraft.Graphics;

public class VoxelTerrain
{
    private GL gl;
    
    private uint computeShader, voxelShaderProgram, voxelTexture;
    private VertexArrayObject<float, uint> quadVAO;
    private BufferObject<float> quadVBO;
    private Vector3 chunkOffset;

    public VoxelTerrain(GL gl, IWindow window)
    {
        var options = WindowOptions.Default;

        this.gl = gl;
    }

    private void OnLoad()
    {
        // Chargement et compilation du compute shader
        computeShader = gl.CreateShader(ShaderType.ComputeShader);
        var computeSource = File.ReadAllText("../../../Assets/Shaders/VoxelComputeShader.glsl");
        gl.ShaderSource(computeShader, computeSource);
        gl.CompileShader(computeShader);
        CheckShaderCompileError(computeShader);

        voxelShaderProgram = gl.CreateProgram();
        gl.AttachShader(voxelShaderProgram, computeShader);
        gl.LinkProgram(voxelShaderProgram);
        CheckProgramLinkError(voxelShaderProgram);

        // Création de la texture 3D pour stocker les voxels
        voxelTexture = gl.GenTexture();
        gl.BindTexture(TextureTarget.Texture3D, voxelTexture);
        gl.TexImage3D(TextureTarget.Texture3D, 0, InternalFormat.Rgba8, 256, 256, 256, 0, PixelFormat.Rgba, PixelType.UnsignedByte, IntPtr.Zero);
        gl.BindTexture(TextureTarget.Texture3D, 0);

        // Chargement et compilation du fragment shader
        var fragmentShader = gl.CreateShader(ShaderType.FragmentShader);
        var fragmentSource = File.ReadAllText("../../../Assets/Shaders/VoxelFragmentShader.glsl");
        gl.ShaderSource(fragmentShader, fragmentSource);
        gl.CompileShader(fragmentShader);
        CheckShaderCompileError(fragmentShader);

        voxelShaderProgram = gl.CreateProgram();
        gl.AttachShader(voxelShaderProgram, fragmentShader);
        gl.LinkProgram(voxelShaderProgram);
        CheckProgramLinkError(voxelShaderProgram);

        gl.UseProgram(voxelShaderProgram);
        gl.Uniform1(gl.GetUniformLocation(voxelShaderProgram, "voxelTexture"), 0);
        
        float[] quadVertices = {
            // Positions        // TexCoords
            -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
            -1.0f, -1.0f,  1.0f,  0.0f, 0.0f, 1.0f,
            1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,

            -1.0f,  1.0f,  1.0f,  0.0f, 1.0f, 1.0f,
            1.0f, -1.0f,  1.0f,  1.0f, 0.0f, 1.0f,
            1.0f,  1.0f,  1.0f,  1.0f, 1.0f, 1.0f,

            // Repeat for all six faces of the cube...
        };
        
        // Initialisation du VAO et du VBO pour dessiner un cube
        quadVBO = new BufferObject<float>(gl, quadVertices, BufferTargetARB.ArrayBuffer);
        quadVAO = new VertexArrayObject<float, uint>(gl, quadVBO);
    }

    private void OnRender(double deltaTime)
    {
        
    }

    private void CheckShaderCompileError(uint shader)
    {
        gl.GetShader(shader, ShaderParameterName.CompileStatus, out var success);
        if (success == 0)
        {
            var infoLog = gl.GetShaderInfoLog(shader);
            Console.WriteLine($"Erreur de compilation du shader:\n{infoLog}");
        }
    }

    private void CheckProgramLinkError(uint program)
    {
        gl.GetProgram(program, ProgramPropertyARB.LinkStatus, out var success);
        if (success == 0)
        {
            var infoLog = gl.GetProgramInfoLog(program);
            Console.WriteLine($"Erreur de linking du shader program:\n{infoLog}");
        }
    }
}
