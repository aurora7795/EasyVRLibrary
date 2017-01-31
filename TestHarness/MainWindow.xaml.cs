using System;
using System.IO.Ports;
using System.Windows;
using EasyVRLibrary;

namespace TestHarness
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private EasyVr _tempVr;

       

        public bool Enabled
        {
            get { return (bool)GetValue(MyBoolProperty); }
            set { SetValue(MyBoolProperty, value); }
        }

        // Using a DependencyProperty as the backing store for myText.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty MyBoolProperty =
            DependencyProperty.Register("Enabled", typeof(bool), typeof(MainWindow), new PropertyMetadata(null));

        public MainWindow()
        {
            InitializeComponent();

            string[] ports = SerialPort.GetPortNames();
            foreach (string port in ports)
            {
                PortComboBox.Items.Add(port);
            }

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _tempVr?.ClosePort();
        }

        private void ConnectButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _tempVr = new EasyVr(PortComboBox.SelectedItem.ToString());

                Enabled = true;
            }
            catch (ArgumentNullException)
            {
                MessageBox.Show("Failure connecting to EasyVR");
            }
        }

        private void SendPhoneToneButton_Click(object sender, RoutedEventArgs e)
        {
            var temp1 = _tempVr.PlayPhoneTone(3, 30);
            ResponseTb.AppendText($"The return was: {temp1}" + Environment.NewLine);
        }

        private void GetModuleIdButton_Click(object sender, RoutedEventArgs e)
        {
            var temp = _tempVr.GetId();
            ResponseTb.AppendText($"The return was: {temp}" + Environment.NewLine);
        }
    }
}

