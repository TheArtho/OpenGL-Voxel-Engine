using System.Numerics;
using Graphics;
using Minecraft.Core;
using Minecraft.Utils;
using Silk.NET.Maths;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;
using Tutorial;

namespace Minecraft.Graphics;

public class Renderer
{
    private GL Gl;
    private IWindow window;
    
    private CubeGeometry cube;
    private QuadGeometry quad;

    private Shader BasicShader;
    private Shader LitShader;
    private Shader LitTransparentShader;
    private Shader UnlitColorShader;
    private Shader SkyShader;
    private Shader QuadShader;
    private Shader SSAOShader;
    private Shader SSAOBlurShader;
    
    private DirectionalLight dirLight;
    
    private List<Vector3> lightPositions;
    private List<Vector3> lightColors;
    private List<Vector3> transformedLightPosition;

    private float lightQuadratic;
    private float lightLinear;
    private float lightOffset;

    private Sky skybox;

    private uint FrameBuffer;
    private uint fbColor;
    private uint fbTransparent;
    private uint fbBrightness;
    private uint fbNormal;
    private uint fbPosition;
    private uint rboDepth;
    
    private uint ssaoFBO, ssaoBlurFBO;
    private uint ssaoColorBuffer, ssaoColorBufferBlur;
    
    // SSAO
    
    private Vector3[] ssaoKernel;
    private const int kernelSize = 64;
    
    private uint ssaoNoiseTexture;
    private float[] ssaoNoise;
    private const int noiseSize = 16;
    
    // World
    
    public Camera camera;
    public Game.World world;

    struct LightParam
    {
        public Vector3 ambient;
        public Vector3 diffuse;
        public Vector3 HorizonColor;
        public Vector3 SkyColor;
    }

    private LightParam DayParams = new LightParam()
    {
        ambient = new Vector3(0.5f),
        diffuse = new Vector3(0.7f),
        HorizonColor = new Vector3(206, 226, 239) / 255,
        SkyColor = new Vector3(140, 199, 254) / 255,
    };
    
    private LightParam SunsetParams = new LightParam()
    {
        ambient = new Vector3(0.4f),
        diffuse = new Vector3(0.5f),
        HorizonColor = new Vector3(157, 64, 50) / 255,
        SkyColor = new Vector3(31, 47, 72) / 255,
    };
    
    private LightParam NightParams = new LightParam()
    {
        ambient = new Vector3(0.2f),
        diffuse = new Vector3(0.2f),
        HorizonColor = new Vector3(44, 50, 64) / 255,
        SkyColor = new Vector3(22, 31, 48) / 255,
    };

    private LightParam currentParams;
    private int paramIndex;

    // public Texture2D modelTex;
    // public Model model;
    
    public Renderer(GL gl, IWindow window, Camera camera)
    {
        Gl = gl;
        this.window = window;
        this.camera = camera;
        
        // BasicShader = new Shader(Gl, 
        //     AssetPaths.shaderPath + "shader.vert", 
        //     AssetPaths.shaderPath + "shader.frag");
        
        LitShader = new Shader(Gl, 
            AssetPaths.shaderPath + "VoxelVertexShader.glsl", 
            AssetPaths.shaderPath + "VoxelFragmentShader.glsl");
        
        LitTransparentShader = new Shader(Gl, 
            AssetPaths.shaderPath + "VoxelVertexShader.glsl", 
            AssetPaths.shaderPath + "VoxelFragmentShaderTransparent.glsl");
        
        UnlitColorShader = new Shader(Gl, 
            AssetPaths.shaderPath + "UnlitColor.vert",
            AssetPaths.shaderPath + "UnlitColor.frag");
        
        SkyShader = new Shader(Gl, 
            AssetPaths.shaderPath + "skybox.vert",
            AssetPaths.shaderPath + "skybox.frag");
        
        QuadShader = new Shader(Gl, 
            AssetPaths.shaderPath + "ForwardRendering/ForwardQuad.vert", 
            AssetPaths.shaderPath + "ForwardRendering/ForwardQuad.frag");
        
        SSAOShader = new Shader(Gl, 
            AssetPaths.shaderPath + "ForwardRendering/ForwardQuad.vert", 
            AssetPaths.shaderPath + "SSAO/ssao.frag");
        
        SSAOBlurShader = new Shader(Gl, 
            AssetPaths.shaderPath + "ForwardRendering/ForwardQuad.vert", 
            AssetPaths.shaderPath + "SSAO/ssaoBlur.frag");

        dirLight = new DirectionalLight()
        {
            direction = new Vector3(-1 , -1, 0),
            ambient = new Vector3(0.5f),
            // ambient = new Vector3(1),
            diffuse = new Vector3(0.7f),
            // diffuse = new Vector3(1),
            specular = new Vector3(0.8f)
        };
        
        lightPositions =
        [
            new Vector3(60 + 10, 50, 8 + 30),
            new Vector3(60, 50, 8 + 30),
            new Vector3(60 - 10, 50, 8 + 30),
            new Vector3(60 - 20, 45, 8 + 10),
            new Vector3(60, 50, 8 + 20),
            new Vector3(60 - 5, 50, 8 + 32),
        ];

        lightColors =
        [
            new Vector3(1, 1, 1),
            new Vector3(0, 1, 1),
            new Vector3(1, 1, 0),
            new Vector3(1, 0, 1),
            new Vector3(1, 0, 0),
            new Vector3(0.2f, 0.8f, 0.4f)
        ];

        cube = new CubeGeometry(Gl);
        quad = new QuadGeometry(Gl);

        skybox = new Sky(Gl);

        // modelTex = new Texture2D(Gl, AssetPaths.texturePath + "../Models/tree_texture.png");
        // model = new Model(Gl, AssetPaths.texturePath + "../Models/tree.obj");
        
        InitBuffers();

        currentParams = DayParams;
    }

    public void InitBuffers()
    {
        // Frame Buffer & Depth Render Buffer Initialization
        InitFrameBuffer();
        InitColorBuffer();
        InitBrightnessBuffer();
        InitTransparentBuffer();
        InitNormalBuffer();
        InitPositionBuffer();
        
        Gl.DrawBuffers(5, new GLEnum[] {
            GLEnum.ColorAttachment0, 
            GLEnum.ColorAttachment1, 
            GLEnum.ColorAttachment2,
            GLEnum.ColorAttachment3,
            GLEnum.ColorAttachment4,
        });
        
        InitDepthRenderBuffer();
        
        InitSSAOBuffers();
        InitSSAOKernel();
        InitSSAONoise();
        
        if (Gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
        {
            Console.WriteLine("Framebuffer not complete!");
        }
    }

    public unsafe void InitFrameBuffer()
    {
        // Buffer configuration
        FrameBuffer = Gl.GenFramebuffer();
    }

    public unsafe void RefreshFramebuffer(Vector2D<int> newSize)
    {
        Gl.BindTexture(TextureTarget.Texture2D, fbColor);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb, (uint) newSize.X, (uint) newSize.Y, 0, GLEnum.Rgb, PixelType.UnsignedByte, null);
        
        Gl.BindTexture(TextureTarget.Texture2D, fbBrightness);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb16f, (uint) newSize.X, (uint) newSize.Y, 0, GLEnum.Rgb, PixelType.Float, null);
        
        Gl.BindTexture(TextureTarget.Texture2D, fbTransparent);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba, (uint) newSize.X, (uint) newSize.Y, 0, GLEnum.Rgba, PixelType.UnsignedByte, null);
        
        Gl.BindTexture(TextureTarget.Texture2D, fbNormal);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb16f, (uint) newSize.X, (uint) newSize.Y, 0, GLEnum.Rgb, PixelType.Float, null);
        
        Gl.BindTexture(TextureTarget.Texture2D, fbPosition);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba32f, (uint) newSize.X, (uint) newSize.Y, 0, GLEnum.Rgba, PixelType.Float, null);
        
        Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
        Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, (uint) newSize.X, (uint) newSize.X);
        
        Gl.BindTexture(TextureTarget.Texture2D, ssaoColorBuffer);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R32f, (uint)newSize.X, (uint)newSize.Y, 0, GLEnum.Red, PixelType.Float, null);
        
        Gl.BindTexture(TextureTarget.Texture2D, ssaoColorBufferBlur);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R32f, (uint)newSize.X, (uint)newSize.Y, 0, GLEnum.Red, PixelType.Float, null);
    }

    public unsafe void InitColorBuffer()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, FrameBuffer);
        
        // Color Buffer
        fbColor = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, fbColor);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb, (uint) window.FramebufferSize.X, (uint) window.FramebufferSize.Y, 0, GLEnum.Rgb, PixelType.UnsignedByte, null);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, fbColor, 0);
    }

    public unsafe void InitBrightnessBuffer()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, FrameBuffer);
        
        // Brightness Buffer
        fbBrightness = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, fbBrightness);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb16f, (uint) window.FramebufferSize.X, (uint) window.FramebufferSize.Y, 0, GLEnum.Rgb, PixelType.Float, null);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment1, TextureTarget.Texture2D, fbBrightness, 0);
    }
    
    public unsafe void InitTransparentBuffer()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, FrameBuffer);
        
        // Color Buffer
        fbTransparent = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, fbTransparent);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba, (uint) window.FramebufferSize.X, (uint) window.FramebufferSize.Y, 0, GLEnum.Rgba, PixelType.UnsignedByte, null);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment2, TextureTarget.Texture2D, fbTransparent, 0);
    }
    
    public unsafe void InitNormalBuffer()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, FrameBuffer);
        
        // Normal Buffer
        fbNormal = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, fbNormal);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb16f, (uint) window.FramebufferSize.X, (uint) window.FramebufferSize.Y, 0, GLEnum.Rgb, PixelType.Float, null);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment3, TextureTarget.Texture2D, fbNormal, 0);
    }
    
    public unsafe void InitPositionBuffer()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, FrameBuffer);
        
        // Position Buffer
        fbPosition = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, fbPosition);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba32f, (uint) window.FramebufferSize.X, (uint) window.FramebufferSize.Y, 0, GLEnum.Rgba, PixelType.Float, null);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapR, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.ClampToEdge);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.ClampToEdge);
        Gl.FramebufferTexture2D(FramebufferTarget.Framebuffer, FramebufferAttachment.ColorAttachment4, TextureTarget.Texture2D, fbPosition, 0);
    }

    public unsafe void InitDepthRenderBuffer()
    {
        // Create and attach Depth Buffer
        rboDepth = Gl.GenRenderbuffer();
        Gl.BindRenderbuffer(RenderbufferTarget.Renderbuffer, rboDepth);
        Gl.RenderbufferStorage(RenderbufferTarget.Renderbuffer, InternalFormat.DepthComponent, (uint) window.FramebufferSize.X, (uint) (uint) window.FramebufferSize.Y);
        Gl.FramebufferRenderbuffer(GLEnum.Framebuffer, FramebufferAttachment.DepthAttachment, RenderbufferTarget.Renderbuffer, rboDepth);
    }
    
    public unsafe void InitSSAOBuffers()
    {
        // Générer et configurer le framebuffer pour SSAO
        ssaoFBO = Gl.GenFramebuffer();
        Gl.BindFramebuffer(GLEnum.Framebuffer, ssaoFBO);

        // Créer et configurer la texture SSAO Color Buffer
        ssaoColorBuffer = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, ssaoColorBuffer);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R32f, (uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y, 0, GLEnum.Red, PixelType.Float, null);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        Gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ssaoColorBuffer, 0);

        // Vérifier que le framebuffer SSAO est complet
        if (Gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
            Console.WriteLine("SSAO Framebuffer not complete!");

        // Générer et configurer le framebuffer pour le flou SSAO
        ssaoBlurFBO = Gl.GenFramebuffer();
        Gl.BindFramebuffer(GLEnum.Framebuffer, ssaoBlurFBO);

        // Créer et configurer la texture SSAO Blur Color Buffer
        ssaoColorBufferBlur = Gl.GenTexture();
        Gl.BindTexture(TextureTarget.Texture2D, ssaoColorBufferBlur);
        Gl.TexImage2D(TextureTarget.Texture2D, 0, (int)InternalFormat.R32f, (uint)window.FramebufferSize.X, (uint)window.FramebufferSize.Y, 0, GLEnum.Red, PixelType.Float, null);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)GLEnum.Nearest);
        Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)GLEnum.Nearest);
        Gl.FramebufferTexture2D(GLEnum.Framebuffer, FramebufferAttachment.ColorAttachment0, TextureTarget.Texture2D, ssaoColorBufferBlur, 0);

        // Vérifier que le framebuffer SSAO Blur est complet
        if (Gl.CheckFramebufferStatus(GLEnum.Framebuffer) != GLEnum.FramebufferComplete)
            Console.WriteLine("SSAO Blur Framebuffer not complete!");

        // Désassocier le framebuffer pour éviter d'éventuelles erreurs
        Gl.BindFramebuffer(GLEnum.Framebuffer, 0);
    }
    
    private void InitSSAOKernel()
    {
        ssaoKernel = new Vector3[kernelSize];
        Random rand = new Random();

        for (int i = 0; i < kernelSize; i++)
        {
            Vector3 sample = new Vector3(
                (float)(rand.NextDouble() * 2.0 - 1.0), // x
                (float)(rand.NextDouble() * 2.0 - 1.0), // y
                (float)rand.NextDouble()               // z
            );
            sample = Vector3.Normalize(sample);
            sample *= (float)rand.NextDouble();

            float scale = (float)i / kernelSize;
            scale = lerp(0.1f, 1.0f, scale * scale);
            sample *= scale;

            ssaoKernel[i] = sample;
        }
        
        SSAOShader.Use();
        SSAOShader.SetUniform("samples", ssaoKernel);
    }
    
    float lerp(float a, float b, float f)
    {
        return a + f * (b - a);
    }  

    private unsafe void InitSSAONoise()
    {
        ssaoNoise = new float[noiseSize * 3];
        Random rand = new Random();

        for (int i = 0; i < noiseSize; i++)
        {
            int index = i * 3;
            
            Vector3 noise = new Vector3(
                (float)(rand.NextDouble() * 2.0 - 1.0), // x
                (float)(rand.NextDouble() * 2.0 - 1.0), // y
                0                                       // z
            );
            ssaoNoise[index] = noise.X;
            ssaoNoise[index + 1] = noise.Y;
            ssaoNoise[index + 2] = noise.Z;
        }

        fixed (void* d = &ssaoNoise[0])
        {
            // Génération de la texture de bruit
            ssaoNoiseTexture = Gl.GenTexture();
            Gl.BindTexture(TextureTarget.Texture2D, ssaoNoiseTexture);
            Gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgb32f, 4, 4, 0, GLEnum.Rgb, PixelType.Float, d);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.Repeat);
            Gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.Repeat);
        }
    }

    public void OnRender()
    {
        Vector3 cameraPosition = camera.Position;
        
        transformedLightPosition = new List<Vector3>(lightPositions);
        
        // for (int i = 0; i < transformedLightPosition.Count; i++)
        // {
        //     transformedLightPosition[i] = lightPositions[i] + World.Up * MathF.Sin((float) window.Time) * 2;
        // }
        
        Gl.Enable(EnableCap.DepthTest);
        
        ColorBrightnessPass();
        SSAOPass();
        SSAOBlurPass();
        FinalPass();
        
        if (!Game.World.PositionsEquals(cameraPosition, Engine.MainCamera.Position))
        {
            world.UpdateLODs();
        }
    }
    
    private void ColorBrightnessPass()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, FrameBuffer);
        
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        var view = camera.GetViewMatrix();
        var projection = camera.GetProjectionMatrix();
        var model = Matrix4x4.Identity;
        
        // Skybox
        
        Gl.DepthMask(false);
        
        SkyShader.Use();
        SkyShader.SetUniform("projection", projection);
        SkyShader.SetUniform("view", view);
        
        SkyShader.SetUniform("SkyColor", currentParams.SkyColor);
        SkyShader.SetUniform("HorizonColor", currentParams.HorizonColor);

        var skyModel = model * skybox.Transform();
        SkyShader.SetUniform("model", skyModel);
        SkyShader.SetUniform("CameraPosition", Engine.MainCamera.Position);
        skybox.RenderSky();
        
        Gl.DepthMask(true);
        
        // Gl.Clear(ClearBufferMask.DepthBufferBit);
        
        Gl.Enable(EnableCap.CullFace);

        LitShader.Use();
        LitShader.SetUniform("uView", view);
        LitShader.SetUniform("uProjection", projection);
        // LitShader.SetUniform("dirLight.direction", dirLight.direction);
        LitShader.SetUniform("dirLight.ambient", currentParams.ambient);
        LitShader.SetUniform("dirLight.diffuse", currentParams.diffuse);
        // LitShader.SetUniform("dirLight.specular", dirLight.specular);
        LitShader.SetUniform("viewPos", camera.Position);
        // LitShader.SetUniform("HorizonColor", currentParams.HorizonColor);

        for (int i = 0; i < lightPositions.Count; i++)
        {
            LitShader.SetUniform($"pointLights[{i}].position", transformedLightPosition[i]);
                
            LitShader.SetUniform($"pointLights[{i}].constant",1f);
            LitShader.SetUniform($"pointLights[{i}].linear",0.35f / 10);
            LitShader.SetUniform($"pointLights[{i}].quadratic",0.44f / 10);
                
            LitShader.SetUniform($"pointLights[{i}].ambient", paramIndex == 0 ? Vector3.Zero : lightColors[i]);
            LitShader.SetUniform($"pointLights[{i}].diffuse", new Vector3(0.8f));
            LitShader.SetUniform($"pointLights[{i}].specular", new Vector3(1f));
        }
        
        world.DrawOpaque(LitShader);
        
        // Transparent
        
        LitTransparentShader.Use();
        LitTransparentShader.SetUniform("uView", view);
        LitTransparentShader.SetUniform("uProjection", projection);
        LitTransparentShader.SetUniform("dirLight.direction", dirLight.direction);
        LitTransparentShader.SetUniform("dirLight.ambient", currentParams.ambient);
        LitTransparentShader.SetUniform("dirLight.diffuse", currentParams.diffuse);
        LitTransparentShader.SetUniform("dirLight.specular", dirLight.specular);
        LitTransparentShader.SetUniform("viewPos", camera.Position);
        // LitTransparentShader.SetUniform("HorizonColor", currentParams.HorizonColor);

        for (int i = 0; i < lightPositions.Count; i++)
        {
            LitTransparentShader.SetUniform($"pointLights[{i}].position", transformedLightPosition[i]);
                
            LitTransparentShader.SetUniform($"pointLights[{i}].constant",1f);
            LitTransparentShader.SetUniform($"pointLights[{i}].linear",0.35f);
            LitTransparentShader.SetUniform($"pointLights[{i}].quadratic",0.44f);
                
            LitTransparentShader.SetUniform($"pointLights[{i}].ambient",  paramIndex == 0 ? Vector3.Zero : lightColors[i]);
            LitTransparentShader.SetUniform($"pointLights[{i}].diffuse", new Vector3(0.8f));
            LitTransparentShader.SetUniform($"pointLights[{i}].specular", new Vector3(1f));
        }
        
        world.DrawTransparent(LitTransparentShader);
        
        Gl.Disable(EnableCap.CullFace);

        // Light source shaders
        
        UnlitColorShader.Use();
        UnlitColorShader.SetUniform("projection", projection);
        UnlitColorShader.SetUniform("view", view);
            
        for (int i = 0; i < lightPositions.Count; i++)
        {
            var newModel = model * Matrix4x4.CreateScale(0.5f) * Matrix4x4.CreateTranslation(transformedLightPosition[i]);
            UnlitColorShader.SetUniform("model", newModel);
            UnlitColorShader.SetUniform("color", lightColors[i]);
            cube.Draw();
        }
    }
    
    public void SSAOPass()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, ssaoFBO);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        SSAOShader.Use();
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, fbPosition);
        Gl.ActiveTexture(TextureUnit.Texture1);
        Gl.BindTexture(TextureTarget.Texture2D, fbNormal);
        Gl.ActiveTexture(TextureUnit.Texture2);
        Gl.BindTexture(TextureTarget.Texture2D, ssaoNoiseTexture);
        SSAOShader.SetUniform("projection", camera.GetProjectionMatrix());
        // SSAOShader.SetUniform("samples", ssaoKernel);
        SSAOShader.SetUniform("noiseScale", new Vector2(window.FramebufferSize.X / 4, window.FramebufferSize.Y / 4));
        quad.Draw();
    }

    public void SSAOBlurPass()
    {
        Gl.BindFramebuffer(GLEnum.Framebuffer, ssaoBlurFBO);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        SSAOBlurShader.Use();
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, ssaoColorBuffer);
        quad.Draw();
    }

    public void FinalPass()
    {
        // Pass on the render quad
        Gl.BindFramebuffer(GLEnum.Framebuffer, 0);
        Gl.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit);
        
        QuadShader.Use();
        
        Gl.ActiveTexture(TextureUnit.Texture0);
        Gl.BindTexture(TextureTarget.Texture2D, fbColor);
        Gl.ActiveTexture(TextureUnit.Texture1);
        Gl.BindTexture(TextureTarget.Texture2D, fbBrightness);
        Gl.ActiveTexture(TextureUnit.Texture2);
        Gl.BindTexture(TextureTarget.Texture2D, fbTransparent);
        Gl.ActiveTexture(TextureUnit.Texture3);
        Gl.BindTexture(TextureTarget.Texture2D, fbNormal);
        Gl.ActiveTexture(TextureUnit.Texture4);
        Gl.BindTexture(TextureTarget.Texture2D, fbPosition);
        Gl.ActiveTexture(TextureUnit.Texture5);
        Gl.BindTexture(TextureTarget.Texture2D, ssaoColorBufferBlur);
        
        quad.Draw();
    }

    public void SwapLightParams()
    {
        paramIndex = (paramIndex + 1) % 3;
        
        if (paramIndex == 0)
        {
            currentParams = DayParams;
        } 
        else if (paramIndex == 1)
        {
            currentParams = SunsetParams;
        }
        else if (paramIndex == 2)
        {
            currentParams = NightParams;
        }
    }

    public void Dispose()
    {
        LitShader.Dispose();
        UnlitColorShader.Dispose();
        QuadShader.Dispose();
        world.Dispose();
    }
}