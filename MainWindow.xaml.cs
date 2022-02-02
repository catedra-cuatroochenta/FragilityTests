
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
            WalkingSpeedTest1 walkingSpeedTest = new WalkingSpeedTest1();
            this.Close();
            walkingSpeedTest.Show();
        }

        private void PerpendicularGaitSpeedTestClicked(object sender, RoutedEventArgs e)
        {
            MainTitle.Text = "Sa cambiao";
            return;
        }
    }
}
