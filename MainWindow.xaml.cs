
using System.Windows;


namespace FragilityTests
{

    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
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
    }
}
