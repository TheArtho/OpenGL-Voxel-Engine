using System.Diagnostics;
using System.Numerics;
using Silk.NET.Input;
using Silk.NET.Vulkan;
using SixLabors.ImageSharp.Processing;

namespace Minecraft;

public class Camera
{
    public Vector3 Position;
    public Vector3 Front { get; protected set; }
    public Vector3 Up { get; protected set; }
    public Vector3 Direction { get; protected set; }
    
    //Used to track change in mouse movement to allow for moving of the Camera
    private static Vector2 LastMousePosition;
    public float CameraYaw = -90f;
    public float CameraPitch = 0f;

    public float FieldOfView
    {
        get => _fov;
        set => _fov = float.Clamp(value, 1, 100);
    }
    
    protected float yaw;
    protected float pitch;
    protected float _fov;

    protected Camera()
    {
        // transform.LookAt(Vector3.Zero);
        Position = Vector3.Zero;
        _fov = 45;
    }
    
    public virtual Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, Position + Front, World.Up);
    }

    /// <summary>
    /// Rotates the camera
    /// </summary>
    /// <param name="yaw">Yaw angle in degrees</param>
    /// <param name="pitch">Pitch angle in degrees</param>
    public virtual void Rotate(float yaw, float pitch) // angle in degrees
    {
        
    }
    
    public virtual void SetFov(float value)
    {
        
    }

    public virtual void OnMouseMove(IMouse mouse, Vector2 position)
    {
        var lookSensitivity = 0.1f;
        if (LastMousePosition == default)
        {
            LastMousePosition = position;
        }
        else
        {
            var xOffset = (position.X - LastMousePosition.X) * lookSensitivity;
            var yOffset = (position.Y - LastMousePosition.Y) * lookSensitivity;
            LastMousePosition = position;

            CameraYaw += xOffset;
            CameraPitch -= yOffset;

            //We don't want to be able to look behind us by going over our head or under our feet so make sure it stays within these bounds
            CameraPitch = Math.Clamp(CameraPitch, -89.0f, 89.0f);
                
            Rotate(CameraYaw, CameraPitch);
        }
    }
}


public class OrbitCamera : Camera
{
    public float Radius => radius;

    private float radius;
    private Vector3 targetPosition = Vector3.Zero;
    
    public OrbitCamera() : base()
    {
        radius = 10f;
    }

    public void SetLookAt(Vector3 target)
    {
        targetPosition = target;
    }
    
    public override Matrix4x4 GetViewMatrix()
    {
        return Matrix4x4.CreateLookAt(Position, targetPosition, World.Up);
    }

    public override void Rotate(float yaw, float pitch) // angle in degrees
    {
        float yawRadians = float.DegreesToRadians(yaw); 
        float pitchRadians = float.DegreesToRadians(pitch); 
        
        pitchRadians = float.Clamp(pitchRadians, 
            -MathF.PI / 2f + 0.1f, 
            MathF.PI / 2f - 0.1f);

        Position = new Vector3(
            targetPosition.X + radius * MathF.Cos(pitchRadians) * MathF.Sin(yawRadians),
            targetPosition.Y + radius * MathF.Sin(pitchRadians),
            targetPosition.Z + radius * MathF.Cos(pitchRadians) * MathF.Cos(yawRadians)
        );
    }

    public void SetRadius(float radius)
    {
        this.radius = radius;
    }
}

public class FPSCamera : Camera
{
    public FPSCamera() : base()
    {
        Front = new Vector3(0, 0, -1);
    }
    
    public override void Rotate(float yaw, float pitch)  // angle in degrees
    {
        Direction = new Vector3(
            MathF.Cos(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch)),
            MathF.Sin(MathHelper.DegreesToRadians(pitch)),
            MathF.Sin(MathHelper.DegreesToRadians(yaw)) * MathF.Cos(MathHelper.DegreesToRadians(pitch))
            );

        Front = Vector3.Normalize(Direction);
    }

    /// <summary>
    /// Increase/decrease the field of view 
    /// </summary>
    /// <param name="value">FOV value between 1 and 45</param>
    public override void SetFov(float value)
    {
        _fov = Math.Clamp(_fov - value, 1.0f, 45f);
    }
}