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
    }

    public class CubeData
    {
        public string? boneParent;
        
        public Vector3 origin;
        public Vector3 rotation;
        public Vector3 pivot;
        public float DistanceToCamera;
        
        public List<List<float>> faces = new List<List<float>>();
    }
    
    private GL _gl;
    
    private List<VertexArrayObject<float, uint>> _vaos;
    private List<BufferObject<float>> _vbos;

    private List<BoneData?> _bonesData;
    private List<CubeData?> _cubesData;
    
    #endregion

    public Material material;
    public float scale = (float) 1/16;

    private List<float> _vertices = new List<float>();
        
    public Mesh(GL gl, GeometryFile geometryFile, Material material)
    {
        _vbos = new List<BufferObject<float>>();
        _vaos = new List<VertexArrayObject<float, uint>>();
        _bonesData = new List<BoneData?>();
        _cubesData = new List<CubeData?>();

        this.material = material;
        
        _gl = gl;

        foreach (var geometry in geometryFile.Geometry)
        {
            foreach (var bone in geometry.Bones)
            {
                BoneData boneData = new BoneData()
                {
                    name = bone.Name,
                    pivot = new Vector3(bone.Pivot[0], bone.Pivot[1], bone.Pivot[2]),
                    rotation = (bone.Rotation != null)
                        ? new Vector3(bone.Rotation[0], bone.Rotation[1], bone.Rotation[2])
                        : Vector3.Zero,
                    parent = bone.Parent
                };
                
                _bonesData.Add(boneData);
                
                if (bone.Cubes == null) continue;
                
                foreach (var cube in bone.Cubes)
                {
                    CubeData cubeData = new CubeData()
                    {
                        boneParent = bone.Name
                    };
                    _cubesData.Add(cubeData);
                    AddCubeVertices(cube, cubeData);
                }
            }
        }

        InitializeBuffers();
    }

    private void AddCubeVertices(Cube cube, CubeData cubeData)
    {
        Vector3 pivot = Vector3.Zero;
        Vector3 rotation = Vector3.Zero;
        Vector3 origin = -Vector3.One;
        origin = new Vector3(cube.Origin[0], cube.Origin[1], cube.Origin[2]);
        Vector3 originUV = origin;
        origin -= Vector3.One * (cube.Inflate / 2);
        Vector3 size = Vector3.One;
        size = new Vector3(cube.Size[0], cube.Size[1], cube.Size[2]);
        Vector3 sizeUV = size; 
        size += Vector3.One * (cube.Inflate / 2);
        Vector2 uv = new Vector2(cube.Uv[0], cube.Uv[1]);
        bool mirror = cube.Mirror;

        if (cube.Pivot != null)
        {
            pivot = new Vector3(cube.Pivot[0], cube.Pivot[1], cube.Pivot[2]);
        }
        
        if (cube.Rotation != null)
        {
            rotation = new Vector3(cube.Rotation[0], cube.Rotation[1], cube.Rotation[2]);
        }
        
        float X_0;
        float X_1;
        float Y_0;
        float Y_1;
        
        // Front Face
        
        X_0 = (uv.X + sizeUV.Z + sizeUV.X) / (float) material.texture.width;
        X_1 = (uv.X + sizeUV.Z) / (float) material.texture.width;
        Y_0 = (uv.Y + sizeUV.Z) / (float) material.texture.height;
        Y_1 = (uv.Y + sizeUV.Z + sizeUV.Y) / (float) material.texture.height;
        
        if (mirror)
        {
            cubeData.faces.Add(new List<float>
                {
                    // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                    origin.X, origin.Y, origin.Z, X_0, Y_1,
                    origin.X + size.X, origin.Y, origin.Z, X_1, Y_1,
                    origin.X + size.X, origin.Y + size.Y, origin.Z, X_1, Y_0,
                    origin.X + size.X, origin.Y + size.Y, origin.Z, X_1, Y_0,
                    origin.X, origin.Y + size.Y, origin.Z, X_0, Y_0,
                    origin.X, origin.Y, origin.Z, X_0, Y_1,
                }
            );
        }
        else
        {
            cubeData.faces.Add(new List<float>
                {
                    // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                    origin.X, origin.Y, origin.Z, X_1, Y_1,
                    origin.X + size.X, origin.Y, origin.Z, X_0, Y_1,
                    origin.X + size.X, origin.Y + size.Y, origin.Z, X_0, Y_0,
                    origin.X + size.X, origin.Y + size.Y, origin.Z, X_0, Y_0,
                    origin.X, origin.Y + size.Y, origin.Z, X_1, Y_0,
                    origin.X, origin.Y, origin.Z, X_1, Y_1,
                }
            );
        }
        
        X_0 = (uv.X + sizeUV.Z*2 + sizeUV.X) / (float) material.texture.width;
        X_1 = (uv.X + sizeUV.Z*2 + sizeUV.X*2) / (float) material.texture.width;
        Y_0 = (uv.Y + sizeUV.Z) / (float) material.texture.height;
        Y_1 = (uv.Y + sizeUV.Z + sizeUV.Y) / (float) material.texture.height;
        
        // Back Face

        if (mirror)
        {
            cubeData.faces.Add(new List<float>
            {
                // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                origin.X, origin.Y, origin.Z + size.Z, X_0, Y_1,
                origin.X + size.X, origin.Y, origin.Z + size.Z, X_1, Y_1,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X, origin.Y, origin.Z + size.Z, X_0, Y_1,
            });
        }
        else
        {
            cubeData.faces.Add(new List<float>
            {
                // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                origin.X, origin.Y, origin.Z + size.Z, X_1, Y_1,
                origin.X + size.X, origin.Y, origin.Z + size.Z, X_0, Y_1,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X, origin.Y + size.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X, origin.Y, origin.Z + size.Z, X_1, Y_1,
            });
        }

        // Left Face

        if (mirror)
        {
            X_1 = (uv.X + sizeUV.Z + sizeUV.X) / (float) material.texture.width;
            X_0 = (uv.X + sizeUV.X*2 + sizeUV.Z) / (float) material.texture.width;
            Y_1 = (uv.Y + sizeUV.Z) / (float) material.texture.height;
            Y_0 = (uv.Y + sizeUV.Z + sizeUV.Y) / (float) material.texture.height;
        }
        else
        {
            X_0 = (uv.X) / (float) material.texture.width;
            X_1 = (uv.X + sizeUV.X) / (float) material.texture.width;
            Y_1 = (uv.Y + sizeUV.Z) / (float) material.texture.height;
            Y_0 = (uv.Y + sizeUV.Z + sizeUV.Y) / (float) material.texture.height;
        }
        
        cubeData.faces.Add(new List<float>
        {
            // Sommet pour une face d'un cube (6 sommets pour deux triangles)
            origin.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_1,
            origin.X, origin.Y + size.Y, origin.Z, X_1, Y_1,
            origin.X, origin.Y, origin.Z, X_1, Y_0,
            origin.X, origin.Y, origin.Z, X_1, Y_0,
            origin.X, origin.Y, origin.Z + size.Z, X_0, Y_0,
            origin.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_1,
        });
        
        // Right Face

        if (mirror)
        {
            X_0 = (uv.X) / (float) material.texture.width;
            X_1 = (uv.X + sizeUV.X) / (float) material.texture.width;
            Y_1 = (uv.Y + sizeUV.Z) / (float) material.texture.height;
            Y_0 = (uv.Y + sizeUV.Z + sizeUV.Y) / (float) material.texture.height;
        }
        else
        {
            X_1 = (uv.X + sizeUV.Z + sizeUV.X) / (float) material.texture.width;
            X_0 = (uv.X + sizeUV.X*2 + sizeUV.Z) / (float) material.texture.width;
            Y_1 = (uv.Y + sizeUV.Z) / (float) material.texture.height;
            Y_0 = (uv.Y + sizeUV.Z + sizeUV.Y) / (float) material.texture.height;
        }

        cubeData.faces.Add(new List<float>
        {
            // Sommet pour une face d'un cube (6 sommets pour deux triangles)
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_1,
            origin.X + size.X, origin.Y + size.Y, origin.Z, X_1, Y_1,
            origin.X + size.X, origin.Y, origin.Z, X_1, Y_0,
            origin.X + size.X, origin.Y, origin.Z, X_1, Y_0,
            origin.X + size.X, origin.Y, origin.Z + size.Z, X_0, Y_0,
            origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_1,
        });
        
        // Bottom Face
        
        X_0 = (uv.X + sizeUV.Z + sizeUV.X) / (float) material.texture.width;
        X_1 = (uv.X + sizeUV.X*2 + sizeUV.Z) / (float) material.texture.width;
        Y_0 = (uv.Y) / (float) material.texture.height;
        Y_1 = (uv.Y + sizeUV.Z) / (float) material.texture.height;

        if (mirror)
        {
            cubeData.faces.Add(new List<float>
            {
                // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                origin.X, origin.Y, origin.Z, X_1, Y_1,
                origin.X + size.X, origin.Y, origin.Z, X_0, Y_1,
                origin.X + size.X, origin.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X + size.X, origin.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X, origin.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X, origin.Y, origin.Z, X_1, Y_1,
            });
        }
        else
        {
            cubeData.faces.Add(new List<float>
            {
                // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                origin.X, origin.Y, origin.Z, X_0, Y_1,
                origin.X + size.X, origin.Y, origin.Z, X_1, Y_1,
                origin.X + size.X, origin.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X + size.X, origin.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X, origin.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X, origin.Y, origin.Z, X_0, Y_1,
            });
        }
        
        
        // Top Face
        
        X_0 = (uv.X + sizeUV.X + sizeUV.Z) / (float) material.texture.width;
        X_1 = (uv.X + sizeUV.Z) / (float) material.texture.width;
        Y_0 = (uv.Y) / (float) material.texture.height;
        Y_1 = (uv.Y + sizeUV.Z) / (float) material.texture.height;

        if (mirror)
        {
            cubeData.faces.Add(new List<float>
            {
                // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                origin.X, origin.Y + size.Y, origin.Z, X_0, Y_1,
                origin.X + size.X, origin.Y + size.Y, origin.Z, X_1, Y_1,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X, origin.Y + size.Y, origin.Z, X_0, Y_1,
            });
        }
        else
        {
            cubeData.faces.Add(new List<float>
            {
                // Sommet pour une face d'un cube (6 sommets pour deux triangles)
                origin.X, origin.Y + size.Y, origin.Z, X_1, Y_1,
                origin.X + size.X, origin.Y + size.Y, origin.Z, X_0, Y_1,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X + size.X, origin.Y + size.Y, origin.Z + size.Z, X_0, Y_0,
                origin.X, origin.Y + size.Y, origin.Z + size.Z, X_1, Y_0,
                origin.X, origin.Y + size.Y, origin.Z, X_1, Y_1,
            });
        }
        
        // origin = new Vector3(cube.Origin[0], cube.Origin[1], cube.Origin[2]);
        // origin -= Vector3.One * (cube.Inflate / 2);
        //
        // size = new Vector3(cube.Size[0], cube.Size[1], cube.Size[2]);
        // size += Vector3.One *  (cube.Inflate / 2);

        Vector3 localPivot = (origin - pivot);
        
        Matrix4x4 R_x = Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(-rotation.X));
        Matrix4x4 R_y = Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(-rotation.Y));
        Matrix4x4 R_z = Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(-rotation.Z));
                
        // calculate rotation matrix
        Matrix4x4 rotationMatrix = R_z * R_y * R_x;
                
        Matrix4x4 T_to_origin = Matrix4x4.CreateTranslation(-origin);
        Matrix4x4 T_back_from_origin = Matrix4x4.CreateTranslation(origin);
        Matrix4x4 T_to_pivot = Matrix4x4.CreateTranslation(-localPivot);
        Matrix4x4 T_back_from_pivot = Matrix4x4.CreateTranslation(localPivot);

        BoneData? parentBone = _bonesData.Find((data => data.name == cubeData.boneParent));        
        
        Matrix4x4 parentRecursiveTransform = parentBone == null ? Matrix4x4.Identity : ApplyTransformBoneRecursive(parentBone);
        
        // Transform each vertice by its rotation and pivot data
        foreach (var face in cubeData.faces)
        {
            for (int i = 0; i < face.Count; i += 5)
            {
                Vector3 transformedVertex;
                Vector3 vertex = new Vector3(face[i], face[i + 1], face[i + 2]);

                Matrix4x4 transformationMatrix = T_to_origin * T_back_from_pivot * rotationMatrix * T_to_pivot * T_back_from_origin * parentRecursiveTransform;

                // Console.WriteLine(T_to_origin);
                
                transformedVertex = Vector3.Transform(vertex, transformationMatrix);

                face[i] = transformedVertex.X;
                face[i+1] = transformedVertex.Y;
                face[i+2] = transformedVertex.Z;
            }
        }

        cubeData.rotation = rotation;
        cubeData.pivot = pivot;
        cubeData.origin = origin;
    }

    private void InitializeBuffers()
    {
        foreach (var bone in _bonesData)
        {
            foreach (var cube in _cubesData)
            {
                foreach (var face in cube.faces)
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
    }

    private Matrix4x4 ApplyTransformRecursive(CubeData cube)
    {
        BoneData? parentBone = _bonesData.Find((data => data.name == cube.boneParent));

        Matrix4x4 boneTransform = Matrix4x4.CreateTranslation(-cube.pivot)
                                  * Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(cube.rotation.Z))
                                  * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(cube.rotation.Y))
                                  * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(-cube.rotation.X))
                                  * Matrix4x4.CreateTranslation(cube.pivot);

        if (parentBone != null)
        {
            return boneTransform * ApplyTransformBoneRecursive(parentBone);
        }
        else
        {
            return boneTransform;
        }
    }

    private Matrix4x4 ApplyTransformBoneRecursive(BoneData? bone)
    {
        BoneData? parentBone = _bonesData.Find((data => data.name == bone.parent));

        Matrix4x4 rotationMatrix = Matrix4x4.CreateRotationZ(MathHelper.DegreesToRadians(-bone.rotation.Z))
                                   * Matrix4x4.CreateRotationY(MathHelper.DegreesToRadians(bone.rotation.Y))
                                   * Matrix4x4.CreateRotationX(MathHelper.DegreesToRadians(-bone.rotation.X));

        Matrix4x4 boneTransform = Matrix4x4.CreateTranslation(-bone.pivot)
                                  * rotationMatrix
                                  * Matrix4x4.CreateTranslation(bone.pivot);

        if (parentBone != null)
        {
            return boneTransform * ApplyTransformBoneRecursive(parentBone);
        }
        else
        {
            return boneTransform;
        }
    }
    
    public void SortObjectsByDistance(Vector3 cameraPosition)
    {
        // Calculer la distance de chaque objet à la caméra
        foreach (var boneData in _bonesData)
        {
            foreach (var cubeData in _cubesData)
            {
                float distance = Vector3.Distance(cubeData.origin, cameraPosition);
                cubeData.DistanceToCamera = distance;
            }
            
            // Trier les cubes de ce bone en fonction de leur distance à la caméra
            _cubesData.Sort((a, b) => a.DistanceToCamera.CompareTo(b.DistanceToCamera));
        }

        // Après avoir trié les cubes de chaque bone, nous pouvons trier les bones eux-mêmes
        _cubesData.Sort((a, b) =>
        {
            // Nous comparons les distances de leurs premiers cubes pour trier les bones
            float aDistance = a.DistanceToCamera;
            float bDistance = b.DistanceToCamera;
            return aDistance.CompareTo(bDistance);
        });
    }

    public void Render(Matrix4x4 model, Camera camera)
    {
        Matrix4x4 newModel;
        
        SortObjectsByDistance(camera.Position);

        int i = 0;
        // Sweeping every Cube
        int faceIndex = 0;
            
        foreach(var cube in _cubesData)
        {
            // Rendering for each face
            foreach (var face in cube.faces)
            {
                _vaos[i].Bind();
                material.texture.Bind(0);
                
                newModel = ApplyTransformRecursive(cube) * model * Matrix4x4.CreateScale(scale);
                
                material.shader.SetUniform("uModel", model * Matrix4x4.CreateScale(scale));
                
                _gl.DrawArrays(PrimitiveType.Triangles, 0, (uint) face.Count / 5);
                
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
