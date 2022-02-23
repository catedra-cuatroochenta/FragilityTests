namespace FragilityTests.DataTest
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public class BalanceTestResult : TestResult
    {
        private float balanceTime;


        public BalanceTestResult(string userId, string date, float balanceTime) : 
            base(FrailtyTestType.BalanceTest, userId, date)
        {
            this.balanceTime = balanceTime;
        }

        public override void Autocomplete()
        {
            if (balanceTime >= 10.0f) points = 2;
            else if (balanceTime >= 3.00) points = 1;
            else points = 0;
        }

        public override string ToCsv()
        {
            string result = String.Empty;
            result += "Test;" + "Balance Test" + "\n";
            result += "UserId;" + userId + "\n";
            result += "Date;" + date + "\n";
            result += "SPPB points;" + points + "\n";
            result += "Balance Time(s);" + balanceTime + "\n";
            return result;
        }
    }
}
