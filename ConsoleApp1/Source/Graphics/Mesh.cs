using Silk.NET.OpenGL;
using System;
using System.Collections.Generic;
using System.Numerics;
using ConsoleApp1.Source.Graphics;
using ConsoleApp1.Source.Mesh;
using Minecraft;
using Cube = ConsoleApp1.Source.Mesh.Cube;
using Shader = Silk.NET.OpenGL.Shader;
using Texture = Silk.NET.OpenGL.Texture;

namespace Graphics;

public class Mesh
{
    #region Private Properties
    private class BoneData
    {
        public string name;
        public Vector3 pivot;
        public Vector3 rotation;
        public string? parent;
        
        public List<Vector3> cubeOrigins = new List<Vector3>();
        public List<Vector3> cubeRotations = new List<Vector3>();
        public List<Vector3> cubePivots = new List<Vector3>();
        public List<List<float>> faces = new List<List<float>>();
    }
    
    private GL _gl;
    
    private List<VertexArrayObject<float, uint>> _vaos;
    private List<BufferObject<float>> _vbos;

    private List<BoneData?> _bonesData;
    
    #endregion

    public Material material;

    public Mesh(GL gl, GeometryFile geometryFile)
    {
        _vbos = new List<BufferObject<float>>();
        _vaos = new List<VertexArrayObject<float, uint>>();
        _bonesData = new List<BoneData?>();
        
        _gl = gl;

        foreach (var geometry in geometryFile.Geometry)
        {
            foreach (var bone in geometry.Bones)
            {
                BoneData? boneData = new BoneData()
                {
                    name = bone.Name,
                    pivot = new Vector3(bone.Pivot[0], bone.Pivot[1], bone.Pivot[2]),
                    rotation = (bone.Rotation != null)
                        ? new Vector3(bone.Rotation[0], bone.Rotation[1], bone.Rotation[2])
                        : Vector3.Zero,
                    parent = bone.Parent
                };
                
                if (bone.Cubes == null) continue;
                
                foreach (var cube in bone.Cubes)
                {
                    AddCubeVertices(cube, boneData);
                }
                
                _bonesData.Add(boneData);
            }
        }

        InitializeBuffers();
    }

    private void AddCubeVertices(Cube cube, BoneData? boneData)
    {
        Vector3 pivot = Vector3.Zero;
        Vector3 rotation = Vector3.Zero;
        Vector3 origin = new Vector3(cube.Origin[0], cube.Origin[1], cube.Origin[2]);
        origin -= Vector3.One * (cube.Inflate / 2);
        Vector3 size = new Vector3(cube.Size[0], cube.Size[1], cube.Size[2]);
        size += Vector3.One *  (cube.Inflate / 2);
        Vector2 uv = new Vector2(cube.Uv[0], cube.Uv[1]);

        if (cube.Pivot != null)
        {
            pivot = new Vector3(cube.Pivot[0], cube.Pivot[1], cube.Pivot[2]);
        }
        
        if (cube.Rotation != null)
        {
            rotation = new Vector3(cube.Rotation[0], cube.Rotation[1], cube.Rotation[2]);
        }
        
        boneData.faces.Add(new List<float>
            {
                // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                origin.X, origin.Y, origin.Z, 0.0f, 1.0f,
                origin.X + size.X, origin.Y, origin.Z, 1.0f, 1.0f,
                origin.X + size.X, origin.Y + size.Y, origin.Z, 1.0f, 0.0f,
                origin.X + size.X, origin.Y + size.Y, origin.Z, 1.0f, 0.0f,
                origin.X, origin.Y + size.Y, origin.Z, 0.0f, 0.0f,
                origin.X, origin.Y, origin.Z, 0.0f, 1.0f,
            }
        );

        boneData.faces.Add(new List<float>
        {
            // Sommet pour une face d'un cube (6 sommets pour deux triangles)
            origin.X, origin.Y, origin.Z + size.Z, 0.0f, 1.0f,
            origin.X + size.X, origin.Y, origin.Z + size.Z, 1.0f, 1.0f,
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, 1.0f, 0.0f,
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, 1.0f, 0.0f,
            origin.X, origin.Y + size.Y, origin.Z + size.Z, 0.0f, 0.0f,
            origin.X, origin.Y, origin.Z + size.Z, 0.0f, 1.0f,
        });

        boneData.faces.Add(new List<float>
        {
            // Sommet pour une face d'un cube (6 sommets pour deux triangles)
            origin.X, origin.Y + size.Y, origin.Z + size.Z, 0.0f, 1.0f,
            origin.X, origin.Y + size.Y, origin.Z, 1.0f, 1.0f,
            origin.X, origin.Y, origin.Z, 1.0f, 0.0f,
            origin.X, origin.Y, origin.Z, 1.0f, 0.0f,
            origin.X, origin.Y, origin.Z + size.Z, 0.0f, 0.0f,
            origin.X, origin.Y + size.Y, origin.Z + size.Z, 0.0f, 1.0f,
        });

        boneData.faces.Add(new List<float>
        {
            // Sommet pour une face d'un cube (6 sommets pour deux triangles)
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, 0.0f, 1.0f,
            origin.X + size.X, origin.Y + size.Y, origin.Z, 1.0f, 1.0f,
            origin.X + size.X, origin.Y, origin.Z, 1.0f, 0.0f,
            origin.X + size.X, origin.Y, origin.Z, 1.0f, 0.0f,
            origin.X + size.X, origin.Y, origin.Z + size.Z, 0.0f, 0.0f,
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, 0.0f, 1.0f,
        });

        boneData.faces.Add(new List<float>
        {
            // Sommet pour une face d'un cube (6 sommets pour deux triangles)
            origin.X, origin.Y, origin.Z, 0.0f, 1.0f,
            origin.X + size.X, origin.Y, origin.Z, 1.0f, 1.0f,
            origin.X + size.X, origin.Y, origin.Z + size.Z, 1.0f, 0.0f,
            origin.X + size.X, origin.Y, origin.Z + size.Z, 1.0f, 0.0f,
            origin.X, origin.Y, origin.Z + size.Z, 0.0f, 0.0f,
            origin.X, origin.Y, origin.Z, 0.0f, 1.0f,
        });

        boneData.faces.Add(new List<float>
        {
            // Sommet pour une face d'un cube (6 sommets pour deux triangles)
            origin.X, origin.Y + size.Y, origin.Z, 0.0f, 1.0f,
            origin.X + size.X, origin.Y + size.Y, origin.Z, 1.0f, 1.0f,
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, 1.0f, 0.0f,
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, 1.0f, 0.0f,
            origin.X, origin.Y + size.Y, origin.Z + size.Z, 0.0f, 0.0f,
            origin.X, origin.Y + size.Y, origin.Z, 0.0f, 1.0f,
        });
        
        boneData.cubeRotations.Add(rotation);
        boneData.cubePivots.Add(pivot);
    }

    private void InitializeBuffers()
    {
        foreach (var bone in _bonesData)
        {
            if (bone.faces.Count == 0) continue;

            foreach (var face in bone.faces)
            {
                var vbo = new BufferObject<float>(_gl, new Span<float>(face.ToArray()), BufferTargetARB.ArrayBuffer);
                var vao = new VertexArrayObject<float, uint>(_gl, vbo);
            
                // Position
                vao.VertexAttributePointer(0, 3, VertexAttribPointerType.Float, 5, 0);
                // Tex Coord
                vao.VertexAttributePointer(1, 2, VertexAttribPointerType.Float, 5, 3);
            
                _vbos.Add(vbo);
                _vaos.Add(vao);
            }
        }
    }

    private Matrix4x4 ApplyTransformRecursive(BoneData? bone, int faceIndex)
    {
        return Matrix4x4.Identity // Matrix4x4.CreateTranslation(bone!.cubePivots[faceIndex])
               * Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(bone.cubeRotations[faceIndex].Z))
               * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(bone.cubeRotations[faceIndex].Y))
               * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(bone.cubeRotations[faceIndex].X))
               * ApplyTransformBoneRecursive(bone);
    }

    private Matrix4x4 ApplyTransformBoneRecursive(BoneData? bone)
    { 
        BoneData? parentBone = _bonesData.Find((data => data!.name == bone!.parent));
        
        return Matrix4x4.Identity // Matrix4x4.CreateTranslation(bone!.pivot)
               * Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(bone.rotation.Z))
               * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(bone.rotation.Y))
               * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(bone.rotation.X))
               * (parentBone == null ? Matrix4x4.Identity : ApplyTransformBoneRecursive(parentBone));
    }

    public void Render(Matrix4x4 model)
    {
        Matrix4x4 newModel;

        int i = 0;
        // Sweeping every bone
        foreach (var bone in _bonesData)
        {
            if (bone.faces.Count == 0) continue;
            
            int faceIndex = 0;
            // Rendering for each face
            foreach (var face in bone.faces)
            {
                _vaos[i].Bind();

                newModel = model; // ApplyTransformRecursive(bone, faceIndex / 6) * model;
                
                material.shader.SetUniform("uModel", newModel);
            
                _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) face.Count / 3);
                
                // Incrementing only once a face is drawn
                i++;
                faceIndex++;
            }
        }
        

    }

    public void Dispose()
    {
        for (int i = 0; i < _vbos.Count; i++)
        {
            _vbos[i].Dispose();
            _vaos[i].Dispose();
        }
    }
}
