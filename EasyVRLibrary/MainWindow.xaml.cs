using System.IO.Ports;
using System.Windows;

namespace EasyVRLibrary
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow
    {
        private readonly EasyVr _tempVr;
        private SerialPort _port;

        public MainWindow()
        {
            InitializeComponent();
            _tempVr = new EasyVr();
        }

        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            _port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);

            // Attach a method to be called when there
            // is data waiting in the port's buffer
            _port.DataReceived += port_DataReceived;

            // Begin communications
            _port.Open();
            _port.WriteLine(RequestTb.Text);
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() => ResponseTb.AppendText(_port.ReadExisting()));

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _tempVr.ClosePort();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
           var temp = _tempVr.GetVersionId();
            ResponseTb.AppendText($"The return was: {temp}");

            var temp1 = _tempVr.PlayPhoneTone(3, 30);
            ResponseTb.AppendText($"The return was: {temp1}");
        }
    }
}
