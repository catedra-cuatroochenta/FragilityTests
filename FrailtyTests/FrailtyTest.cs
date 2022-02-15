namespace FragilityTests
{
    using System.Collections.Generic;
    using Microsoft.Kinect;
    using System.Diagnostics;

    using System.IO;

    public abstract class FrailtyTest
    {
        protected FrailtyTestType frailtyTestType;

        protected StreamWriter streamWriter;

        protected Stopwatch stopWatch = null;

        protected BodyJointsPosition bodyJointsPosition;


        public FrailtyTest(FrailtyTestType frailtyTestType)
        {
            this.frailtyTestType = frailtyTestType;
            this.streamWriter = new StreamWriter("Results.csv");
            this.bodyJointsPosition = new BodyJointsPosition();
            this.stopWatch = Stopwatch.StartNew();
            this.stopWatch.Reset();
        }
        
        public void CloseStreamWriter()
        {
            if (this.streamWriter != null)
            {
                this.streamWriter.Close();
                this.streamWriter = null;
            }
        }

        public abstract void EvaluateFrailtyTest(IReadOnlyDictionary<JointType, Joint> joints);

        public abstract string[] GetDebugValues();

        public abstract string GetCurrentStatus();

        public abstract void SaveOnFileTestResult();
    }


}
