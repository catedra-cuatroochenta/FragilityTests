namespace FragilityTests.DataTest
{
    using FragilityTests.FrailtyTests;
    using Microsoft.Kinect;
    using System;
    using System.Collections.Generic;
    using System.IO;

    /// <summary>
    /// Esta clase tiene la responsabilidad de gestionar los datos de las pruebas
    /// Tiene la responsabilidad de guardar o enviar estos resultados
    /// </summary>
    public class DataTestManager
    {
        // Para la escritura en fichero en caso de que se deseen guardar los resultados en csv
        private StreamWriter streamWriter;

        // Diccionario para almacenar el estado de las articulaciones <Value> en cada instante de tiempo <Key>
        private Dictionary<float, BodyJointsOnInstant> bodyJoints;


        public DataTestManager()
        {
            this.streamWriter = new StreamWriter("Results.csv");
            this.bodyJoints = new Dictionary<float, BodyJointsOnInstant>();
        }

        public void StartTest()
        {
            this.bodyJoints = new Dictionary<float, BodyJointsOnInstant>();
        }

        /// <summary>
        /// Método para añadir las posiciones de las articulaciones de un Body
        /// en un nuevo instante de tiempo.
        /// WARNING: ¡No se controla que la clave coincida!
        /// </summary>
        public void RegisterBodyStatus(float instant, IReadOnlyDictionary<JointType, Joint> joints)
        {
            BodyJointsOnInstant bodyJointsOnInstant = new BodyJointsOnInstant(joints);
            this.bodyJoints.Add(instant, bodyJointsOnInstant);
        }

        public void EndTest(bool validTest, TestResult testResult)
        {
            if (validTest)
            {
                testResult.Autocomplete();
                WriteResultOnFile(testResult);
                WriteRawDataOnFile();
                SendResultToServer(testResult);
                SendRawDataToServer();
            }

            this.bodyJoints.Clear();
            this.bodyJoints = null;
        }

        private void WriteResultOnFile(TestResult testResult)
        {
            streamWriter.WriteLine(testResult.ToCsv());
            streamWriter.Flush();
        }

        private void WriteRawDataOnFile()
        {
            string rawData = RawDataToCsv();
            streamWriter.WriteLine(rawData);
            streamWriter.Flush();
        }

        private void SendResultToServer(TestResult testResult)
        {
            // TODO: send testResult.ToCsv();
        }
        
        private void SendRawDataToServer()
        {
            string rawData = RawDataToCsv();
            // TODO: send rawData
        }

        public void CloseStreamWriter()
        {
            if (this.streamWriter != null)
            {
                this.streamWriter.Close();
                this.streamWriter = null;
            }
        }

        /// <summary>
        /// Construye una cadena en formato csv para guardar la información en un fichero
        /// </summary>
        private string RawDataToCsv()
        {
            string joints = "Instant;";
            for (int i = 0; i < 25; i++)
            {
                joints += ((JointType)i).ToString() + ";";
            }

            string xValues = String.Empty;
            string yValues = String.Empty;
            string zValues = String.Empty;

            BodyJointsOnInstant bodyJointsOnInstant;

            foreach (float instant in bodyJoints.Keys)
            {
                bodyJointsOnInstant = this.bodyJoints[instant];
                xValues += instant + ";" + bodyJointsOnInstant.GetXJointsValuesInCsv() + "\n";
                yValues += instant + ";" + bodyJointsOnInstant.GetYJointsValuesInCsv() + "\n";
                zValues += instant + ";" + bodyJointsOnInstant.GetZJointsValuesInCsv() + "\n";
            }
            string result = "X Values" + "\n";
            result += joints + "\n" + xValues;
            result += "Y Values" + "\n";
            result += joints + "\n" + yValues;
            result += "Z Values" + "\n";
            result += joints + "\n" + zValues;
            return result;

        }
    }
}
