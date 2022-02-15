using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Kinect;

namespace FragilityTests
{
    // Esta clase almacena los valores de las articulaciones de un Body
    //  en un espacio tridimensional (x, y, z) en un conjunto de instantes de tiempo 
    public class BodyJointsPosition
    {
        // Diccionario para almacenar el estado de las articulaciones <Value>
        //  en cada instante determinado <Key>
        private Dictionary<float, BodyJointsOnInstant> bodyJoints;

        public BodyJointsPosition()
        {
            this.bodyJoints = new Dictionary<float, BodyJointsOnInstant>();
        }

        // Metodo para añadir las posiciones de las articulaciones de un Body
        // en un nuevo instante de tiempo.
        // WARNING: No se controla que la clave coincida!
        public void AddBodyStatus(float instant, IReadOnlyDictionary<JointType, Joint> joints)
        {
            BodyJointsOnInstant bodyJointsOnInstant = new BodyJointsOnInstant(joints);
            this.bodyJoints.Add(instant, bodyJointsOnInstant);
        }

        // Libera la memoria
        public void ClearBodyStatus()
        {
            this.bodyJoints.Clear();
        }

        // Construye una cadena csv para guardar la información en un fichero
        public string ToCsv()
        {
            string joints = "Instant;";
            for(int i = 0; i < 25; i++)
            {
                joints += ((JointType) i).ToString() + ";";
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

    // Esta clase define la posicion de todas las articulaciones de un Body
    //  en un espacio tridimensional (x, y, z) de un instante determinado
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
                this.xJointsValues[(int) jointType] = joint.Position.X;
                this.yJointsValues[(int) jointType] = joint.Position.Y;
                this.zJointsValues[(int) jointType] = joint.Position.Z;
            }

        }

        public string GetXJointsValuesInCsv()
        {
            string result = string.Empty;
            for(int i = 0; i < numberOfJoints; i++)
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
