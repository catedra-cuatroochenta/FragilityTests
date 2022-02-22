namespace FragilityTests.DataTest
{
    using System;
    
    public class GetUpTestResult : TestResult
    {
        // Tiempo que ha requerido el paciente en realizar las sentadillas
        private float time;

        // Número de sentadillas determinadas para el test
        private int squats;

        // Squat range of motion: Rango de recorrido de la sentadilla 
        private float squatRom;

        // Velocidad media por sentadilla
        private float squatSpeed;

        public GetUpTestResult(string userId, string date, float time, int squats) 
            : base(FrailtyTestType.GetUpTest, userId, date)
        {
            this.time = time;
            this.squats = squats;
        }
        public override void Autocomplete()
        {
            ComputeSquatRom();
            ComputeSquatSpeed();
            ComputePoints();
        }

        private void ComputeSquatRom()
        {
            squatRom = 0.5f;
        }

        private void ComputeSquatSpeed()
        {
            squatSpeed = time / squats;
        }

        private void ComputePoints()
        {
            if (time < 11.20f) points = 4;
            else if (time < 13.70f) points = 3;
            else if (time < 16.70f) points = 2;
            else if (time < 60.00f) points = 1;
            else points = 0;
        }

        public override string ToCsv()
        {
            string result = string.Empty;
            result += "Test;" + "Get Up Test" + "\n";
            result += "UserId;" + userId + "\n";
            result += "Date;" + date + "\n";
            result += "SPPB points;" + points + "\n";
            result += "Squats;" + squats + "\n";
            result += "Time(s);" + time + "\n";
            result += "Squat Rom;" + squatRom + "\n";
            result += "Squat Speed avg;" + squatSpeed + "\n";
            return result;
        }
    }
}
