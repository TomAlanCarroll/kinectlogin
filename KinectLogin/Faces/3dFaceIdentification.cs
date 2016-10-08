using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.FaceTracking;
using Emgu.CV.Structure;
using Emgu.CV.ML;
using Emgu.CV.ML.Structure;
using System.IO;
using Emgu.CV;
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
using SharpNeat.Phenomes;
using SharpNeat.SpeciationStrategies;
using SharpNeat.Domains;

namespace KinectLogin
{
    /// <summary>
    /// Identifies Faces from 3D face feature points
    /// </summary>
    class _3dFaceIdentification
    {
        // Naive Bayes classifier
        private NormalBayesClassifier classifier = new NormalBayesClassifier();

        // Flags for initial setup of naive bayes
        private bool naiveBayesPointsIsSetup = false;
        private bool naiveBayesDistancesIsSetup = false;

        // R-Trees classifier
        private RTrees rTrees = new RTrees();

        // Flag for the initial setup of rTrees
        private bool rTreesIsSetup = false;

        // SVM
        private SVM svm = new SVM();

        // Flag for the initial setup of SVM
        private bool svmIsSetup = false;
        
        // Face model to compare against
        private EnumIndexableCollection<FeaturePoint, Vector3DF> faceModel;

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

		// flag for initial setup of hyperneat
		private bool _isHyperNeatSetup = false;

		// hyperNEAT
		private HyperNeatExperiment _hyperNeatExperiment = null;
		private IGenomeFactory<NeatGenome> _hyperNeatGenomeFactory = null;
		private List<NeatGenome> _hyperNeatGenomeList = null;
		private NeatEvolutionAlgorithm<NeatGenome> _hyperNeatEvolutionAlgorithm = null;
		private NeatGenome _hyperNeatChampGenome = null;
		private double _hyperNeatChampGenomeFitness = 0.0;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="faceModel">The saved 3D model</param>
        public _3dFaceIdentification(EnumIndexableCollection<FeaturePoint, Vector3DF> faceModel)
        {
            this.faceModel = faceModel;
        }

        public bool isValid(EnumIndexableCollection<FeaturePoint, Vector3DF> faceModelCandidate, IdentificationAlgorithms.FaceComparisonType comparisonAlgorithm)
        {
            int likelyInstance;
            int index;

            SortedDictionary<FeaturePoint, Vector3DF> candidate;

            switch (comparisonAlgorithm)
            {
                case IdentificationAlgorithms.FaceComparisonType.Naive_Bayes_Point_Locations:

                    if (!naiveBayesPointsIsSetup)
                    {
                        // Setup our classifier once
                        setupNaiveBayesForPointLocations();
                        naiveBayesPointsIsSetup = true;
                    }

                    candidate = translateFace(faceModelCandidate);

                    Matrix<float> bayesPointCandidate = new Matrix<float>(1, candidate.Keys.Count * 3);

                    // For each FeaturePoint
                    index = 0;
                    foreach (FeaturePoint featurePoint in candidate.Keys)
                    {
                        if (candidate[featurePoint] != null)
                        {
                            bayesPointCandidate[0, index] = candidate[featurePoint].X;
                            bayesPointCandidate[0, index + 1] = candidate[featurePoint].Y;
                            bayesPointCandidate[0, index + 2] = candidate[featurePoint].Y;
                        }

                        index += 3;
                    }

                    likelyInstance = (int)classifier.Predict(bayesPointCandidate, null);


                    if (nameMappings != null && nameMappings.Keys.Contains(likelyInstance))
                    {
                        System.Console.WriteLine("Naive Bayes FP Location likely instance: " + likelyInstance + " - " + nameMappings[likelyInstance]);

                        // Check to see if this is our target
                        try
                        {
                            // Check to see if this is our target
                            string predictedName = nameMappings[likelyInstance];
                            if (KinectManager.getFaceModelName().Equals(predictedName))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            System.Console.WriteLine("Error: Unable to retrieve predicted name for 3D face identification. " +
                                "Exception: " + exception.Message);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Naive Bayes FP Location likely instance: " + likelyInstance);
                    }
                    break;

                case IdentificationAlgorithms.FaceComparisonType.Random_Trees_Point_Locations:
                    if (!rTreesIsSetup)
                    {
                        // Setup our classifier once
                        setupRTrees();
                        rTreesIsSetup = true;
                    }

                    candidate = translateFace(faceModelCandidate);

                    Matrix<float> rTreesPointCandidate = new Matrix<float>(1, candidate.Keys.Count * 3);

                    // For each FeaturePoint
                    index = 0;
                    foreach (FeaturePoint featurePoint in candidate.Keys)
                    {
                        if (candidate[featurePoint] != null)
                        {
                            rTreesPointCandidate[0, index] = candidate[featurePoint].X;
                            rTreesPointCandidate[0, index + 1] = candidate[featurePoint].Y;
                            rTreesPointCandidate[0, index + 2] = candidate[featurePoint].Y;
                        }

                        index += 3;
                    }

                    likelyInstance = (int)rTrees.Predict(rTreesPointCandidate, null);

                    if (nameMappings != null && nameMappings.Keys.Contains(likelyInstance))
                    {
                        System.Console.WriteLine("RTrees likely instance: " + likelyInstance + " - " + nameMappings[likelyInstance]);

                        try
                        {
                            // Check to see if this is our target
                            string predictedName = nameMappings[likelyInstance];
                            if (KinectManager.getFaceModelName().Equals(predictedName))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            System.Console.WriteLine("Error: Unable to retrieve predicted name for 3D face identification. " +
                                "Exception: " + exception.Message);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("RTrees likely instance: " + likelyInstance);
                    }
                    break;
                case IdentificationAlgorithms.FaceComparisonType.Support_Vector_Machine_Locations:
                    if (!svmIsSetup)
                    {
                        // Setup our SVM
                        setupSVM();
                        svmIsSetup = true;
                    }

                    candidate = translateFace(faceModelCandidate);

                    Matrix<float> svmPointCandidate = new Matrix<float>(1, candidate.Keys.Count * 3);

                    // For each FeaturePoint
                    index = 0;
                    foreach (FeaturePoint featurePoint in candidate.Keys)
                    {
                        if (candidate[featurePoint] != null)
                        {
                            svmPointCandidate[0, index] = candidate[featurePoint].X;
                            svmPointCandidate[0, index + 1] = candidate[featurePoint].Y;
                            svmPointCandidate[0, index + 2] = candidate[featurePoint].Y;
                        }

                        index += 3;
                    }

                    likelyInstance = (int)svm.Predict(svmPointCandidate);

                    if (nameMappings != null && nameMappings.Keys.Contains(likelyInstance))
                    {
                        System.Console.WriteLine("SVM likely instance: " + likelyInstance + " - " + nameMappings[likelyInstance]);
                        
                        try
                        {
                            // Check to see if this is our target
                            string predictedName = nameMappings[likelyInstance];
                            if (KinectManager.getFaceModelName().Equals(predictedName))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            System.Console.WriteLine("Error: Unable to retrieve predicted name for 3D face identification. " +
                                "Exception: " + exception.Message);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("SVM likely instance: " + likelyInstance);
                    }
                    break;
                case IdentificationAlgorithms.FaceComparisonType.Naive_Bayes_Point_Distances:

                    if (!naiveBayesDistancesIsSetup)
                    {
                        // Setup our classifier once
                        setupNaiveBayesForPointDistances();
                        naiveBayesDistancesIsSetup = true;
                    }

                    candidate = convertCollectionToDictionary(faceModelCandidate);

                    Matrix<float> bayesCandidate = new Matrix<float>(1, candidate.Keys.Count);

                    // For each FeaturePoint
                    // Calculate the distance between each FP and the next
                    for (index = 0; index < candidate.Keys.Count; index++)
                    {
                        // Compare with the first point if we are at the last point of the keyset
                        if (index == candidate.Keys.Count - 1)
                        {
                            bayesCandidate[0, index] = pointDistance3D(candidate[candidate.Keys.ElementAt(index)], candidate[candidate.Keys.ElementAt(0)]);
                        }
                        else
                        {
                            bayesCandidate[0, index] = pointDistance3D(candidate[candidate.Keys.ElementAt(index)], candidate[candidate.Keys.ElementAt(index + 1)]);
                        }
                    }

                    likelyInstance = (int)classifier.Predict(bayesCandidate, null);


                    if (nameMappings != null && nameMappings.Keys.Contains(likelyInstance))
                    {
                        System.Console.WriteLine("Naive Bayes FP Distance likely instance: " + likelyInstance + " - " + nameMappings[likelyInstance]);

                        try
                        {
                            // Check to see if this is our target
                            string predictedName = nameMappings[likelyInstance];
                            if (KinectManager.getFaceModelName().Equals(predictedName))
                            {
                                return true;
                            }
                            else
                            {
                                return false;
                            }
                        }
                        catch (Exception exception)
                        {
                            System.Console.WriteLine("Error: Unable to retrieve predicted name for 3D face identification. " +
                                "Exception: " + exception.Message);
                        }
                    }
                    else
                    {
                        System.Console.WriteLine("Naive Bayes FP Distance likely instance: " + likelyInstance);
                    }
                    break;
                case IdentificationAlgorithms.FaceComparisonType.Threshold_All_Distances_Between_All_Feature_Points:
                    // +/- error for distance threshold
                    float distanceErrorThreshold = 0.1F;
                    float lowDistance, highDistance;

                    // threshold for the number of wrong distances
                    // This is an allowance for noisy data in the face model
                    int numWrongPointsThreshold = 500;
                    int numWrongPoints = 0;

                    int numFaceFeatures = faceModel.Count;
                    int numFaceCandidateFeatures = faceModelCandidate.Count;

                    // Build a distance matrix for each point that corresponds to every other point
                    float[,] faceModelDistanceMatrix = new float[numFaceFeatures, numFaceFeatures];

                    float[,] faceModelCandidateDistanceMatrix = new float[numFaceCandidateFeatures, numFaceCandidateFeatures];

                    // Calculate the distances:

                    // For each row of the distanceMatrix
                    foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
                    {
                        // For each column of the distanceMatrix
                        foreach (int j in Enum.GetValues(typeof(FeaturePoint)))
                        {
                            // Add the distances to the matrices
                            faceModelDistanceMatrix[i, j] = pointDistance3D(faceModel[i], faceModel[j]);

                            faceModelCandidateDistanceMatrix[i, j] = pointDistance3D(faceModelCandidate[i], faceModelCandidate[j]);

                            // Verify these distances are not 0
                            if (faceModelDistanceMatrix[i, j] != 0 && faceModelCandidateDistanceMatrix[i, j] != 0)
                            {
                                // Compare these using the threshold; candidate must be within low and high distances
                                lowDistance = faceModelDistanceMatrix[i, j] * (1 - distanceErrorThreshold);
                                highDistance = faceModelDistanceMatrix[i, j] * (1 + distanceErrorThreshold);

                                // Check that lowDistance <= faceModelCandidateDistanceMatrix[i, j] <= highDistance
                                if (lowDistance >= faceModelCandidateDistanceMatrix[i, j] || highDistance <= faceModelCandidateDistanceMatrix[i, j])
                                {
                                    numWrongPoints++;
                                }
                            }
                        }
                    }

                    if (numWrongPoints < numWrongPointsThreshold)
                    {
                        // Match
                        return true;
                    }
                    else
                    {
                        return false;
                    }
                    break;
				case IdentificationAlgorithms.FaceComparisonType.HyperNEAT:
					if (!_isHyperNeatSetup)
					{
						_isHyperNeatSetup = true;
						setupHyperNEAT();
					}
					return true;
					break;
            }

            return false;
        }

		/// <summary>
		/// Sets up the HyperNEAT classifier for classifying faces using feature point locations.
		/// Basically, we're going to train a neural network to check whether or not a provided face
		/// has been seen before. This function does exactly that.
		/// </summary>
		public void setupHyperNEAT()
		{
			// Counters
			int faceIndex = 0;
			int i = 0;

			// split array for lines
			string[] split;

			// The face model read in from the files
			SortedDictionary<FeaturePoint, Vector3DF> faceModel = new SortedDictionary<FeaturePoint, Vector3DF>();

			// The translated face
			SortedDictionary<FeaturePoint, Vector3DF> face;

			// Input for HyperNEAT classifier. 3D facial data.
			// We need the following: x-coord, y-coord, and z-coord will be the input value.
			Matrix<float> trainData = new Matrix<float>(71, 71);

			// for each saved file
            foreach (string file in Directory.EnumerateFiles(Face.faceFileDirectory3D))
			{
                if (file.Equals(KinectManager.getFaceModelName()))
				{
					KinectManager.setFaceModelFileIndex(faceIndex);
				}
				faceIndex++;
			}

			// Parse the file which contains our training data.
			foreach (string line in File.ReadLines(KinectManager.getFaceModelFileName()))
			{
				if (line != null && !line.Trim().Equals(""))
				{
					split = line.Split(new Char[] { ':', ',' });

					// Split contents:
					// [0]: FeaturePoint as an int
					// [1]: Vector3DF.X
					// [2]: Vector3DF.Y
					// [3]: Vector3DF.Z
					if (split != null && split.Length >= 4)
					{
						faceModel.Add(((FeaturePoint)Convert.ToInt32(split[0])),
							new Vector3DF(
								Convert.ToSingle(split[1]),
								Convert.ToSingle(split[2]),
								Convert.ToSingle(split[3])
							)
						);
					}
				}
			}

			// Translate the face around the origin
			face = translateFace(faceModel);

			// Create and train the HyperNEAT classifier.
			_hyperNeatExperiment = new HyperNeatExperiment(face);
			_hyperNeatExperiment.Initialize("3dFaceIdentification", null);

			// create random genomes
			_hyperNeatGenomeFactory = _hyperNeatExperiment.CreateGenomeFactory(); 
			_hyperNeatGenomeList = _hyperNeatGenomeFactory.CreateGenomeList(_hyperNeatExperiment.PopulationSize, 0u);
			
			// Update experimental parameters TODO
			NeatGenomeParameters hyperNeatGenomeParams = _hyperNeatExperiment.NeatGenomeParameters;
			hyperNeatGenomeParams.ConnectionWeightRange = 0.0;
			hyperNeatGenomeParams.DeleteConnectionMutationProbability = 0.01;
			hyperNeatGenomeParams.AddNodeMutationProbability = 0.01;
			hyperNeatGenomeParams.AddConnectionMutationProbability = 0.01;
			hyperNeatGenomeParams.DeleteConnectionMutationProbability = 0.01;
			NeatEvolutionAlgorithmParameters hyperNeatParams = _hyperNeatExperiment.NeatEvolutionAlgorithmParameters;
			hyperNeatParams.SpecieCount = 10;
			hyperNeatParams.ElitismProportion = 0.1;
			hyperNeatParams.SelectionProportion = 0.1;
			hyperNeatParams.OffspringAsexualProportion = 0.1;
			hyperNeatParams.OffspringSexualProportion = 0.1;
			hyperNeatParams.InterspeciesMatingProportion = 0.1;
			
			// create evolution algorithm
			_hyperNeatEvolutionAlgorithm = _hyperNeatExperiment.CreateEvolutionAlgorithm(_hyperNeatGenomeFactory, _hyperNeatGenomeList);
			_hyperNeatEvolutionAlgorithm.UpdateEvent += new EventHandler(_hyperNeatUpdateHandler);
			_hyperNeatEvolutionAlgorithm.StartContinue();
		}

		private void _hyperNeatUpdateHandler(object sender, EventArgs args)
		{
			// get the champion genome and fitness
			_hyperNeatChampGenome = _hyperNeatEvolutionAlgorithm.CurrentChampGenome;
			_hyperNeatChampGenomeFitness = _hyperNeatChampGenome.EvaluationInfo.Fitness;
			uint currentGeneration = _hyperNeatEvolutionAlgorithm.CurrentGeneration;
			System.Console.WriteLine("(Gen , BestFit) : (" + currentGeneration + " , " + _hyperNeatChampGenomeFitness + ")");
		}

        /// <summary>
        /// Sets up the Naive Bayes classifier for classifying faces using feature point locations
        /// </summary>
        public void setupNaiveBayesForPointLocations()
        {
            // Dictionary of file names
            nameMappings = new SortedDictionary<int, string>();

            // Find the number of files in C:\KinectLogin
            int faceCount = Directory.GetFiles(Face.faceFileDirectory3D).Length;

            // Counters
            int faceIndex = 0;
            int i = 0;

            // split array for lines
            string[] split;

            // The face model read in from the files
            SortedDictionary<FeaturePoint, Vector3DF> faceModel;

            // The translated face
            SortedDictionary<FeaturePoint, Vector3DF> face;

            // Inputs for naive bayes; The 3D facial data;
            // the set of [x, y] coordinates of the FeaturePoints
            // Todo: Establish number of columns from the data
            Matrix<float> trainData = new Matrix<float>(faceCount, 71 * 3);

            // Outputs for naive bayes: Index integers that correspond to a face file
            Matrix<int> trainClasses = new Matrix<int>(faceCount, 1);

            // Load each face in C:\KinectLogin
            foreach (string file in Directory.EnumerateFiles(Face.faceFileDirectory3D))
            {
                nameMappings.Add(faceIndex, ExtensionMethods.getNameFromFile(file));

                faceModel = new SortedDictionary<FeaturePoint, Vector3DF>();

                // Parse the face file into SortedDictionary<FeaturePoint, Vector3DF> face
                foreach (string line in File.ReadLines(file))
                {
                    if (line != null && !line.Trim().Equals(""))
                    {
                        split = line.Split(new Char[] { ':', ',' });

                        // Split contents:
                        // [0]: FeaturePoint as an int
                        // [1]: Vector3DF.X
                        // [2]: Vector3DF.Y
                        // [3]: Vector3DF.Z
                        if (split != null && split.Length >= 4)
                        {
                            faceModel.Add(((FeaturePoint)Convert.ToInt32(split[0])),
                                new Vector3DF(
                                    Convert.ToSingle(split[1]),
                                    Convert.ToSingle(split[2]),
                                    Convert.ToSingle(split[3])
                                )
                            );
                        }
                    }
                }

                /************** Naive Bayes Point Location Setup First Step **************/
                // Translate the facial data coordinates with respect to the midpoint between 
                // FeaturePoint.TopRightForehead and FeaturePoint.TopLeftForehead

                face = translateFace(faceModel);

                /************** Naive Bayes Point Location Setup Second Step **************/
                // Outputs: the file names
                trainClasses[faceIndex, 0] = faceIndex;

                // For each FeaturePoint
                i = 0;
                foreach (FeaturePoint featurePoint in face.Keys)
                {
                    if (face[featurePoint] != null)
                    {
                        trainData[faceIndex, i] = face[featurePoint].X;
                        trainData[faceIndex, i + 1] = face[featurePoint].Y;
                        trainData[faceIndex, i + 2] = face[featurePoint].Y;
                    }

                    i += 3;
                }

                faceIndex++;
            }

            /************** Naive Bayes Point Location Setup Third Step **************/
            // Train the classifier with the loaded face model data as inputs and the filenames as outputs
            classifier.Train(trainData, trainClasses, null, null, false);
        }


        /// <summary>
        /// Sets up the rTrees classifier for classifying faces using feature point locations
        /// </summary>
        public void setupRTrees()
        {
            // Dictionary of file names
            nameMappings = new SortedDictionary<int, string>();

            // Find the number of files in C:\KinectLogin
            int faceCount = Directory.GetFiles(Face.faceFileDirectory3D).Length;

            // Counters
            int faceIndex = 0;
            int i = 0;

            // split array for lines
            string[] split;

            // The face model read in from the files
            SortedDictionary<FeaturePoint, Vector3DF> faceModel;

            // The translated face
            SortedDictionary<FeaturePoint, Vector3DF> face;

            // Inputs for rTrees; The 3D facial data;
            // the set of [x, y] coordinates of the FeaturePoints
            // Todo: Establish number of columns from the data
            Matrix<float> trainData = new Matrix<float>(faceCount, 71 * 3);

            // Outputs for rTrees: Index integers that correspond to a face file
            Matrix<float> trainClasses = new Matrix<float>(faceCount, 1);

            // Load each face in C:\KinectLogin
            foreach (string file in Directory.EnumerateFiles(Face.faceFileDirectory3D))
            {
                nameMappings.Add(faceIndex, ExtensionMethods.getNameFromFile(file));

                faceModel = new SortedDictionary<FeaturePoint, Vector3DF>();

                // Parse the face file into SortedDictionary<FeaturePoint, Vector3DF> face
                foreach (string line in File.ReadLines(file))
                {
                    if (line != null && !line.Trim().Equals(""))
                    {
                        split = line.Split(new Char[] { ':', ',' });

                        // Split contents:
                        // [0]: FeaturePoint as an int
                        // [1]: Vector3DF.X
                        // [2]: Vector3DF.Y
                        // [3]: Vector3DF.Z
                        if (split != null && split.Length >= 4)
                        {
                            faceModel.Add(((FeaturePoint)Convert.ToInt32(split[0])),
                                new Vector3DF(
                                    Convert.ToSingle(split[1]),
                                    Convert.ToSingle(split[2]),
                                    Convert.ToSingle(split[3])
                                )
                            );
                        }
                    }
                }

                /************** rTrees Point Location Setup First Step **************/
                // Translate the facial data coordinates with respect to the midpoint between 
                // FeaturePoint.TopRightForehead and FeaturePoint.TopLeftForehead

                face = translateFace(faceModel);

                /************** rTrees Point Location Setup Second Step **************/
                // Outputs: the file names
                trainClasses[faceIndex, 0] = faceIndex;

                // For each FeaturePoint
                i = 0;
                foreach (FeaturePoint featurePoint in face.Keys)
                {
                    if (face[featurePoint] != null)
                    {
                        trainData[faceIndex, i] = face[featurePoint].X;
                        trainData[faceIndex, i + 1] = face[featurePoint].Y;
                        trainData[faceIndex, i + 2] = face[featurePoint].Y;
                    }

                    i += 3;
                }

                faceIndex++;
            }

            /************** rTrees Point Location Setup Third Step **************/
            // Train the classifier with the loaded face model data as inputs and the filenames as outputs
            rTrees.Train(trainData, Emgu.CV.ML.MlEnum.DATA_LAYOUT_TYPE.ROW_SAMPLE, trainClasses, null, null, null, null, MCvRTParams.GetDefaultParameter());
        }

        /// <summary>
        /// Sets up the SVM for classifying faces using feature point locations
        /// </summary>
        public void setupSVM()
        {
            // Dictionary of file names
            nameMappings = new SortedDictionary<int, string>();

            // Find the number of files in C:\KinectLogin
            int faceCount = Directory.GetFiles(Face.faceFileDirectory3D).Length;

            // Counters
            int faceIndex = 0;
            int i = 0;

            // split array for lines
            string[] split;

            // The face model read in from the files
            SortedDictionary<FeaturePoint, Vector3DF> faceModel;

            // The translated face
            SortedDictionary<FeaturePoint, Vector3DF> face;

            // Inputs for SVM; The 3D facial data;
            // the set of [x, y] coordinates of the FeaturePoints
            // Todo: Establish number of columns from the data
            Matrix<float> trainData = new Matrix<float>(faceCount, 71 * 3);

            // Outputs for SVM: Index integers that correspond to a face file
            Matrix<float> trainClasses = new Matrix<float>(faceCount, 1);

            // Load each face in C:\KinectLogin
            foreach (string file in Directory.EnumerateFiles(Face.faceFileDirectory3D))
            {
                nameMappings.Add(faceIndex, ExtensionMethods.getNameFromFile(file));

                faceModel = new SortedDictionary<FeaturePoint, Vector3DF>();

                // Parse the face file into SortedDictionary<FeaturePoint, Vector3DF> face
                foreach (string line in File.ReadLines(file))
                {
                    if (line != null && !line.Trim().Equals(""))
                    {
                        split = line.Split(new Char[] { ':', ',' });

                        // Split contents:
                        // [0]: FeaturePoint as an int
                        // [1]: Vector3DF.X
                        // [2]: Vector3DF.Y
                        // [3]: Vector3DF.Z
                        if (split != null && split.Length >= 4)
                        {
                            faceModel.Add(((FeaturePoint)Convert.ToInt32(split[0])),
                                new Vector3DF(
                                    Convert.ToSingle(split[1]),
                                    Convert.ToSingle(split[2]),
                                    Convert.ToSingle(split[3])
                                )
                            );
                        }
                    }
                }

                /************** SVM Point Location Setup First Step **************/
                // Translate the facial data coordinates with respect to the midpoint between 
                // FeaturePoint.TopRightForehead and FeaturePoint.TopLeftForehead

                face = translateFace(faceModel);

                /************** SVM Point Location Setup Second Step **************/
                // Outputs: the file names
                trainClasses[faceIndex, 0] = faceIndex;

                // For each FeaturePoint
                i = 0;
                foreach (FeaturePoint featurePoint in face.Keys)
                {
                    if (face[featurePoint] != null)
                    {
                        trainData[faceIndex, i] = face[featurePoint].X;
                        trainData[faceIndex, i + 1] = face[featurePoint].Y;
                        trainData[faceIndex, i + 2] = face[featurePoint].Y;
                    }

                    i += 3;
                }

                faceIndex++;
            }

            /************** SVM Point Location Setup Third Step **************/

            SVMParams p = new SVMParams();
            p.KernelType = Emgu.CV.ML.MlEnum.SVM_KERNEL_TYPE.LINEAR;
            p.SVMType = Emgu.CV.ML.MlEnum.SVM_TYPE.C_SVC;
            p.C = 5;
            p.TermCrit = new MCvTermCriteria(100, 0.00001);
 

            // Train the classifier with the loaded face model data as inputs and the filenames as outputs
            svm.Train(trainData, trainClasses, null, null, p);
        }

        /// <summary>
        /// Sets up the Naive Bayes classifier for classifying faces using feature point distances 
        /// (i.e. the distances between each FeaturePoint and the next Feature Point)
        /// </summary>
        public void setupNaiveBayesForPointDistances()
        {
            // Dictionary of file names
            nameMappings = new SortedDictionary<int, string>();

            // Find the number of files in C:\KinectLogin
            int faceCount = Directory.GetFiles(Face.faceFileDirectory3D).Length;

            // Counters
            int faceIndex = 0;

            // split array for lines
            string[] split;

            // The face model read in from the files
            SortedDictionary<FeaturePoint, Vector3DF> faceModel;

            // Inputs for naive bayes; The 3D facial data;
            // the set of [x, y] coordinates of the FeaturePoints
            // Todo: Establish number of columns from the data
            Matrix<float> trainData = new Matrix<float>(faceCount, 71);

            // Outputs for naive bayes: Index integers that correspond to a face file
            Matrix<int> trainClasses = new Matrix<int>(faceCount, 1);

            // Load each face in C:\KinectLogin
            foreach (string file in Directory.EnumerateFiles(Face.faceFileDirectory3D))
            {
                nameMappings.Add(faceIndex, ExtensionMethods.getNameFromFile(file));

                faceModel = new SortedDictionary<FeaturePoint, Vector3DF>();

                // Parse the face file into SortedDictionary<FeaturePoint, Vector3DF> face
                foreach (string line in File.ReadLines(file))
                {
                    if (line != null && !line.Trim().Equals(""))
                    {
                        split = line.Split(new Char[] { ':', ',' });

                        // Split contents:
                        // [0]: FeaturePoint as an int
                        // [1]: Vector3DF.X
                        // [2]: Vector3DF.Y
                        // [3]: Vector3DF.Z
                        if (split != null && split.Length >= 4)
                        {
                            faceModel.Add(((FeaturePoint)Convert.ToInt32(split[0])),
                                new Vector3DF(
                                    Convert.ToSingle(split[1]),
                                    Convert.ToSingle(split[2]),
                                    Convert.ToSingle(split[3])
                                )
                            );
                        }
                    }
                }

                /************** Naive Bayes Point Distance Setup First Step **************/
                // Outputs: the file names
                trainClasses[faceIndex, 0] = faceIndex;

                // Calculate the distance between each FPa and every other FPb
                int i;
                for (i = 0; i < faceModel.Keys.Count; i++)
                {
                    // Compare with the first point if we are at the last point of the keyset
                    if (i == faceModel.Keys.Count - 1)
                    {
                        trainData[faceIndex, i] = pointDistance3D(faceModel[faceModel.Keys.ElementAt(i)], faceModel[faceModel.Keys.ElementAt(0)]);
                    }
                    else
                    {
                        trainData[faceIndex, i] = pointDistance3D(faceModel[faceModel.Keys.ElementAt(i)], faceModel[faceModel.Keys.ElementAt(i + 1)]);
                    }
                }

                faceIndex++;
            }

            /************** Naive Bayes Point Location Setup Second Step **************/
            // Train the classifier with the loaded face model data as inputs and the filenames as outputs
            classifier.Train(trainData, trainClasses, null, null, false);
        }

        /// <summary>
        /// Translates the face data such that the midpoint on the forhead is now the origin in x, y, z space
        /// </summary>
        /// <param name="face">The face to translate</param>
        /// <returns>null if there is missing data in the face model; otherwise the translated face data</returns>
        public SortedDictionary<FeaturePoint, Vector3DF> translateFace(SortedDictionary<FeaturePoint, Vector3DF> face)
        {
            SortedDictionary<FeaturePoint, Vector3DF> translatedFace = new SortedDictionary<FeaturePoint, Vector3DF>();
            // No match if we can't translate WRT these FeaturePoints
            if (face[FeaturePoint.TopLeftForehead] == null ||
                face[FeaturePoint.TopLeftForehead] == null)
            {
                return null;
            }
            Vector3DF faceForeheadMidpoint = new Vector3DF(
                (face[FeaturePoint.TopLeftForehead].X + face[FeaturePoint.TopRightForehead].X) / 2,
                (face[FeaturePoint.TopLeftForehead].Y + face[FeaturePoint.TopRightForehead].Y) / 2,
                (face[FeaturePoint.TopLeftForehead].Z + face[FeaturePoint.TopRightForehead].Z) / 2);

            // Make foreheadMidpoint the origin
            // Subtract forheadMidpoint from each FeaturePoint to translate the coordinates
            foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
            {
                if (face[((FeaturePoint)i)] == null)
                {
                    return null;
                }

                translatedFace[((FeaturePoint)i)] = new Vector3DF(
                    face[((FeaturePoint)i)].X - faceForeheadMidpoint.X,
                    face[((FeaturePoint)i)].Y - faceForeheadMidpoint.Y,
                    face[((FeaturePoint)i)].Z - faceForeheadMidpoint.Z);

            }

            return translatedFace;
        }

        public SortedDictionary<FeaturePoint, Vector3DF> translateFace(EnumIndexableCollection<FeaturePoint, Vector3DF> face)
        {
            return translateFace(convertCollectionToDictionary(face));
        }

        public SortedDictionary<FeaturePoint, Vector3DF> convertCollectionToDictionary(EnumIndexableCollection<FeaturePoint, Vector3DF> face)
        {
            // Convert EnumIndexableCollection to a Dictionary and call the previous function
            SortedDictionary<FeaturePoint, Vector3DF> preparedFace = new SortedDictionary<FeaturePoint, Vector3DF>();

            foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
            {
                if (face[((FeaturePoint)i)] == null)
                {
                    return null;
                }

                preparedFace[((FeaturePoint)i)] = new Vector3DF(
                    face[i].X,
                    face[i].Y,
                    face[i].Z);
            }

            return preparedFace;
        }

        /// <summary>
        /// Calculates the 3D euclidean distance between the two points
        /// </summary>
        /// <param name="vector1">A 3D point</param>
        /// <param name="vector2">A 3D point</param>
        /// <returns></returns>
        public float pointDistance3D(Vector3DF point1, Vector3DF point2)
        {
            float diffX, diffY, diffZ;
            double distance;

            // calculate the difference between the two points
            diffX = point1.X - point2.X;
            diffY = point1.Y - point2.Y;
            diffZ = point1.Z - point2.Z;

            // calculate the Euclidean distance between the two points
            distance = Math.Sqrt(Math.Pow(diffX, 2) + Math.Pow(diffY, 2) + Math.Pow(diffZ, 2));

            return (float)distance;  // return the distance as a float
        }

		class HyperNeatExperiment : INeatExperiment
		{
			string _name;
			string _description;
			const int _inputCount = 71 * 3; // kinect provides 71 feature points
			const int _outputCount = 2; // weight and bias
			int _populationSize;
			int _specieCount;
			bool _lengthCppnInput;
			NeatEvolutionAlgorithmParameters _eaParams;
			NeatGenomeParameters _neatGenomeParams;
			NetworkActivationScheme _activationSchemeCppn;
			NetworkActivationScheme _activationScheme;
			string _complexityRegulationStr;
			int? _complexityThreshold;
			ParallelOptions _parallelOptions;
			int _visualFieldResolution;
			int _visualFieldPixelCount;
			SortedDictionary<FeaturePoint, Vector3DF> _trainingPoints;

			public HyperNeatExperiment(SortedDictionary<FeaturePoint, Vector3DF> facePoints)
			{
				// copy face points into the training points dictionary
				_trainingPoints = new SortedDictionary<FeaturePoint,Vector3DF>();
				foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
				{
					if (facePoints[((FeaturePoint)i)] != null)
					{
						_trainingPoints[((FeaturePoint)i)] = new Vector3DF(
							facePoints[((FeaturePoint)i)].X,
							facePoints[((FeaturePoint)i)].Y,
							facePoints[((FeaturePoint)i)].Z);
					}
				}
			}

			public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(IGenomeFactory<NeatGenome> genomeFactory, List<NeatGenome> genomeList)
			{
				// Create distance metric. Mismatched genes have a fixed distance of 10; for matched genes the distance is their weigth difference.
				IDistanceMetric distanceMetric = new ManhattanDistanceMetric(1.0, 0.0, 10.0);
				ISpeciationStrategy<NeatGenome> speciationStrategy = new ParallelKMeansClusteringStrategy<NeatGenome>(distanceMetric, _parallelOptions);

				// Create complexity regulation strategy.
				IComplexityRegulationStrategy complexityRegulationStrategy = ExperimentUtils.CreateComplexityRegulationStrategy(_complexityRegulationStr, _complexityThreshold);

				// Create the evolution algorithm.
				NeatEvolutionAlgorithm<NeatGenome> ea = new NeatEvolutionAlgorithm<NeatGenome>(_eaParams, speciationStrategy, complexityRegulationStrategy);

				// Create IBlackBox evaluator.
				HyperNeatEvaluator evaluator = new HyperNeatEvaluator();
				evaluator.SetTrainingPoints(_trainingPoints);

				// Create genome decoder.
				IGenomeDecoder<NeatGenome, IBlackBox> genomeDecoder = CreateGenomeDecoder();

				// Create a genome list evaluator. This packages up the genome decoder with the genome evaluator.
				IGenomeListEvaluator<NeatGenome> innerEvaluator = new ParallelGenomeListEvaluator<NeatGenome, IBlackBox>(genomeDecoder, evaluator, _parallelOptions);


				// Wrap the list evaluator in a 'selective' evaulator that will only evaluate new genomes. That is, we skip re-evaluating any genomes
				// that were in the population in previous generations (elite genomes). This is determined by examining each genome's evaluation info object.
				IGenomeListEvaluator<NeatGenome> selectiveEvaluator = new SelectiveGenomeListEvaluator<NeatGenome>(
																						innerEvaluator,
																						SelectiveGenomeListEvaluator<NeatGenome>.CreatePredicate_OnceOnly());
				// Initialize the evolution algorithm.
				ea.Initialize(selectiveEvaluator, genomeFactory, genomeList);

				// Finished. Return the evolution algorithm
				return ea;
			}

			public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm(int populationSize)
			{
				// Create a genome factory with our neat genome parameters object and the appropriate number of input and output neuron genes.
				IGenomeFactory<NeatGenome> genomeFactory = CreateGenomeFactory();

				// Create an initial population of randomly generated genomes.
				List<NeatGenome> genomeList = genomeFactory.CreateGenomeList(populationSize, 0);

				// Create evolution algorithm.
				return CreateEvolutionAlgorithm(genomeFactory, genomeList);
			}

			public NeatEvolutionAlgorithm<NeatGenome> CreateEvolutionAlgorithm()
			{
				return CreateEvolutionAlgorithm(_populationSize);
			}

			public IGenomeDecoder<NeatGenome, SharpNeat.Phenomes.IBlackBox> CreateGenomeDecoder()
			{
				// create HyperNEAT network substrate

				// for input layer
				SubstrateNodeSet inputLayer = new SubstrateNodeSet(_inputCount);

				// add all training points to the input layer
				uint id = 1;
				foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
				{
					FeaturePoint iFP = (FeaturePoint)i;
					if (_trainingPoints[iFP] != null)
					{
						Vector3DF iFpV3dF = _trainingPoints[iFP];
						inputLayer.NodeList.Add(new SubstrateNode(id++, new double[] {iFpV3dF.X, 0.0, 0.0}));
						inputLayer.NodeList.Add(new SubstrateNode(id++, new double[] {0.0, iFpV3dF.Y, 0.0}));
						inputLayer.NodeList.Add(new SubstrateNode(id++, new double[] { 0.0, 0.0, iFpV3dF.Z }));
					}
				}
				
				// create output layer - exactly one output: how far are we from a match?
				SubstrateNodeSet outputLayer = new SubstrateNodeSet(1);
				outputLayer.NodeList.Add(new SubstrateNode(id++, new double[] {0.0, 0.0, 0.0}));
				
				// create a hidden layer
				SubstrateNodeSet hiddenLayer = new SubstrateNodeSet(_inputCount / 3);
				foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
				{
					FeaturePoint iFP = (FeaturePoint)i;
					if (_trainingPoints[iFP] != null)
					{
						Vector3DF iFpV3dF = _trainingPoints[iFP];
						hiddenLayer.NodeList.Add(new SubstrateNode(id++, new double[] { iFpV3dF.X, iFpV3dF.Y, iFpV3dF.Z }));
					}
				}
				
				// connect layers
				List<SubstrateNodeSet> nodeSetList = new List<SubstrateNodeSet>(2);
				nodeSetList.Add(inputLayer);
				nodeSetList.Add(outputLayer);
				nodeSetList.Add(hiddenLayer);

				// map layers
				List<NodeSetMapping> nodeSetMappingList = new List<NodeSetMapping>();
				nodeSetMappingList.Add(NodeSetMapping.Create(0, 2, (double?)null)); // input -> hidden
				nodeSetMappingList.Add(NodeSetMapping.Create(2, 1, (double?)null)); // hidden -> output

				// Construct substrate.
				int activationFunctionId = 0;
				IActivationFunctionLibrary activationFunction = DefaultActivationFunctionLibrary.
					CreateLibraryNeat(SteepenedSigmoid.__DefaultInstance);
				double weightThreshold = 0.2;
				double maxWeight = 5;
				Substrate substrate = new Substrate(nodeSetList, activationFunction, activationFunctionId,
					weightThreshold, maxWeight, nodeSetMappingList);

				// Create genome decoder. Decodes to a neural network packaged with an activation scheme that
				// defines a fixed number of activations per evaluation.
				return new HyperNeatDecoder(substrate, _activationSchemeCppn, _activationScheme, _lengthCppnInput);
			}

			public IGenomeFactory<NeatGenome> CreateGenomeFactory()
			{
				return new CppnGenomeFactory(InputCount, OutputCount, GetCppnActivationFunctionLibrary(), _neatGenomeParams);
			}

			private IActivationFunctionLibrary GetCppnActivationFunctionLibrary()
			{
				return DefaultActivationFunctionLibrary.CreateLibraryCppn();
			}

			public int DefaultPopulationSize
			{
				get { return _populationSize; }
			}

			public string Description
			{
				get { return _description; }
			}

			public void Initialize(string name, System.Xml.XmlElement xmlConfig)
			{
				_name = name;
				_populationSize = 150;
				_specieCount = 10;
				_activationSchemeCppn = NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(4); // 4 iterations
				_activationScheme = NetworkActivationScheme.CreateCyclicFixedTimestepsScheme(1); // 1 iteration
				_complexityRegulationStr = "Relative";
				_complexityThreshold = 10;
				_description = "Whatev's";
				_parallelOptions = new ParallelOptions();

				_visualFieldResolution = _inputCount;
				_visualFieldPixelCount = _visualFieldResolution * _visualFieldResolution;
				_lengthCppnInput = false;

				_eaParams = new NeatEvolutionAlgorithmParameters();
				_eaParams.SpecieCount = _specieCount;
				_neatGenomeParams = new NeatGenomeParameters();
			}

			public int PopulationSize
			{
				get { return _populationSize; }
			}

			public int InputCount
			{
				get { return _lengthCppnInput ? 7 : 6; }
			}

			public List<NeatGenome> LoadPopulation(System.Xml.XmlReader xr)
			{
				NeatGenomeFactory genomeFactory = (NeatGenomeFactory)CreateGenomeFactory();
				return NeatGenomeXmlIO.ReadCompleteGenomeList(xr, false, genomeFactory);
			}

			public string Name
			{
				get { return _name ; }
			}

			public NeatEvolutionAlgorithmParameters NeatEvolutionAlgorithmParameters
			{
				get { return _eaParams; }
			}

			public NeatGenomeParameters NeatGenomeParameters
			{
				get { return _neatGenomeParams; }
			}

			public int OutputCount
			{
				get { return 2; }
			}

			public void SavePopulation(System.Xml.XmlWriter xw, IList<NeatGenome> genomeList)
			{
				NeatGenomeXmlIO.WriteComplete(xw, genomeList, true);
			}
		}

		/// <summary>
		/// Class to contain the HyperNEAT evaluator.
		/// </summary>
		class HyperNeatEvaluator : IPhenomeEvaluator<IBlackBox>
		{
			/// <summary>
			/// Number of evaluations.
			/// </summary>
			ulong _evalCount;

			/// <summary>
			/// Indicates if some stop condition has been achieved.
			/// </summary>
			bool _stopConditionSatisfied;

			/// <summary>
			/// Training points.
			/// </summary>
			ArrayList _trainingPoints;

			public void SetTrainingPoints(SortedDictionary<FeaturePoint, Vector3DF> points)
			{
				_trainingPoints = new ArrayList();
				foreach (int i in Enum.GetValues(typeof(FeaturePoint)))
				{
					FeaturePoint iFP = (FeaturePoint)i;
					if (points[iFP] != null)
					{
						Vector3DF iFpV3dF = points[iFP];
						_trainingPoints.Add(new Vector3DF(iFpV3dF.X, iFpV3dF.Y, iFpV3dF.Z));
					}
				}
			}


			/// <summary>
			/// Evaluate the provided IBlackBox against the problem domain and return its fitness score.
			/// </summary>
			public FitnessInfo Evaluate(IBlackBox box)
			{
				// reset the box
				box.ResetState();

				double stopCondition = 0.00001;
				double output;
				double fitness;
				ISignalArray inputArray = box.InputSignalArray;
				ISignalArray outputArray = box.OutputSignalArray;

				// increate the number of evaluations
				_evalCount++;

				// check how close the values in the blackbox are to the training values - if this comes out to 0, training is complete.
				int i = 0;
				foreach (Vector3DF vector in _trainingPoints)
				{
					inputArray[i++] = vector.X;
					inputArray[i++] = vector.Y;
					inputArray[i++] = vector.Z;
				}

				// activate the box
				box.Activate();
				if (!box.IsStateValid)
				{
					return FitnessInfo.Zero;
				}
				
				// get the output
				output = outputArray[0];
				if (output < stopCondition)
				{
					_stopConditionSatisfied = true;
				}
				fitness = 1 / output;
				System.Console.WriteLine("output: " + output + " fitness: " + fitness );

				return new FitnessInfo(fitness, fitness);
			}

			/// <summary>
			/// Gets the total number of evaluations that have been preformed.
			/// </summary>
			public ulong EvaluationCount
			{
				get { return _evalCount; }
			}

			/// <summary>
			/// Reset the internal states of the evaluation scheme if any exists.
			/// This problem domain has no internal state.
			/// </summary>
			public void Reset()
			{
			}

			/// <summary>
			/// Gets a value indicating whether some goal fitness has been achieved and that
			/// the the evolutionary algorithm/search should stop.
			/// </summary>
			public bool StopConditionSatisfied
			{
				get { return _stopConditionSatisfied; }
			}
		}
    }
}
