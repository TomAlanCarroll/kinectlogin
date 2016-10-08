using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.FaceTracking;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.ML;
using Emgu.CV.ML.Structure;
using System.IO;
using Emgu.CV;

namespace KinectLogin
{
    public class Face
    {
        public const string faceFileDirectory3D = @"C:\KinectLogin\3D";
        public const string faceFileDirectory2D = @"C:\KinectLogin\2D";
        public const string faceTrainingFileDirectory2D = @"C:\KinectLogin\2D\train";
        public const string faceTestingFileDirectory2D = @"C:\KinectLogin\2D\test";

        _2dFaceIdentification identifier2d;

        _3dFaceIdentification identifier3d;
        
        public Face(Image<Bgr, Byte> faceImage, EnumIndexableCollection<FeaturePoint, Vector3DF> faceModel)
        {
            this.identifier2d = new _2dFaceIdentification(faceImage);
            this.identifier3d = new _3dFaceIdentification(faceModel);

            // Save the faceModel and faceImage to a file
            Directory.CreateDirectory(faceFileDirectory2D);
            Directory.CreateDirectory(faceTrainingFileDirectory2D);
            Directory.CreateDirectory(faceTestingFileDirectory2D);
            Directory.CreateDirectory(faceFileDirectory3D);
            string timestamp = System.DateTime.Now.ToString("MMddyy HHmmss");

            // Save the faceModel
            string filename = faceFileDirectory3D + @"\" + KinectHelper.getName() + timestamp + ".txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
                {
                    file.WriteLine(i + ":" + faceModel[i].X + "," + faceModel[i].Y + "," + faceModel[i].Z);
                }

                // Add the face model class (i.e. the name of the user)
                KinectManager.setFaceModelName(KinectHelper.getName());

                // Set the filename for the model
                KinectManager.setFaceModelFileName(filename);

                // Add the face image class (i.e. the name of the user)
                KinectManager.setFaceImageName(KinectHelper.getName());
            }

            // Convert to grayscale
            Image<Gray, Byte> grayscaleFaceImage = faceImage.Convert<Gray, Byte>();

            // Save the faceImage
            //grayscaleFaceImage.Save(faceTrainingFileDirectory2D + @"\" + KinectHelper.getName() + "_" + timestamp + ".pgm");

            // Save the image after equalization
            //grayscaleFaceImage._EqualizeHist();
            //grayscaleFaceImage.Save(faceTrainingFileDirectory2D + @"\" + KinectHelper.getName() + "_" + timestamp + "_equalized.pgm");

            // Save the image after pca
            //ExtensionMethods.CalculatePCAofImage(grayscaleFaceImage, 2).Save(faceTrainingFileDirectory2D + @"\" + KinectHelper.getName() + "_" + timestamp + "_pca.pgm");
        }

        /// <summary>
        /// Compares this.faceModel with faceModelCandidate
        /// </summary>
        /// <param name="faceModelCandidate">The feature map with 3D vectors</param>
        /// <returns>true if the models are likely similar; false otherwise</returns>
        public bool compareModel(EnumIndexableCollection<FeaturePoint, Vector3DF> faceModelCandidate, IdentificationAlgorithms.FaceComparisonType comparisonAlgorithm)
        {
            return identifier3d.isValid(faceModelCandidate, comparisonAlgorithm);
        }


        /// <summary>
        /// Compares this.faceImage with faceImageCandidate
        /// </summary>
        /// <param name="faceImageCandidate">The image of the face</param>
        /// <returns>true if the face images are likely similar; false otherwise</returns>
        public bool compareImage(Image<Bgr, Byte> faceImageCandidate, IdentificationAlgorithms.FaceComparisonType comparisonAlgorithm)
        {
            return identifier2d.isValid(faceImageCandidate, comparisonAlgorithm);
        }
    }
}
