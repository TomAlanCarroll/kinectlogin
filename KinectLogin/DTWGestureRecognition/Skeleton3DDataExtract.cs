namespace DTWGestureRecognition
{
    using System;
    using System.Windows;
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using Microsoft.Xna.Framework;


    /// <summary>
    /// This class is used to transform the data of the skeleton
    /// </summary>
    internal class Skeleton3DDataExtract
    {
        /// <summary>
        /// Skeleton3DDataCoordEventHandler delegate
        /// </summary>
        /// <param name="sender">The sender object</param>
        /// <param name="a">Skeleton 3Ddata Coord Event Args</param>
        public delegate void Skeleton3DDataCoordEventHandler(object sender, Skeleton3DDataCoordEventArgs a);

        /// <summary>
        /// The Skeleton 3Ddata Coord Ready event
        /// </summary>
        public static event Skeleton3DDataCoordEventHandler Skeleton3DDataCoordReady;

        /// <summary>
        /// Crunches Kinect SDK's Skeleton Data and spits out a format more useful for DTW
        /// </summary>
        /// <param name="data">Kinect SDK's Skeleton Data</param>
        public static void ProcessData(Skeleton data, int jointsTracked)
        {
            // Extract the coordinates of the points.
            var p = new Vector3[jointsTracked];
            Vector3 shoulderRight = new Vector3(), shoulderLeft = new Vector3();
            foreach (Joint j in data.Joints)
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
            var center = new Vector3( (shoulderLeft.X + shoulderRight.X) / 2, (shoulderLeft.Y + shoulderRight.Y) / 2, 
                                      (shoulderLeft.Z + shoulderRight.Z) / 2);
            
            for (int i = 0; i < jointsTracked -2; i++)
            {
                p[i].X -= center.X;
                p[i].Y -= center.Y;
                p[i].Z -= center.Z;
            }

            // Normalization of the coordinates
            double shoulderDist =
                Math.Sqrt(Math.Pow((shoulderLeft.X - shoulderRight.X), 2) +
                          Math.Pow((shoulderLeft.Y - shoulderRight.Y), 2) +
                          Math.Pow((shoulderLeft.Z - shoulderRight.Z), 2));
            //for (int i = 0; i < jointsTracked; i++)
            for (int i = 0; i < jointsTracked - 2; i++)
            {
                p[i] = Vector3.Divide(p[i], (float)shoulderDist);                
            }

            // Now put everything into the dictionary, and send it to the event. 
            Dictionary<JointType, Vector3> _skeletonSnapshot = new Dictionary<JointType, Vector3> 
            {
                {JointType.HandLeft, p[0]},
                {JointType.WristLeft, p[1]},
                {JointType.ElbowLeft, p[2]},
                {JointType.HandRight, p[3]},
                {JointType.WristRight, p[4]},
                {JointType.ElbowRight, p[5]},
                {JointType.ShoulderLeft, p[6]},
                {JointType.ShoulderRight, p[7]},
            };

            // Launch the event!
            Skeleton3DDataCoordReady(null, new Skeleton3DDataCoordEventArgs(_skeletonSnapshot));
        }
    }
}
