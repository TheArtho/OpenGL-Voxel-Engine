using Silk.NET.OpenGL;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System;
using Silk.NET.Assimp;

namespace Minecraft
{
    public class Texture2D : IDisposable
    {
        private static uint texUnit;

        public uint width;
        public uint height;
        
        private uint _handle;
        private GL _gl;
        
        public string Path { get; set; }
        public TextureType Type { get; }

        public unsafe Texture2D(GL gl, Span<byte> data, uint width, uint height)
        {
            _gl = gl;

            _handle = _gl.GenTexture();
            Bind(0);

            fixed (void* d = &data[0])
            {
                _gl.TexImage2D(TextureTarget.Texture2D, 0, (int) InternalFormat.Rgba, width, height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, d);
                SetParameters();
            }
        }

        public unsafe Texture2D(GL gl, Image<Rgba32> image, TextureType type = TextureType.None)
        {
            _gl = gl;
            
            _handle = _gl.GenTexture();
            Bind(0);

            using (var img = image)
            {
                gl.TexImage2D(TextureTarget.Texture2D, 0, InternalFormat.Rgba8, (uint) img.Width, (uint) img.Height, 0, PixelFormat.Rgba, PixelType.UnsignedByte, null);

                width = (uint) img.Width;
                height = (uint) img.Height;
                
                img.ProcessPixelRows(accessor =>
                {
                    for (int y = 0; y < accessor.Height; y++)
                    {
                        fixed (void* data = accessor.GetRowSpan(y))
                        {
                            gl.TexSubImage2D(TextureTarget.Texture2D, 0, 0, y, (uint) accessor.Width, 1, PixelFormat.Rgba, PixelType.UnsignedByte, data);
                        }
                    }
                });
            }

            SetParameters();
        }
        
        public unsafe Texture2D(GL gl, string path, TextureType type = TextureType.None) : this(gl, Image.Load<Rgba32>(path))
        {
            Path = path;
        }

        private void SetParameters()
        {
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapS, (int) GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureWrapT, (int) GLEnum.Repeat);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int) GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int) GLEnum.Nearest);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, 0);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, 8);
            _gl.GenerateMipmap(TextureTarget.Texture2D);
        }

        public void Bind(uint textureSlot)
        {
            _gl.ActiveTexture(TextureUnit.Texture0 + (int) textureSlot);
            _gl.BindTexture(TextureTarget.Texture2D, _handle);
        }

        public void BindWithLod(uint textureSlot, uint lodLevel)
        {
            Bind(textureSlot);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureBaseLevel, (int)lodLevel);
            _gl.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMaxLevel, (int)lodLevel);
        }
        
        public void Dispose()
        {
            _gl.DeleteTexture(_handle);
        }
    }
}
