using System.Numerics;
using Minecraft;
using Minecraft.Graphics;
using Minecraft.Utils;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using PixelType = Silk.NET.OpenGL.PixelType;
using Shader = Minecraft.Shader;

public class DeferredRenderer
{
    private GL _gl;
    private IWindow window;
    
    private uint width, height;

    private uint gBuffer;

    private uint gPosition, gNormal, gAlbedoSpec;

    private uint rboDepth;

    private Shader shaderGeometryPass, shaderLightingPass;

    private Shader unlitColorShader;

    private QuadGeometry quad;
    private CubeGeometry cube;
    
    public Camera camera;
    public ChunkMesh[,] chunks;
    public List<Vector3> lightPositions;
    public List<Vector3> lightColors;

    public DeferredRenderer(GL gl, IWindow window, Camera camera)
    {
        this._gl = gl;
        this.window = window;
        this.width = (uint) window.FramebufferSize.X;
        this.height = (uint) window.FramebufferSize.Y;

        this.camera = camera;

        shaderGeometryPass = new Shader(_gl, 
            AssetPaths.shaderPath + "DeferredRendering/gBuffer.vert",
            AssetPaths.shaderPath + "DeferredRendering/gBuffer.frag");
        shaderLightingPass = new Shader(_gl, 
            AssetPaths.shaderPath + "DeferredRendering/deferredShading.vert",
            AssetPaths.shaderPath + "DeferredRendering/deferredShading.frag");

        unlitColorShader = new Shader(_gl, 
            AssetPaths.shaderPath + "UnlitColor.vert",
            AssetPaths.shaderPath + "UnlitColor.frag");
        
        lightPositions = new List<Vector3>();
        lightPositions.Add(new Vector3(-9,9,-9));
        lightPositions.Add(new Vector3(9,9,9));
        lightPositions.Add(new Vector3(0,20,0));

        lightColors = new List<Vector3>();
        lightColors.Add(new Vector3(1,1,1));
        lightColors.Add(new Vector3(1,1,1));
        lightColors.Add(new Vector3(1,1,1));

        quad = new QuadGeometry(_gl);
        cube = new CubeGeometry(_gl);

        // InitGBuffer();
    }
    
    private unsafe void InitGBuffer()
    {
        // G-Buffer Configuration
        gBuffer = _gl.GenFramebuffer();
        _gl.BindFramebuffer(GLEnum.Framebuffer, gBuffer);
        
        // Position Color Buffer
        gPosition = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, gPosition);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb16f, (uint) width, (uint) height, 0, GLEnum.Rgb, PixelType.Float, null);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, gPosition, 0);
        
        // Normal Color Buffer
        gNormal = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, gNormal);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb16f, (uint) width, (uint) height, 0, GLEnum.Rgb, PixelType.Float, null);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, gNormal, 0);
        
        // Color + Specular Color Buffer
        gAlbedoSpec = _gl.GenTexture();
        _gl.BindTexture(TextureTarget.Texture2D, gAlbedoSpec);
        _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba, (uint) width, (uint) height, 0, GLEnum.Rgba, PixelType.UnsignedByte, null);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        _gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, gAlbedoSpec, 0);
        
        // Tell OpenGL which color attachment of the framebuffer we'll use for rendering
        DrawBufferMode[] attachments = new[]
        {
            DrawBufferMode.ColorAttachment0, 
            DrawBufferMode.ColorAttachment1,
            DrawBufferMode.ColorAttachment2
        };
        _gl.DrawBuffers(3, new Span<DrawBufferMode>(attachments));
        
        // Create and attach Depth Buffer
        rboDepth = _gl.GenRenderbuffer();
        _gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
        _gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, (uint) width, (uint) height);
        _gl.FramebufferRenderbuffer(GLEnum.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, rboDepth);

        if (_gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer not complete!");
        }
    }

    public void RenderDeferred()
    {
        // 1. Geometry Pass
        _gl.BindFramebuffer(GLEnum.Framebuffer, gBuffer);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FieldOfView),
            (float) width / height, camera.nearClipPlane, camera.farClipPlane);
        var view = camera.GetViewMatrix();
        var model = Matrix4x4.Identity;
        
        shaderGeometryPass.Use();
        shaderGeometryPass.SetUniform("projection", projection);
        shaderGeometryPass.SetUniform("view", view);
        
        for (int x = 0; x < chunks.GetLength(0); x++)
        {
            for (int z = 0; z < chunks.GetLength(1); z++)
            {
                var newModel = chunks[x,z].Transform(model);
                shaderGeometryPass.SetUniform("model", newModel);
                chunks[x,z].DrawOpaque();
            }
        }

        // 2. Lighting Pass
        _gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        _gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        shaderLightingPass.Use();
        
        _gl.ActiveTexture(TextureUnit.Texture0);
        _gl.BindTexture(TextureTarget.Texture2D, gPosition);
        _gl.ActiveTexture(TextureUnit.Texture1);
        _gl.BindTexture(TextureTarget.Texture2D, gNormal);
        _gl.ActiveTexture(TextureUnit.Texture2);
        _gl.BindTexture(TextureTarget.Texture2D, gAlbedoSpec);
        
        // Send light relevant uniforms
        for (int i = 0; i < lightPositions.Count; i++)
        {
            shaderLightingPass.SetUniform($"lights[{i}].Position", lightPositions[i]);
            shaderLightingPass.SetUniform($"lights[{i}].Color", lightColors[i]);
        
            const float constant = 1.0f;
            const float linear = 1f;
            const float quadratic = 1.8f;
            shaderLightingPass.SetUniform($"lights[{i}].Linear", linear);
            shaderLightingPass.SetUniform($"lights[{i}].Quadratic", quadratic);
        }
        shaderLightingPass.SetUniform("viewPos", camera.Position);
        
        // Rendering on a quad
        RenderQuad();
    }

    public void RenderForward()
    {
        // Forward Rendering Pass for cube lights
        
        var projection = Matrix4x4.CreatePerspectiveFieldOfView(MathHelper.DegreesToRadians(camera.FieldOfView),
            (float) width / height, camera.nearClipPlane, camera.farClipPlane);
        var view = camera.GetViewMatrix();
        var model = Matrix4x4.Identity;
        
        unlitColorShader.Use();
        unlitColorShader.SetUniform("projection", projection);
        unlitColorShader.SetUniform("view", view);
        
        for (int i = 0; i < lightPositions.Count; i++)
        {
            var newModel = model * Matrix4x4.CreateTranslation(lightPositions[i]);
            unlitColorShader.SetUniform("model", newModel);
            unlitColorShader.SetUniform("color", lightColors[i]);
            cube.Draw();
        }
    }

    public void RenderQuad()
    {
        quad.Draw();
    }
}