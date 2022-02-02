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

        public const float START_LINE = 4.5f;

        public const float END_LINE = 0.7f;

        private float _bodyDistance = 0f;

        private float _walkingTime = 0f;

        private string _statusTest = initStatus;

        private Stopwatch _stopWatch = null;

        private bool TestIsRunning = false;

        private bool WasInStartZone = false;

        private StreamWriter streamWriter = new StreamWriter("Results.csv");

        private const string initStatus = "Para empezar el test, cóloquese a una distancia \n de 4,5 metros y camine hacia la cámara";

        private const string readyStatus = "Ahora, camine hacia la cámara";

        private const string transitionStatus = "Siga caminando hacia la cámara.";

        private const string endStatus = "¡Genial! Ha terminado el test. Muchas gracias.";

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



        /// Gets or sets the current Walking test status
        public string StatusTest
        {
            get
            {
                return this._statusTest;
            }

            set
            {
                if (this._statusTest != value)
                {
                    this._statusTest = value;

                    // notify any bound elements that the text has changed
                    if (this.PropertyChanged != null)
                    {
                        this.PropertyChanged(this, new PropertyChangedEventArgs("StatusTest"));
                    }
                }
            }
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// Gets or sets the body distance to display
        public float BodyDistance
        {
            get { return _bodyDistance; }
            set
            {
                if (_bodyDistance != value)
                {
                    _bodyDistance = value;
                    this.OnPropertyChanged("BodyDistance");
                }
            }
        }

        /// Gets or sets the walking time to display
        public float WalkingTime
        {
            get
            {
                return this._walkingTime;
            }

            set
            {
                if (_walkingTime != value)
                {
                    _walkingTime = value;
                    this.PropertyChanged(this, new PropertyChangedEventArgs("WalkingTime"));
                }
            }
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

            /// Display the body depth
            this.DisplayBodyDistance(bodyDepth);

            /// Tercero, evaluamos la posicion del body y estado del test (ver pseudocodigo)
            if (this.BodyInEndZone(bodyDepth) && TestIsRunning)
            {
                _stopWatch.Stop();
                this.DisplayTime();
                TestIsRunning = false;
                StatusTest = endStatus;
                SaveOnFileTestResults();
            }
            else if (this.BodyInTransitionZone(bodyDepth) && TestIsRunning)
            {
                this.DisplayTime();
                jointsStatus.AddBodyStatus(WalkingTime, joints);
            }
            else if (this.BodyInTransitionZone(bodyDepth) && WasInStartZone)
            {
                _stopWatch = Stopwatch.StartNew();
                this.DisplayTime();
                TestIsRunning = true;
                WasInStartZone = false;
                StatusTest = transitionStatus;
                jointsStatus.AddBodyStatus(WalkingTime, joints);
            }
            else if (this.BodyInStartZone(bodyDepth) && TestIsRunning)
            {
                _stopWatch.Reset();
                this.DisplayTime();
                jointsStatus.ClearBodyStatus();
                StatusTest = readyStatus;
                TestIsRunning = false;
                WasInStartZone = true;
            }
            else if (this.BodyInStartZone(bodyDepth) && !WasInStartZone)
            {
                this.DisplayTime();
                StatusTest = readyStatus;
                jointsStatus.ClearBodyStatus();
                WasInStartZone = true;
            }


        }

        private void SaveOnFileTestResults()
        {
            try
            {
                streamWriter.WriteLine("Date;" + DateTime.Now.ToString());
                streamWriter.WriteLine("Distance Walked (m);" + (START_LINE - END_LINE));
                streamWriter.WriteLine("Walking Time(s);" + WalkingTime);
                streamWriter.WriteLine("Walking Speed(m/s);" + (START_LINE - END_LINE) / WalkingTime);
                streamWriter.WriteLine("Instant" + ";" + "Head Depth" + ";" +
                    "Spine Shoulder Depth" + ";" + "Mid Spine Depth" + ";" + "Spine Base Depth" + ";" +
                    "Right Knee Depth" + ";" + "Left Knee Depth" + ";" +
                    "Rigth Foot Depth" + ";" + "Left Foot Depth");
                streamWriter.WriteLine(jointsStatus.ToString());
                streamWriter.WriteLine("\n");
            }
            catch (Exception e)
            {
                StatusTest = e.Message;
            }

        }

        private void DisplayTime()
        {
            if (_stopWatch != null)
            {
                var elapsedMillis = _stopWatch.ElapsedMilliseconds;
                this.WalkingTime = elapsedMillis / 1000f;
            }
            else
            {
                this.WalkingTime = 0;
            }
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
            if (this.bodyIsTooFar(mean)) return 4.6f;
            if (this.bodyIsTooClose(mean)) return 0.6f;
            return mean;
        }

        private bool bodyIsTooClose(float depthBody)
        {
            return depthBody <= 0.6f && _bodyDistance <= 0.6f;
        }

        private bool bodyIsTooFar(float depthBody)
        {
            return depthBody >= 4.6f && _bodyDistance >= 4.6f;
        }

        private void DisplayBodyDistance(float bodyDepth)
        {
            this.BodyDistance = bodyDepth;
        }

        private bool BodyInEndZone(float bodyDepth)
        {
            return (bodyDepth < END_LINE);
        }

        private bool BodyInTransitionZone(float bodyDepth)
        {
            return (bodyDepth > END_LINE && bodyDepth < START_LINE);
        }

        private bool BodyInStartZone(float bodyDepth)
        {
            return (bodyDepth > START_LINE);
        }
    }
}
