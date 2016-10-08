using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Kinect;
using DTWGestureRecognition;

namespace KinectLogin
{
    [Serializable()]
    public class Gesture
    {
        /// <summary>
        /// Enum Comparer for speeding up use of enums as keys in dictionary.
        /// </summary>
        //private static EnumComparer<JointType> comparer = EnumComparer<JointType>.Instance;

        private Dictionary<JointType, List<Vector3>> gestureData = new Dictionary<JointType, List<Vector3>>()
        {
                {JointType.HandLeft, new List<Vector3>()},
                {JointType.WristLeft, new List<Vector3>()},
                {JointType.ElbowLeft, new List<Vector3>()},
                {JointType.HandRight, new List<Vector3>()},
                {JointType.WristRight, new List<Vector3>()},
                {JointType.ElbowRight, new List<Vector3>()},
                {JointType.ShoulderLeft, new List<Vector3>()},
                {JointType.ShoulderRight, new List<Vector3>()}
              
        };
        
        public void addGestureData(Skeleton skeleton)
        {
            int jointsTracked = 8;
            // Extract the coordinates of the points.
            Vector3[] p = new Vector3[jointsTracked];

            Vector3 shoulderRight = new Vector3(0, 0, 0), shoulderLeft = new Vector3(0, 0, 0);

            foreach (Joint j in skeleton.Joints)
            {
                switch (j.JointType)
                {
                    case JointType.HandLeft:
                        p[0] = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristLeft:
                        p[1] = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowLeft:
                        p[2] = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.HandRight:
                        p[3] = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.WristRight:
                        p[4] = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ElbowRight:
                        p[5] = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        break;
                    case JointType.ShoulderLeft:
                        shoulderLeft = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        p[6] = shoulderLeft;
                        break;
                    case JointType.ShoulderRight:
                        shoulderRight = new Vector3(j.Position.X, j.Position.Y, j.Position.Z);
                        p[7] = shoulderRight;
                        break;
                }
            }

            // Centre the data
            Vector3 center = new Vector3((shoulderLeft.x + shoulderRight.x) / 2, (shoulderLeft.y + shoulderRight.y) / 2,
                                      (shoulderLeft.z + shoulderRight.z) / 2);

            for (int i = 0; i < jointsTracked - 2; i++)
            {
                p[i].x -= center.x;
                p[i].y -= center.y;
                p[i].z -= center.z;
            }

            // Normalization of the coordinates
            double shoulderDist =
                Math.Sqrt(Math.Pow((shoulderLeft.x - shoulderRight.x), 2) +
                          Math.Pow((shoulderLeft.y - shoulderRight.y), 2) +
                          Math.Pow((shoulderLeft.z - shoulderRight.z), 2));
            //for (int i = 0; i < jointsTracked; i++)
            for (int i = 0; i < jointsTracked - 2; i++)
            {
                p[i] = p[i] / (float)shoulderDist;
            }

            // Now put everything into the gestureData. 
            gestureData[JointType.HandLeft].Add(p[0]);
            gestureData[JointType.WristLeft].Add(p[1]);
            gestureData[JointType.ElbowLeft].Add(p[2]);
            gestureData[JointType.HandRight].Add(p[3]);
            gestureData[JointType.WristRight].Add(p[4]);
            gestureData[JointType.ElbowRight].Add(p[5]);
            gestureData[JointType.ShoulderLeft].Add(p[6]);
            gestureData[JointType.ShoulderRight].Add(p[7]);
        }

        public Dictionary<JointType, List<Vector3>> getGestureData()
        {
            return ExtensionMethods.DeepClone(this.gestureData);
        }

        private class Joints
        {
            public Vector3 HIP_CENTER,
            SPINE,
            SHOULDER_CENTER,
            HEAD,
            SHOULDER_LEFT,
            ELBOW_LEFT,
            WRIST_LEFT,
            HAND_LEFT,
            SHOULDER_RIGHT,
            ELBOW_RIGHT,
            WRIST_RIGHT,
            HAND_RIGHT,
            HIP_LEFT,
            KNEE_LEFT,
            ANKLE_LEFT,
            FOOT_LEFT,
            HIP_RIGHT,
            KNEE_RIGHT,
            ANKLE_RIGHT,
            FOOT_RIGHT;

            public void addJoints(Joint j, int type)
            {
                switch (type)
                {
                    case 0:
                        this.HIP_CENTER = new Vector3(j);
                        break;
                    case 1:
                        this.SPINE = new Vector3(j);
                        break;
                    case 2:
                        this.SHOULDER_CENTER = new Vector3(j);
                        break;
                    case 3:
                        this.HEAD = new Vector3(j);
                        break;
                    case 4:
                        this.SHOULDER_LEFT = new Vector3(j);
                        break;
                    case 5:
                        this.ELBOW_LEFT = new Vector3(j);
                        break;
                    case 6:
                        this.WRIST_LEFT = new Vector3(j);
                        break;
                    case 7:
                        this.HAND_LEFT = new Vector3(j);
                        break;
                    case 8:
                        this.SHOULDER_RIGHT = new Vector3(j);
                        break;
                    case 9:
                        this.ELBOW_RIGHT = new Vector3(j);
                        break;
                    case 10:
                        this.WRIST_RIGHT = new Vector3(j);
                        break;
                    case 11:
                        this.HAND_RIGHT = new Vector3(j);
                        break;
                    case 12:
                        this.HIP_LEFT = new Vector3(j);
                        break;
                    case 13:
                        this.KNEE_LEFT = new Vector3(j);
                        break;
                    case 14:
                        this.ANKLE_LEFT = new Vector3(j);
                        break;
                    case 15:
                        this.FOOT_LEFT = new Vector3(j);
                        break;
                    case 16:
                        this.HIP_RIGHT = new Vector3(j);
                        break;
                    case 17:
                        this.KNEE_RIGHT = new Vector3(j);
                        break;
                    case 18:
                        this.ANKLE_RIGHT = new Vector3(j);
                        break;
                    case 19:
                        this.FOOT_RIGHT = new Vector3(j);
                        break;
                    default:
                        break;
                }
            }
        }
    }
}
