using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO.Ports;
using System.Linq;
using System.Windows;   
using GalaSoft.MvvmLight;
using GalaSoft.MvvmLight.Command;
using TestHarnessMvvm.Model;
using EasyVRLibrary;

namespace TestHarnessMvvm.ViewModel
{
    /// <summary>
    /// This class contains properties that the main View can data bind to.
    /// <para>
    /// See http://www.mvvmlight.net
    /// </para>
    /// </summary>
    public class MainViewModel : ViewModelBase
    {
        private readonly IDataService _dataService;

        private readonly BackgroundWorker _worker = new BackgroundWorker();
        private EasyVr _tempVr;

        /// <summary>
        /// The <see cref="Enabled" /> property's name.
        /// </summary>
        public const string EnabledPropertyName = "Enabled";

        private bool _enabled = false;

        /// <summary>
        /// Sets and gets the Enabled property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// This property's value is broadcasted by the MessengerInstance when it changes.
        /// </summary>
        public bool Enabled
        {
            get
            {
                return _enabled;
            }

            set
            {
                if (_enabled == value)
                {
                    return;
                }

                var oldValue = _enabled;
                _enabled = value;
                RaisePropertyChanged(EnabledPropertyName, oldValue, value, true);
            }
        }

        /// <summary>
        /// The <see cref="Response" /> property's name.
        /// </summary>
        public const string ResponsePropertyName = "Response";

        private string _response = string.Empty;

        /// <summary>
        /// Sets and gets the response property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// This property's value is broadcasted by the MessengerInstance when it changes.
        /// </summary>
        public string Response
        {
            get
            {
                return _response;
            }

            set
            {
                if (_response == value)
                {
                    return;
                }

                var oldValue = _response;
                _response = value;
                RaisePropertyChanged(ResponsePropertyName, oldValue, value, true);
            }
        }

        /// <summary>
        /// The <see cref="PortList" /> property's name.
        /// </summary>
        public const string PortListPropertyName = "PortList";

        private List<string> _portList = new List<string>();

        /// <summary>
        /// Sets and gets the PortList property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// This property's value is broadcasted by the MessengerInstance when it changes.
        /// </summary>
        public List<string> PortList
        {
            get
            {
                return _portList;
            }

            set
            {
                if (_portList == value)
                {
                    return;
                }

                var oldValue = _portList;
                _portList = value;
                RaisePropertyChanged(PortListPropertyName, oldValue, value, true);
            }
        }

        /// <summary>
        /// The <see cref="SelectedPort" /> property's name.
        /// </summary>
        public const string SelectedPortPropertyName = "SelectedPort";

        private string _selectedPort = string.Empty;

        /// <summary>
        /// Sets and gets the SelectedPort property.
        /// Changes to that property's value raise the PropertyChanged event. 
        /// This property's value is broadcasted by the MessengerInstance when it changes.
        /// </summary>
        public string SelectedPort
        {
            get
            {
                return _selectedPort;
            }

            set
            {
                if (_selectedPort == value)
                {
                    return;
                }

                var oldValue = _selectedPort;
                _selectedPort = value;
                RaisePropertyChanged(SelectedPortPropertyName, oldValue, value, true);
            }
        }

        private RelayCommand _getModuleIdCommand;

        /// <summary>
        /// Gets the MyCommand.
        /// </summary>
        public RelayCommand GetModuleIdCommand
        {
            get
            {
                return _getModuleIdCommand
                    ?? (_getModuleIdCommand = new RelayCommand(
                    () =>
                    {
                        var temp = _tempVr.GetId();
                        Response = Response + ($"The return was: {temp}" + Environment.NewLine);

                    }));
            }
        }

        private RelayCommand _sendPhoneToneCommand;

        /// <summary>
        /// Gets the SendPhoneToneCommand.
        /// </summary>
        public RelayCommand SendPhoneToneCommand
        {
            get
            {
                return _sendPhoneToneCommand
                    ?? (_sendPhoneToneCommand = new RelayCommand(
                    () =>
                    {
                        var temp1 = _tempVr.PlayPhoneTone(3, 30);
                        Response = $"{Response} The return was: {temp1}{Environment.NewLine}";
                    }));
            }
        }

        private RelayCommand _connectCommand;

        /// <summary>
        /// Gets the ConnectCommand.
        /// </summary>
        public RelayCommand ConnectCommand
        {
            get
            {
                return _connectCommand
                    ?? (_connectCommand = new RelayCommand(
                    () =>
                    {
                        try
                        {
                            _tempVr = new EasyVr(SelectedPort);

                            Enabled = true;
                        }
                        catch (ArgumentNullException)
                        {
                            MessageBox.Show("Failure connecting to EasyVR");
                        }
                    }));
            }
        }

        private RelayCommand _refreshPortListCommand;

        /// <summary>
        /// Gets the RefreshPortList.
        /// </summary>
        public RelayCommand RefreshPortList
        {
            get
            {
                return _refreshPortListCommand
                    ?? (_refreshPortListCommand = new RelayCommand(
                    () =>
                    {
                        var ports = SerialPort.GetPortNames();
                        PortList = ports.ToList();
                    }));
            }
        }

        private RelayCommand _startVoiceRecognitionCommand;

        /// <summary>
        /// Gets the StartVoiceRecognition.
        /// </summary>
        public RelayCommand StartVoiceRecognition
        {
            get
            {
                return _startVoiceRecognitionCommand
                    ?? (_startVoiceRecognitionCommand = new RelayCommand(
                    () =>
                    {
                        _worker.RunWorkerAsync();
                        Enabled = false;
                    }));
            }
        }

        /// <summary>
        /// Initializes a new instance of the MainViewModel class.
        /// </summary>
        public MainViewModel(IDataService dataService)
        {
            var ports = SerialPort.GetPortNames();
            PortList = ports.ToList();
            _worker.DoWork += worker_DoWork;
            _worker.RunWorkerCompleted += worker_RunWorkerCompleted;
            _worker.WorkerSupportsCancellation = true;

            _dataService = dataService;
            _dataService.GetData(
                (item, error) =>
                {
                    if (error != null)
                    {
                        // Report error here
                        return;
                    }
                });
        }

        private void worker_DoWork(object sender, DoWorkEventArgs e)
        {
            //Logic for simple recognition activity

            // Set a 3 second timeout for the recognition (optional)
            _tempVr.SetTimeout(3);
            //instruct the module to listen for a built in word from the 1st wordset
            _tempVr.RecognizeWord(1);


            Response = Response + ("Speak" + Environment.NewLine);


            //need to wait until HasFinished has completed before collecting results
            while (!_tempVr.HasFinished())
            {
                Response = Response + (".");
            }

            // Once HasFinished has returned true, we can ask the module for the index of the word it recognised. If you're new to using the EasyVR module,
            // download the Easy VR Commander (http://www.veear.eu/downloads/) to interrogate the config of your module and see what the indexes correspond to
            // Here is a standard setup at time of writing for an EASYVR 3 module:
            // 0=Action,1=Move,2=Turn,3=Run,4=Look,5=Attack,6=Stop,7=Hello

            // NOTE: Depending on what you are looking to recognise, you may need a different method to GetWord() - GetToken and GetCommand are also available
            var indexOfRecognisedWord = _tempVr.GetWord();

            Response = Response + ("Response: " + indexOfRecognisedWord + Environment.NewLine);
            Response = Response + ("Recognition finished" + Environment.NewLine);

        }

        private void worker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            Enabled = true;

        }

        ////public override void Cleanup()
        ////{
        ////    // Clean up if needed

        ////    base.Cleanup();
        ////}
    }
}