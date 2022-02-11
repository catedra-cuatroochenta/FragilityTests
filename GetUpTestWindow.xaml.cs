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
    
    
    /// <summary>
    /// Lógica de interacción para GetUpTestWindow.xaml
    /// </summary>
    public partial class GetUpTestWindow : Window
    {
        /// Active Kinect sensor
        private KinectSensor kinectSensor = null;

        /// kinect reader for color, depth, body, etc, frames
        MultiSourceFrameReader reader;

        /// Bitmap to display
        private WriteableBitmap colorBitmap = null;

        private Body[] bodies = null;

        private float testTime = 0f;

        private Stopwatch stopWatch = null;

        private bool testIsRunning = false;

        private bool isOnCountdown = false;

        private BodyJointsPosition bodyJointsPosition;

        private int isUprightConfidence = 0; // Max confidence = 5, Min confidence = 0

        private bool alreadyUpright = false;

        private int squats = 0;

        private StreamWriter streamWriter = new StreamWriter("Results.csv");

        private const string initStatus = "Inicio";

        private const string countdownStatus = "Empieza la cuenta atrás";

        private const string transitionStatus = "Continue";

        private const string endStatus = "Fin";

        private const string errorStatus = "¡Ups! Ha ocurrido algún problema, pruebe a reiniciar el sistema.";

        private const string outOfRangeStatus = "Se encuentra demasiado alejado, acérquese a la cámara.";


        public GetUpTestWindow()
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
        private void GetUpTestWindow_Closing(object sender, CancelEventArgs e)
        {
            if (this.reader != null)
            {
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
                        EvaluateTheGetUpTest(joints);
                        break;
                    }

                }
            }
        }

        private void EvaluateTheGetUpTest(IReadOnlyDictionary<JointType, Joint> joints)
        {
            float rightKneeDegree = GetThreeJointsDegree(joints[JointType.HipRight], joints[JointType.KneeRight], joints[JointType.AnkleRight]);
            float leftKneeDegree = GetThreeJointsDegree(joints[JointType.HipLeft], joints[JointType.KneeLeft], joints[JointType.AnkleLeft]);
            

            // Si se encuentra en posición de empezar el test
            if (!testIsRunning && BodyIsSeated(leftKneeDegree, rightKneeDegree)
                && BodyInStartingPosition(joints))
            {
                isOnCountdown = true;
                testIsRunning = false;
                stopWatch.Start();
                UpdateTestStatus(countdownStatus);
            }

            else if (isOnCountdown && testTime >= 3f)
            {
                isOnCountdown = false;
                testIsRunning = true;
                stopWatch.Restart();
                UpdateTestTime();
                // TODO: Store data
                UpdateTestStatus(transitionStatus);
            }

            else if (isOnCountdown)
            {
                UpdateTestTime();
            }

            else if (testIsRunning)
            {
                UpdateTestTime();
                // TODO: Store data

                // Si se acaba de poner en pie
                if (BodyIsUpright(rightKneeDegree, leftKneeDegree) && !alreadyUpright)
                {
                    alreadyUpright = true;
                    squats++;
                    if (squats == 5)
                    {
                        testIsRunning = false;
                        isOnCountdown = false;
                        stopWatch.Stop();
                        UpdateTestStatus(endStatus);
                    }
                }

                // Si se acaba de sentar
                else if (BodyIsSeated(rightKneeDegree, leftKneeDegree) && alreadyUpright)
                {
                    alreadyUpright = false;
                }
            }

            DisplayDebugValues(rightKneeDegree, leftKneeDegree);
        }

        private bool BodyIsSeated(float rightKneeDegrees, float leftKneeDegrees)
        {
            if (rightKneeDegrees <= 95f && leftKneeDegrees <= 95f)
            {
                if (isUprightConfidence > 0) isUprightConfidence--;
                return true;
            }
            return false;
        }
        
        private bool BodyIsUpright(float rightKneeDegrees, float leftKneeDegrees)
        {
            if (rightKneeDegrees > 165f && leftKneeDegrees > 165f
                && isUprightConfidence < 5) isUprightConfidence++;
            
            if (isUprightConfidence == 5) return true;
            return false;
        }

        private float GetThreeJointsDegree(Joint jointA, Joint jointB, Joint jointC)
        {
            float[] vectorA = GetVector(jointB, jointA);
            float[] vectorB = GetVector(jointB, jointC);

            float scalarProduct = GetScalarProduct(vectorA, vectorB);

            double vectorNormA = GetVectorNorm(vectorA);
            double vectorNormB = GetVectorNorm(vectorB);

            double radians = Math.Acos(scalarProduct / (vectorNormA * vectorNormB));
            return (float) (radians * 180 / Math.PI);
        }

        private double GetVectorNorm(float[] vector)
        {
            float norm = 0;
            for (int i = 0; i < vector.Length; i++)
            {
                norm += vector[i] * vector[i];
            }
            return Math.Sqrt(norm);
        }

        private float GetScalarProduct(float[] vectorA, float[] vectorB)
        {
            float scalarProduct = 0;
            for(int i = 0; i <  vectorA.Length; i++)
            {
                scalarProduct += vectorA[i] * vectorB[i];
            }
            return scalarProduct;
        }

        private float[] GetVector(Joint pointA, Joint pointB)
        {
            float[] vector = new float[3];
            vector[0] = pointB.Position.X - pointA.Position.X;
            vector[1] = pointB.Position.Y - pointA.Position.Y;
            vector[2] = pointB.Position.Z - pointA.Position.Z;
            return vector;
        }

        private void DisplayDebugValues(float rightKneeDegrees, float leftKneeDegrees)
        {
            TestTime.Text = "Tiempo: " + this.testTime.ToString();
            RightKneeDegrees.Text = "Grados rodilla derecha: " + rightKneeDegrees;
            LeftKneeDegrees.Text = "Grados rodilla izquierda: " + leftKneeDegrees;
            UprightConfidence.Text = "Upright Confidence: " + isUprightConfidence;
            SquatRep.Text = "Sentadillas: " + squats;
        }

        private void UpdateTestTime()
        {
            //Store the time on walkingTime var
            if (stopWatch != null)
            {
                var elapsedMillis = stopWatch.ElapsedMilliseconds;
                testTime = elapsedMillis / 1000f;
            }
            else
            {
                testTime = 0;
            }
        }

        private void UpdateTestStatus(string newStatus)
        {
            StatusTest.Text = newStatus;
        }

        private bool BodyInStartingPosition(IReadOnlyDictionary<JointType, Joint> joints)
        {
            float leftHandHeight = joints[JointType.HandLeft].Position.Y;
            float rightHandHeight = joints[JointType.HandRight].Position.Y;
            float headHeight = joints[JointType.Head].Position.Y;
            if (leftHandHeight >= headHeight && rightHandHeight >= headHeight) return true;
            return false; 
        }

    }
}
