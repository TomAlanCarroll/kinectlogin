using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace KinectLogin
{
    public class IdentificationAlgorithms
    {
        public enum FaceComparisonType
        {
            Naive_Bayes_Point_Locations,
            Random_Trees_Point_Locations,
            Support_Vector_Machine_Locations,
            Naive_Bayes_Point_Distances,
            // My first attempt at facial recognition; Does not disambiguate well between different faces
            Threshold_All_Distances_Between_All_Feature_Points,
			HyperNEAT
        }
    }
}
