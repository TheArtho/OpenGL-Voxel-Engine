using System.Numerics;

namespace Minecraft
{
    public class Transform
    {
        public Transform Parent;
        protected Vector3 _position = Vector3.Zero;
        public Vector3 Position 
        { 
            get => _position;
            set
            {
                if (VectorHelper.IsValidVector(value))
                {
                    _position = value;
                }
                else
                {
                    Console.WriteLine("Invalid position vector assigned.");
                }
            }
        }
        public float Scale { get; set; } = 1f;
        public Quaternion Rotation = Quaternion.Identity;

        public Vector3 EulerRotation => Rotation.ToEulerAngles();

        public Vector3 Front => Vector3.Transform(Vector3.UnitZ, Rotation);
        public Vector3 Up => Vector3.Transform(Vector3.UnitY, Rotation);

        //Note: The order here does matter.
        public Matrix4x4 ViewMatrix => Matrix4x4.Identity * Matrix4x4.CreateFromQuaternion(Rotation) * Matrix4x4.CreateScale(Scale) * Matrix4x4.CreateTranslation(Position);
        
        public static Vector3 ToEulerAngles(Quaternion q)
        {
            Vector3 angles = new();

            // roll / x
            double sinr_cosp = 2 * (q.W * q.X + q.Y * q.Z);
            double cosr_cosp = 1 - 2 * (q.X * q.X + q.Y * q.Y);
            angles.X = (float)Math.Atan2(sinr_cosp, cosr_cosp);

            // pitch / y
            double sinp = 2 * (q.W * q.Y - q.Z * q.X);
            if (Math.Abs(sinp) >= 1)
            {
                angles.Y = (float)Math.CopySign(Math.PI / 2, sinp);
            }
            else
            {
                angles.Y = (float)Math.Asin(sinp);
            }

            // yaw / z
            double siny_cosp = 2 * (q.W * q.Z + q.X * q.Y);
            double cosy_cosp = 1 - 2 * (q.Y * q.Y + q.Z * q.Z);
            angles.Z = (float)Math.Atan2(siny_cosp, cosy_cosp);

            return angles;
        }

        public void LookAt(Vector3 target)
        {
            Vector3 direction = target - Position;
            
            if (direction == Vector3.Zero)
                return;
            
            direction = Vector3.Normalize(direction);
    
            // Cross product of the Z axis and the direction
            Vector3 axis = Vector3.Cross(Vector3.UnitZ, direction);
            
            // If axis is null then direction is colinear, so no rotation needed
            if (axis == Vector3.Zero)
            {
                // Check rotation inversion
                if (Vector3.Dot(Vector3.UnitZ, direction) < 0)
                {
                    axis = Vector3.UnitX;
                    Rotation = Quaternion.CreateFromAxisAngle(axis, MathF.PI);
                }
                else
                {
                    // Aligned so no rotation needed
                    Rotation = Quaternion.Identity;
                }
            }
            else
            {
                axis = Vector3.Normalize(axis);
        
                // Rotation angle
                float angle = MathF.Acos(Vector3.Dot(Vector3.UnitZ, direction));
                Quaternion newRotation = Quaternion.CreateFromAxisAngle(axis, angle);
                
                Rotation = newRotation;
            }
            // Console.WriteLine(Rotation);
        }
    }
    
    public static class TransformExtension 
    {
        public static Vector3 ToEulerAngles(this Quaternion q)
        {
            return Transform.ToEulerAngles(q);
        }
    }
}
