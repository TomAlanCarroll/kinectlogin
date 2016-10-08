namespace DTWGestureRecognition
{
    using System.Windows;
    using Microsoft.Xna.Framework;
    using System.Collections.Generic;
    using Microsoft.Kinect;

    /// <summary>
    /// Takes Kinect SDK Skeletal Frame coordinates and converts them into a format useful to th DTW
    /// </summary>
    internal class Skeleton3DDataCoordEventArgs
    {
        
        /// <summary>
        /// Map of joints and their positions
        /// </summary>
        private readonly Dictionary<JointType, Vector3> _skeletonSnapshot;


        /// <summary>
        /// Construct the event
        /// </summary>
        /// <returns>A map of joints versus their positions at a particular frame</returns>
        public Skeleton3DDataCoordEventArgs(Dictionary<JointType, Vector3> skeletonSnapshot) 
        {
            _skeletonSnapshot = skeletonSnapshot;
        }
          

        /// <summary>
        /// Gets the snapshot of the skeleton
        /// </summary>
        /// <returns>A map of joints versus their positions at a particular frame</returns>
        public Dictionary<JointType, Vector3> GetSkeletonSnapshot() 
        {
            return _skeletonSnapshot;
        }
    }
}