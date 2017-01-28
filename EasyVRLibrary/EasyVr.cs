using System;
using System.IO;
using System.IO.Ports;
using static EasyVRLibrary.Protocol;


namespace EasyVRLibrary
{
    public class EasyVr
    {
        public const int EASYVR_RX_TIMEOUT = 100;
        public const int EASYVR_STORAGE_TIMEOUT = 500;
        public const int EASYVR_WAKE_TIMEOUT = 200;
        public const int EASYVR_PLAY_TIMEOUT = 5000;
        public const int EASYVR_TOKEN_TIMEOUT = 1500;

        private static SerialPort _serialPort; // communication interface for the EasyVR module

        private int _value; // store last result or error code

        public EasyVr(string portName, Baudrate baudRate = Baudrate.B9600)
        {
            if (_serialPort != null) return;
            // Create the serial port with basic settings
            _serialPort = new SerialPort(portName, (int)baudRate, Parity.None, 8, StopBits.One);

            _serialPort.Open();

            _value = -1;
            _group = -1;
            _id = -1;
            _status.v = 0;
        }

        public void ClosePort()
        {
            _serialPort.Close();
        }

        private class Status
        {
            public sbyte v;

            public bool _command;
            public bool _builtin;
            public bool _error;
            public bool _timeout;
            public bool _invalid;
            public bool _memfull;
            public bool _conflict;
            public bool _token;
            public bool _awakened;
        }

        private readonly Status _status = new Status();

        protected sbyte _group; // last used group (cached by the module)

        protected sbyte _id; // last detected module id (can optimize some functions)

        private sbyte NO_TIMEOUT = 0;
        private sbyte INFINITE = -1;
        private const sbyte DEF_TIMEOUT = EASYVR_RX_TIMEOUT;
        private const short WAKE_TIMEOUT = EASYVR_WAKE_TIMEOUT;
        private short PLAY_TIMEOUT = EASYVR_PLAY_TIMEOUT;
        private short TOKEN_TIMEOUT = EASYVR_TOKEN_TIMEOUT;
        private const short STORAGE_TIMEOUT = EASYVR_STORAGE_TIMEOUT;

        private static void SendCommand(char command)
        {
            _serialPort.Write(command.ToString());
        }

        private static void SendArgument(int argument)
        {
            _serialPort.Write(ArgumentEncoding.IntToArgumentString(argument));
        }

        private static char GetResponse()
        {
            var temp = _serialPort.ReadByte();

            return (char)temp;
        }

        private static bool ReceiveArgument(out char response)
        {
            SendCommand((char)ARG_ACK);
            response = GetResponse();
            return response >= ARG_MIN && response <= ARG_MAX;
        }

        private void ReadStatus(char rx)
        {
            _status.v = 0;
            _value = 0;

            switch (rx)
            {
                case STS_SUCCESS:
                    return;

                case STS_SIMILAR:
                    _status._builtin = true;
                    goto GET_WORD_INDEX;

                case STS_RESULT:
                    _status._command = true;

                    GET_WORD_INDEX:
                    if (ReceiveArgument(out rx))
                    {
                        _value = rx;
                        return;
                    }
                    break;

                case STS_TOKEN:
                    _status._token = true;

                    if (ReceiveArgument(out rx))
                    {
                        _value = rx << 5;
                        if (ReceiveArgument(out rx))
                        {
                            _value |= rx;
                            return;
                        }
                    }
                    break;

                case STS_AWAKEN:
                    _status._awakened = true;
                    return;

                case STS_TIMEOUT:
                    _status._timeout = true;
                    return;

                case STS_INVALID:
                    _status._invalid = true;
                    return;

                case STS_ERROR:
                    _status._error = true;


                    if (ReceiveArgument(out rx))
                    {
                        _value = rx << 4;
                        if (ReceiveArgument(out rx))
                        {
                            _value |= rx;
                            return;
                        }
                    }
                    break;
            }

            // unexpected condition (communication error)
            _status.v = 0;
            _status._error = true;
            _value = 0;
        }
        
        /// <summary>
        ///   Detects an EasyVR module, waking it from sleep mode and checking it responds correctly.
        /// </summary>
        /// <returns>true if a compatible module has been found</returns>
        public bool Detect()
        {
            int i;
            for (i = 0; i < 5; ++i)
            {
                SendCommand(CMD_BREAK);

                if (GetResponse() == STS_SUCCESS)
                    return true;
            }
            return false;
        }


        /// <summary>
        ///  Interrupts pending recognition or playback operations.
        /// </summary>
        /// <returns>True if the request is satisfied and the module is back to ready</returns>
        public bool Stop()
        {
            SendCommand(CMD_BREAK);

            var rx = GetResponse();
            return rx == STS_INTERR || rx == STS_SUCCESS;
        }
        
        /// <summary>
        ///  Sets the language to use for recognition of built-in words.
        /// </summary>
        /// <param name="lang">(0-5) is one of values in #Language</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetLanguage(Language lang)
        {
            SendCommand(CMD_LANGUAGE);
            SendArgument((int)lang);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Sets the timeout to use for any recognition task.
        /// </summary>
        /// <param name="seconds">(0-31) is the maximum time the module keep listening</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetTimeout(int seconds)
        {
            if (seconds < 0 || seconds > 31) throw new ArgumentOutOfRangeException(nameof(seconds));
            SendCommand(CMD_TIMEOUT);
            SendArgument(seconds);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Sets the operating distance of the microphone.
        /// This setting represents the distance between the microphone and the
        /// user's mouth, in one of three possible configurations.
        /// </summary>
        /// <param name="distance">dist (1-3) is one of values in #Distance</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetMicDistance(Distance distance)
        {
            SendCommand(CMD_MIC_DIST);
            SendArgument((int)distance);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///  Sets the confidence threshold to use for recognition of built-in words or custom grammars.
        /// </summary>
        /// <param name="knob">(0-4) is one of values in #Knob</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetKnob(Knob knob)
        {
            SendCommand(CMD_KNOB);
            SendArgument((int)knob);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Gets the module identification number (firmware version).
        /// </summary>
        /// <returns>Module ID for the easy VR module</returns>
        public ModuleId GetId()
        {
            SendCommand(STS_ID);

            var response = GetResponse();
            if (response != STS_ID)
            {
                throw new Exception($"Invalid response: {response}");
            }

            ReceiveArgument(out response);

            var decodedValue = ArgumentEncoding.ConvertArgumentCode(response);
            var tempModule = (ModuleId)decodedValue;

            return tempModule;
        }


        /// <summary>
        /// Sets the trailing silence duration for recognition of built-in words or custom grammars.
        /// </summary>
        /// <param name="duration">(0-31) is the silence duration as defined in #TrailingSilence</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetTrailingSilence(TrailingSilence duration)
        {
            SendCommand(CMD_TRAILING);
            SendArgument((int)duration);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Sets the strictness level to use for recognition of custom commands.
        /// </summary>
        /// <param name="level">level (1-5) is one of values in #Level</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetLevel(Level level)
        {
            SendCommand(CMD_LEVEL);
            SendArgument((int)level);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Enables or disables fast recognition for custom commands and passwords.
        /// Fast SD/SV recognition can improve response time.
        /// </summary>
        /// <param name="mode">(0-1) is one of the values in #CommandLatency</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetCommandLatency(CommandLatency mode)
        {
            SendCommand(CMD_FAST_SD);
            SendArgument((int)mode);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Sets the delay before any reply of the module.
        /// </summary>
        /// <param name="millis">millis (0-1000) is the delay duration in milliseconds, rounded to
        ///  10 units in range 10-100 and to 100 units in range 100-1000.</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetDelay(ushort millis)
        {
            SendCommand(CMD_DELAY);
            if (millis <= 10)
                SendArgument((sbyte)millis);
            else if (millis <= 100)
                SendArgument((sbyte)(millis / 10 + 9));
            else if (millis <= 1000)
                SendArgument((sbyte)(millis / 100 + 18));
            else
                return false;

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///  Sets the new communication speed. You need to modify the baudrate of the
        ///  underlying Stream object accordingly, after the function returns successfully.
        /// </summary>
        /// <param name="baudRate">one of values in #Baudrate</param>
        /// <returns>true if the operation is successful</returns>
        public bool ChangeBaudrate(Baudrate baudRate)
        {
            SendCommand(CMD_BAUDRATE);
            SendArgument((int)baudRate);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Puts the module in sleep mode.
        /// </summary>
        /// <param name="mode">mode is one of values in #WakeMode, optionally combined with one of
        /// the values in #ClapSense</param>
        /// <returns>true if the operation is successful</returns>
        public bool Sleep(WakeMode mode)
        {
            SendCommand(CMD_SLEEP);
            SendArgument((int)mode);

            return GetResponse() == STS_SUCCESS;
        }

        // command management

        /// <summary>
        ///  Adds a new custom command to a group.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        /// <returns>true if the operation is successful</returns>
        public bool AddCommand(int group, int index)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));
            if (index < 0 || index > 31) throw new ArgumentOutOfRangeException(nameof(index));

            SendCommand(CMD_GROUP_SD);
            SendArgument(group);
            SendArgument(index);

            var rx = GetResponse();
            if (rx == STS_SUCCESS)
                return true;
            _status.v = 0;
            if (rx == STS_OUT_OF_MEM)
                _status._memfull = true;
            return false;
        }

        /// <summary>
        /// Removes a custom command from a group.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        /// <returns>true if the operation is successful</returns>
        public bool RemoveCommand(int group, int index)
        {
            SendCommand(CMD_UNGROUP_SD);
            SendArgument(group);
            SendArgument(index);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        /// Sets the name of a custom command.
        /// </summary>
        /// <param name="group">group (0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">index (0-31) is the index of the command within the selected group</param>
        /// <param name="name">name is a string containing the label to be assigned to the specified command</param>
        /// <returns>true if the operation is successful</returns>

        //TODO: Fix this setCommandLabel method
        //private bool SetCommandLabel(sbyte group, sbyte index, string name)
        //{
        //    SendCommand(CMD_NAME_SD);
        //    SendArgument(group);
        //    SendArgument(index);

        //    sbyte len = 31;
        //    for (var p = name; p != 0 && len > 0; ++p, --len)

        //    {
        //        int n;
        //        if (int.TryParse(p, out n))
        //            --len;
        //    }
        //    len = (sbyte)(31 - len);

        //    SendCommand((char) len);

        //    for (sbyte i = 0; i < len; ++i)
        //    {
        //        char c = name[i];
        //        if (char.IsDigit(c))
        //        {
        //            SendCommand('^');
        //            SendCommand((sbyte)(c - '0'));
        //        }
        //        else if (char.IsLetter(c))
        //        {
        //            SendCommand((char) (c & ~0x20)); // to uppercase
        //        }
        //        else
        //        {
        //            SendCommand('_');
        //        }
        //    }

        //    return GetResponse() == STS_SUCCESS;
        //}

        /// <summary>
        /// Erases the training data of a custom command.
        /// </summary>
        /// <param name="group"> (0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        /// <returns>true if the operation is successful</returns>
        public bool EraseCommand(int group, int index)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));
            if (index < 0 || index > 31) throw new ArgumentOutOfRangeException(nameof(index));

            SendCommand(CMD_ERASE_SD);
            SendArgument(group);
            SendArgument(index);

            return GetResponse() == STS_SUCCESS;
        }

        //TODO: wtf! don't understadn this C code - review with dad

        /// <summary>
        /// Gets a bit mask of groups that contain at least one command.
        /// </summary>
        /// <param name="mask">mask is a variable to hold the group mask when the function returns</param>
        /// <returns>true if the operation is successful</returns>
        //public bool GetGroupMask(out uint mask)
        // {
        //     SendCommand(CMD_MASK_SD);
        //     mask = 0;
        //     if (GetResponse() != STS_MASK) return false;

        //     for (var i = 0; i < 4; ++i)
        //     {
        //         char rx ;
        //         if (!ReceiveArgument(out rx))
        //             return false;
        //         ((int)mask)[i] |= rx & 0x0F;
        //         if (!ReceiveArgument(out rx))
        //             return false;
        //         ((int)mask)[i] |= (rx << 4) & 0xF0;
        //     }
        //     return true;
        // }

        /// <summary>
        /// Gets the number of commands in the specified group.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <returns>integer is the count of commands (negative in case of errors)</returns>
        public int GetCommandCount(int group)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));

            SendCommand(CMD_COUNT_SD);
            SendArgument(group);

            if (GetResponse() != STS_COUNT) return -1;

            var response = GetResponse();
            return ArgumentEncoding.ConvertArgumentCode(response);
        }

        /**
                  Retrieves the name and training data of a custom command.
                  @param group (0-16) is the target group, or one of the values in #Groups
                  @param index (0-31) is the index of the command within the selected group
                  @param name points to an array of at least 32 characters that holds the
                  command label when the function returns
                  @param training is a variable that holds the training count when the
                  function returns. Additional information about training is available
                  through the functions #isConflict() and #getWord() or #getCommand()
                  @retval true if the operation is successful
                */
        //C++ TO C# CONVERTER TODO TASK: The implementation of the following method could not be found:
        //bool dumpCommand(sbyte group, sbyte index, ref string name, ref byte training)
        //{
        //    //TODO: compicated one
        //}

        // custom grammars

        /// <summary>
        /// Gets the total number of grammars available, including built-in and custom.
        /// </summary>
        /// <returns>integer is the count of grammars (negative in case of errors)</returns>
        public int GetGrammarsCount()
        {
            SendCommand(CMD_DUMP_SI);

            if (GetResponse() != STS_COUNT) return -1;
            var response = GetResponse();
            return ArgumentEncoding.ConvertArgumentCode(response);
        }

        /// <summary>
        /// Retrieves the contents of a built-in or a custom grammar.
        /// Command labels contained in the grammar can be obtained by calling #getNextWordLabel()
        /// </summary>
        /// <param name="grammar">(0-31) is the target grammar, or one of the values in #Wordset</param>
        /// <param name="flags">is a variable that holds some grammar flags when the function returns. See #GrammarFlag</param>
        /// <param name="count">count is a variable that holds the number of words in the grammar when the function returns.</param>
        /// <returns>true if the operation is successful</returns>
        public bool DumpGrammar(sbyte grammar, ref byte flags, ref byte count)
        {
            if (grammar < 0 || grammar > 31) throw new ArgumentOutOfRangeException(nameof(grammar));

            SendCommand(CMD_DUMP_SI);
            SendArgument(grammar);

            if (GetResponse() != STS_GRAMMAR)
                return false;

            char rx;
            if (!ReceiveArgument(out rx))
                return false;
            flags = (byte)(rx == -1 ? 32 : rx);

            if (!ReceiveArgument(out rx))
                return false;
            count = (byte)rx;
            return true;
        }

        /**
          Retrieves the name of a command contained in a custom grammar.
          It must be called after #dumpGrammar()
          @param name points to an array of at least 32 characters that holds the
          command label when the function returns
          @retval true if the operation is successful
        */

        // TODO: what is going here?
        //bool getNextWordLabel(ref string name)
        //{
        //    int count =0;
        //    if (!recvArg(ref count))
        //        return false;
        //    if (count == -1)
        //        count = 32;

        //    for (; count > 0; --count, ++name)
        //    {
        //        var rx =0;
        //        if (!recvArg(ref rx))
        //            return false;

        //        if (rx == '^' - char.ARG_ZERO)
        //        {
        //            if (!recvArg(ref rx))
        //                return false;

        //            name = '0' + rx;
        //            --count;
        //        }
        //        else
        //        {
        //            *name = ARG_ZERO + rx;
        //        }
        //    }
        //    *name = 0;
        //    return true; 
        //}
        // recognition/training


        /// <summary>
        /// Starts training of a custom command. Results are available after #hasFinished() returns true.
        /// 
        /// The module is busy until training completes and it cannot accept other commands. You can interrupt training with #stop().
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        public void TrainCommand(int group, int index)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));
            if (index < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(index));

            SendCommand(CMD_TRAIN_SD);
            SendArgument(group);
            SendArgument(index);

        }

        /// <summary>
        /// Starts recognition of a custom command. Results are available after #hasFinished() returns true.
        /// 
        /// The module is busy until recognition completes and it cannot accept other commands. You can interrupt recognition with #stop().
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        public void RecognizeCommand(int group)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));

            SendCommand(CMD_RECOG_SD);
            SendArgument(group);

        }

        /// <summary>
        /// Starts recognition of a built-in word. Results are available after #hasFinished() returns true.
        /// 
        /// The module is busy until recognition completes and it cannot saccept other commands. You can interrupt recognition with #stop().
        /// </summary>
        /// <param name="wordset">(0-3) is the target word set, or one of the values in #Wordset, (4-31) is the target custom grammar, if present</param>
        public void RecognizeWord(int wordset)
        {
            if (wordset < 0 || wordset > 31) throw new ArgumentOutOfRangeException(nameof(wordset));

            SendCommand(CMD_RECOG_SI);
            SendArgument(wordset);
        }

        /// <summary>
        ///  Polls the status of on-going recognition, training or asynchronous playback tasks.
        /// </summary>
        /// <returns>true if the operation has completed</returns>
        public bool HasFinished()
        {
            var rx = GetResponse();
            if (rx < 0)
                return false;

            ReadStatus(rx);
            return true;
        }


        // analyse result

        /// <summary>
        /// Gets the recognised command index if any.
        /// </summary>
        /// <returns>(0-31) is the command index if recognition is successful, (-1) if no command has been recognized or an error occurred</returns>
        public sbyte GetCommand()
        {
            if (_status._command) return (sbyte)_value;
            return -1;
        }

        /// <summary>
        /// Gets the recognised word index if any, from built-in sets or custom grammars.
        /// </summary>
        /// <returns>(0-31) is the command index if recognition is successful, (-1) if no built-in word has been recognized or an error occurred</returns>
        public sbyte GetWord()
        {
            if (_status._builtin) return (sbyte)_value;
            return -1;
        }

        /// <summary>
        /// Gets the index of the received SonicNet token if any.
        /// </summary>
        /// <returns>integer is the index of the received SonicNet token (0-255 for 8-bit tokens or 0-15 for 4-bit tokens) 
        /// if detection was successful, (-1) if no token has been received or an error occurred</returns>
        public short GetToken()
        {
            if (_status._token) return (short)_value;
            return -1;
        }

        /// <summary>
        /// Gets the last error code if any.
        /// </summary>
        /// <returns>(0-255) is the error code, (-1) if no error occurred</returns>
        public short GetError()
        {
            if (_status._error) return (short)_value;
            return -1;
        }

        /// <summary>
        /// Retrieves the timeout indicator.
        /// </summary>
        /// <returns>true if a timeout occurred</returns>
        public bool IsTimeout()
        {
            return _status._timeout;
        }

        /// <summary>
        ///  Retrieves the wake-up indicator (only valid after #hasFinished() has been called).
        /// </summary>
        /// <returns>true if the module has been awakened from sleep mode</returns>
        public bool IsAwakened()
        {
            return _status._awakened;
        }

        /// <summary>
        /// Retrieves the conflict indicator.
        /// </summary>
        /// <returns>true is a conflict occurred during training. To know what caused the conflict, use #getCommand() and #getWord() (only valid for triggers)</returns>
        public bool IsConflict()
        {
            return _status._conflict;
        }

        /// <summary>
        ///  Retrieves the memory full indicator (only valid after #addCommand() returned false).
        /// </summary>
        /// <returns>true if a command could not be added because of memory size constaints(up to 32 custom commands can be created)</returns>
        public bool IsMemoryFull()
        {
            return _status._memfull;
        }

        /// <summary>
        ///   Retrieves the invalid protocol indicator.
        /// </summary>
        /// <returns>true if an invalid sequence has been detected in the communication protocol</returns>
        public bool IsInvalid()
        {
            return _status._invalid;
        }


        // pin I/O functions

        /// <summary>
        ///  Configures an I/O pin as an output and sets its value
        /// </summary>
        /// <param name="pin">(1-3) is one of values in #PinNumber</param>
        /// <param name="value">(0-1) is one of the output values in #PinConfig, or Arduino style HIGH and LOW macros</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetPinOutput(PinNumber pin, PinConfig value)
        {
            if ((int)pin > 3)
            {
                throw new ArgumentException("Invalid Pin number");
            }

            if ((int)value > 1)
            {
                throw new ArgumentException("Invalid output value");
            }

            SendCommand(CMD_QUERY_IO);
            SendArgument((int)pin);
            SendArgument((int)value);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///  Configures an I/O pin as an input with optional pull-up and return its value
        /// </summary>
        /// <param name="pin">(1-3) is one of values in #PinNumber</param>
        /// <param name="config"> (2-4) is one of the input values in #PinConfig</param>
        /// <returns>integer is the logical value of the pin</returns>
        public int SetPinInput(PinNumber pin, PinConfig config)
        {
            if (config == PinConfig.INPUT_HIZ || config == PinConfig.INPUT_STRONG)
            {
                throw new ArgumentException("Invalid Pin Configuration");
            }

            SendCommand(CMD_QUERY_IO);
            SendArgument((int)pin);
            SendArgument((int)config);

            if (GetResponse() == STS_PIN)
            {
                return ArgumentEncoding.ConvertArgumentCode(GetResponse());
            }
            return -1;
        }


        // sound table functions

        /// <summary>
        /// Starts listening for a SonicNet token. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="bits">(4 or 8) specifies the length of received tokens</param>
        /// <param name="rejection">rejection (0-2) specifies the noise rejection level, it can be one of the values in #RejectionLevel</param>
        /// <param name="timeout">timeout (1-28090) is the maximum time in milliseconds to keep listening for a valid token or(0) to listen without time limits.</param>
        /// <remarks>The module is busy until token detection completes and it cannot accept other commands.You can interrupt listening with #stop().</remarks>
        public void DetectToken(BitNumber bits, RejectionLevel rejection, int timeout)
        {
            SendCommand(CMD_RECV_SN);
            SendArgument((int)bits);
            SendArgument((int)rejection);

            if (timeout > 0)
                timeout = (timeout * 2 + 53) / 55; // approx / 27.46 - err < 0.15%
            SendArgument((timeout >> 5) & 0x1F);
            SendArgument(timeout & 0x1F);
        }

        /// <summary>
        /// Starts immediate playback of a SonicNet token. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="bits">bits (4 or 8) specifies the length of trasmitted token</param>
        /// <param name="token">token is the index of the SonicNet token to play (0-255 for 8-bit tokens or 0-15 for 4-bit tokens)</param>
        /// <remarks>The module is busy until playback completes and it cannot accept other commands.You can interrupt playback with #stop().</remarks>
        public void SendTokenAsync(BitNumber bits, byte token)
        {
            switch (bits)
            {
                case BitNumber.BITS_4:
                    if (token > 15)
                    {
                        throw new ArgumentException("Invalid token for token length (must be between 0-15)");
                    }
                    break;
                case BitNumber.BITS_8:
                    if (token > 255)
                    {
                        throw new ArgumentException("Invalid token for token length (must be between 0-255)");
                    }
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bits), bits, null);
            }

            SendCommand(CMD_SEND_SN);
            SendArgument((int)bits);
            SendArgument(token);
        }

        /// <summary>
        /// Plays a SonicNet token and waits for completion.
        /// </summary>
        /// <param name="bits">bits (4 or 8) specifies the length of trasmitted token</param>
        /// <param name="token">token is the index of the SonicNet token to play (0-255 for 8-bit tokens or 0-15 for 4-bit tokens)</param>
        /// <returns>true if the operation is successful</returns>
        public bool SendToken(sbyte bits, byte token)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Schedules playback of a SonicNet token after the next sound starts playing.
        /// </summary>
        /// <param name="bits">bits (4 or 8) specifies the length of trasmitted token</param>
        /// <param name="token">token is the index of the SonicNet token to play (0-255 for 8-bit tokens or 0-15 for 4-bit tokens)</param>
        /// <param name="delay">delay (1-28090) is the time in milliseconds at which to send the token, since the beginning of the next sound playback</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>The scheduled token remains valid for one operation only, so you have to call #playSound() or #playSoundAsync() immediately after this function.</remarks>
        public bool EmbedToken(sbyte bits, byte token, ushort delay)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///  Starts playback of a sound from the sound table. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">index is the index of the target sound in the sound table</param>
        /// <param name="volume">volume (0-31) may be one of the values in #SoundVolume</param>
        /// <remarks>The module is busy until playback completes and it cannot accept other commands.You can interrupt playback with #stop().</remarks>
        public void PlaySoundAsync(short index, sbyte volume)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Plays a sound from the sound table and waits for completion
        /// </summary>
        /// <param name="index">index is the index of the target sound in the sound table</param>
        /// <param name="volume">volume (0-31) may be one of the values in #SoundVolume</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>To alter the maximum time for the wait, define the EASYVR_PLAY_TIMEOUT macro before including the EasyVR library.</remarks>
        public bool PlaySound(short index, int volume)
        {
            if (volume < 0 || volume > 31) throw new ArgumentOutOfRangeException(nameof(volume));

            SendCommand(CMD_PLAY_SX);
            SendArgument((sbyte)((index >> 5) & 0x1F));
            SendArgument((sbyte)(index & 0x1F));
            SendArgument(volume);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///  Retrieves the name of the sound table and the number of sounds it contains
        /// </summary>
        /// <param name="name">points to an array of at least 32 characters that holds the sound table label when the function returns</param>
        /// <param name="count">is a variable that holds the number of sounds when the function returns</param>
        /// <returns>true if the operation is successful</returns>
        public bool DumpSoundTable(ref string name, ref short count)
        {
            throw new NotImplementedException();
        }


        /// <summary>
        /// Plays a phone tone and waits for completion
        /// </summary>
        /// <param name="tone">is the index of the tone (0-9 for digits, 10 for '*' key, 11 for '#' key and 12-15 for extra keys 'A' to 'D', -1 for the dial tone)</param>
        /// <param name="duration">(1-32) is the tone duration in 40 milliseconds units, or  in seconds for the dial tone</param>
        /// <returns>true if the operation is successful</returns>
        public bool PlayPhoneTone(int tone, int duration)
        {
            if (tone < -1 || tone > 15) throw new ArgumentOutOfRangeException(nameof(tone));
            if (duration < 1 || duration > 32) throw new ArgumentOutOfRangeException(nameof(duration));

            SendCommand(CMD_PLAY_DTMF);
            SendArgument(-1);
            SendArgument(tone);
            SendArgument(duration);

            var response = GetResponse();
            return response == STS_SUCCESS;
        }

        /// <summary>
        ///  Empties internal memory for custom commands/groups and messages.
        /// </summary>
        /// <param name="wait">specifies whether to wait until the operation is complete (or times out)</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>It will take some time for the whole process to complete (EasyVR3 is faster) 
        /// and it cannot be interrupted.During this time the module cannot accept any other command. 
        /// The sound table and custom grammars data is not affected.</remarks>
        public bool ResetAll(bool wait)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Empties internal memory for custom commands/groups only. Messages are not affected.
        /// </summary>
        /// <param name="wait">specifies whether to wait until the operation is complete (or times out)</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>It will take some time for the whole process to complete (EasyVR3 is faster) and it cannot be interrupted.
        /// During this time the module cannot accept any other command. 
        /// The sound table and custom grammars data is not affected.</remarks>
        public bool ResetCommands(bool wait)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Empties internal memory used for messages only. Commands/groups are not affected.
        /// </summary>
        /// <param name="wait">specifies whether to wait until the operation is complete (or times out)</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>It will take some time for the whole process to complete (EasyVR3 is faster) and it cannot be interrupted.
        /// During this time the module cannot accept any other command. The sound table and custom grammars data is not affected.</remarks>
        public bool ResetMessages(bool wait)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a memory check for consistency.
        /// </summary>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>If a memory write or erase operation does not complete due to unexpecte conditions, like power losses, the memory contents may be corrupted.
        /// When the check fails #getError() returns #ERR_CUSTOM_INVALID.</remarks>
        public bool CheckMessages()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs a memory check and attempt recovery if necessary. Incomplete data wil be erased.Custom commands/groups are not affected.
        /// </summary>
        /// <param name="wait">specifies whether to wait until the operation is complete (or times out)</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>It will take some time for the whole process to complete (several seconds) and it cannot be interrupted.
        /// During this time the module cannot accept any other command. The sound table and custom grammars data is not affected.</remarks>
        public bool FixMessages(bool wait)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts recording a message. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">index (0-31) is the index of the target message slot</param>
        /// <param name="bits">bits (8) specifies the audio format (see #MessageType)</param>
        /// <param name="timeout">timeout (0-31) is the maximum recording time (0=infinite)</param>
        /// <remarks>The module is busy until recording times out or the end of memory is reached.You can interrupt an ongoing recording with #stop().</remarks>
        public void RecordMessageAsync(sbyte index, sbyte bits, sbyte timeout)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts playback of a recorded message. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">(0-31) is the index of the target message slot</param>
        /// <param name="speed">(0-1) may be one of the values in #MessageSpeed</param>
        /// <param name="atten">(0-3) may be one of the values in #MessageAttenuation</param>
        /// <remarks>The module is busy until playback completes and it cannot accept other commands.You can interrupt playback with #stop().</remarks>
        public void PlayMessageAsync(sbyte index, sbyte speed, sbyte atten)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Erases a recorded message. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">(0-31) is the index of the target message slot</param>
        public void EraseMessageAsync(sbyte index)
        {

        }

        /// <summary>
        /// Retrieves the type and length of a recorded message
        /// </summary>
        /// <param name="index">(0-31) is the index of the target message slot</param>
        /// <param name="type">(0,8) is a variable that holds the message format when the function returns(see #MessageType)</param>
        /// <param name="length">length is a variable that holds the message length in bytes whe the function returns</param>
        /// <remarks>The specified message may have errors. Use #getError() when the function fails, to know the reason of the failure.</remarks>
        /// <returns></returns>
        public bool DumpMessage(sbyte index, ref sbyte type, ref int length)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Starts real-time lip-sync on the input voice signal. Retrieve output values with #fetchMouthPosition() or abort with #stop().
        /// </summary>
        /// <param name="threshold">(0-1023) is a measure of the strength of the input signal below which the mouth is considered to be closed(see #LipsyncThreshold, 
        /// adjust based on microphone settings, distance and background noise)</param>
        /// <param name="timeout">(0-255) is the maximum duration of the function in seconds, 0 means infinite</param>
        /// <returns>true if the operation is successfully started</returns>
        public bool RealtimeLipsync(short threshold, byte timeout)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Retrieves the current mouth position during lip-sync.
        /// </summary>
        /// <param name="value">(0-31) is filled in with the current mouth opening position</param>
        /// <returns>true if the operation is successful, false if lip-sync has finished</returns>
        public bool FetchMouthPosition(ref sbyte value)
        {
            throw new NotImplementedException();
        }

        // service functions

        /// <summary>
        /// Retrieves all internal data associated to a custom command.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        /// <param name="data">points to an array of at least 258 bytes that holds the command raw data</param>
        /// <returns>true if the operation is successful</returns>
        public bool ExportCommand(sbyte group, sbyte index, ref byte data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Overwrites all internal data associated to a custom command. When commands are imported this way, their training should be tested again with #verifyCommand()
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group </param>
        /// <param name="data">data points to an array of at least 258 bytes that holds the command raw data</param>
        /// <returns>true if the operation is successful</returns>
        public bool ImportCommand(sbyte group, sbyte index, byte data)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Verifies training of a custom command (useful after import). Similarly to #trainCommand(), you should check results after #hasFinished() returns true
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        public void VerifyCommand(sbyte group, sbyte index)
        {
            throw new NotImplementedException();
        }

        // bridge mode

        /// <summary>
        /// Tests if bridge mode has been requested on the specified port
        /// </summary>
        /// <param name="port">is the target serial port (usually the PC serial port)</param>
        /// <returns>non zero if bridge mode should be started</returns>
        /// <remarks>The %EasyVR Commander software can request bridge mode when connected to the specified serial port, with a special handshake sequence.</remarks>
        public int BridgeRequested(Stream port)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Performs bridge mode between the EasyVR serial port and the specified por in a continuous loop.It can be aborted by sending a question mark('?') on the target port.
        /// </summary>
        /// <param name="port">is the target serial port (usually the PC serial port)</param>
        public void BridgeLoop(Stream port)
        {
            throw new NotImplementedException();
        }
    }
}