using System.IO.Ports;
using System.Windows;


namespace comPortConsole
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class Console
    {

        private readonly SerialPort _port;
        public Console()
        {
            InitializeComponent();
            _port = new SerialPort("COM3", 9600, Parity.None, 8, StopBits.One);

            // Attach a method to be called when there
            // is data waiting in the port's buffer
            _port.DataReceived += port_DataReceived;

            // Begin communications
            _port.Open();
        }

        private void SubmitBtn_Click(object sender, RoutedEventArgs e)
        {
            _port.Write(RequestTb.Text);
        }

        private void port_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            Dispatcher.Invoke(() => ResponseTb.AppendText(_port.ReadExisting()));
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            _port.Close();
        }
    }
}
