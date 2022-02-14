using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kinect;

namespace FragilityTests
{
    public class BodyJointsPosition
    {

        private Dictionary<float, float[]> xAxisBodyJointsPos;
        private Dictionary<float, float[]> yAxisBodyJointsPos;
        private Dictionary<float, float[]> zAxisBodyJointsPos;

        public BodyJointsPosition()
        {
            this.xAxisBodyJointsPos = new Dictionary<float, float[]>();
            this.yAxisBodyJointsPos = new Dictionary<float, float[]>();
            this.zAxisBodyJointsPos = new Dictionary<float, float[]>();

        }

        public void AddBodyStatus(float instant, IReadOnlyDictionary<JointType, Joint> joints)
        {
            float[] xValues = new float[25];
            float[] yValues = new float[25];
            float[] zValues = new float[25];
            int i = 0;

            foreach (JointType jointType in joints.Keys)
            {
                Joint joint = joints[jointType];
                xValues[i] = joint.Position.X;
                yValues[i] = joint.Position.Y;
                zValues[i] = joint.Position.Z;
                i++;
            }

            this.xAxisBodyJointsPos.Add(instant, xValues);
            this.yAxisBodyJointsPos.Add(instant, yValues);
            this.zAxisBodyJointsPos.Add(instant, zValues);
        }

        public void ClearBodyStatus()
        {
            this.xAxisBodyJointsPos.Clear();
            this.yAxisBodyJointsPos.Clear();
            this.zAxisBodyJointsPos.Clear();

        }

        public string ToCsv()
        {

            string result = string.Empty;
            foreach (KeyValuePair<float, float[]> kvp in xAxisBodyJointsPos)
            {
                string xAxisValues = string.Empty;
                foreach (float xAxisPos in kvp.Value)
                {
                    xAxisValues += (xAxisPos + ";");
                }
                result += string.Format("{0};{1}\n", kvp.Key, xAxisValues);
            }
            result += "\n";
            
            foreach (KeyValuePair<float, float[]> kvp in yAxisBodyJointsPos)
            {
                string xAxisValues = string.Empty;
                foreach (float xAxisPos in kvp.Value)
                {
                    xAxisValues += (xAxisPos + ";");
                }
                result += string.Format("{0};{1}\n", kvp.Key, xAxisValues);
            }
            result += "\n";

            foreach (KeyValuePair<float, float[]> kvp in zAxisBodyJointsPos)
            {
                string xAxisValues = string.Empty;
                foreach (float xAxisPos in kvp.Value)
                {
                    xAxisValues += (xAxisPos + ";");
                }
                result += string.Format("{0};{1}\n", kvp.Key, xAxisValues);
            }
            return result;
            
        }



    }

}
