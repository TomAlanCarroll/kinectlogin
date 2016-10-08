using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using Microsoft.Kinect;

namespace KinectLogin
{
    public class SecurityGestureSet
    {
        /// <summary>
        /// The valid gestures to match against
        /// </summary>
        private Gesture[] gestures;

        /// <summary>
        /// The supplied gestures to compare to the valid gestures 
        /// </summary>
        private Gesture[] candidateGestures;

        private List<double> errors;
        private Thread AuthenticationThread;
        private bool authenticationStatus = false;

        //How many horizontal steps you can take before you have to take a vertical step, or vice versa. 
        static readonly int _maxSlope = 2;

        //Measure of how far apart the final position of the input gesture has to be from the final position of the sample gesture. 
        //Original framework value was 2.0
        static readonly double _positionThreshold = 1.2;

        //Measure of how far apart the input sequence has to be from the sample sequence. 
        //original framework value was 0.6
        //static readonly double _sequenceSimilarityThreshold = 1.0;
        static readonly double _recognitionThreshold = 1.0;

        /// <summary>
        /// Constructor
        /// </summary>
        public SecurityGestureSet()
        {
            this.gestures = new Gesture[10];
            this.errors = new List<double>();
        }

        /// <summary>
        /// Gets the candidate gestures
        /// </summary>
        /// <returns>An array of candidate gestures</returns>
        public Gesture[] getCandidateGestures()
        {
            return this.candidateGestures;
        }

        /// <summary>
        /// Sets the candidate gestures
        /// </summary>
        public void setCandidateGestures(Gesture[] candidateGestures)
        {
            this.candidateGestures = candidateGestures;
        }

        /// <summary>
        /// Sets the candidate gesture
        /// </summary>
        public void setCandidateGesture(int index, Gesture candidateGesture)
        {
            this.candidateGestures[index] = candidateGesture;
        }

        /// <summary>
        /// Gets the gestures
        /// </summary>
        /// <returns>An array of gestures</returns>
        public Gesture[] getGestures()
        {
            return this.gestures;
        }

        public bool getAuthenticationStatus()
        {
            return this.authenticationStatus;
        }
       
        /// <summary>
        /// Records gestures
        /// </summary>
        /// <param name="numGestures">The number of gestures to record</param>
        /// <param name="seconds">The length of each record in seconds</param>
        public void record(int numGestures, int seconds)
        {
            if (gestures == null)
            {
                gestures = new Gesture[KinectHelper.getNumberOfGestures()];
            }

            for (int i = 1; i <= numGestures; i++)
            {
                System.Console.WriteLine("Recording " + i);
                KinectHelper.startRecording();
                ExtensionMethods.timer(seconds);
                gestures[i - 1] = KinectHelper.stopRecording();
                System.Console.WriteLine("Finished Recording " + i + "\n");
            }
        }
        
        /// <summary>
        /// Computes the error between two 3D skeletal points p1 and p2
        /// </summary>
        /// <param name="p1">The first 3D skeletal point</param>
        /// <param name="p2">The second 3D skeletal point</param>
        /// <returns></returns>
        double error(Vector3 p1, Vector3 p2)
        {
            double error = Math.Abs(p1.x - p2.x) + Math.Abs(p1.y - p2.y) + Math.Abs(p1.z - p2.z);
            return error;
        }

        /// <summary>
        /// Compares two Gestures g1 and g2 and prints "No Match" if they are not similar enough for some threshold or "Match" if they are
        /// </summary>
        /// <param name="g1">The first Gesture to compare</param>
        /// <param name="g2">The second Gesture to compare</param>
        /// <returns>true if g1 and g2 are a match; false otherwise</returns>
        public bool compare(Gesture g1, Gesture g2)
        {
            bool match = false;
            double cost = 0;

            double minDist = double.PositiveInfinity;

            foreach (JointType joint in g1.getGestureData().Keys)
            {
                int lastG1Position = g1.getGestureData()[joint].Count - 1;
                int lastG2Position = g2.getGestureData()[joint].Count - 1;

                if (CalculateSnapshotPositionDistance(g1.getGestureData(), lastG1Position, g2.getGestureData(), lastG2Position) < _positionThreshold)
                {
                    //We've met the positionThreshold requirement, now perform DTW recognition
                    double d = Dtw(g1.getGestureData(), g2.getGestureData()) / g1.getGestureData()[joint].Count;
                    if (d < minDist)
                    {
                        //Mark the gesture this is most simiilar to. 
                        minDist = d;
                    }

                }
            }

            if (minDist < _recognitionThreshold)
            {
                System.Console.WriteLine("Gestures match. minDist: " + minDist);
                return true;
            }
            else
            {
                System.Console.WriteLine("Gestures do not match. Please try again. minDist: " + minDist);
                return false;
            }
        }

        /// <summary>
        /// Runs as a thread to autenticate the gestures
        /// </summary>
        public void Authenticate()
        {
            int i;
            bool unionAuthenticationStatus = true;
            int numGestures = KinectHelper.getNumberOfGestures();
            int numSeconds = KinectHelper.getNumberOfSeconds();

            // Record the number of gestures provided previously
            for (i = 0; i < numGestures; i++)
            {
                System.Console.WriteLine("Recording gesture #" + (i + 1));
                KinectHelper.startRecording();
                ExtensionMethods.timer(numSeconds);
                candidateGestures[i] = KinectHelper.stopRecording();
                System.Console.WriteLine("Finished Recording\n");
            }

            // Compare the candidate gestures against the valid gestures
            // If any do not match, set the union authentication status to false and exit the loop
            for (i = 0; i < numGestures; i++)
            {
                if (compare(candidateGestures[i], gestures[i]) == false)
                {
                    unionAuthenticationStatus = false;
                    break;
                }
            }

            // Set the authentication status to the union (g1 && g2 && g3 ...) of the comparisons
            authenticationStatus = unionAuthenticationStatus;
        }

        /// <summary>
        /// Compute the min DTW distance between the inputSequence and all possible endings of recorded gestures.
        /// </summary>
        /// <param name="inputSequence">The input gesture</param>
        /// <param name="recordedGesture">Gestures we want to recognize against</param>
        /// <returns>a double indicating level of similarity with closest recorded gesture</returns>        
        public double Dtw(Dictionary<JointType, List<Vector3>> inputSequence, Dictionary<JointType, List<Vector3>> recordedGesture)
        {
            //Make assumption that all lists are same length! 
            var inputSeqIterator = inputSequence.GetEnumerator();
            inputSeqIterator.MoveNext();
            int inputLength = inputSeqIterator.Current.Value.Count;

            //Make assumption that all lists are same length! 
            var recordedGestureSeqIterator = recordedGesture.GetEnumerator();
            recordedGestureSeqIterator.MoveNext();
            int recordLength = recordedGestureSeqIterator.Current.Value.Count;

            //Book keeping, setting up and initialization.
            var tab = new double[inputLength + 1, recordLength + 1];
            var horizStepsMoved = new int[inputLength + 1, recordLength + 1];
            var vertStepsMoved = new int[inputLength + 1, recordLength + 1];

            for (int i = 0; i < inputLength + 1; ++i)
            {
                for (int j = 0; j < recordLength + 1; ++j)
                {
                    tab[i, j] = double.PositiveInfinity;
                    horizStepsMoved[i, j] = 0;
                    vertStepsMoved[i, j] = 0;
                }
            }

            tab[inputLength, recordLength] = 0;

            //Actually do the DTW algo. Read
            //http://web.science.mq.edu.au/~cassidy/comp449/html/ch11s02.html
            //For a great summary as to what it does. 
            for (int i = inputLength - 1; i > -1; --i)
            {
                for (int j = recordLength - 1; j > -1; --j)
                {
                    if (tab[i, j + 1] < tab[i + 1, j + 1] && tab[i, j + 1] < tab[i + 1, j] &&
                        horizStepsMoved[i, j + 1] < _maxSlope)
                    {
                        //Move right, move left on reverse
                        tab[i, j] = CalculateSnapshotPositionDistance(inputSequence, i, recordedGesture, j) + tab[i, j + 1];
                        horizStepsMoved[i, j] = horizStepsMoved[i, j + 1] + 1;
                        vertStepsMoved[i, j] = vertStepsMoved[i, j + 1];

                    }

                    else if (tab[i + 1, j] < tab[i + 1, j + 1] && tab[i + 1, j] < tab[i, j + 1] &&
                             vertStepsMoved[i + 1, j] < _maxSlope)
                    {
                        //Move down, move up on reverse
                        tab[i, j] = CalculateSnapshotPositionDistance(inputSequence, i, recordedGesture, j) + tab[i + 1, j];
                        horizStepsMoved[i, j] = horizStepsMoved[i + 1, j];
                        vertStepsMoved[i, j] = vertStepsMoved[i + 1, j] + 1;
                    }

                    else
                    {
                        //Move diagonally down-right
                        if (tab[i + 1, j + 1] == double.PositiveInfinity)
                        {
                            tab[i, j] = double.PositiveInfinity;
                        }
                        else
                        {
                            tab[i, j] = CalculateSnapshotPositionDistance(inputSequence, i, recordedGesture, j) + tab[i + 1, j + 1];
                        }

                        horizStepsMoved[i, j] = 0;
                        vertStepsMoved[i, j] = 0;

                    }
                }
            }

            double bestMatch = double.PositiveInfinity;

            for (int i = 0; i < inputLength; ++i)
            {
                if (tab[i, 0] < bestMatch)
                {
                    bestMatch = tab[i, 0];
                }
            }
            return bestMatch;


        }

        /// <summary>
        /// Compute the length between a frame of the inputSequence and a frame of a recorded gesture.
        /// </summary>
        /// <param name="inputSequence">The input gesture</param>
        /// <param name="inputPosition">Which frame we want from the input gesture to compare against the recorded gesture's frame</param> 
        /// <param name="recordedGesture">Gestures we want to recognize against</param>
        /// <param name="recordedPosition">Which frame we want from the recorded gesture to compare against the input gesture's frame</param>
        /// <returns>a double that is the length between the specified frame of the input gesture versus the specified frame of the recorded gesture</returns>        
        private double CalculateSnapshotPositionDistance(Dictionary<JointType, List<Vector3>> inputSequence, int inputPosition, Dictionary<JointType, List<Vector3>> recordedGesture, int recordedPosition)
        {
            double d = 0;
            foreach (JointType joint in recordedGesture.Keys)
            {
                d += (Vector3.DistanceSquared(inputSequence[joint][inputPosition], recordedGesture[joint][recordedPosition]));
            }

            return Math.Sqrt(d);
        }
    }
}
