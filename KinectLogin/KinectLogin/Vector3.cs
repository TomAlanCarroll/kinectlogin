using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;

namespace KinectLogin
{
    [Serializable()]
    public class Vector3
    {
        public float x;
        public float y;
        public float z;

        public static Vector3 Up = new Vector3(0.0f, 1.0f, 0.0f);
        public static Vector3 Down = new Vector3(0.0f, -1.0f, 0.0f);
        public static Vector3 Right = new Vector3(1.0f, 0.0f, 0.0f);
        public static Vector3 Left = new Vector3(-1.0f, 0.0f, 0.0f);
        public static Vector3 Forward = new Vector3(0.0f, 0.0f, 1.0f);
        public static Vector3 Back = new Vector3(0.0f, 0.0f, -1.0f);

        public Vector3(Joint j)
        {
            this.x = j.Position.X;
            this.y = j.Position.Y;
            this.z = j.Position.Z;
        }

        public Vector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static Vector3 operator +(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x + v2.x, v1.y + v2.y, v1.z + v2.z);
        }

        public static Vector3 operator -(Vector3 v1, Vector3 v2)
        {
            return new Vector3(v1.x - v2.x, v1.y - v2.y, v1.z - v2.z);
        }

        public static Vector3 operator /(Vector3 v1, float d)
        {
            return new Vector3(v1.x / d, v1.y / d, v1.z / d);
        }

        public static float Dot(Vector3 v1, Vector3 v2)
        {
            return v1.x * v2.x + v1.y * v2.y + v1.z * v2.z;
        }

        public static float Magnitude(Vector3 v)
        {
            return (float)Math.Sqrt(v.x * v.x + v.y * v.y + v.z * v.z);
        }

        public static Vector3 Normalize(Vector3 v)
        {
            float magnitude = Magnitude(v);
            return new Vector3(v.x / magnitude, v.y / magnitude, v.z / magnitude);
        }

        public static float Distance(Vector3 value1, Vector3 value2)
        {
            return (float)Math.Sqrt((value1.x - value2.x) * (value1.x - value2.x) +
                     (value1.y - value2.y) * (value1.y - value2.y) +
                     (value1.z - value2.z) * (value1.z - value2.z));
        }

        public static void Distance(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            result = (float)Math.Sqrt((value1.x - value2.x) * (value1.x - value2.x) +
                     (value1.y - value2.y) * (value1.y - value2.y) +
                     (value1.z - value2.z) * (value1.z - value2.z));
        }

        public static float DistanceSquared(Vector3 value1, Vector3 value2)
        {
            return (value1.x - value2.x) * (value1.x - value2.x) +
                     (value1.y - value2.y) * (value1.y - value2.y) +
                     (value1.z - value2.z) * (value1.z - value2.z); ;
        }

        public static void DistanceSquared(ref Vector3 value1, ref Vector3 value2, out float result)
        {
            result = (value1.x - value2.x) * (value1.x - value2.x) +
                     (value1.y - value2.y) * (value1.y - value2.y) +
                     (value1.z - value2.z) * (value1.z - value2.z);
        }
    }
}
