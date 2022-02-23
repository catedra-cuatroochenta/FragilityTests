using FragilityTests.DataTest;
using Microsoft.Kinect;
using System;
using System.Collections.Generic;

namespace FragilityTests.FrailtyTests
{
    public class GetUpTest : FrailtyTest
    {
        private float testTime = 0f;

        private bool testIsRunning = false;

        private bool isOnCountdown = false;

        private int isUprightConfidence = 0; // Max confidence = 3, Min confidence = 0

        private int maxUprightConfidence = 2;

        private bool alreadyUpright = false;

        private int squats = 0;

        private float rightKneeDegree; 

        private float leftKneeDegree;

        private const string initStatus = "Inicio";

        private const string countdownStatus = "Empieza la cuenta atrás";

        private const string transitionStatus = "Continue";

        private const string endStatus = "Fin";

        private string currentStatus = initStatus;

        public GetUpTest() 
            : base(FrailtyTestType.GetUpTest)
        {

        }

        public override void EvaluateFrailtyTest(IReadOnlyDictionary<JointType, Joint> joints)
        {
            rightKneeDegree = GetThreeJointsDegree(joints[JointType.HipRight], joints[JointType.KneeRight], joints[JointType.AnkleRight]);
            leftKneeDegree = GetThreeJointsDegree(joints[JointType.HipLeft], joints[JointType.KneeLeft], joints[JointType.AnkleLeft]);


            // Si se encuentra en posición de empezar el test
            if (!testIsRunning && !isOnCountdown &&
                BodyIsSeated() && BodyInStartingPosition(joints))
            {
                isOnCountdown = true;
                testIsRunning = false;
                stopWatch.Restart();
                UpdateTestTime();
                currentStatus = countdownStatus;
            }

            // Si termina la cuenta atrás
            else if (isOnCountdown && testTime >= 3f)
            {
                squats = 0;
                isOnCountdown = false;
                testIsRunning = true;
                stopWatch.Restart();
                UpdateTestTime();
                currentStatus = transitionStatus;
                dtManager.RegisterBodyStatus(testTime, joints);
            }

            // Si está en la cuenta atrás
            else if (isOnCountdown)
            {
                UpdateTestTime();
            }

            // Si está realizando el test
            else if (testIsRunning)
            {
                UpdateTestTime();
                dtManager.RegisterBodyStatus(testTime, joints);

                // Si se acaba de poner en pie
                if (BodyIsUpright() && !alreadyUpright)
                {
                    alreadyUpright = true;
                    squats++;
                    if (squats == 5)
                    {
                        testIsRunning = false;
                        isOnCountdown = false;
                        stopWatch.Stop();
                        currentStatus = endStatus;
                        TestResult testResult = new GetUpTestResult("Miguel", DateTime.Now.ToString(), testTime, squats);
                        dtManager.EndTest(validTest:true, testResult);
                    }
                }

                // Si se acaba de sentar
                else if (BodyIsSeated() && alreadyUpright)
                {
                    alreadyUpright = false;
                }
            }
        }

        public override string GetCurrentStatus()
        {
            return currentStatus;
        }

        public override string[] GetDebugValues()
        {
            string[] debugValues = new string[5];
            debugValues[0] = testTime.ToString();
            debugValues[1] = rightKneeDegree.ToString();
            debugValues[2] = leftKneeDegree.ToString();
            debugValues[3] = isUprightConfidence.ToString();
            debugValues[4] = squats.ToString();
            return debugValues;
        }
 
        private bool BodyIsSeated()
        {
            if (rightKneeDegree <= 105f && leftKneeDegree <= 105f)
            {
                if (isUprightConfidence > 0) isUprightConfidence--;
                return true;
            }
            return false;
        }

        private bool BodyIsUpright()
        {
            if (rightKneeDegree > 150f && leftKneeDegree > 150f
                && isUprightConfidence < maxUprightConfidence) isUprightConfidence++;

            if (isUprightConfidence == maxUprightConfidence) return true;
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
            return (float)(radians * 180 / Math.PI);
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
            for (int i = 0; i < vectorA.Length; i++)
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
