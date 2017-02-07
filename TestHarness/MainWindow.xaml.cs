using System;
using System.ComponentModel;
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

        private readonly BackgroundWorker worker = new BackgroundWorker();

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


            worker.DoWork += worker_DoWork;
            worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            worker.WorkerSupportsCancellation = true;
        }
        
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //_tempVr.RecognizeCommand(1);
            _tempVr.RecognizeWord(1);
            while (!_tempVr.HasFinished())
            {
              
            }

            Dispatcher.BeginInvoke((Action)delegate {
                ResponseTb.AppendText(_tempVr.GetWord() + Environment.NewLine);
            });

            Dispatcher.BeginInvoke((Action)delegate {
                ResponseTb.AppendText("Loop finished" + Environment.NewLine);
            });
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StartBtn.IsEnabled = true;
            StopBtn.IsEnabled = false;
        }

        private void Window_Closing(object sender, CancelEventArgs e)
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

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
          worker.RunWorkerAsync();
            StartBtn.IsEnabled = false;
        }

        private void StopBtn_Click(object sender, RoutedEventArgs e)
        {
            worker.CancelAsync();
            StartBtn.IsEnabled = true;
        }
    }
}

