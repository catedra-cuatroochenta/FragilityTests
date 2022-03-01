
using FragilityTests.Communication;
using QRCoder;
using QRCoder.Xaml;
using System.ComponentModel;
using System.Drawing;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace FragilityTests
{

    public partial class MainWindow : Window
    {
        Thread listenerThread;


        public MainWindow()
        {
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode("hola", QRCodeGenerator.ECCLevel.H);
            XamlQRCode qrCode = new XamlQRCode(qrCodeData);
            DrawingImage qrCodeAsXaml = qrCode.GetGraphic(20);

            InitializeComponent();
            QrPicture.Source = qrCodeAsXaml;
            listenerThread = new Thread(ThreadWork.DoWork);
            listenerThread.IsBackground = true;
            listenerThread.Start();

        }


        private void GaitSpeedTestClicked(object sender, RoutedEventArgs e)
        {
            GaitSpeedTestWindow window = new GaitSpeedTestWindow();
            this.Close();
            window.Show();
        }

        private void PerpendicularGaitSpeedTestClicked(object sender, RoutedEventArgs e)
        {
            PerpendicularGaitSpeedTestWindow window = new PerpendicularGaitSpeedTestWindow();
            this.Close();
            window.Show();
        }

        private void GetUpTestClicked(object sender, RoutedEventArgs e)
        {
            GetUpTestWindow window = new GetUpTestWindow();
            this.Close();
            window.Show();
        }

        private void BalanceTestClicked(object sender, RoutedEventArgs e)
        {
            BalanceTestWindow window = new BalanceTestWindow();
            this.Close();
            window.Show();
        }

        private void MainWindow_Closing(object sender, CancelEventArgs e)
        {
            listenerThread.Abort();
        }
        

    }
}
