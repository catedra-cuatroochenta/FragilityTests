using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace FragilityTests.FrailtyTests
{
    public class GaitSpeedTest : FrailtyTest
    {
        private const float startLine = 4f;   // Max: 4.3f (Ideal = 4f)

        private const float endLine = 1f;     // Min: 0.8f (Ideal = 1f)

        private float walkingTime = 0f;

        private float bodyDepth;

        private bool testIsRunning = false;

        private bool wasInStartZone = false;

        private bool bodyTooFar = false;

        private bool bodyOutOfRange = false;

        private const string initStatus = "Para empezar el test, alejese de la cámara";

        private const string readyStatus = "Perfecto, ahora camine hacia la cámara";

        private const string transitionStatus = "Siga caminando hacia la cámara.";

        private const string endStatus = "¡Genial! Ha terminado el test. ¡Muchas gracias!";

        private const string outOfRangeStatus = "Se encuentra demasiado alejado, acérquese a la cámara.";

        private string currentStatus = initStatus;

        public GaitSpeedTest()
            : base(FrailtyTestType.GaitSpeedTest)
        {

        }

        public override void EvaluateFrailtyTest(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Get the body depth
            bodyDepth = GetBodyDepth(joints);

            /// Si completa el test
            if (BodyInEndZone(bodyDepth) && testIsRunning)
            {
                stopWatch.Stop();
                UpdateWalkingTime();
                testIsRunning = false;
                bodyJointsPosition.AddBodyStatus(walkingTime, joints);
                SaveOnFileTestResult();
                bodyJointsPosition.ClearBodyStatus();
                currentStatus = endStatus;
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
                currentStatus = transitionStatus;
            }
            /// Si estaba haciendo el test pero vuelve a empezar
            else if (BodyInStartZone(bodyDepth) && testIsRunning)
            {
                stopWatch.Reset();
                UpdateWalkingTime();
                testIsRunning = false;
                wasInStartZone = true;
                bodyJointsPosition.ClearBodyStatus();
                currentStatus = readyStatus;
            }
            /// Si esta preparado para empezar el test
            else if (BodyInStartZone(bodyDepth) && !wasInStartZone)
            {
                stopWatch.Reset();
                UpdateWalkingTime();
                wasInStartZone = true;
                currentStatus = readyStatus;
            }
        }

        public override string GetCurrentStatus()
        {
            return currentStatus;
        }

        public override string[] GetDebugValues()
        {
            string[] debugValues = new string[3];
            debugValues[0] = bodyDepth.ToString();
            debugValues[1] = bodyTooFar.ToString();
            debugValues [2] = walkingTime.ToString();
            return debugValues;
        }

        public override void SaveOnFileTestResult()
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
                Console.WriteLine(e.Message);
            }
        }

        private float GetBodyDepth(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Inicialmente devuelvo la media
            /// Lo ideal sería descartar los valores que se alejen mucho 
            float midSpineDepth = joints[JointType.SpineMid].Position.Z;
            float baseSpineDepth = joints[JointType.SpineBase].Position.Z;
            float shoulderSpineDepth = joints[JointType.SpineShoulder].Position.Z;
            float headDepth = joints[JointType.Head].Position.Z;
            float mean = (headDepth + midSpineDepth + baseSpineDepth + shoulderSpineDepth) / 4;
            EvaluateBodyDepth(mean);
            if (bodyOutOfRange)
            {
                currentStatus = outOfRangeStatus;
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
