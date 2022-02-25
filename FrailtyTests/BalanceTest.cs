namespace FragilityTests.FrailtyTests
{
    using FragilityTests.DataTest;
    using Microsoft.Kinect;
    using System;
    using System.Collections.Generic;

    public class BalanceTest : FrailtyTest
    {
        private float balanceTime = 0f;

        private bool testIsRunning = false;

        private int onBalanceConfidence = 0;

        private const string initStatus = "Colóquese a unos dos metros de la cámara y junte los pies.";

        private const string transitionStatus = "Mantenga el equilibrio";

        private const string endStatus = "!Genial! !Buen trabajo!";

        private string currentStatus = initStatus;

        public BalanceTest() : base(FrailtyTestType.BalanceTest)
        {
        }

        public override void EvaluateFrailtyTest(IReadOnlyDictionary<JointType, Joint> joints)
        {

            // Si está realizando el test 
            if (BodyInBalancePosition(joints) && testIsRunning && balanceTime < 10.0f)
            {
                UpdateBalanceTime();
                dtManager.RegisterBodyStatus(balanceTime, joints);
            }
            // Acaba de empezar el test
            else if (BodyInBalancePosition(joints) && !testIsRunning)
            {
                testIsRunning = true;
                stopWatch.Restart();
                UpdateBalanceTime();
                currentStatus = transitionStatus;
                dtManager.RegisterBodyStatus(balanceTime, joints);
            }

            // Acaba de perder el equilibrio 
            else if (!BodyInBalancePosition(joints) && testIsRunning)
            {
                testIsRunning = false;
                stopWatch.Stop();
                UpdateBalanceTime();
                currentStatus = endStatus;
                TestResult testResult = new BalanceTestResult("Miguel", DateTime.Now.ToString(), balanceTime);
                dtManager.EndTest(validTest: true, testResult);
            }
            
            // Si supera los diez segundos
            else if (testIsRunning && balanceTime >= 10.0f)
            {
                testIsRunning = false;
                stopWatch.Stop();
                UpdateBalanceTime();
                currentStatus = endStatus;
                TestResult testResult = new BalanceTestResult("Miguel", DateTime.Now.ToString(), balanceTime);
                dtManager.EndTest(validTest: true, testResult);
            }
        }

        public override string GetCurrentStatus()
        {
            return currentStatus;
        }

        public override string[] GetDebugValues()
        {
            string[] debugValues = new string[1];
            debugValues[0] = balanceTime.ToString();
            return debugValues;
        }

        private bool BodyInBalancePosition(IReadOnlyDictionary<JointType, Joint> joints)
        {
            Joint leftFoot = joints[JointType.FootLeft];
            Joint rightFoot = joints[JointType.FootRight];
            bool closeOnX = Math.Round(rightFoot.Position.X, 1) == Math.Round(leftFoot.Position.X, 1);

            if (closeOnX && onBalanceConfidence < 3) onBalanceConfidence++;
            else if ((!closeOnX) && onBalanceConfidence > 0) onBalanceConfidence--;

            if (onBalanceConfidence >= 2) return true;
            return false;
        }

        private void UpdateBalanceTime()
        {
            //Store the time on walkingTime var
            if (stopWatch != null)
            {
                var elapsedMillis = stopWatch.ElapsedMilliseconds;
                balanceTime = elapsedMillis / 1000f;
            }
            else
            {
                balanceTime = 0;
            }
        }
    }
}
