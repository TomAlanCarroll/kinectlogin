using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Emgu.CV;
using Emgu.CV.Structure;
using System.Collections;

using SharpNeat.Core;
using SharpNeat.Decoders;
using SharpNeat.Decoders.HyperNeat;
using SharpNeat.DistanceMetrics;
using SharpNeat.EvolutionAlgorithms;
using SharpNeat.EvolutionAlgorithms.ComplexityRegulation;
using SharpNeat.Genomes.HyperNeat;
using SharpNeat.Genomes.Neat;
using SharpNeat.Network;
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using SharpNeat.Domains;
using System.Diagnostics;
using System.Drawing;

namespace KinectLogin
{
    /// <summary>
    /// Identifies Faces from 2D Images
    /// </summary>
    class _2dFaceIdentification
    {
        // The face image we are comparing with the candidate face images
        private Image<Bgr, Byte> faceImage;

        // Flag for the initial setup of HyperNEAT
        private bool hyperNeatIsSetup = false;

        // Flags for the initial setup of Eigenfaces
        private bool eigenFaceIsSetup = false;

        // Maps the file indices to their class names
        // The class name is used for comparison in the function isValid
        // Example:
        /*
         0 -> "Joe"
         1 -> "Joe"
         2 -> "Lisa"
         3 -> "Mary"
         4 -> "Joe"
         5 -> "Todd"
        */ 
        public SortedDictionary<int, string> nameMappings;

        private const string UNRECOGNIZED_FACE_NAME = "UNKNOWN_FACE";
        private const string INCORRECT_NAME = "INCORRECT";
        private const string LOW_CONFIDENCE_NAME = "LOW_CONFIDENCE";


        // Collections of training and testing data
        private ArrayList trainingFaceImages;
        private ArrayList trainingFaceFileIndices;
        private ArrayList testingFaceImages;

        // flag to see if done training
        private bool doneTraining = false;

        // HyperNEAT
        private IBlackBox brain;
        private _2dHyperNeatExperiment _hyperNeatExperiment = null;
        private IGenomeFactory<NeatGenome> _hyperNeatGenomeFactory = null;
        private List<NeatGenome> _hyperNeatGenomeList = null;
        private NeatEvolutionAlgorithm<NeatGenome> _hyperNeatEvolutionAlgorithm = null;
        private NeatGenome _hyperNeatChampGenome = null;
        private double _hyperNeatChampGenomeFitness = 0.0;

        // Face Recognizer (using OpenCV)
        FaceRecognizer recognizer;

        // A default of 2000 is used for the eigen face threshld but be increasing 
        // this (e.g. to 5000) will reduce recognizer false positives
        private const int EIGEN_THRESHOLD = 4500;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="faceImage">The face image</param>
        public _2dFaceIdentification(Image<Bgr, Byte> faceImage)
        {
            this.faceImage = faceImage;
        }

        public bool isValid(Image<Bgr, Byte> candidateFaceImage, IdentificationAlgorithms.FaceComparisonType comparisonAlgorithm)
        {
            // Convert to grayscale
            Image<Gray, Byte> grayscaleFaceImage = candidateFaceImage.Convert<Gray, Byte>();
            grayscaleFaceImage._EqualizeHist();

            switch (comparisonAlgorithm)
            {
                case IdentificationAlgorithms.FaceComparisonType.HyperNEAT:

                    if (!hyperNeatIsSetup)
                    {
                        // Setup our classifier once

                        populateTrainingData();

                        setupHyperNeat();

                        trainHyperNeat();

                        hyperNeatIsSetup = true;
                    }

                    if (doneTraining)
                    {
                        this.brain.ResetState();

                        ISignalArray inputArray = this.brain.InputSignalArray;
                        ISignalArray outputArray = this.brain.OutputSignalArray;

                        // push the eigenvectors into the black box
                        int i = 0;
                        Image<Gray, float> pca = ExtensionMethods.CalculatePCAofImage(grayscaleFaceImage, 2);

                        this.brain.Activate();

                        // check whether or not this evalutation passed the test
                        int numCorrect = 0;
                        for (i = 0; i < outputArray.Length; i++)
                        {
                            if (outputArray[i] > 0.75) // must be at least 75% close
                            {
                                numCorrect++;
                            }
                        }

                        // all inputs passed
                        return (numCorrect / 400) >= 0.90; // 90% accuracy
                    }
                    break;
                default: // Use eigenface recognition bundled with OpenCV
                    if (!eigenFaceIsSetup)
                    {
                        // Train the NN once
                        populateTrainingData();

                        trainEigenFaces();
                    }

                    if (doneTraining)
                    {
                        string authenticated = testEigenFace(grayscaleFaceImage, KinectManager.getFaceImageName());

                        if (authenticated != null)
                        {
                            if (authenticated == UNRECOGNIZED_FACE_NAME || authenticated == INCORRECT_NAME || authenticated == LOW_CONFIDENCE_NAME)
                            {
                                return false;
                            }
                            else
                            {
                                return true;
                            }
                        }
                        else
                        {
                            return false;
                        }
                    }
                    break;
            }

            return false;
        }


        /// <summary>
        /// Sets up the training data by loading the training images in Face.faceFileDirectory2D
        /// </summary>
        public void populateTrainingData()
        {
            this.trainingFaceImages = new ArrayList();
            this.trainingFaceFileIndices = new ArrayList();
            this.testingFaceImages = new ArrayList();

            // Dictionary of file names
            nameMappings = new SortedDictionary<int, string>();

            // Find the number of files in C:\KinectLogin
            int faceCount = Directory.GetFiles(Face.faceTrainingFileDirectory2D).Length;

            // Counters
            int faceIndex = 0;

            // Load each face in Face.faceFileDirectory2D
            foreach (string file in Directory.EnumerateFiles(Face.faceTrainingFileDirectory2D))
            {
                string name = ExtensionMethods.getNameFromFile(file);

                nameMappings.Add(faceIndex, name);

                Image<Gray, Byte> grayscaleFaceImage = new Image<Gray, Byte>(file);
                grayscaleFaceImage._EqualizeHist();

                trainingFaceImages.Add(grayscaleFaceImage);
                trainingFaceFileIndices.Add(faceIndex);

                faceIndex++;
            }
        }

		public void trainHyperNeat()
		{
			doneTraining = false;
			while (_hyperNeatEvolutionAlgorithm.RunState == RunState.NotReady)
			{
			}
			// force the network to improve until it's terminated
			do {
				_hyperNeatEvolutionAlgorithm.StartContinue();
			} while (_hyperNeatEvolutionAlgorithm.RunState == RunState.Running || _hyperNeatEvolutionAlgorithm.RunState == RunState.Ready);
		}

		public void setupHyperNeat()
		{
			// Create and train the HyperNEAT classifier.
			_hyperNeatExperiment = new _2dHyperNeatExperiment(trainingFaceImages);
			_hyperNeatExperiment.Initialize("2dFaceIdentification", null);

			// create random genomes
			_hyperNeatGenomeFactory = _hyperNeatExperiment.CreateGenomeFactory();
			_hyperNeatGenomeList = _hyperNeatGenomeFactory.CreateGenomeList(_hyperNeatExperiment.PopulationSize, 0u);

			// Update experimental parameters
			NeatGenomeParameters hyperNeatGenomeParams = _hyperNeatExperiment.NeatGenomeParameters;
			hyperNeatGenomeParams.ConnectionWeightRange = 5.0;
			hyperNeatGenomeParams.DeleteConnectionMutationProbability = 0.01;
			hyperNeatGenomeParams.AddNodeMutationProbability = 0.01;
            hyperNeatGenomeParams.AddConnectionMutationProbability = 0.1;
            hyperNeatGenomeParams.ConnectionWeightMutationProbability = 0.01;
			NeatEvolutionAlgorithmParameters hyperNeatParams = _hyperNeatExperiment.NeatEvolutionAlgorithmParameters;
			hyperNeatParams.SpecieCount = 10;
            hyperNeatParams.ElitismProportion = 0.2;
            hyperNeatParams.SelectionProportion = 0.2;
            hyperNeatParams.OffspringAsexualProportion = 0.5;
            hyperNeatParams.OffspringSexualProportion = 0.5;
            hyperNeatParams.InterspeciesMatingProportion = 0.01;

			// create evolution algorithm
			_hyperNeatEvolutionAlgorithm = _hyperNeatExperiment.CreateEvolutionAlgorithm(_hyperNeatGenomeFactory, _hyperNeatGenomeList);
			_hyperNeatEvolutionAlgorithm.UpdateEvent += new EventHandler(_hyperNeatUpdateHandler);
            Debug.WriteLine(_hyperNeatEvolutionAlgorithm.RunState);
		}

		private void _hyperNeatUpdateHandler(object sender, EventArgs args)
		{
			// get the champion genome and fitness
			_hyperNeatChampGenome = _hyperNeatEvolutionAlgorithm.CurrentChampGenome;
			_hyperNeatChampGenomeFitness = _hyperNeatChampGenome.EvaluationInfo.Fitness;
			uint currentGeneration = _hyperNeatEvolutionAlgorithm.CurrentGeneration;
            uint numberEvalutations = _hyperNeatChampGenome.EvaluationInfo.EvaluationCount;
            Debug.WriteLine("(Evalutation, Generation , Best Fitness) : (" + numberEvalutations + " , " + currentGeneration + " , " + _hyperNeatChampGenomeFitness + ")");
            if (_hyperNeatEvolutionAlgorithm.StopConditionSatisfied)
			{
                _hyperNeatEvolutionAlgorithm.Stop();
				doneTraining = true;
                this.setupTest();
			}
		}

        public void setupTest()
        {
            IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = _hyperNeatExperiment.CreateGenomeDecoder();
            this.brain = genomeDecoder.Decode(_hyperNeatChampGenome);
        }

        public void trainEigenFaces()
        {
            doneTraining = false;
            eigenFaceIsSetup = true;

            recognizer = new EigenFaceRecognizer(80, double.PositiveInfinity);

            IImage[] trainingImages = new IImage[trainingFaceImages.Count];
            int faceCounter = 0;
            foreach (Image<Gray, Byte> face in trainingFaceImages)
            {
                trainingImages[faceCounter] = face;
                faceCounter++;
            }

            int[] trainingImageIndices = new int[trainingFaceFileIndices.Count];
            faceCounter = 0;
            foreach (int index in trainingFaceFileIndices)
            {
                trainingImageIndices[faceCounter] = index;
                faceCounter++;
            }

            recognizer.Train(trainingImages, trainingImageIndices);

            doneTraining = true;

            testRecognizer();

        }

        /**
         * Tests a candidate face using the Eigenface recognizer and returns the 
         * predicted name if the eigen-distance is above EIGEN_THRESHOLD 
         */
        public string testEigenFace(Image<Gray, Byte> candidate, string targetName)
        {
            FaceRecognizer.PredictionResult eigenPrediction = recognizer.Predict(candidate);

            if (eigenPrediction.Label == -1)
            {
                System.Diagnostics.Debug.WriteLine("Unable to recognize 2D face image.");
                // Unable to recognize the provided face image; Return false
                return UNRECOGNIZED_FACE_NAME;
            }
            else
            {
                try
                {
                    string predictedName = nameMappings[eigenPrediction.Label];
                    if (targetName.Equals(predictedName)) 
                    {
                        if ((float)eigenPrediction.Distance >= EIGEN_THRESHOLD)
                        {
                            System.Diagnostics.Debug.WriteLine("Likely face image corresponds to "
                                + eigenPrediction.Label + ": " + nameMappings[eigenPrediction.Label]
                                + " with distance " + eigenPrediction.Distance);
                            return predictedName;
                        }
                        else
                        {
                            return LOW_CONFIDENCE_NAME;
                        }
                    }
                    else 
                    {
                        return INCORRECT_NAME;
                    }
                }
                catch (Exception exception)
                {
                    Debug.WriteLine("Error: Unable to retrieve predicted name for 2D face identification. " +
                        "Exception: " + exception.Message);
                    return null;
                }
            }
        }

        /**
         * Tests the recognizer with the face images in Face.faceTestingFileDirectory2D and outputs statistics
         */
        public void testRecognizer()
        {
            ArrayList testFaceImages = new ArrayList();

            // Find the number of files in C:\KinectLogin
            int faceCount = Directory.GetFiles(Face.faceTestingFileDirectory2D).Length;

            // Counters
            int faceIndex = 0;
            int correctPrediction = 0;
            int falsePositives = 0;
            int falseNegatives = 0;

            // write the statistics to a file
            string timestamp = System.DateTime.Now.ToString("MMddyy HHmmss");
            string filename = Face.faceFileDirectory2D + @"\statistics" + timestamp + ".txt";
            using (System.IO.StreamWriter file = new System.IO.StreamWriter(filename))
            {
                // Load each face in Face.faceTestingFileDirectory2D
                foreach (string imageFileName in Directory.EnumerateFiles(Face.faceTestingFileDirectory2D))
                {
                    string name = ExtensionMethods.getNameFromFile(imageFileName);

                    Image<Gray, Byte> grayscaleFaceImage = new Image<Gray, Byte>(imageFileName);
                    grayscaleFaceImage._EqualizeHist();

                    string predictedName = testEigenFace(grayscaleFaceImage, name);

                    if (predictedName != null)
                    {
                        if (predictedName == UNRECOGNIZED_FACE_NAME)
                        {
                            falseNegatives++;

                            file.WriteLine("False negative:" + name + ", predicted: " + predictedName);
                        }
                        else if (predictedName == name)
                        {
                            correctPrediction++;

                            file.WriteLine("Correct:" + name);
                        }
                        else if (predictedName == INCORRECT_NAME)
                        {
                            falsePositives++;

                            file.WriteLine("False positive:" + name + ", predicted: " + predictedName);
                        }
                        else if (predictedName == LOW_CONFIDENCE_NAME)
                        {
                            falseNegatives++;

                            file.WriteLine("False negative:" + name + ", predicted: " + predictedName);
                        }
                        else
                        {
                            falsePositives++;

                            file.WriteLine("False positive:" + name + ", predicted: " + predictedName);
                        }
                    }
                    else
                    {
                        falseNegatives++;

                        file.WriteLine("False negative:" + name + ", predicted: " + predictedName);
                    }

                    faceIndex++;
                }

                file.WriteLine("Number of Training Faces:" + trainingFaceImages.Count);
                file.WriteLine("Number of Testing Faces:" + faceCount);
                file.WriteLine("Number of Correct Predictions:" + correctPrediction);
                file.WriteLine("Number of Incorrect Predictions:" + (falsePositives + falseNegatives));
                file.WriteLine("Number of False Positives:" + falsePositives);
                file.WriteLine("Number of False Negatives:" + falseNegatives);
            }
        }
    }
}
