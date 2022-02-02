namespace FragilityTests
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

    public partial class WalkingSpeedTest1 : Window
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

        public const float startLine = 3.5f;

        public const float endLine = 1f;

        private float walkingTime = 0f;

        private Stopwatch stopWatch = null;

        private bool testIsRunning = false;

        private bool wasInStartZone = false;

        private StreamWriter streamWriter = new StreamWriter("Results.csv");

        private const string initStatus = "Para empezar el test, cóloquese a una distancia \n de 4,5 metros y camine hacia la cámara";

        private const string readyStatus = "Ahora, camine hacia la cámara";

        private const string transitionStatus = "Siga caminando hacia la cámara.";

        private const string endStatus = "¡Genial! Ha terminado el test. Muchas gracias.";

        private const string errorStatus = "¡Ups! Ha ocurrido algún problema, pruebe a reiniciar el sistema.";

        private class BodyJointsStatus
        {
            Dictionary<float, float[]> depthBodyJoints;

            public BodyJointsStatus()
            {
                this.depthBodyJoints = new Dictionary<float, float[]>();
            }

            public void AddBodyStatus(float instant, IReadOnlyDictionary<JointType, Joint> joints)
            {
                float[] jointsDepth = new float[8];
                jointsDepth[0] = joints[JointType.Head].Position.Z;
                jointsDepth[1] = joints[JointType.SpineShoulder].Position.Z;
                jointsDepth[2] = joints[JointType.SpineMid].Position.Z;
                jointsDepth[3] = joints[JointType.SpineBase].Position.Z;
                jointsDepth[4] = joints[JointType.KneeRight].Position.Z;
                jointsDepth[5] = joints[JointType.KneeLeft].Position.Z;
                jointsDepth[6] = joints[JointType.FootRight].Position.Z;
                jointsDepth[7] = joints[JointType.FootLeft].Position.Z;

                this.depthBodyJoints.Add(instant, jointsDepth);
            }

            public void ClearBodyStatus()
            {
                this.depthBodyJoints.Clear();
            }


            public override String ToString()
            {
                string result = string.Empty;
                foreach (KeyValuePair<float, float[]> kvp in depthBodyJoints)
                {
                    string depthValues = string.Empty;
                    foreach (float depth in kvp.Value)
                    {
                        depthValues += (depth + ";");
                    }
                    result += string.Format("{0};{1}\n", kvp.Key, depthValues);
                }
                return result;

            }

        }

        private BodyJointsStatus jointsStatus;

        public WalkingSpeedTest1()
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

            this.jointsStatus = new BodyJointsStatus();

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
        private void MainWindow_Closing(object sender, CancelEventArgs e)
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
            streamWriter.Close();

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
                        EvaluateTheWalkingTest(joints);
                    }
                }
            }
        }

        private void EvaluateTheWalkingTest(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Get the body depth
            float bodyDepth = this.GetBodyDepth(joints);

            /// Display the body depth and the gait speed time
            this.DisplayBodyDistance(bodyDepth);
            this.StoreAndDisplayTime();

            /// Tercero, evaluamos la posicion del body y estado del test (ver pseudocodigo)
            if (this.BodyInEndZone(bodyDepth) && testIsRunning)
            {
                stopWatch.Stop();
                testIsRunning = false;
                UpdateTestStatus(endStatus);
                SaveOnFileTestResults();
            }
            else if (this.BodyInTransitionZone(bodyDepth) && testIsRunning)
            {
                jointsStatus.AddBodyStatus(walkingTime, joints);
            }
            else if (this.BodyInTransitionZone(bodyDepth) && wasInStartZone)
            {
                stopWatch = Stopwatch.StartNew();
                testIsRunning = true;
                wasInStartZone = false;
                UpdateTestStatus(transitionStatus);
                jointsStatus.AddBodyStatus(walkingTime, joints);
            }
            else if (this.BodyInStartZone(bodyDepth) && testIsRunning)
            {
                stopWatch.Reset();
                jointsStatus.ClearBodyStatus();
                UpdateTestStatus(readyStatus);
                testIsRunning = false;
                wasInStartZone = true;
            }
            else if (this.BodyInStartZone(bodyDepth) && !wasInStartZone)
            {
                UpdateTestStatus(readyStatus);
                jointsStatus.ClearBodyStatus();
                wasInStartZone = true;
            }


        }

        private void SaveOnFileTestResults()
        {
            try
            {
                streamWriter.WriteLine("Date;" + DateTime.Now.ToString());
                streamWriter.WriteLine("Distance Walked (m);" + (startLine - endLine));
                streamWriter.WriteLine("Walking Time(s);" + walkingTime);
                streamWriter.WriteLine("Walking Speed(m/s);" + (startLine - endLine) / walkingTime);
                streamWriter.WriteLine("Instant" + ";" + "Head Depth" + ";" +
                    "Spine Shoulder Depth" + ";" + "Mid Spine Depth" + ";" + "Spine Base Depth" + ";" +
                    "Right Knee Depth" + ";" + "Left Knee Depth" + ";" +
                    "Rigth Foot Depth" + ";" + "Left Foot Depth");
                streamWriter.WriteLine(jointsStatus.ToString());
                streamWriter.WriteLine("\n");
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

        private void StoreAndDisplayTime()
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
            
            //Display the time 
            WalkingTime.Text = walkingTime.ToString();
        }

        private float GetBodyDepth(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Inicialmente devuelvo la media
            /// Lo ideal sería descartar los valores que se alejen mucho 
            //float midSpineDepth = joints[JointType.SpineMid].Position.Z;
            //float baseSpineDepth = joints[JointType.SpineBase].Position.Z;
            float shoulderSpineDepth = joints[JointType.SpineShoulder].Position.Z;
            float headSpineDepth = joints[JointType.Head].Position.Z;
            float mean = (shoulderSpineDepth + headSpineDepth) / 2;
            return mean;
        }


        private void DisplayBodyDistance(float bodyDepth)
        {
            
            BodyDepth.Text = bodyDepth.ToString();
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
