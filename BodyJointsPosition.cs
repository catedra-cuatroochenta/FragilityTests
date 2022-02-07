using System;
using System.Collections.Generic;
using Microsoft.Kinect;

namespace FragilityTests
{
    public class BodyJointsPosition
    {
        Dictionary<float, float[]> depthBodyJoints;
        Dictionary<float, float[]> xAxisBodyJoints;

        public BodyJointsPosition()
        {
            this.depthBodyJoints = new Dictionary<float, float[]>();
            this.xAxisBodyJoints = new Dictionary<float, float[]>();
        }

        public void AddBodyStatus(float instant, IReadOnlyDictionary<JointType, Joint> joints)
        {
            float[] jointsDepth = new float[8];
            jointsDepth[0] = joints[JointType.Head].Position.Z;
            jointsDepth[1] = joints[JointType.SpineShoulder].Position.Z;
            jointsDepth[2] = joints[JointType.SpineMid].Position.Z;
            jointsDepth[3] = joints[JointType.SpineBase].Position.Z;
            jointsDepth[4] = joints[JointType.KneeRight].Position.Z;
            jointsDepth[5] = joints[JointType.KneeLeft].Position.Z;
            jointsDepth[6] = joints[JointType.FootRight].Position.Z;
            jointsDepth[7] = joints[JointType.FootLeft].Position.Z;

            this.depthBodyJoints.Add(instant, jointsDepth);

            float[] jointsxValues = new float[8];
            jointsxValues[0] = joints[JointType.Head].Position.X;
            jointsxValues[1] = joints[JointType.SpineShoulder].Position.X;
            jointsxValues[2] = joints[JointType.SpineMid].Position.X;
            jointsxValues[3] = joints[JointType.SpineBase].Position.X;
            jointsxValues[4] = joints[JointType.KneeRight].Position.X;
            jointsxValues[5] = joints[JointType.KneeLeft].Position.X;
            jointsxValues[6] = joints[JointType.FootRight].Position.X;
            jointsxValues[7] = joints[JointType.FootLeft].Position.X;

            this.xAxisBodyJoints.Add(instant, jointsxValues);
        }

        public void ClearBodyStatus()
        {
            this.depthBodyJoints.Clear();
            this.xAxisBodyJoints.Clear();
        }


        public String ToStringDepthValues()
        {
            string result = string.Empty;
            foreach (KeyValuePair<float, float[]> kvp in depthBodyJoints)
            {
                string depthValues = string.Empty;
                foreach (float depth in kvp.Value)
                {
                    depthValues += (depth + ";");
                }
                result += string.Format("{0};{1}\n", kvp.Key, depthValues);
            }
            return result;

        }

        public String ToStringXAxisValues()
        {
            string result = string.Empty;
            foreach (KeyValuePair<float, float[]> kvp in xAxisBodyJoints)
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
