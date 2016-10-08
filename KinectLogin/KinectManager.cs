using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit.FaceTracking;
using System.IO;
using System.Drawing;
using Emgu.CV;
using Emgu.CV.Structure;
using Emgu.CV.CvEnum;

namespace KinectLogin
{
    public static class KinectManager
    {
        private static IdentificationAlgorithms.FaceComparisonType faceComparisonType;
        private static Face face;
        private static string faceModelFileName;
        private static int faceModelFileIndex;
        private static string faceModelName;
        private static string faceImageName;
        private static int faceImageFileIndex;

        private static KinectHelper helper;
        private static SecurityGestureSet gestureSet;

        private static VoiceRecognition voiceRecognition;
        private static Voice voicePassword;

        /// <summary>
        /// Boolean marking whether a gesture attempt has failed. Used for feedback
        /// </summary>
        private static bool gesturesFailed = false;

        public static void setup()
        {
            if (helper == null)
            {
                helper = new KinectHelper();
            }

            if (gestureSet == null)
            {
                gestureSet = new SecurityGestureSet();
            }

            if (voiceRecognition == null)
            {
                voiceRecognition = new VoiceRecognition();
            }
        }

        public static KinectHelper getKinectHelper() {
            if (helper == null)
            {
                helper = new KinectHelper();
                return helper;
            }
            else
            {
                return helper;
            }
        }

        public static SecurityGestureSet getGestureSet()
        {
            if (gestureSet == null)
            {
                gestureSet = new SecurityGestureSet();
                return gestureSet;
            }
            else
            {
                return gestureSet;
            }
        }

        public static VoiceRecognition getVoiceRecognition()
        {
            if (voiceRecognition == null)
            {
                voiceRecognition = new VoiceRecognition();
                return voiceRecognition;
            }
            else
            {
                return voiceRecognition;
            }
        }

        /// <summary>
        /// Records the gestures using KinectHelper. This is intended to be ran on its own thread.
        /// </summary>
        /// <param name="numGestures">The number of gestures to record</param>
        /// <param name="numSeconds">The number of seconds for each gesture</param>
        public static void recordGestures(int numGestures, int numSeconds)
        {
            gestureSet.record(numGestures, numSeconds);
        }

        public static void saveVoicePassword(Voice voice)
        {
            voicePassword = voice;
        }

        public static Voice getVoicePassword()
        {
            return voicePassword;
        }

        /// <summary>
        /// This event is fired when the voice data is updated. 
        /// It checks for password matches in the voice data against the saved voice password in KinectManager.
        /// It is intended to only be executed when the Login form is active.
        /// </summary>
        public static void UpdateVoiceData(object sender, EventArgs e)
        {
            if (voiceRecognition != null)
            {
                int i;
                Voice[] voices = voiceRecognition.getVoices();

                for (i = 0; i < voices.Count(); i++)
                {
                    bool match = voiceRecognition.compare(voices[i], voicePassword);
                    if (match)
                    {
                    } // else no match
                }
            }
        }

        // Face model filename getters/setters
        public static string getFaceModelFileName()
        {
            if (faceModelFileName == null)
            {
                return "";
            }
            else
            {
                return faceModelFileName;
            }
        }

        public static void setFaceModelFileName(string filename)
        {
            faceModelFileName = filename;
        }

        // Face model name getters/setters
        public static string getFaceModelName()
        {
            if (faceModelName == null)
            {
                return "";
            }
            else
            {
                return faceModelName;
            }
        }

        public static void setFaceModelName(string name)
        {
            faceModelName = name;
        }

        public static int getModelFaceFileIndex()
        {
            return faceModelFileIndex;
        }

        public static void setFaceModelFileIndex(int index)
        {
            faceModelFileIndex = index;
        }

        // Face image name getters/setters
        public static string getFaceImageName()
        {
            if (faceImageName == null)
            {
                return "";
            }
            else
            {
                return faceImageName;
            }
        }

        public static void setFaceImageName(string name)
        {
            faceImageName = name;
        }

        public static int getImageFaceFileIndex()
        {
            return faceImageFileIndex;
        }

        public static void setFaceImageFileIndex(int index)
        {
            faceImageFileIndex = index;
        }

        public static IdentificationAlgorithms.FaceComparisonType getFaceComparisonType()
        {
            if (faceComparisonType == null)
            {
                // Use Naive Bayes Point Locations by default
                return IdentificationAlgorithms.FaceComparisonType.Naive_Bayes_Point_Locations;
            }
            else
            {
                return faceComparisonType;
            }
        }

        public static void setFaceComparisonType(IdentificationAlgorithms.FaceComparisonType algorithm)
        {
            faceComparisonType = algorithm;
        }

        /// <summary>
        /// Creates a new face object with the provided faceModel and the current Kinect image
        /// </summary>
        /// <param name="faceImage">The current image of the face by the Kinect</param>
        /// <param name="faceModel">The set of 3D feature points describing this face.</param>
        public static void SaveFace(Image<Bgr, Byte> faceImage, EnumIndexableCollection<FeaturePoint, Vector3DF> faceModel)
        {
            face = new Face(faceImage, faceModel);
        }

        /// <summary>
        /// Gets the face
        /// </summary>
        /// <returns>The KinectManager's current face value</returns>
        public static Face getFace()
        {
            return face;
        }

        /// <summary>
        /// Compares the current face with the faceModelCandidate
        /// </summary>
        /// <param name="faceModelCandidate"></param>
        public static bool CompareFaces(Image<Bgr, Byte> faceImageCandidate, EnumIndexableCollection<FeaturePoint, Vector3DF> faceModelCandidate)
        {
            bool modelMatch = face.compareModel(faceModelCandidate, KinectManager.getFaceComparisonType());
            bool imageMatch = face.compareImage(faceImageCandidate, KinectManager.getFaceComparisonType());
            return modelMatch && imageMatch;
        }

        public static bool getGesturesFailed()
        {
            return gesturesFailed;
        }

        public static void setGesturesFailed(bool flag)
        {
            gesturesFailed = flag;
        }
    }
}
