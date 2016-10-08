using Microsoft.Kinect.Toolkit;
using Microsoft.Kinect.Toolkit.FaceTracking;
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
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Emgu.CV;
using Emgu.CV.Structure;

namespace KinectLogin
{
	class _2dHyperNeatEvaluator : IPhenomeEvaluator<IBlackBox>
	{
		/// <summary>
		/// Number of evaluations.
		/// </summary>
		ulong _evalCount = 0;

		/// <summary>
		/// Indicates if some stop condition has been achieved.
		/// </summary>
		bool _stopConditionSatisfied = false;

		/// <summary>
		/// Training images.
		/// </summary>
		ArrayList _trainingFaceImages;

		/// <summary>
		/// Set the training images.
		/// </summary>
		public ArrayList TrainingFaceImages
		{
			set { _trainingFaceImages = value; }
		}

		/// <summary>
		/// Evaluate the provided IBlackBox against the problem domain and return its fitness score.
		/// </summary>
		public FitnessInfo Evaluate(IBlackBox box)
		{
            int numberCorrect = 0;
            int numberPointsCorrect = 0;
            double localFitness = 0.0;
			//int numberCorrect = 0;
			double fitness = 0;
			ISignalArray inputArray = box.InputSignalArray;
			ISignalArray outputArray = box.OutputSignalArray;

			// increase the number of evaluations
			_evalCount++;

			// for each training face image
			foreach (Image<Gray, Byte> faceImage in _trainingFaceImages)
			{
                numberPointsCorrect = 0;
                localFitness = 0.0;

				// reset the box
				box.ResetState();

                faceImage._EqualizeHist();

                // push the eigenvectors into the black box
				int i = 0;
                Image<Gray, float> pca = ExtensionMethods.CalculatePCAofImage(faceImage, 2);
                int numRows = pca.Rows;
                int numCols = pca.Cols;
                int numPixels = numRows * numCols;

                pca.Save(@"C:\KinectLogin\2D\pca" + i++ + ".png");

                int inputIndex = 0;
                for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
                {
                    for (int colIndex = 0; colIndex < numCols; colIndex++)
                    {
                        inputArray[inputIndex] = pca.Data[rowIndex, colIndex, 0];
                        inputIndex++;
                    }
                }

				// activate the box
				box.Activate();
				if (!box.IsStateValid)
				{
					return FitnessInfo.Zero;
				}

                // check whether or not this evalutation passed the test
                if (outputArray[0] > 0.90)
                {
                    numberPointsCorrect++;
                    _stopConditionSatisfied = true;
                }
			}

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
