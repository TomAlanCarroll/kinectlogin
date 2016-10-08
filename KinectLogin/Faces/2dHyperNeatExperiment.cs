using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using System.Threading.Tasks;
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
using System.Collections;
using Emgu.CV;
using Emgu.CV.Structure;

namespace KinectLogin
{
    class _2dHyperNeatExperiment : INeatExperiment
	{
		string _name;
		string _description;
		const int _inputCount = 400; // 20x20 pixels
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
		ArrayList _trainingFaceImage;

		public _2dHyperNeatExperiment(ArrayList trainingFaceImage)
		{
			this._trainingFaceImage = trainingFaceImage;
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
            _2dHyperNeatEvaluator evaluator = new _2dHyperNeatEvaluator();
            evaluator.TrainingFaceImages = _trainingFaceImage;

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
            int numRows = ((Image<Gray, Byte>)_trainingFaceImage[0]).Rows;
            int numCols = ((Image<Gray, Byte>)_trainingFaceImage[0]).Cols;
            int numPixels = numRows * numCols;
			// create HyperNEAT network substrate

			// for input layer
            SubstrateNodeSet inputLayer = new SubstrateNodeSet(numPixels);
            // for hidden layer
            SubstrateNodeSet hiddenLayer = new SubstrateNodeSet(numPixels);

			// add all training points to the input layer
			uint id = 1;
            for (int i = 0; i < _trainingFaceImage.Count; i++)
			{
                for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
                {
                    for (int colIndex = 0; colIndex < numCols; colIndex++)
                    {
                        inputLayer.NodeList.Add(new SubstrateNode(id++, 
                            new double[] { 
                                rowIndex, colIndex, 0
                            }));
                    }
                }
			}

            for (int rowIndex = 0; rowIndex < numRows; rowIndex++)
            {
                for (int colIndex = 0; colIndex < numCols; colIndex++)
                {
                    // add to hidden layer
                    hiddenLayer.NodeList.Add(new SubstrateNode(id++, new double[] { rowIndex, colIndex, 0 }));
                }
            }

			// create output layer - one output for each input pixel
            SubstrateNodeSet outputLayer = new SubstrateNodeSet();
            outputLayer.NodeList.Add(new SubstrateNode(id++, new double[] { 0, 0, 0 }));


			// connect layers
			List<SubstrateNodeSet> nodeSetList = new List<SubstrateNodeSet>();
            nodeSetList.Add(inputLayer);
            nodeSetList.Add(hiddenLayer);
			nodeSetList.Add(outputLayer);

			// map layers
			List<NodeSetMapping> nodeSetMappingList = new List<NodeSetMapping>();
            //nodeSetMappingList.Add(NodeSetMapping.Create(0, 1, (double?)null)); // input -> output
            nodeSetMappingList.Add(NodeSetMapping.Create(0, 1, (double?)null)); // input -> hidden
            nodeSetMappingList.Add(NodeSetMapping.Create(1, 2, (double?)null)); // hidden -> output

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
			_description = "2D Face Recognition";
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
			get { return _name; }
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
}
