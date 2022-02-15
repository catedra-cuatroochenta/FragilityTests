namespace FragilityTests
{
    using Microsoft.Kinect;
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.IO;

    public class PerpendicularGaitSpeedTest : FrailtyTest
    {

        public const float startLine = 2.0f;   // Ideal: 2f

        public const float endLine = -2.0f;     // Ideal: -2f

        private float walkingTime;
        
        private float bodyXAxisPos;

        private bool testIsRunning = false;

        private bool wasInStartZone = false;

        private bool bodyTooFar = false;

        private const string initStatus = "Para empezar el test, colóquese a la derecha de la cámara.";

        private const string readyStatus = "Perfecto, ahora camine recto de forma perpendicular a la cámara.";

        private const string transitionStatus = "¡Siga caminando!";

        private const string endStatus = "¡Genial! Ha terminado el test. ¡Muchas gracias!";

        private const string errorStatus = "¡Ups! Ha ocurrido algún problema, pruebe a reiniciar el sistema.";

        private const string outOfRangeStatus = "Se encuentra demasiado alejado, acérquese a la cámara.";

        private string currentStatus = initStatus;


        public PerpendicularGaitSpeedTest() 
            : base(FrailtyTestType.PerpendicularGaitSpeedTest)
        {

        }

        public override void EvaluateFrailtyTest(IReadOnlyDictionary<JointType, Joint> joints)
        {
            /// Get the body pos
            this.bodyXAxisPos = GetBodyXAxisPos(joints);

            /// Si completa el test
            if (BodyInEndZone() && testIsRunning)
            {
                base.stopWatch.Stop();
                UpdateWalkingTime();
                testIsRunning = false;
                base.bodyJointsPosition.AddBodyStatus(walkingTime, joints);
                SaveOnFileTestResult();
                base.bodyJointsPosition.ClearBodyStatus();
                currentStatus = endStatus;
            }
            /// Si esta en medio del test
            else if (BodyInTransitionZone() && testIsRunning)
            {
                UpdateWalkingTime();
                bodyJointsPosition.AddBodyStatus(walkingTime, joints);
            }
            /// Si acaba de empezar el test
            else if (BodyInTransitionZone() && wasInStartZone)
            {
                base.stopWatch.Start();
                UpdateWalkingTime();
                testIsRunning = true;
                wasInStartZone = false;
                base.bodyJointsPosition.AddBodyStatus(walkingTime, joints);
                currentStatus = transitionStatus;
            }
            /// Si estaba haciendo el test pero vuelve a empezar
            else if (BodyInStartZone() && testIsRunning)
            {
                base.stopWatch.Reset();
                UpdateWalkingTime();
                testIsRunning = false;
                wasInStartZone = true;
                base.bodyJointsPosition.ClearBodyStatus();
                currentStatus = readyStatus;
            }
            /// Si esta preparado para empezar el test
            else if (BodyInStartZone() && !wasInStartZone)
            {
                base.stopWatch.Reset();
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
            debugValues[0] = walkingTime.ToString();
            debugValues[1] = bodyXAxisPos.ToString();
            debugValues[2] = bodyTooFar.ToString();
            return debugValues;
        }
       
        public override void SaveOnFileTestResult()
        {
            try
            {
                base.streamWriter.WriteLine("Test;" + "Perpendicular Gait Speed Test");
                base.streamWriter.WriteLine("Date;" + DateTime.Now.ToString());
                base.streamWriter.WriteLine("Distance Walked (m);" + (startLine - endLine));
                base.streamWriter.WriteLine("Walking Time(s);" + walkingTime);
                base.streamWriter.WriteLine("Walking Speed(m/s);" + (startLine - endLine) / walkingTime);
                base.streamWriter.WriteLine(bodyJointsPosition.ToCsv());
                base.streamWriter.WriteLine("\n");
                base.streamWriter.Flush();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.Message);
            }
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

        private bool BodyInEndZone()
        {
            return (bodyXAxisPos < endLine);
        }

        private bool BodyInTransitionZone()
        {
            return (bodyXAxisPos > endLine && bodyXAxisPos < startLine);
        }

        private bool BodyInStartZone()
        {
            return (bodyXAxisPos > startLine);
        }

    }
}
