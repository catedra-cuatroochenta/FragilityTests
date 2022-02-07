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

    public partial class PerpendicularGaitSpeedTestWindow : Window
    {
        /// Active Kinect sensor
        private KinectSensor kinectSensor = null;

        /// Reader for color frames
        private ColorFrameReader colorFrameReader = null;

        /// Reader for body frames
        private BodyFrameReader bodyFrameReader = null;

        /// Bitmap to display
        private WriteableBitmap colorBitmap = null;

        private Body[] bodies = null;

        public const float startLine = 1.0f;   // Ideal: 2f

        public const float endLine = -1.0f;     // Ideal: -2f

        private float walkingTime = 0f;

        private Stopwatch stopWatch = null;

        private bool testIsRunning = false;

        private bool wasInStartZone = false;

        private bool bodyTooFar = false;

        private BodyJointsPosition bodyJointsPosition;

        private StreamWriter streamWriter = new StreamWriter("Results.csv");

        private const string initStatus = "Para empezar el test, colóquese a la derecha de la cámara.";

        private const string readyStatus = "Perfecto, ahora camine recto de forma perpendicular a la cámara.";

        private const string transitionStatus = "¡Siga caminando!";

        private const string endStatus = "¡Genial! Ha terminado el test. ¡Muchas gracias!";

        private const string errorStatus = "¡Ups! Ha ocurrido algún problema, pruebe a reiniciar el sistema.";

        private const string outOfRangeStatus = "Se encuentra demasiado alejado, acérquese a la cámara.";

        public PerpendicularGaitSpeedTestWindow()
        {
            this.kinectSensor = KinectSensor.GetDefault();

            this.colorFrameReader = this.kinectSensor.ColorFrameSource.OpenReader();
            this.colorFrameReader.FrameArrived += this.Reader_ColorFrameArrived;
            this.bodyFrameReader = this.kinectSensor.BodyFrameSource.OpenReader();
            this.bodyFrameReader.FrameArrived += this.Reader_FrameArrived;

            FrameDescription colorFrameDescription = this.kinectSensor.ColorFrameSource.CreateFrameDescription(ColorImageFormat.Bgra);
            this.colorBitmap = new WriteableBitmap(colorFrameDescription.Width, colorFrameDescription.Height, 96.0, 96.0, PixelFormats.Bgr32, null);

            this.kinectSensor.Open();

            this.DataContext = this;

            this.bodyJointsPosition = new BodyJointsPosition();

            this.stopWatch = Stopwatch.StartNew();
            this.stopWatch.Reset();

            this.InitializeComponent();

            UpdateTestStatus(initStatus);
        }

        /// INotifyPropertyChangedPropertyChanged event to allow window controls to bind to changeable data
        public event PropertyChangedEventHandler PropertyChanged;

        /// Gets the bitmap to display
        public ImageSource ImageSource
        {
            get
            {
                return this.colorBitmap;
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }


        /// Execute shutdown tasks
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void GaitSpeedTestWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.bodyFrameReader != null)
            {
                // BodyFrameReader is IDisposable
                this.bodyFrameReader.Dispose();
                this.bodyFrameReader = null;
            }

            if (this.colorFrameReader != null)
            {
                // ColorFrameReder is IDisposable
                this.colorFrameReader.Dispose();
                this.colorFrameReader = null;
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

        /// Handles the color frame data arriving from the sensor
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            // ColorFrame is IDisposable
            using (ColorFrame colorFrame = e.FrameReference.AcquireFrame())
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

        }

        /// Handles the body frame data arriving from the sensor
        /// <param name="sender">object sending the event</param>
        /// <param name="e">event arguments</param>
        private void Reader_FrameArrived(object sender, BodyFrameArrivedEventArgs e)
        {
            bool dataReceived = false;

            using (BodyFrame bodyFrame = e.FrameReference.AcquireFrame())
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
            /// Get the body pos
            float bodyXAxisPos = this.GetBodyXAxisPos(joints);

            /// Si completa el test
            if (BodyInEndZone(bodyXAxisPos) && testIsRunning)
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
            else if (BodyInTransitionZone(bodyXAxisPos) && testIsRunning)
            {
                UpdateWalkingTime();
                bodyJointsPosition.AddBodyStatus(walkingTime, joints);
            }
            /// Si acaba de empezar el test
            else if (BodyInTransitionZone(bodyXAxisPos) && wasInStartZone)
            {
                stopWatch.Start();
                UpdateWalkingTime();
                testIsRunning = true;
                wasInStartZone = false;
                bodyJointsPosition.AddBodyStatus(walkingTime, joints);
                UpdateTestStatus(transitionStatus);
            }
            /// Si estaba haciendo el test pero vuelve a empezar
            else if (BodyInStartZone(bodyXAxisPos) && testIsRunning)
            {
                stopWatch.Reset();
                UpdateWalkingTime();
                testIsRunning = false;
                wasInStartZone = true;
                bodyJointsPosition.ClearBodyStatus();
                UpdateTestStatus(readyStatus);
            }
            /// Si esta preparado para empezar el test
            else if (BodyInStartZone(bodyXAxisPos) && !wasInStartZone)
            {
                stopWatch.Reset();
                UpdateWalkingTime();
                wasInStartZone = true;
                UpdateTestStatus(readyStatus);
            }

            /// Display values
            DisplayDebugValues();
            DisplayBodyPos(bodyXAxisPos);

        }

        private float GetBodyXAxisPos(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Inicialmente devuelvo la media
            /// Lo ideal sería descartar los valores que se alejen mucho 
            float midSpinePos = joints[JointType.SpineMid].Position.X;
            float baseSpinePos = joints[JointType.SpineBase].Position.X;
            float shoulderSpinePos = joints[JointType.SpineShoulder].Position.X;
            float headPos = joints[JointType.Head].Position.X;
            float mean = (headPos + midSpinePos + baseSpinePos + shoulderSpinePos) / 4;
            return mean;
        }

        private void DisplayBodyPos(float bodyXAxisPos)
        {
            BodyPos.Text = "Posicion en X: " + bodyXAxisPos.ToString();
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
                this.walkingTime = elapsedMillis / 1000f;
            }
            else
            {
                this.walkingTime = 0;
            }
        }

        private void SaveOnFileTestResults()
        {
            try
            {
                streamWriter.WriteLine("Test;" + "Perpendicular Gait Speed Test");
                streamWriter.WriteLine("Date;" + DateTime.Now.ToString());
                streamWriter.WriteLine("Distance Walked (m);" + (startLine - endLine));
                streamWriter.WriteLine("Walking Time(s);" + walkingTime);
                streamWriter.WriteLine("Walking Speed(m/s);" + (startLine - endLine) / walkingTime);
                streamWriter.WriteLine("Instant" + ";" + "Head Pos" + ";" +
                    "Spine Shoulder Pos" + ";" + "Mid Spine Pos" + ";" + "Spine Base Pos" + ";" +
                    "Right Knee Pos" + ";" + "Left Knee Pos" + ";" +
                    "Rigth Foot Pos" + ";" + "Left Foot Pos");
                streamWriter.WriteLine(bodyJointsPosition.ToStringXAxisValues());
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

        private bool BodyInEndZone(float bodyXAxisPos)
        {
            return (bodyXAxisPos < endLine);
        }

        private bool BodyInTransitionZone(float bodyXAxisPos)
        {
            return (bodyXAxisPos > endLine && bodyXAxisPos < startLine);
        }

        private bool BodyInStartZone(float bodyXAxisPos)
        {
            return (bodyXAxisPos > startLine);
        }
    }
}