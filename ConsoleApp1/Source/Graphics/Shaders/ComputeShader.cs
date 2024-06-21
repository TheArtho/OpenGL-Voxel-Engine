using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices.JavaScript;
using Newtonsoft.Json;
using Silk.NET.OpenGL;
using Silk.NET.Windowing;

namespace Minecraft
{
    public class ComputeShader : IDisposable
    {
        private uint _handle;
        private GL _gl;

        public ComputeShader(GL gl, string path)
        {
            _gl = gl;
            
            uint compute = LoadShader(ShaderType.ComputeShader, path);
            _handle = _gl.CreateProgram();
            _gl.AttachShader(_handle, compute);
            _gl.LinkProgram(_handle);
            _gl.GetProgram(_handle, GLEnum.LinkStatus, out var status);
            if (status == 0)
            {
                throw new Exception($"Program failed to link with error: {_gl.GetProgramInfoLog(_handle)}");
            }
            _gl.DetachShader(_handle, compute);
            _gl.DeleteShader(compute);
        }

        public void Use()
        {
            _gl.UseProgram(_handle);
        }

        public void Unbind()
        {
            _gl.UseProgram(0);
        }
        
        public unsafe void SetUniform(string name, Vector2 value)
        {
            //A new overload has been created for setting a uniform so we can use the transform in our shader.
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform2(location, (float) value.X, (float) value.Y);
        }
        
        public unsafe void SetUniform(string name, Vector3 value)
        {
            //A new overload has been created for setting a uniform so we can use the transform in our shader.
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform3(location, (float) value.X, (float) value.Y, value.Z);
        }
        
        public unsafe void SetUniform(string name, Vector4 value)
        {
            //A new overload has been created for setting a uniform so we can use the transform in our shader.
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform4(location, (float) value.X, (float) value.Y, value.Z, value.W);
        }

        public unsafe void SetUniform(string name, Matrix4x4 value)
        {
            //A new overload has been created for setting a uniform so we can use the transform in our shader.
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.UniformMatrix4(location, 1, false, (float*) &value);
        }

        public void SetUniform(string name, float value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform1(location, value);
        }
        
        public void SetUniform(string name, uint value)
        {
            int location = _gl.GetUniformLocation(_handle, name);
            if (location == -1)
            {
                throw new Exception($"{name} uniform not found on shader.");
            }
            _gl.Uniform1(location, value);
        }

        public void Dispose()
        {
            _gl.DeleteProgram(_handle);
        }

        private uint LoadShader(ShaderType type, string path)
        {
            string src = File.ReadAllText(path);
            uint handle = _gl.CreateShader(type);
            _gl.ShaderSource(handle, src);
            _gl.CompileShader(handle);
            string infoLog = _gl.GetShaderInfoLog(handle);
            if (!string.IsNullOrWhiteSpace(infoLog))
            {
                throw new Exception($"Error compiling shader of type {type}, failed with error {infoLog}");
            }

            return handle;
        }
    }
}

