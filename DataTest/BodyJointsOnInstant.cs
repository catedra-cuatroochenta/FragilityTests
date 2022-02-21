namespace FragilityTests.FrailtyTests
{
    using Microsoft.Kinect;
    using System.Collections.Generic;


    /// </summary>
    /// Esta clase define la posicion de todas las articulaciones de un Body
    /// en un espacio tridimensional (x, y, z) de un instante determinado
    /// <summary>
    public class BodyJointsOnInstant
    {
        // Total de articulaciones con las que trabaja la kinect v2 
        const int numberOfJoints = 25;

        // Valores de todas las articulaciones con respecto al eje X
        private float[] xJointsValues;

        // Valores de todas las articulaciones con respecto al eje Y
        private float[] yJointsValues;

        // Valores de todas las articulaciones con respecto al eje Z
        private float[] zJointsValues;

        public BodyJointsOnInstant(IReadOnlyDictionary<JointType, Joint> joints)
        {
            this.xJointsValues = new float[numberOfJoints];
            this.yJointsValues = new float[numberOfJoints];
            this.zJointsValues = new float[numberOfJoints];

            // Almacenamos en los vectores segun el orden de la enumeracion JointType
            foreach (JointType jointType in joints.Keys)
            {
                Joint joint = joints[jointType];
                this.xJointsValues[(int)jointType] = joint.Position.X;
                this.yJointsValues[(int)jointType] = joint.Position.Y;
                this.zJointsValues[(int)jointType] = joint.Position.Z;
            }

        }

        public string GetXJointsValuesInCsv()
        {
            string result = string.Empty;
            for (int i = 0; i < numberOfJoints; i++)
            {
                result += xJointsValues[i] + ";";
            }
            return result;
        }

        public string GetYJointsValuesInCsv()
        {
            string result = string.Empty;
            for (int i = 0; i < numberOfJoints; i++)
            {
                result += yJointsValues[i] + ";";
            }
            return result;
        }

        public string GetZJointsValuesInCsv()
        {
            string result = string.Empty;
            for (int i = 0; i < numberOfJoints; i++)
            {
                result += zJointsValues[i] + ";";
            }
            return result;
        }
    }
}
