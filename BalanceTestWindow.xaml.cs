﻿namespace FragilityTests
{
    using FragilityTests.FrailtyTests;
    using Microsoft.Kinect;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;

    /// <summary>
    /// Lógica de interacción para BalanceTestWindow.xaml
    /// </summary>
    public partial class BalanceTestWindow : Window
    {
        /// Active Kinect sensor
        private KinectSensor kinectSensor = null;

        /// kinect reader for color, depth, body, etc, frames
        MultiSourceFrameReader reader;

        /// Bitmap to display
        private WriteableBitmap colorBitmap = null;

        private Body[] bodies = null;

        private FrailtyTest frailtyTest;

        public BalanceTestWindow()
        {
            kinectSensor = KinectSensor.GetDefault();
            kinectSensor.Open();

            reader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color |
                                                        FrameSourceTypes.Body);
            reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            DataContext = this;

            frailtyTest = new BalanceTest();

            InitializeComponent();

            UpdateTestStatus();
        }

        /// Gets the bitmap to display
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        /// Execute shutdown tasks
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void BalanceTestWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.reader != null)
            {
                // BodyFrameReader is IDisposable
                this.reader.Dispose();
                this.reader = null;
            }

            if (this.kinectSensor != null)
            {
                this.kinectSensor.Close();
                this.kinectSensor = null;
            }

            frailtyTest.Close();

        }

        /// Handles the body frame data and color frame data arriving from the sensor
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            bool dataReceived = false;
            var reference = e.FrameReference.AcquireFrame();

            using (BodyFrame bodyFrame = reference.BodyFrameReference.AcquireFrame())
            {
                if (bodyFrame != null)
                {
                    if (this.bodies == null)
                    {
                        this.bodies = new Body[bodyFrame.BodyCount];
                    }

                    // The first time GetAndRefreshBodyData is called, Kinect will allocate each Body in the array.
                    bodyFrame.GetAndRefreshBodyData(this.bodies);
                    dataReceived = true;
                }
            }

            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = reference.ColorFrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    FrameDescription colorFrameDescription = colorFrame.FrameDescription;

                    using (KinectBuffer colorBuffer = colorFrame.LockRawImageBuffer())
                    {
                        this.colorBitmap.Lock();

                        // verify data and write the new color frame data to the display bitmap
                        if ((colorFrameDescription.Width == this.colorBitmap.PixelWidth) && (colorFrameDescription.Height == this.colorBitmap.PixelHeight))
                        {
                            colorFrame.CopyConvertedFrameDataToIntPtr(
                                this.colorBitmap.BackBuffer,
                                (uint)(colorFrameDescription.Width * colorFrameDescription.Height * 4),
                                ColorImageFormat.Bgra);

                            this.colorBitmap.AddDirtyRect(new Int32Rect(0, 0, this.colorBitmap.PixelWidth, this.colorBitmap.PixelHeight));
                        }

                        this.colorBitmap.Unlock();
                    }
                }
            }

            if (dataReceived)
            {
                foreach (Body body in this.bodies)
                {
                    if (body.IsTracked)
                    {
                        IReadOnlyDictionary<JointType, Joint> joints = body.Joints;
                        EvaluateTheBalanceTest(joints);
                        break;
                    }

                }
            }

        }

        private void EvaluateTheBalanceTest(IReadOnlyDictionary<JointType, Joint> joints)
        {
            frailtyTest.EvaluateFrailtyTest(joints);
            DisplayDebugValues();
            UpdateTestStatus();
        }


        private void DisplayDebugValues()
        {
            string[] debugValues = frailtyTest.GetDebugValues();
            WalkingTime.Text = "Tiempo: " + debugValues[0];
        }

        private void UpdateTestStatus()
        {
            string currentStatus = frailtyTest.GetCurrentStatus();
            StatusTest.Text = currentStatus;
        }

    }
}
