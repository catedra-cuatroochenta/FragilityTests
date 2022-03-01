using System;
using System.IO;

namespace FragilityTests.Communication
{
    public class KinectManager
    {
        private static string kinectId = null;

        private KinectManager() { }

        public static string GetKinectId()
        {
            if (kinectId == null)
            {
                kinectId = ReadKinectId();
            }
            return kinectId;
        }

        private static string ReadKinectId()
        {
            string id = String.Empty;
            try
            {
                using (StreamReader sr = new StreamReader("KinectId.txt"))
                {
                    string line;
                    if ((line = sr.ReadLine()) != null)
                    {
                        id = line.Trim();
                    }
                }
            }
            catch (Exception e)
            {
                id = CreateNewKinectId();
            }

            return id;
        }

        private static string CreateNewKinectId()
        {
            StreamWriter sw = new StreamWriter("KinectId.txt");
            Guid guid = Guid.NewGuid();
            sw.WriteLine(guid.ToString());
            sw.Close();
            return guid.ToString();
        }
    }
}
