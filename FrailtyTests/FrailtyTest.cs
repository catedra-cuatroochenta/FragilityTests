namespace FragilityTests
{
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using System.Diagnostics;
    using System.IO;
    using FragilityTests.DataTest;

    public abstract class FrailtyTest
    {
        protected FrailtyTestType frailtyTestType;

        protected Stopwatch stopWatch = null;

        protected DataTestManager dtManager;


        public FrailtyTest(FrailtyTestType frailtyTestType)
        {
            this.frailtyTestType = frailtyTestType;
            this.dtManager = new DataTestManager();
            this.stopWatch = Stopwatch.StartNew();
            this.stopWatch.Reset();
        }
        
        public void Close()
        {
            dtManager.CloseStreamWriter();
        }

        public abstract void EvaluateFrailtyTest(IReadOnlyDictionary<JointType, Joint> joints);

        public abstract string[] GetDebugValues();

        public abstract string GetCurrentStatus();

    }


}
