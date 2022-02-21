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
            walkingSpeed = distanceWalked / walkingTime;
            steps = 5f;
            stepWidth = 1.2f;
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
