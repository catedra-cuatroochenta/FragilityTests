namespace FragilityTests.DataTest
{
    using System;

    public class GaitSpeedTestResult : TestResult
    {
        private float walkingTime;

        private float walkingSpeed;

        private float distanceWalked;

        private float steps;

        private float stepWidth;

        public GaitSpeedTestResult(string userId, string date, float walkingTime, float distanceWalked) : 
            base(FrailtyTestType.GaitSpeedTest, userId, date)
        {
            this.walkingTime = walkingTime;
            this.distanceWalked = distanceWalked;
        }

        public override void Autocomplete()
        {
            ComputeWalkingSpeed();
            ComputeSteps();
            ComputeStepWidth();
            ComputePoints();
        }

        private void ComputeWalkingSpeed()
        {
            walkingSpeed = distanceWalked / walkingTime;
        }

        private void ComputeStepWidth()
        {
            stepWidth = 1.2f;
        }

        private void ComputeSteps()
        {
            steps = 5f;
        }

        private void ComputePoints()
        {
            if(distanceWalked == 4)
            {
                if (walkingSpeed > 8.70f) points = 1;
                if (walkingSpeed > 6.20f) points = 2;
                if (walkingSpeed > 4.81f) points = 3;
                else points = 4;
            } 
            
            else if(distanceWalked == 3)
            {
                if (walkingSpeed > 6.52f) points = 1;
                if (walkingSpeed > 4.65f) points = 2;
                if (walkingSpeed > 3.61f) points = 3;
                else points = 4;
            }
        }

        public override string ToCsv()
        {
            string result = String.Empty;
            result += "Test;" + "Gait Speed Test" + "\n";
            result += "UserId;" + userId + "\n";
            result += "Date;" + date + "\n";
            result += "SPPB points;" + points + "\n";
            result += "Distance Walked (m);" + distanceWalked + "\n";
            result += "Walking Time(s);" + walkingTime + "\n";
            result += "Walking Speed(m/s);" + walkingSpeed + "\n";
            result += "Steps;" + steps + "\n";
            result += "Step width;" + stepWidth + "\n";
            return result;
        }
    }
}
