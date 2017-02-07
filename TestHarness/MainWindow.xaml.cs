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

        private readonly BackgroundWorker _worker = new BackgroundWorker();

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

            var ports = SerialPort.GetPortNames();
            foreach (var port in ports)
            {
                PortComboBox.Items.Add(port);
            }


            _worker.DoWork += worker_DoWork;
            _worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            _worker.WorkerSupportsCancellation = true;
        }
        
        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Logic for simple recognition activity

            // Set a 3 second timeout for the recognition (optional)
            _tempVr.SetTimeout(3);
            //instruct the module to listen for a built in word from the 1st wordset
            _tempVr.RecognizeWord(1);

            Dispatcher.BeginInvoke((Action)delegate {
                ResponseTb.AppendText("Speak" + Environment.NewLine);
                ResponseTb.ScrollToEnd();
            });

            //need to wait until HasFinished has completed before collecting results
            while (!_tempVr.HasFinished())
            {
                Dispatcher.BeginInvoke((Action)delegate {
                    ResponseTb.AppendText(".");
                });
            }

            // Once HasFinished has returned true, we can ask the module for the index of the word it recognised. If you're new to using the EasyVR module,
            // download the Easy VR Commander (http://www.veear.eu/downloads/) to interrogate the config of your module and see what the indexes correspond to
            // Here is a standard setup at time of writing for an EASYVR 3 module:
            // 0=Action,1=Move,2=Turn,3=Run,4=Look,5=Attack,6=Stop,7=Hello
            var indexOfRecognisedWord = _tempVr.GetWord();
            
            Dispatcher.BeginInvoke((Action)delegate {
                ResponseTb.AppendText("Response: "+indexOfRecognisedWord + Environment.NewLine);
                ResponseTb.AppendText("Recognition finished" + Environment.NewLine);
                ResponseTb.ScrollToEnd();
            });
            
        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            StartBtn.IsEnabled = true;
          
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
            ResponseTb.ScrollToEnd();
        }

        private void GetModuleIdButton_Click(object sender, RoutedEventArgs e)
        {
            var temp = _tempVr.GetId();
            ResponseTb.AppendText($"The return was: {temp}" + Environment.NewLine);
            ResponseTb.ScrollToEnd();
        }

        private void StartBtn_Click(object sender, RoutedEventArgs e)
        {
          _worker.RunWorkerAsync();
            StartBtn.IsEnabled = false;
        }
    }
}

