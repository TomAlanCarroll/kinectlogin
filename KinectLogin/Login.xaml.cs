using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Kinect;
using Microsoft.Kinect.Toolkit;

namespace KinectLogin
{
    /// <summary>
    /// Interaction logic for Login.xaml
    /// </summary>
    public partial class Login : Window
    {
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private WriteableBitmap colorImageWritableBitmap;
        private byte[] colorImageData;
        private ColorImageFormat currentColorImageFormat = ColorImageFormat.Undefined;
        private bool gesturesProvided, voiceProvided;
        private bool faceAuthenticated, gesturesAuthenticated, voiceAuthenticated;
        private bool successDialogShown;
        private Thread GestureAuthenticationThread = null;
        private Thread SpeechAuthenticationThread = null;
        private BackgroundWorker authenticateGestures = new BackgroundWorker();

        //private Queue<string> voiceInputTokenQueue;
        //private string voicePassword;
        private VoiceRecognition voiceRecognition;

        public Login()
        {
            InitializeComponent();

            // Show message for modes where no data was recorded
            if (KinectManager.getGestureSet() == null || 
                KinectManager.getGestureSet().getGestures() == null ||
                KinectManager.getGestureSet().getGestures().Count(g => g != null) == 0)
            {
                gesturesProvided = false;
                gesturesAuthenticated = true;
                this.gestureRecognitionAuthenticationStatus.Text = "Not Provided";
                this.gestureRecognitionAuthenticationStatus.Foreground = Brushes.Black;
            }
            else
            {
                gesturesProvided = true;

                authenticateGestures.WorkerSupportsCancellation = true;
                authenticateGestures.WorkerReportsProgress = true;

                authenticateGestures.DoWork += new DoWorkEventHandler(authenticateGestures_DoWork);
                authenticateGestures.RunWorkerCompleted += new RunWorkerCompletedEventHandler(authenticateGestures_RunWorkerCompleted);
                authenticateGestures.ProgressChanged += new ProgressChangedEventHandler(authenticateGestures_ProgressChanged);
            }

            if (KinectManager.getVoicePassword() != null &&
                    KinectManager.getVoicePassword().getVoiceData() != null &&
                    KinectManager.getVoicePassword().getVoiceData().Count > 0)
            {
                voiceProvided = true;

                voiceRecognition = new VoiceRecognition();

                voiceRecognition.record(true, 0, KinectManager.UpdateVoiceData);
            }
            else
            {
                voiceProvided = false;
                voiceAuthenticated = true;
                this.voiceRecognitionAuthenticationStatus.Text = "Not Provided";
                this.voiceRecognitionAuthenticationStatus.Foreground = Brushes.Black;
            }

            var faceTrackingViewerBinding = new Binding("Kinect") { Source = sensorChooser };
            faceTrackingViewer.SetBinding(FaceTrackingViewer.KinectProperty, faceTrackingViewerBinding);

            sensorChooser.KinectChanged += SensorChooserOnKinectChanged;

            sensorChooser.Start();

            //voiceInputTokenQueue = new Queue<string>();

            //if (KinectManager.getVoicePassword() != null && KinectManager.getVoicePassword().getVoiceData() != null)
            //{
            //    int i;
            //    voicePassword = "";
            //    for (i = 0; i < KinectManager.getVoicePassword().getVoiceData().Count; i++)
            //    {
            //        if (KinectManager.getVoicePassword().getVoiceData()[i] != null)
            //        {
            //            voicePassword += KinectManager.getVoicePassword().getVoiceData()[i].ToLowerInvariant() + " ";
            //        }
            //    }
            //}



            //KinectHelper.speechEngine.SpeechRecognized += speechEngine_PasswordCheck;

        }

        //private void speechEngine_PasswordCheck(object sender, Microsoft.Speech.Recognition.SpeechRecognizedEventArgs e)
        //{
        //    // Speech utterance confidence below which we treat speech as if it hadn't been heard
        //    const double ConfidenceThreshold = 0.3;

        //    if (e.Result.Confidence >= ConfidenceThreshold)
        //    {
        //        if (voiceInputTokenQueue.Count >= Voice.MAX_PASSWORD_TOKENS)
        //        {
        //            voiceInputTokenQueue.Dequeue();
        //            voiceInputTokenQueue.Enqueue(e.Result.Semantics.Value.ToString());
        //        }
        //        else
        //        {
        //            voiceInputTokenQueue.Enqueue(e.Result.Semantics.Value.ToString());
        //        }

        //        this.voiceRecorded.Text = "";
        //        foreach (String token in voiceInputTokenQueue)
        //        {
        //            this.voiceRecorded.Text += token + " ";
        //        }

        //        // see if queue contains password
        //        int i;
        //        string voicePasswordQueue = "";
        //        for (i = 0; i < voiceInputTokenQueue.Count; i++)
        //        {
        //            if (voiceInputTokenQueue.ElementAt(i) != null)
        //            {
        //                voicePasswordQueue += voiceInputTokenQueue.ElementAt(i).ToLowerInvariant() + " ";
        //            }
        //        }

        //        if (voicePasswordQueue.Contains(voicePassword))
        //        { // Match
        //            voiceAuthenticated = true;

        //            // Stop the recording
        //            voiceRecognition.record(false, 0, KinectManager.UpdateVoiceData);
        //        }
        //    }
        //}

        private void SensorChooserOnKinectChanged(object sender, KinectChangedEventArgs kinectChangedEventArgs)
        {
            KinectSensor oldSensor = kinectChangedEventArgs.OldSensor;
            KinectSensor newSensor = kinectChangedEventArgs.NewSensor;

            if (oldSensor != null)
            {
                oldSensor.AllFramesReady -= KinectSensorOnAllFramesReady;
                oldSensor.ColorStream.Disable();
                oldSensor.DepthStream.Disable();
                oldSensor.DepthStream.Range = DepthRange.Default;
                oldSensor.SkeletonStream.Disable();
                oldSensor.SkeletonStream.EnableTrackingInNearRange = false;
                oldSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Default;
            }

            if (newSensor != null)
            {
                try
                {
                    newSensor.ColorStream.Enable(ColorImageFormat.RgbResolution640x480Fps30);
                    newSensor.DepthStream.Enable(DepthImageFormat.Resolution320x240Fps30);
                    try
                    {
                        // This will throw on non Kinect For Windows devices.
                        newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        newSensor.DepthStream.Range = DepthRange.Default;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = false;
                    }

                    newSensor.SkeletonStream.TrackingMode = SkeletonTrackingMode.Seated;
                    newSensor.SkeletonStream.Enable();
                    newSensor.AllFramesReady += KinectSensorOnAllFramesReady;
                }
                catch (InvalidOperationException)
                {
                    // This exception can be thrown when we are trying to
                    // enable streams on a device that has gone away.  This
                    // can occur, say, in app shutdown scenarios when the sensor
                    // goes away between the time it changed status and the
                    // time we get the sensor changed notification.
                    //
                    // Behavior here is to just eat the exception and assume
                    // another notification will come along if a sensor
                    // comes back.
                }
            }
        }

        private void WindowClosed(object sender, EventArgs e)
        {
            sensorChooser.Stop();
            faceTrackingViewer.Dispose();
            if(GestureAuthenticationThread != null)
                GestureAuthenticationThread.Abort();
            if (SpeechAuthenticationThread != null)
                SpeechAuthenticationThread.Abort();
            System.Environment.Exit(0);
        }

        public void stopKinect()
        {
            sensorChooser.Stop();
            faceTrackingViewer.Dispose();
        }

        private void authenticateGestures_DoWork(object sender, DoWorkEventArgs e)
        {
            int i;
            int numGestures = KinectHelper.getNumberOfGestures();
            int numSeconds = KinectHelper.getNumberOfSeconds();
            String statusText;

            // Initialize the candidate gestures (or overwrite the previously recorded gestures)
            KinectManager.getGestureSet().setCandidateGestures(new Gesture[KinectHelper.getNumberOfGestures()]);

            // Show an error message if there has been a failed previous attempt
            if (KinectManager.getGesturesFailed())
            {
                System.Console.WriteLine("The gestures are incorrect. Please try again.");
                statusText = "The gestures are incorrect. Please try again.";
                authenticateGestures.ReportProgress(0, statusText);
                Thread.Sleep(2000);    
            }

            // Record the number of gestures provided previously
            for (i = 0; i < numGestures; i++)
            {
                System.Console.WriteLine("Recording gesture");
                statusText = "Recording gesture in 3";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += " 2";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += " 1";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText += ".";
                authenticateGestures.ReportProgress(i, statusText);
                Thread.Sleep(250);

                statusText = "Recording";
                authenticateGestures.ReportProgress(i, statusText);
                KinectHelper.startRecording();
                ExtensionMethods.timer(numSeconds);

                KinectManager.getGestureSet().setCandidateGesture(i, KinectHelper.stopRecording());

                System.Console.WriteLine("Finished Recording\n");
            }

            e.Cancel = true;
        }

        private void authenticateGestures_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            if (e != null && e.UserState != null)
            {
                this.gestureRecognitionAuthenticationStatus.Text = e.UserState.ToString();
            }
        }

        private void authenticateGestures_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            int numGestures = KinectHelper.getNumberOfGestures(), i;
            bool unionAuthenticationStatus = true;

            if ((e.Cancelled == true))
            {
                // Compare the candidate gestures against the valid gestures
                // If any do not match, set the union authentication status to false and exit the loop
                for (i = 0; i < numGestures; i++)
                {
                    if (KinectManager.getGestureSet().compare(KinectManager.getGestureSet().getCandidateGestures()[i],
                        KinectManager.getGestureSet().getGestures()[i]) == false)
                    {
                        unionAuthenticationStatus = false;
                        break;
                    }
                }

                if (unionAuthenticationStatus)
                {
                    gesturesAuthenticated = true;
                    this.gestureRecognitionAuthenticationStatus.Text = "Authenticated";
                    this.gestureRecognitionAuthenticationStatus.Foreground = Brushes.Green;

                    KinectManager.setGesturesFailed(false);
                }
                else
                {
                    KinectManager.setGesturesFailed(true);
                }
            }
            else if (!(e.Error == null))
            {
                this.gestureRecognitionAuthenticationStatus.Text = ("Error: " + e.Error.Message);
            }
            else
            {
                this.gestureRecognitionAuthenticationStatus.Text = "Error!";
            }
        }

        private void KinectSensorOnAllFramesReady(object sender, AllFramesReadyEventArgs allFramesReadyEventArgs)
        {
            using (var colorImageFrame = allFramesReadyEventArgs.OpenColorImageFrame())
            {
                if (colorImageFrame == null)
                {
                    return;
                }

                // Make a copy of the color frame for displaying.
                var haveNewFormat = this.currentColorImageFormat != colorImageFrame.Format;
                if (haveNewFormat)
                {
                    this.currentColorImageFormat = colorImageFrame.Format;
                    this.colorImageData = new byte[colorImageFrame.PixelDataLength];
                    this.colorImageWritableBitmap = new WriteableBitmap(
                        colorImageFrame.Width, colorImageFrame.Height, 96, 96, PixelFormats.Bgr32, null);
                    ColorImage.Source = this.colorImageWritableBitmap;
                }

                colorImageFrame.CopyPixelDataTo(this.colorImageData);
                this.colorImageWritableBitmap.WritePixels(
                    new Int32Rect(0, 0, colorImageFrame.Width, colorImageFrame.Height),
                    this.colorImageData,
                    colorImageFrame.Width * Bgr32BytesPerPixel,
                    0);
            }

            // Check the facial recognition
            if (!faceAuthenticated && faceTrackingViewer.getFaceModel() != null)
            {
                bool matched = KinectManager.CompareFaces(faceTrackingViewer.getFaceImage(), faceTrackingViewer.getFaceModel());

                if (matched)
                {
                    // Increment the progress bar
                    faceProgressBar.Value = faceProgressBar.Value + 1;

                    if (faceProgressBar.Value == 100)
                    {
                        faceAuthenticated = true;
                        this.facialRecognitionAuthenticationStatus.Text = "Authenticated";
                        this.facialRecognitionAuthenticationStatus.Foreground = Brushes.Green;
                    }
                }
                else if (faceProgressBar.Value > 0)
                {
                    // Decrement the progress bar
                    faceProgressBar.Value = faceProgressBar.Value - 1;
                }
            }

            // Optional:
            // Check the gesture recognition AFTER facial recognition
            if (!gesturesAuthenticated && gesturesProvided && faceAuthenticated)
            {
                if (authenticateGestures.IsBusy != true)
                {                    
                    authenticateGestures.RunWorkerAsync();
                }
            }

            // Optional:
            // Check the voice recognition all the time
            if (!voiceAuthenticated && voiceProvided)
            {
                bool matched = false;
                if (SpeechAuthenticationThread == null)
                {
                    SpeechAuthenticationThread = new Thread(new ThreadStart(KinectManager.getVoiceRecognition().Authenticate));
                    SpeechAuthenticationThread.Start();
                }
                else if (!SpeechAuthenticationThread.IsAlive)
                {
                    matched = KinectManager.getVoiceRecognition().compare(KinectHelper.voice, KinectManager.getVoicePassword());
                    SpeechAuthenticationThread = null;
                }

                if (matched)
                {
                    voiceAuthenticated = true;
                    this.voiceRecognitionAuthenticationStatus.Text = "Authenticated";
                    this.voiceRecognitionAuthenticationStatus.Foreground = Brushes.Green;

                    this.voiceRecorded.Text = "***********";
                } 
                else
                {
                    this.voiceRecorded.Text = "";
                    foreach (String token in KinectHelper.voice.getVoiceData())
                    {
                        this.voiceRecorded.Text += token + " ";
                    }
                }
            }

            // Check that all modes are authenticated
            if (faceAuthenticated && gesturesAuthenticated && voiceAuthenticated)
            {
                //this.stopKinect();

                if (!successDialogShown)
                {
                    successDialogShown = true;
                    MessageBox.Show("Access Granted.", "Notice");
                }
            }
        }

        public void reset()
        {
            // Reset the progress bar
            faceProgressBar.Value = 0;

            faceAuthenticated = false;
            this.facialRecognitionAuthenticationStatus.Text = "Not Authenticated";
            this.facialRecognitionAuthenticationStatus.Foreground = Brushes.Red;

            // Optional: 
            if (gesturesProvided)
            {
                gesturesAuthenticated = false;
                this.gestureRecognitionAuthenticationStatus.Text = "Not Authenticated";
                this.gestureRecognitionAuthenticationStatus.Foreground = Brushes.Red;
            }
            
            // Optional: 
            if (voiceProvided)
            {
                voiceAuthenticated = false;
                this.voiceRecognitionAuthenticationStatus.Text = "Not Authenticated";
                this.voiceRecognitionAuthenticationStatus.Foreground = Brushes.Red;

                // Reset the voice
                KinectHelper.voice = new Voice();
            }

            successDialogShown = false;
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            reset();
        }
    }
}
