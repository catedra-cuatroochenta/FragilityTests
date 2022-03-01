using System;
using System.IO;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Threading;
using uPLibrary.Networking.M2Mqtt;
using uPLibrary.Networking.M2Mqtt.Messages;

namespace FragilityTests.Communication
{

    public class ThreadWork
    {
        public static void DoWork()
        {
            new Example();
        }
    }

    public class Example
    {

        public Example()
        {
            Console.WriteLine("Conectando");
            
            MqttClient client = new MqttClient("35.180.55.79");
            string clientId = "cameraKinect2";
            client.Connect(clientId);
            client.Subscribe(new string[] { "camaraKinect2/login" }, new byte[] { MqttMsgBase.QOS_LEVEL_AT_MOST_ONCE });
            client.MqttMsgPublishReceived += client_MqttMsgPublishReceived;
        }

        async void client_MqttMsgPublishReceived(object sender, MqttMsgPublishEventArgs e)
        {
            Console.WriteLine("He recibido algo");

            string receivedMessage = Encoding.UTF8.GetString(e.Message);
            
            Console.WriteLine(receivedMessage);
        }



    }
}
