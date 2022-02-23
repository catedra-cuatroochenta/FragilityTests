
using FragilityTests.Communication;
using System;
using System.ComponentModel;
using System.Threading;
using System.Windows;


namespace FragilityTests
{

    public partial class MainWindow : Window
    {
        Thread listenerThread;

        public MainWindow()
        {
            InitializeComponent();
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
