﻿namespace FragilityTests
{

    using System.ComponentModel;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Imaging;
    using Microsoft.Kinect;
    using System.IO;
    using System;

    public partial class GaitSpeedTestWindow : Window
    {
        /// Active Kinect sensor
        private KinectSensor kinectSensor = null;

        /// kinect reader for color, depth, body, etc, frames
        MultiSourceFrameReader reader;

        /// Bitmap to display
        private WriteableBitmap colorBitmap = null;

        private Body[] bodies = null;

        private const float startLine = 4.3f;   // Ideal: 4.3f

        private const float endLine = 0.8f;     // Ideal: 0.8f

        private float walkingTime = 0f;

        private Stopwatch stopWatch = null;

        private bool testIsRunning = false;

        private bool wasInStartZone = false;

        private bool bodyTooFar = false;

        private bool bodyOutOfRange = false;

        private BodyJointsPosition bodyJointsPosition;

        private StreamWriter streamWriter = new StreamWriter("Results.csv");

        private const string initStatus = "Para empezar el test, alejese de la cámara";

        private const string readyStatus = "Perfecto, ahora camine hacia la cámara";

        private const string transitionStatus = "Siga caminando hacia la cámara.";

        private const string endStatus = "¡Genial! Ha terminado el test. ¡Muchas gracias!";

        private const string errorStatus = "¡Ups! Ha ocurrido algún problema, pruebe a reiniciar el sistema.";

        private const string outOfRangeStatus = "Se encuentra demasiado alejado, acérquese a la cámara.";

        public GaitSpeedTestWindow()
        {
            kinectSensor = KinectSensor.GetDefault();
            kinectSensor.Open();

            reader = kinectSensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color |
                                                        FrameSourceTypes.Body);
            reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);


            DataContext = this;

            bodyJointsPosition = new BodyJointsPosition();

            stopWatch = Stopwatch.StartNew();
            stopWatch.Reset();

            InitializeComponent();

            UpdateTestStatus(initStatus);
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
        private void GaitSpeedTestWindow_Closing(object sender, CancelEventArgs e)
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

            if (this.streamWriter != null)
            {
                streamWriter.Close();
                streamWriter = null;
            }
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
                        EvaluateTheGaitSpeedTest(joints);
                        break;
                    }

                }
            }

        }

        private void EvaluateTheGaitSpeedTest(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Get the body depth
            float bodyDepth = GetBodyDepth(joints);
                        
            /// Si completa el test
            if (BodyInEndZone(bodyDepth) && testIsRunning)
            {
                stopWatch.Stop();
                UpdateWalkingTime();
                testIsRunning = false;
                bodyJointsPosition.AddBodyStatus(walkingTime, joints);
                SaveOnFileTestResults();
                bodyJointsPosition.ClearBodyStatus();
                UpdateTestStatus(endStatus);
            }
            /// Si esta en medio del test
            else if (BodyInTransitionZone(bodyDepth) && testIsRunning)
            {
                UpdateWalkingTime();
                bodyJointsPosition.AddBodyStatus(walkingTime, joints);
            }
            /// Si acaba de empezar el test
            else if (BodyInTransitionZone(bodyDepth) && wasInStartZone)
            {
                stopWatch.Start();
                UpdateWalkingTime();
                testIsRunning = true;
                wasInStartZone = false;
                bodyJointsPosition.AddBodyStatus(walkingTime, joints);
                UpdateTestStatus(transitionStatus);
            }
            /// Si estaba haciendo el test pero vuelve a empezar
            else if (BodyInStartZone(bodyDepth) && testIsRunning)
            {
                stopWatch.Reset();
                UpdateWalkingTime();
                testIsRunning = false;
                wasInStartZone = true;
                bodyJointsPosition.ClearBodyStatus();
                UpdateTestStatus(readyStatus);
            }
            /// Si esta preparado para empezar el test
            else if (BodyInStartZone(bodyDepth) && !wasInStartZone)
            {
                stopWatch.Reset();
                UpdateWalkingTime();
                wasInStartZone = true;
                UpdateTestStatus(readyStatus);
            }

            /// Display values
            DisplayDebugValues();
            DisplayBodyDistance(bodyDepth);
        }

        private float GetBodyDepth(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Inicialmente devuelvo la media
            /// Lo ideal sería descartar los valores que se alejen mucho 
            float midSpineDepth = joints[JointType.SpineMid].Position.Z;
            float baseSpineDepth = joints[JointType.SpineBase].Position.Z;
            float shoulderSpineDepth = joints[JointType.SpineShoulder].Position.Z;
            float headDepth = joints[JointType.Head].Position.Z;
            float mean = (headDepth+ midSpineDepth + baseSpineDepth + shoulderSpineDepth) / 4;
            EvaluateBodyDepth(mean);
            if (bodyOutOfRange)
            {
                UpdateTestStatus(outOfRangeStatus);
                return float.PositiveInfinity;
            }
            return mean;
        }
        
        private void EvaluateBodyDepth(float currrentBodyDepth)
        {

            // Fuera de rango:
            if (bodyTooFar && currrentBodyDepth <= 1f)
            {
                bodyOutOfRange = true;
                bodyTooFar = false;
            }
            // Deja de estar fuera de rango
            else if (bodyOutOfRange && currrentBodyDepth >= 4f)
            {
                bodyOutOfRange = false;
                bodyTooFar = true;
            }
            // Deja de estar lejos:
            else if (bodyTooFar && currrentBodyDepth <= 4f && currrentBodyDepth >= 3f)
            {
                bodyTooFar = false;
            }
            // Vuelve a estar lejos
            else if (currrentBodyDepth >= 4f)
            {
                bodyTooFar = true;
            }
        }
        
        private void DisplayBodyDistance(float bodyDepth)
        {
            BodyDepth.Text = "Profundidad: " +  bodyDepth.ToString();
        }
        
        private void DisplayDebugValues()
        {
            BodyFarOutput.Text = "Cuerpo alejado: " + this.bodyTooFar.ToString();
            WalkingTime.Text = "Tiempo: " + this.walkingTime.ToString();
        }

        private void UpdateWalkingTime()
        {
            //Store the time on walkingTime var
            if (stopWatch != null)
            {
                var elapsedMillis = stopWatch.ElapsedMilliseconds;
                walkingTime = elapsedMillis / 1000f;
            }
            else
            {
                walkingTime = 0;
            }
        }
        
        private void SaveOnFileTestResults()
        {
            try
            {
                streamWriter.WriteLine("Test;" + "Gait Speed Test");
                streamWriter.WriteLine("Date;" + DateTime.Now.ToString());
                streamWriter.WriteLine("Distance Walked (m);" + (startLine - endLine));
                streamWriter.WriteLine("Walking Time(s);" + walkingTime);
                streamWriter.WriteLine("Walking Speed(m/s);" + (startLine - endLine) / walkingTime);
                streamWriter.WriteLine(bodyJointsPosition.ToCsv());
                streamWriter.WriteLine("\n");
                streamWriter.Flush();
            }
            catch (Exception e)
            {
                UpdateTestStatus(errorStatus);
            }
        }
        
        private void UpdateTestStatus(string newStatus)
        {
            StatusTest.Text = newStatus;
        }

        private bool BodyInEndZone(float bodyDepth)
        {
            return (bodyDepth < endLine);
        }

        private bool BodyInTransitionZone(float bodyDepth)
        {
            return (bodyDepth > endLine && bodyDepth < startLine);
        }

        private bool BodyInStartZone(float bodyDepth)
        {
            return (bodyDepth > startLine);
        }
    }
}
