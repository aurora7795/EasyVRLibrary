using System;
using System.IO;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using static EasyVRLibrary.Protocol;


namespace EasyVRLibrary
{
    public class EasyVr
    {
        public const int EASYVR_RX_TIMEOUT = 500;
        public const int EASYVR_STORAGE_TIMEOUT = 500;
        public const int EASYVR_WAKE_TIMEOUT = 200;
        public const int EASYVR_PLAY_TIMEOUT = 5000;
        public const int EASYVR_TOKEN_TIMEOUT = 1500;
        public const int DEF_TIMEOUT = EASYVR_RX_TIMEOUT;
        public static int WAKE_TIMEOUT { get; } = EASYVR_WAKE_TIMEOUT;
        public static int STORAGE_TIMEOUT { get; } = EASYVR_STORAGE_TIMEOUT;

        private static SerialPort _serialPort; // communication interface for the EasyVR module

        private readonly Status _status = new Status();

        protected sbyte Group; // last used group (cached by the module)

        private int Value { get; set; }

        public sbyte INFINITE { get; } = -1;

        public sbyte NO_TIMEOUT { get; } = 0;
        public int PLAY_TIMEOUT { get; } = EASYVR_PLAY_TIMEOUT;
        public int TOKEN_TIMEOUT { get; } = EASYVR_TOKEN_TIMEOUT;

        public EasyVr(string portName, int baudRate = 9600)
        {
            if (_serialPort != null) return;
            // Create the serial port with basic settings
            _serialPort = new SerialPort(portName, baudRate, Parity.None, 8, StopBits.One);
            _serialPort.Open();

            Value = -1;
            Group = -1;
            Id = -1;
            _status.V = 0;
        }

        protected sbyte Id { get; }

        // command management

        /// <summary>
        ///     Adds a new custom command to a group.
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

            _status.V = 0;

            if (rx == STS_OUT_OF_MEM)
                _status.Memfull = true;

            return false;
        }

        /// <summary>
        ///     Performs bridge mode between the EasyVR serial port and the specified port in a continuous loop.It can be aborted by
        ///     sending a question mark('?') on the target port.
        /// </summary>
        /// <param name="port">is the target serial port (usually the PC serial port)</param>
        public void BridgeLoop(Stream port)
        {
            throw new NotImplementedException();
        }

        // bridge mode

        /// <summary>
        ///     Tests if bridge mode has been requested on the specified port
        /// </summary>
        /// <param name="port">is the target serial port (usually the PC serial port)</param>
        /// <returns>non zero if bridge mode should be started</returns>
        /// <remarks>
        ///     The %EasyVR Commander software can request bridge mode when connected to the specified serial port, with a
        ///     special handshake sequence.
        /// </remarks>
        public int BridgeRequested(Stream port)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     Sets the new communication speed. You need to modify the baudrate of the
        ///     underlying Stream object accordingly, after the function returns successfully.
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
        ///     Performs a memory check for consistency.
        /// </summary>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>
        ///     If a memory write or erase operation does not complete due to unexpecte conditions, like power losses, the memory
        ///     contents may be corrupted.
        ///     When the check fails #getError() returns #ERR_CUSTOM_INVALID.
        /// </remarks>
        public bool CheckMessages()
        {
            SendCommand(CMD_VERIFY_RP);
            SendArgument(-1);
            SendArgument(0);

            char rx = GetResponse(STORAGE_TIMEOUT);
            ReadStatus(rx);
            return (_status.V == 0);
        }

        public void ClosePort()
        {
            _serialPort.Close();
        }

        /// <summary>
        ///     Detects an EasyVR module, waking it from sleep mode and checking it responds correctly.
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


        // sound table functions

        /// <summary>
        ///     Starts listening for a SonicNet token. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="bits">(4 or 8) specifies the length of received tokens</param>
        /// <param name="rejection">
        ///     rejection (0-2) specifies the noise rejection level, it can be one of the values in
        ///     #RejectionLevel
        /// </param>
        /// <param name="timeout">
        ///     timeout (1-28090) is the maximum time in milliseconds to keep listening for a valid token or(0)
        ///     to listen without time limits.
        /// </param>
        /// <remarks>
        ///     The module is busy until token detection completes and it cannot accept other commands.You can interrupt
        ///     listening with #stop().
        /// </remarks>
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
        ///     Retrieves the contents of a built-in or a custom grammar.
        ///     Command labels contained in the grammar can be obtained by calling #getNextWordLabel()
        /// </summary>
        /// <param name="grammar">(0-31) is the target grammar, or one of the values in #Wordset</param>
        /// <param name="flags">is a variable that holds some grammar flags when the function returns. See #GrammarFlag</param>
        /// <param name="count">count is a variable that holds the number of words in the grammar when the function returns.</param>
        /// <returns>true if the operation is successful</returns>
        public bool DumpGrammar(int grammar, out byte flags, out int count)
        {
            if (grammar < 0 || grammar > 31) throw new ArgumentOutOfRangeException(nameof(grammar));

            SendCommand(CMD_DUMP_SI);
            SendArgument(grammar);

            if (GetResponse() != STS_GRAMMAR)
            {
                count = 0;
                flags = 0;
                return false;
            }

            char rx;
            if (!ReceiveArgument(out rx))
            {
                count = 0;
                flags = 0;
                return false;

            }
            flags = (byte)(rx == -1 ? 32 : rx);

            if (!ReceiveArgument(out rx))
            {
                count = 0;
                return false;
            }
            count = (byte)rx;
            return true;
        }

        /// <summary>
        ///     Retrieves the type and length of a recorded message
        /// </summary>
        /// <param name="index">(0-31) is the index of the target message slot</param>
        /// <param name="type">(0,8) is a variable that holds the message format when the function returns(see #MessageType)</param>
        /// <param name="length">length is a variable that holds the message length in bytes when the function returns</param>
        /// <remarks>
        ///     The specified message may have errors. Use #getError() when the function fails, to know the reason of the
        ///     failure.
        /// </remarks>
        /// <returns></returns>
        public bool DumpMessage(sbyte index, out int type, out int length)
        {
            SendCommand(CMD_DUMP_RP);
            SendArgument(-1);
            SendArgument(index);

            var sts = GetResponse(STORAGE_TIMEOUT);
            if (sts != STS_MESSAGE)
            {
                ReadStatus(sts);
                length = 0;
                type = 0;
                return false;
            }

            // if communication should fail
            _status.V = 0;
            _status.Error = true;

            if (!ReceiveArgument(out type))
            {
                length = 0;
                type = 0;
                return false;
            }

            length = 0;
            if (type == 0)
                return true; // skip reading if empty

            var tempArray = new int[7];

            for (var i = 0; i < 6; ++i)
            {
                char rx;
                if (!ReceiveArgument(out rx))
                    return false;
                tempArray[i] |= rx & 0x0F;
                if (!ReceiveArgument(out rx))
                    return false;
                tempArray[i] |= (rx << 4) & 0xF0;
            }

            _status.V = 0;
            return true;
        }

        /// <summary>
        ///     Retrieves the name of the sound table and the number of sounds it contains
        /// </summary>
        /// <param name="name">
        ///     points to an array of at least 32 characters that holds the sound table label when the function
        ///     returns
        /// </param>
        /// <param name="count">is a variable that holds the number of sounds when the function returns</param>
        /// <returns>true if the operation is successful</returns>
        public bool DumpSoundTable(out string name, out int count)
        {
            name = null;
            count = 0;
            SendCommand(CMD_DUMP_SX);

            if (GetResponse() != STS_TABLE_SX)
            {
                return false;
            }

            int rx;
            if (!ReceiveArgument(out rx))
            {
                return false;
            }
            count = rx << 5;
            if (!ReceiveArgument(out rx))
            {
                return false;
            }
            count |= rx;

            if (!ReceiveArgument(out rx))
            {
                return false;
            }
            var length = rx;

            var tempString = new StringBuilder();

            for (var i = 0; i < length; ++i)
            {
                char rxChar;
                if (!ReceiveArgument(out rxChar))
                {
                    return false;
                }
                if (rx == '^')
                {
                    if (!ReceiveArgument(out rxChar))
                    {
                        return false;
                    }
                    tempString.Append(ArgumentEncoding.ConvertArgumentCode(rxChar));
                    --length;
                }
                else
                {
                    tempString.Append(rxChar);
                }

            }
            return true;
        }

        /// <summary>
        ///     Schedules playback of a SonicNet token after the next sound starts playing.
        /// </summary>
        /// <param name="bits">bits (4 or 8) specifies the length of trasmitted token</param>
        /// <param name="token">token is the index of the SonicNet token to play (0-255 for 8-bit tokens or 0-15 for 4-bit tokens)</param>
        /// <param name="delay">
        ///     delay (1-28090) is the time in milliseconds at which to send the token, since the beginning of the
        ///     next sound playback
        /// </param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>
        ///     The scheduled token remains valid for one operation only, so you have to call #playSound() or
        ///     #playSoundAsync() immediately after this function.
        /// </remarks>
        public bool EmbedToken(int bits, int token, int delay)
        {
            SendCommand(CMD_SEND_SN);
            SendArgument(bits);
            SendArgument((token >> 5) & 0x1F);
            SendArgument(token & 0x1F);
            delay = (delay * 2 + 27) / 55; // approx / 27.46 - err < 0.15%
            if (delay == 0) // must be > 0 to embed in some audio
                delay = 1;
            SendArgument((delay >> 5) & 0x1F);
            SendArgument(delay & 0x1F);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///     Sets the name of a custom command.
        /// </summary>
        /// <param name="group">group (0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">index (0-31) is the index of the command within the selected group</param>
        /// <param name="name">name is a string containing the label to be assigned to the specified command</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetCommandLabel(int group, int index, string name)
        {
            SendCommand(CMD_NAME_SD);
            SendArgument(group);
            SendArgument(index);

            // numeric characters in the label string must be prefixed with a '^' - this increases the overall length of the 
            // name and needs to be taken into account when determining how many characters will be sent to the Easy VR module
            var escapedCharsNeeded = name.Count(char.IsDigit);

            SendArgument(name.Length + escapedCharsNeeded);

            foreach (var c in name)
            {
                if (char.IsDigit(c))
                {
                    SendCharacter('^');
                    SendArgument(c - '0');
                }
                else if (char.IsLetter(c))
                {
                    SendCharacter((char)(c & ~0x20)); // to uppercase
                }
                else
                {
                    SendCharacter('_');
                }
            }

            return GetResponse(STORAGE_TIMEOUT) == STS_SUCCESS;
        }

        /// <summary>
        ///     Erases the training data of a custom command.
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

        /// <summary>
        ///     Erases a recorded message. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">(0-31) is the index of the target message slot</param>
        public void EraseMessageAsync(sbyte index)
        {
        }

        // service functions

        /// <summary>
        ///     Retrieves all internal data associated to a custom command.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        /// <param name="data">points to an array of at least 258 bytes that holds the command raw data</param>
        /// <returns>true if the operation is successful</returns>
        public bool ExportCommand(sbyte group, sbyte index, ref int[] data)
        {
            SendCommand(CMD_SERVICE);
            SendArgument(SVC_EXPORT_SD - ARG_ZERO);
            SendArgument(group);
            SendArgument(index);

            if (GetResponse(STORAGE_TIMEOUT) != STS_SERVICE)
                return false;

            char rx;
            if (!ReceiveArgument(out rx) || rx != SVC_DUMP_SD - ARG_ZERO)
                return false;

            for (var i = 0; i < 258; ++i)
            {
                if (!ReceiveArgument(out rx))
                    return false;
                data[i] = (rx << 4) & 0xF0;
                if (!ReceiveArgument(out rx))
                    return false;
                data[i] |= rx & 0x0F;
            }
            return true;
        }

        /// <summary>
        ///     Retrieves the current mouth position during lip-sync.
        /// </summary>
        /// <param name="value">(0-31) is filled in with the current mouth opening position</param>
        /// <returns>true if the operation is successful, false if lip-sync has finished</returns>
        public bool FetchMouthPosition(out int value)
        {
            value = 0;
            SendCharacter(' ');
            var rx = GetResponse();
            if (rx >= ARG_MIN && rx <= ARG_MAX)
            {
                value = rx;
                return true;
            }
            // check if finished
            if (rx >= 0)
                ReadStatus(rx);
            return false;
        }

        /// <summary>
        ///     Performs a memory check and attempt recovery if necessary. Incomplete data wil be erased.Custom commands/groups are
        ///     not affected.
        /// </summary>
        /// <param name="wait">specifies whether to wait until the operation is complete (or times out)</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>
        ///     It will take some time for the whole process to complete (several seconds) and it cannot be interrupted.
        ///     During this time the module cannot accept any other command. The sound table and custom grammars data is not
        ///     affected.
        /// </remarks>
        public bool FixMessages(bool wait)
        {
            SendCommand(CMD_VERIFY_RP);
            SendArgument(-1);
            SendArgument(1);

            if (!wait)
                return true;

            Thread.Sleep(25000);
            return GetResponse() == STS_SUCCESS;
        }


        // analyse result

        /// <summary>
        ///     Gets the recognised command index if any.
        /// </summary>
        /// <returns>
        ///     (0-31) is the command index if recognition is successful, (-1) if no command has been recognized or an error
        ///     occurred
        /// </returns>
        public int GetCommand()
        {
            if (_status.Command) return ArgumentEncoding.ConvertArgumentCode((char)Value);
            return -1;
        }

        /// <summary>
        ///     Gets a bit mask of groups that contain at least one command.
        /// </summary>
        /// <param name="mask">mask is a variable to hold the group mask when the function returns</param>
        /// <returns>true if the operation is successful</returns>
        public bool GetGroupMask(int mask)
        {
            //todo: go over this one with dad

            throw new NotImplementedException();

            //SendCommand(CMD_MASK_SD);

            //if (GetResponse() != STS_MASK) return false;

            //mask = 0;

            //for (var i = 0; i < 4; ++i)
            //{
            //    char rx;
            //    if (!ReceiveArgument(out rx))
            //        return false;
            //    mask[i] |= rx & 0x0F;
            //    if (!ReceiveArgument(out rx))
            //        return false;
            //    mask[i] |= (rx << 4) & 0xF0;
            //}
            //return true;
        }

        /// <summary>
        ///     Gets the number of commands in the specified group.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <returns>integer is the count of commands (negative in case of errors)</returns>
        public int GetCommandCount(int group)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));

            SendCommand(CMD_COUNT_SD);
            SendArgument(group);

            if (GetResponse() != STS_COUNT) return -1;
            char rx;
            if (ReceiveArgument(out rx))
                return ArgumentEncoding.ConvertArgumentCode(rx);
            return -1;
        }

        /// <summary>
        ///     Gets the last error code if any.
        /// </summary>
        /// <returns>(0-255) is the error code, (-1) if no error occurred</returns>
        public short GetError()
        {
            if (_status.Error) return (short)Value;
            return -1;
        }

        /// <summary>
        /// Retrieves the name and training data of a custom command.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        /// <param name="name">points to an array of at least 32 characters that holds the command 
        /// label when the function returns</param>
        /// <param name="training">training is a variable that holds the training count when the function returns.
        /// Additional information about training is available through the functions #isConflict() 
        /// and #getWord() or #getCommand()</param>
        /// <returns>true if the operation is successful</returns>
        public bool DumpCommand(int group, int index, ref string name, ref int training)
        {
            SendCommand(CMD_DUMP_SD);
            SendArgument(group);
            SendArgument(index);

            if (GetResponse() != STS_DATA)
                return false;

            int rx;
            if (!ReceiveArgument(out rx))
                return false;

            training = rx & 0x07;
            if (rx == -1 || training == 7)
                training = 0;

            _status.V = 0;
            _status.Conflict = (rx & 0x18) != 0;
            _status.Command = (rx & 0x08) != 0;
            _status.Builtin = (rx & 0x10) != 0;

            if (!ReceiveArgument(out rx))
                return false;

            Value = rx;

            if (!ReceiveArgument(out rx))
                return false;

            var tempString = new StringBuilder();

            for (var length = rx; length > 0; length--)
            {
                char rxChar;
                if (!ReceiveArgument(out rxChar))
                    return false;
                if (rxChar == '^')
                {
                    if (!ReceiveArgument(out rxChar))
                        return false;
                    tempString.Append(ArgumentEncoding.ConvertArgumentCode(rxChar));
                    --length;
                }
                else
                {
                    tempString.Append(rxChar);
                }
            }

            name = tempString.ToString();
            return true;
        }

        /// <summary>
        ///     Gets the total number of grammars available, including built-in and custom.
        /// </summary>
        /// <returns>integer is the count of grammars (negative in case of errors)</returns>
        public int GetGrammarsCount()
        {
            SendCommand(CMD_DUMP_SI);
            SendArgument(-1);

            if (GetResponse() != STS_COUNT) return -1;
            char rx;
            if (ReceiveArgument(out rx))
                return ArgumentEncoding.ConvertArgumentCode(rx);
            return -1;
        }

        /// <summary>
        ///     Gets the module identification number (firmware version).
        /// </summary>
        /// <returns>Module ID for the easy VR module</returns>
        public ModuleId GetId()
        {
            SendCommand(STS_ID);

            var response = GetResponse();
            if (response != STS_ID)
                throw new Exception($"Invalid response: {response}");

            ReceiveArgument(out response);

            var decodedValue = ArgumentEncoding.ConvertArgumentCode(response);
            var tempModule = (ModuleId)decodedValue;

            return tempModule;
        }

        /// <summary>
        ///     Gets the index of the received SonicNet token if any.
        /// </summary>
        /// <returns>
        ///     integer is the index of the received SonicNet token (0-255 for 8-bit tokens or 0-15 for 4-bit tokens)
        ///     if detection was successful, (-1) if no token has been received or an error occurred
        /// </returns>
        public int GetToken()
        {
            if (_status.Token) return ArgumentEncoding.ConvertArgumentCode((char) Value);
            return -1;
        }

        /// <summary>
        ///     Gets the recognised word index if any, from built-in sets or custom grammars.
        /// </summary>
        /// <returns>
        ///     (0-31) is the command index if recognition is successful, (-1) if no built-in word has been recognized or an
        ///     error occurred
        /// </returns>
        public int GetWord()
        {
            if (_status.Builtin) return ArgumentEncoding.ConvertArgumentCode((char)Value);
            return -1;
        }

        /// <summary>
        ///     Polls the status of on-going recognition, training or asynchronous playback tasks.
        /// </summary>
        /// <returns>true if the operation has completed</returns>
        public bool HasFinished()
        {
            var rx = GetResponse(NO_TIMEOUT);
            if (rx < 0)
                return false;

            ReadStatus(rx);
            return true;
        }

        /// <summary>
        ///     Overwrites all internal data associated to a custom command. When commands are imported this way, their training
        ///     should be tested again with #verifyCommand()
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group </param>
        /// <param name="data">data points to an array of at least 258 bytes that holds the command raw data</param>
        /// <returns>true if the operation is successful</returns>
        public bool ImportCommand(int group, int index, byte[] data)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));
            if (index < 0 || index > 31) throw new ArgumentOutOfRangeException(nameof(index));
            SendCommand(CMD_SERVICE);
            SendArgument(SVC_IMPORT_SD - ARG_ZERO);
            SendArgument(group);
            SendArgument(index);

            for (var i = 0; i < 258; ++i)
            {
                var tx = (data[i] >> 4) & 0x0F;
                SendArgument(tx);
                tx = data[i] & 0x0F;
                SendArgument(tx);
            }
            return GetResponse(STORAGE_TIMEOUT) == STS_SUCCESS;
        }

        /// <summary>
        ///     Retrieves the wake-up indicator (only valid after #hasFinished() has been called).
        /// </summary>
        /// <returns>true if the module has been awakened from sleep mode</returns>
        public bool IsAwakened()
        {
            return _status.Awakened;
        }

        /// <summary>
        ///     Retrieves the conflict indicator.
        /// </summary>
        /// <returns>
        ///     true is a conflict occurred during training. To know what caused the conflict, use #getCommand() and
        ///     #getWord() (only valid for triggers)
        /// </returns>
        public bool IsConflict()
        {
            return _status.Conflict;
        }

        /// <summary>
        ///     Retrieves the invalid protocol indicator.
        /// </summary>
        /// <returns>true if an invalid sequence has been detected in the communication protocol</returns>
        public bool IsInvalid()
        {
            return _status.Invalid;
        }

        /// <summary>
        ///     Retrieves the memory full indicator (only valid after #addCommand() returned false).
        /// </summary>
        /// <returns>
        ///     true if a command could not be added because of memory size constaints(up to 32 custom commands can be
        ///     created)
        /// </returns>
        public bool IsMemoryFull()
        {
            return _status.Memfull;
        }

        /// <summary>
        ///     Retrieves the timeout indicator.
        /// </summary>
        /// <returns>true if a timeout occurred</returns>
        public bool IsTimeout()
        {
            return _status.Timeout;
        }

        /// <summary>
        ///     Starts playback of a recorded message. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">(0-31) is the index of the target message slot</param>
        /// <param name="speed">(0-1) may be one of the values in #MessageSpeed</param>
        /// <param name="attenuation">(0-3) may be one of the values in #MessageAttenuation</param>
        /// <remarks>
        ///     The module is busy until playback completes and it cannot accept other commands.You can interrupt playback
        ///     with #stop().
        /// </remarks>
        public void PlayMessageAsync(sbyte index, MessageSpeed speed, MessageAttenuation attenuation)
        {
            SendCommand(CMD_PLAY_RP);
            SendArgument(-1);
            SendArgument(index);
            SendArgument(((int)speed << 2) | ((int)attenuation & 3));
        }


        /// <summary>
        ///     Plays a phone tone and waits for completion
        /// </summary>
        /// <param name="tone">
        ///     is the index of the tone (0-9 for digits, 10 for '*' key, 11 for '#' key and 12-15 for extra keys
        ///     'A' to 'D', -1 for the dial tone)
        /// </param>
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

            var response = GetResponse(5000);
            return response == STS_SUCCESS;
        }

        /// <summary>
        ///     Plays a sound from the sound table and waits for completion
        /// </summary>
        /// <param name="index">index is the index of the target sound in the sound table</param>
        /// <param name="volume">volume (0-31) may be one of the values in #SoundVolume</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>
        ///     To alter the maximum time for the wait, define the EASYVR_PLAY_TIMEOUT macro before including the EasyVR
        ///     library.
        /// </remarks>
        public bool PlaySound(short index, int volume)
        {
            if (volume < 0 || volume > 31) throw new ArgumentOutOfRangeException(nameof(volume));

            SendCommand(CMD_PLAY_SX);
            SendArgument((sbyte)((index >> 5) & 0x1F));
            SendArgument((sbyte)(index & 0x1F));
            SendArgument(volume);

            return GetResponse(PLAY_TIMEOUT) == STS_SUCCESS;
        }

        /// <summary>
        ///     Starts playback of a sound from the sound table. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">index is the index of the target sound in the sound table</param>
        /// <param name="volume">volume (0-31) may be one of the values in #SoundVolume</param>
        /// <remarks>
        ///     The module is busy until playback completes and it cannot accept other commands.You can interrupt playback
        ///     with #stop().
        /// </remarks>
        public void PlaySoundAsync(short index, int volume)
        {
            if (volume < 0 || volume > 31) throw new ArgumentOutOfRangeException(nameof(volume));

            SendCommand(CMD_PLAY_SX);
            SendArgument((index >> 5) & 0x1F);
            SendArgument(index & 0x1F);
            SendArgument(volume);
        }

        /// <summary>
        ///     Starts real-time lip-sync on the input voice signal. Retrieve output values with #fetchMouthPosition() or abort
        ///     with #stop().
        /// </summary>
        /// <param name="threshold">
        ///     (0-1023) is a measure of the strength of the input signal below which the mouth is considered to be closed(see
        ///     #LipsyncThreshold,
        ///     adjust based on microphone settings, distance and background noise)
        /// </param>
        /// <param name="timeout">(0-255) is the maximum duration of the function in seconds, 0 means infinite</param>
        /// <returns>true if the operation is successfully started</returns>
        public bool RealtimeLipsync(short threshold, byte timeout)
        {
            if (threshold > 1023) throw new ArgumentOutOfRangeException(nameof(threshold));
            if (timeout > 255) throw new ArgumentOutOfRangeException(nameof(timeout));

            SendCommand(CMD_LIPSYNC);
            SendArgument(-1);
            SendArgument((threshold >> 5) & 0x1F);
            SendArgument(threshold & 0x1F);
            SendArgument((timeout >> 4) & 0x0F);
            SendArgument(timeout & 0x0F);

            char sts;
            ReceiveArgument(out sts);
            if (sts == STS_LIPSYNC) return true;
            ReadStatus(sts);
            return false;
        }

        /// <summary>
        ///     Starts recognition of a custom command. Results are available after #hasFinished() returns true.
        ///     The module is busy until recognition completes and it cannot accept other commands. You can interrupt recognition
        ///     with #stop().
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        public void RecognizeCommand(int group)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));

            SendCommand(CMD_RECOG_SD);
            SendArgument(group);
        }

        /// <summary>
        ///     Starts recognition of a built-in word. Results are available after #hasFinished() returns true.
        ///     The module is busy until recognition completes and it cannot saccept other commands. You can interrupt recognition
        ///     with #stop().
        /// </summary>
        /// <param name="wordset">
        ///     (0-3) is the target word set, or one of the values in #Wordset, (4-31) is the target custom
        ///     grammar, if present
        /// </param>
        public void RecognizeWord(int wordset)
        {
            if (wordset < 0 || wordset > 31) throw new ArgumentOutOfRangeException(nameof(wordset));

            SendCommand(CMD_RECOG_SI);
            SendArgument(wordset);
        }

        /// <summary>
        ///     Starts recording a message. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="index">index (0-31) is the index of the target message slot</param>
        /// <param name="bits">bits (8) specifies the audio format (see #MessageType)</param>
        /// <param name="timeout">timeout (0-31) is the maximum recording time (0=infinite)</param>
        /// <remarks>
        ///     The module is busy until recording times out or the end of memory is reached.You can interrupt an ongoing
        ///     recording with #stop().
        /// </remarks>
        public void RecordMessageAsync(sbyte index, MessageType bits, sbyte timeout)
        {
            if (index < 0 || index > 31) throw new ArgumentOutOfRangeException(nameof(index));
            if (timeout < 0 || timeout > 31) throw new ArgumentOutOfRangeException(nameof(timeout));

            SendCommand(CMD_RECORD_RP);
            SendArgument(-1);
            SendArgument(index);
            SendArgument((int)bits);
            SendArgument(timeout);
        }

        /// <summary>
        ///     Removes a custom command from a group.
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        /// <returns>true if the operation is successful</returns>
        public bool RemoveCommand(int group, int index)
        {
            if (group < 0 || group > 16) throw new ArgumentOutOfRangeException(nameof(group));
            if (index < 0 || index > 31) throw new ArgumentOutOfRangeException(nameof(index));


            SendCommand(CMD_UNGROUP_SD);
            SendArgument(group);
            SendArgument(index);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///     Empties internal memory for custom commands/groups and messages.
        /// </summary>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>
        ///     It will take some time for the whole process to complete (EasyVR3 is faster)
        ///     and it cannot be interrupted.During this time the module cannot accept any other command.
        ///     The sound table and custom grammars data is not affected.
        /// </remarks>
        public bool ResetAll()
        {
            SendCommand(CMD_RESETALL);
            SendCommand('R');

            return GetResponse(5000) == STS_SUCCESS;
        }

        /// <summary>
        ///     Empties internal memory for custom commands/groups only. Messages are not affected.
        /// </summary>
        /// <param name="wait">specifies whether to wait until the operation is complete (or times out)</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>
        ///     It will take some time for the whole process to complete (EasyVR3 is faster) and it cannot be interrupted.
        ///     During this time the module cannot accept any other command.
        ///     The sound table and custom grammars data is not affected.
        /// </remarks>
        public bool ResetCommands(bool wait)
        {
            SendCommand(CMD_RESETALL);
            SendArgument('D');

            return GetResponse(5000) == STS_SUCCESS;
        }

        /// <summary>
        ///     Empties internal memory used for messages only. Commands/groups are not affected.
        /// </summary>
        /// <param name="wait">specifies whether to wait until the operation is complete (or times out)</param>
        /// <returns>true if the operation is successful</returns>
        /// <remarks>
        ///     It will take some time for the whole process to complete (EasyVR3 is faster) and it cannot be interrupted.
        ///     During this time the module cannot accept any other command. The sound table and custom grammars data is not
        ///     affected.
        /// </remarks>
        public bool ResetMessages(bool wait)
        {
            SendCommand(CMD_RESETALL);
            SendArgument('M');

            return GetResponse(5000) == STS_SUCCESS;
        }

        /// <summary>
        ///     Plays a SonicNet token and waits for completion.
        /// </summary>
        /// <param name="bits">bits (4 or 8) specifies the length of trasmitted token</param>
        /// <param name="token">token is the index of the SonicNet token to play (0-255 for 8-bit tokens or 0-15 for 4-bit tokens)</param>
        /// <returns>true if the operation is successful</returns>
        public bool SendToken(BitNumber bits, byte token)
        {
            SendCommand(CMD_SEND_SN);
            SendArgument((int)bits);
            SendArgument((token >> 5) & 0x1F);
            SendArgument(token & 0x1F);
            SendArgument(0);
            SendArgument(0);

            if (GetResponse(TOKEN_TIMEOUT) == STS_SUCCESS)
                return true;
            return false;
        }

        /// <summary>
        ///     Starts immediate playback of a SonicNet token. Manually check for completion with #hasFinished().
        /// </summary>
        /// <param name="bits">bits (4 or 8) specifies the length of trasmitted token</param>
        /// <param name="token">token is the index of the SonicNet token to play (0-255 for 8-bit tokens or 0-15 for 4-bit tokens)</param>
        /// <remarks>
        ///     The module is busy until playback completes and it cannot accept other commands.You can interrupt playback
        ///     with #stop().
        /// </remarks>
        public void SendTokenAsync(BitNumber bits, byte token)
        {
            switch (bits)
            {
                case BitNumber.BITS_4:
                    if (token > 15)
                        throw new ArgumentException("Invalid token for token length (must be between 0-15)");
                    break;
                case BitNumber.BITS_8:
                    if (token > 255)
                        throw new ArgumentException("Invalid token for token length (must be between 0-255)");
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(bits), bits, null);
            }

            SendCommand(CMD_SEND_SN);
            SendArgument((int)bits);
            SendArgument(token);
        }

        /// <summary>
        ///     Enables or disables fast recognition for custom commands and passwords.
        ///     Fast SD/SV recognition can improve response time.
        /// </summary>
        /// <param name="mode">(0-1) is one of the values in #CommandLatency</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetCommandLatency(CommandLatency mode)
        {
            SendCommand(CMD_FAST_SD);
            SendArgument(-1);
            SendArgument((int)mode);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///     Sets the delay before any reply of the module.
        /// </summary>
        /// <param name="millis">
        ///     millis (0-1000) is the delay duration in milliseconds, rounded to
        ///     10 units in range 10-100 and to 100 units in range 100-1000.
        /// </param>
        /// <returns>true if the operation is successful</returns>
        public bool SetDelay(ushort millis)
        {
            if (millis > 1000) throw new ArgumentOutOfRangeException(nameof(millis));

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
        ///     Sets the confidence threshold to use for recognition of built-in words or custom grammars.
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
        ///     Sets the language to use for recognition of built-in words.
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
        ///     Sets the strictness level to use for recognition of custom commands.
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
        ///     Sets the operating distance of the microphone.
        ///     This setting represents the distance between the microphone and the
        ///     user's mouth, in one of three possible configurations.
        /// </summary>
        /// <param name="distance">dist (1-3) is one of values in #Distance</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetMicDistance(Distance distance)
        {
            SendCommand(CMD_MIC_DIST);
            SendArgument(-1);
            SendArgument((int)distance);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///     Configures an I/O pin as an input with optional pull-up and return its value
        /// </summary>
        /// <param name="pin">(1-3) is one of values in #PinNumber</param>
        /// <param name="config"> (2-4) is one of the input values in #PinConfig</param>
        /// <returns>integer is the logical value of the pin</returns>
        public int SetPinInput(PinNumber pin, PinConfig config)
        {
            if (config == PinConfig.INPUT_HIZ || config == PinConfig.INPUT_STRONG)
                throw new ArgumentException("Invalid Pin Configuration");

            SendCommand(CMD_QUERY_IO);
            SendArgument((int)pin);
            SendArgument((int)config);

            if (GetResponse() == STS_PIN)
                return ArgumentEncoding.ConvertArgumentCode(GetResponse());
            return -1;
        }


        // pin I/O functions

        /// <summary>
        ///     Configures an I/O pin as an output and sets its value
        /// </summary>
        /// <param name="pin">(1-3) is one of values in #PinNumber</param>
        /// <param name="value">(0-1) is one of the output values in #PinConfig, or Arduino style HIGH and LOW macros</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetPinOutput(PinNumber pin, PinConfig value)
        {
            if ((int)pin > 3)
                throw new ArgumentException("Invalid Pin number");

            if ((int)value > 1)
                throw new ArgumentException("Invalid output value");

            SendCommand(CMD_QUERY_IO);
            SendArgument((int)pin);
            SendArgument((int)value);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///     Sets the timeout to use for any recognition task.
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
        ///     Sets the trailing silence duration for recognition of built-in words or custom grammars.
        /// </summary>
        /// <param name="duration">(0-31) is the silence duration as defined in #TrailingSilence</param>
        /// <returns>true if the operation is successful</returns>
        public bool SetTrailingSilence(TrailingSilence duration)
        {
            SendCommand(CMD_TRAILING);
            SendArgument(-1);
            SendArgument((int)duration);

            return GetResponse() == STS_SUCCESS;
        }

        /// <summary>
        ///     Puts the module in sleep mode.
        /// </summary>
        /// <param name="mode">
        ///     mode is one of values in #WakeMode, optionally combined with one of
        ///     the values in #ClapSense
        /// </param>
        /// <returns>true if the operation is successful</returns>
        public bool Sleep(WakeMode mode)
        {
            SendCommand(CMD_SLEEP);
            SendArgument((int)mode);

            return GetResponse() == STS_SUCCESS;
        }


        /// <summary>
        ///     Interrupts pending recognition or playback operations.
        /// </summary>
        /// <returns>True if the request is satisfied and the module is back to ready</returns>
        public bool Stop()
        {
            SendCommand(CMD_BREAK);

            var rx = GetResponse();
            return rx == STS_INTERR || rx == STS_SUCCESS;
        }

        /// <summary>
        /// Retrieves the name of a command contained in a custom grammar. It must be called after #dumpGrammar()
        /// </summary>
        /// <param name="name">points to an array of at least 32 characters that holds the command label when 
        /// the function returns</param>
        /// <returns>true if the operation is successful</returns>
        public bool GetNextWordLabel(out string name)
        {
            name = null;

            char count;
            if (!ReceiveArgument(out count))
                return false;
            if (count == -1)
                count = (char)32;

            int length = ArgumentEncoding.ConvertArgumentCode(count);

            var tempString = new StringBuilder();

            for (var i = 0; i < length; i++)
            {
                char rxChar;
                if (!ReceiveArgument(out rxChar))
                {
                    name = tempString.ToString();
                    return false;
                }
                if (rxChar == '^')
                {
                    if (!ReceiveArgument(out rxChar))
                        return false;
                    tempString.Append(ArgumentEncoding.ConvertArgumentCode(rxChar));
                    --length;
                }
                else
                {
                    tempString.Append(rxChar);
                }
            }

            name = tempString.ToString();
            return true;
        }
      
        /// <summary>
        ///     Starts training of a custom command. Results are available after #hasFinished() returns true.
        ///     The module is busy until training completes and it cannot accept other commands. You can interrupt training with
        ///     #stop().
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
        ///     Verifies training of a custom command (useful after import). Similarly to #trainCommand(), you should check results
        ///     after #hasFinished() returns true
        /// </summary>
        /// <param name="group">(0-16) is the target group, or one of the values in #Groups</param>
        /// <param name="index">(0-31) is the index of the command within the selected group</param>
        public void VerifyCommand(sbyte group, sbyte index)
        {
            if (group < 0 || group > 31) throw new ArgumentOutOfRangeException(nameof(group));
            if (index < 0 || index > 31) throw new ArgumentOutOfRangeException(nameof(index));

            SendCommand(CMD_SERVICE);
            SendArgument(SVC_VERIFY_SD);
            SendArgument(group);
            SendArgument(index);
        }

        private static char GetResponse(int timeout = DEF_TIMEOUT)
        {
            _serialPort.ReadTimeout = timeout > 0 ? timeout : SerialPort.InfiniteTimeout;

            var temp = _serialPort.ReadByte();

            return (char)temp;
        }

        private void ReadStatus(char rx)
        {
            _status.V = 0;
            Value = 0;

            switch (rx)
            {
                case STS_SUCCESS:
                    return;

                case STS_SIMILAR:
                    _status.Builtin = true;
                    goto GET_WORD_INDEX;

                case STS_RESULT:
                    _status.Command = true;

                    GET_WORD_INDEX:
                    if (ReceiveArgument(out rx))
                    {
                        Value = rx;
                        return;
                    }
                    break;

                case STS_TOKEN:
                    _status.Token = true;

                    if (ReceiveArgument(out rx))
                    {
                        Value = rx << 5;
                        if (ReceiveArgument(out rx))
                        {
                            Value |= rx;
                            return;
                        }
                    }
                    break;

                case STS_AWAKEN:
                    _status.Awakened = true;
                    return;

                case STS_TIMEOUT:
                    _status.Timeout = true;
                    return;

                case STS_INVALID:
                    _status.Invalid = true;
                    return;

                case STS_ERROR:
                    _status.Error = true;


                    if (ReceiveArgument(out rx))
                    {
                        Value = rx << 4;
                        if (ReceiveArgument(out rx))
                        {
                            Value |= rx;
                            return;
                        }
                    }
                    break;
            }

            // unexpected condition (communication error)
            _status.V = 0;
            _status.Error = true;
            Value = 0;
        }

        private static bool ReceiveArgument(out char response)
        {
            response = ' ';
            SendCommand((char)ARG_ACK);
            try
            {
                response = GetResponse();
                return response >= ARG_MIN && response <= ARG_MAX;
            }
            catch (TimeoutException)
            {
                return false;
            }
        }

        private static bool ReceiveArgument(out int response)
        {
            SendCommand((char)ARG_ACK);
            response = ArgumentEncoding.ConvertArgumentCode(GetResponse());
            return response >= -1 && response <= 31;
        }


        private static void SendArgument(int argument)
        {
            _serialPort.Write(ArgumentEncoding.IntToArgumentString(argument));
        }

        private static void SendCharacter(char argument)
        {
            _serialPort.Write(argument.ToString());
        }

        private static void SendCommand(char command)
        {
            _serialPort.Write(command.ToString());
        }

        private class Status
        {
            public bool Awakened { get; set; }
            public bool Builtin { get; set; }
            public bool Command { get; set; }
            public bool Conflict { get; set; }
            public bool Error { get; set; }
            public bool Invalid { get; set; }
            public bool Memfull { get; set; }
            public bool Timeout { get; set; }
            public bool Token { get; set; }
            public sbyte V { get; set; }
        }
    }
}