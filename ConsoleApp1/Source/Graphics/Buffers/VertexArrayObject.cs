using Silk.NET.OpenGL;
using System;

namespace Minecraft
{
    public class VertexArrayObject<TVertexType, TIndexType> : IDisposable
        where TVertexType : unmanaged
        where TIndexType : unmanaged
    {
        private uint _handle;
        private GL _gl;

        public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo, BufferObject<TIndexType> ebo)
        {
            _gl = gl;

            _handle = _gl.GenVertexArray();
            Bind();
            vbo.Bind();
            ebo.Bind();
        }
        
        public VertexArrayObject(GL gl, BufferObject<TVertexType> vbo)
        {
            _gl = gl;

            _handle = _gl.GenVertexArray();
            Bind();
            vbo.Bind();
        }

        public unsafe void VertexAttributePointer(uint index, int count, VertexAttribPointerType type, uint vertexSize, int offSet)
        {
            _gl.VertexAttribPointer(index, count, type, false, vertexSize * (uint) sizeof(TVertexType), (void*) (offSet * sizeof(TVertexType)));
            _gl.EnableVertexAttribArray(index);
        }

        public void Bind()
        {
            _gl.BindVertexArray(_handle);
        }

        public void unBind()
        {
            _gl.BindVertexArray(0);
        }

        public void Dispose()
        {
            _gl.DeleteVertexArray(_handle);
        }
    }
}