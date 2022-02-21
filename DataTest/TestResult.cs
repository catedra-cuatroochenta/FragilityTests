namespace FragilityTests.DataTest
{

    public abstract class TestResult
    {
        // Tipo de test asociado al resultado
        protected FrailtyTestType frailtyTestType;

        // Identificador del usuario que ha realizado el test
        protected string userId;

        // Fecha en la que se ha realizado el test
        protected string date;

        // Puntuación asignada según la batería de pruebas SPPB. Si es -1 es que aún no se ha calculado
        protected int points = -1;

        public TestResult(FrailtyTestType frailtyTestType, string userId, string date)
        {
            this.frailtyTestType = frailtyTestType;
            this.userId = userId;
            this.date = date;
        }

        public abstract void Autocomplete();

        public abstract string ToCsv();
    }
}
