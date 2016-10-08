namespace KinectLogin
{
    using System;
    using System.Threading;
    using System.Windows;
    using System.Windows.Data;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using Microsoft.Kinect.Toolkit;

    /// <summary>
    /// Interaction logic for FacialRecognitionWindow.xaml
    /// </summary>
    public partial class FacialRecognitionWindow : Window
    {
        private static readonly int Bgr32BytesPerPixel = (PixelFormats.Bgr32.BitsPerPixel + 7) / 8;
        private readonly KinectSensorChooser sensorChooser = new KinectSensorChooser();
        private WriteableBitmap colorImageWritableBitmap;
        private byte[] colorImageData;
        private ColorImageFormat currentColorImageFormat = ColorImageFormat.Undefined;
        private bool savedDataExists;

        public FacialRecognitionWindow()
        {
            InitializeComponent();

            var faceTrackingViewerBinding = new Binding("Kinect") { Source = sensorChooser };
            faceTrackingViewer.SetBinding(FaceTrackingViewer.KinectProperty, faceTrackingViewerBinding);

            sensorChooser.KinectChanged += SensorChooserOnKinectChanged;

            sensorChooser.Start();
        }

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
                        //newSensor.DepthStream.Range = DepthRange.Near;
                        newSensor.SkeletonStream.EnableTrackingInNearRange = true;
                    }
                    catch (InvalidOperationException)
                    {
                        //newSensor.DepthStream.Range = DepthRange.Default;
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
            if (sensorChooser != null && sensorChooser.Status == ChooserStatus.SensorStarted)
            {
                sensorChooser.Stop();
            }
            if (faceTrackingViewer != null)
            {
                faceTrackingViewer.Dispose();
            }
        }

        public void stopKinect()
        {
            if (sensorChooser != null && sensorChooser.Status == ChooserStatus.SensorStarted)
            {
                sensorChooser.Stop();
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

                if (this.savedDataExists && faceTrackingViewer.getFaceModel() != null)
                {
                    bool matched = KinectManager.CompareFaces(faceTrackingViewer.getFaceImage(), faceTrackingViewer.getFaceModel());

                    if (matched)
                    {
                        this.matchStatus.Text = "Current face matches saved face.";
                        this.matchStatus.Foreground = Brushes.Green;
                    }
                    else
                    {
                        this.matchStatus.Text = "Current face DOES NOT match.";
                        this.matchStatus.Foreground = Brushes.Red;
                    }
                }
                else if (savedDataExists)
                {
                    this.matchStatus.Text = "Could not locate face.";
                    this.matchStatus.Foreground = Brushes.Red;
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            // Set the identification algorithm
            if (Naive_Bayes_Point_Locations.IsChecked == true)
            {
                KinectManager.setFaceComparisonType(IdentificationAlgorithms.FaceComparisonType.Naive_Bayes_Point_Locations);
            }
            else if (Random_Trees_Point_Locations.IsChecked == true)
            {
                KinectManager.setFaceComparisonType(IdentificationAlgorithms.FaceComparisonType.Random_Trees_Point_Locations);
            }
            else if (Support_Vector_Machine_Distances.IsChecked == true)
            {
                KinectManager.setFaceComparisonType(IdentificationAlgorithms.FaceComparisonType.Support_Vector_Machine_Locations);
            }
            else if (Naive_Bayes_Point_Distances.IsChecked == true)
            {
                KinectManager.setFaceComparisonType(IdentificationAlgorithms.FaceComparisonType.Naive_Bayes_Point_Distances);
            }
            else if (Threshold_All_Distances_Between_All_Feature_Points.IsChecked == true)
            {
                KinectManager.setFaceComparisonType(IdentificationAlgorithms.FaceComparisonType.Threshold_All_Distances_Between_All_Feature_Points);
            }
            else if (HyperNEAT.IsChecked == true)
            {
                KinectManager.setFaceComparisonType(IdentificationAlgorithms.FaceComparisonType.HyperNEAT);
            }

            KinectHelper.setName(this.name.Text);

            if (faceTrackingViewer.getFaceModel() != null)
            {
                this.faceStatus.Text = "Saving...";

                KinectManager.SaveFace(faceTrackingViewer.getFaceImage(), faceTrackingViewer.getFaceModel());

                this.faceStatus.Text = "Face was saved.";

                this.savedDataExists = true;

                this.continueButton.IsEnabled = true;
            }
        }

        private void Finish_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
