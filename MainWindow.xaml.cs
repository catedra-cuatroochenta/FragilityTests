
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
            GaitSpeedTestWindow gaitSpeedTestWindow = new GaitSpeedTestWindow();
            this.Close();
            gaitSpeedTestWindow.Show();
        }

        private void PerpendicularGaitSpeedTestClicked(object sender, RoutedEventArgs e)
        {
            MainTitle.Text = "Sa cambiao";
            return;
        }
    }
}
