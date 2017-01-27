namespace EasyVRLibrary
{
    public class Protocol
    {
        public const char CMD_BREAK = 'b'; // abort recog or ping
        public const char CMD_SLEEP = 's'; // go to power down
        public const char CMD_KNOB = 'k'; // set si knob <1>
        public const char CMD_MIC_DIST = 'k'; // set microphone (<1>=-1) distance <2>
        public const char CMD_LEVEL = 'v'; // set sd level <1>
        public const char CMD_VERIFY_RP = 'v'; // verify filesystem (<1>=-1) with flags <2> (0=check only, 1=fix)
        public const char CMD_LANGUAGE = 'l'; // set si language <1>
        public const char CMD_LIPSYNC = 'l'; // start real-time lipsync (<1>=-1) with threshold <2-3>, timeout <4-5>
        public const char CMD_TIMEOUT = 'o'; // set timeout <1>
        public const char CMD_RECOG_SI = 'i'; // do si recog from ws <1>
        public const char CMD_TRAIN_SD = 't'; // train sd command at group <1> pos <2>
        public const char CMD_TRAILING = 't'; // set trailing (<1>=-1) silence <2> (0-31 = 100-875 milliseconds)
        public const char CMD_GROUP_SD = 'g'; // insert new command at group <1> pos <2>
        public const char CMD_UNGROUP_SD = 'u'; // remove command at group <1> pos <2>
        public const char CMD_RECOG_SD = 'd'; // do sd recog at group <1> (0 = trigger mixed si/sd)
        public const char CMD_DUMP_RP = 'd'; // dump message (<1>=-1) at pos <2>
        public const char CMD_ERASE_SD = 'e'; // reset command at group <1> pos <2>
        public const char CMD_ERASE_RP = 'e'; // erase recording (<1>=-1) at pos <2>
        public const char CMD_NAME_SD = 'n'; // label command at group <1> pos <2> with length <3> name <4-n>
        public const char CMD_COUNT_SD = 'c'; // get command count for group <1>
        public const char CMD_DUMP_SD = 'p'; // read command data at group <1> pos <2>
        public const char CMD_PLAY_RP = 'p'; // play recording (<1>=-1) at pos <2> with flags <3>
        public const char CMD_MASK_SD = 'm'; // get active group mask
        public const char CMD_RESETALL = 'r'; // reset all memory (commands/groups and messages), with <1>='R'
        public const char CMD_RESET_SD = 'r'; // reset only commands/groups, with <1>='D'
        public const char CMD_RESET_RP = 'r'; // reset only messages, with <1>='M'
        public const char CMD_RECORD_RP = 'r'; // record message (<1>=-1) at pos <2> with bits <3> and timeout <4>
        public const char CMD_ID = 'x'; // get version id
        public const char CMD_DELAY = 'y'; // set transmit delay <1> (log scale)
        public const char CMD_BAUDRATE = 'a'; // set baudrate <1> (bit time, 1=>115200)
        public const char CMD_QUERY_IO = 'q'; // configure, read or write I/O pin <1> of type <2>
        public const char CMD_PLAY_SX = 'w'; // wave table entry <1-2> (10-bit) playback at volume <3>
        public const char CMD_PLAY_DTMF = 'w'; // play (<1>=-1) dial tone <2> for duration <3>
        public const char CMD_DUMP_SX = 'h'; // dump wave table entries
        public const char CMD_DUMP_SI = 'z'; // dump si settings for ws <1> (or total ws count if -1)
        public const char CMD_SEND_SN = 'j'; // send sonicnet token with bits <1> index <2-3> at time <4-5>
        public const char CMD_RECV_SN = 'f'; // receive sonicnet token with bits <1> rejection <2> timeout <3-4>
        public const char CMD_FAST_SD = 'f'; // set sd/sv (<1>=-1) to use fast recognition <2> (0=normal/default, 1=fast)

        public const char CMD_SERVICE = '~'; // send service request
        public const char SVC_EXPORT_SD = 'X'; // request export of command <2> in group <1> as raw dump
        public const char SVC_IMPORT_SD = 'I'; // request import of command <2> in group <1> as raw dump
        public const char SVC_VERIFY_SD = 'V'; // verify training of imported raw command <2> in group <1>

        public const char STS_SERVICE = '~'; // get service reply
        public const char SVC_DUMP_SD = 'D'; // provide raw command data <1-512> followed by checksum <513-516>

        public const char STS_MASK = 'k'; // mask of active groups <1-8>
        public const char STS_COUNT = 'c'; // count of commands <1> (or number of ws <1>)
        public const char STS_AWAKEN = 'w'; // back from power down mode
        public const char STS_DATA = 'd'; // provide training <1>, conflict <2>, command label <3-35> (counted string)
        public const char STS_ERROR = 'e'; // signal error code <1-2>
        public const char STS_INVALID = 'v'; // invalid command or argument
        public const char STS_TIMEOUT = 't'; // timeout expired
        public const char STS_LIPSYNC = 'l'; // lipsync stream follows
        public const char STS_INTERR = 'i'; // back from aborted recognition (see 'break')
        public const char STS_SUCCESS = 'o'; // no errors status
        public const char STS_RESULT = 'r'; // recognised sd command <1> - training similar to sd <1>
        public const char STS_SIMILAR = 's'; // recognised si <1> (in mixed si/sd) - training similar to si <1>
        public const char STS_OUT_OF_MEM = 'm'; // no more available commands (see 'group')
        public const char STS_ID = 'x'; // provide version id <1>
        public const char STS_PIN = 'p'; // return pin state <1>
        public const char STS_TABLE_SX = 'h'; // table entries count <1-2> (10-bit), table name <3-35> (counted string)
        public const char STS_GRAMMAR = 'z'; // si grammar: flags <1>, word count <2>, labels... <3-35> (n counted strings)
        public const char STS_TOKEN = 'f'; // received sonicnet token <1-2>
        public const char STS_MESSAGE = 'g'; // message status <1> (0=empty, 4/8=bits format), length <2-7>





        // protocol arguments are in the range 0x40 (-1) to 0x60 (+31) inclusive
        public const sbyte ARG_MIN = 0x40;
        public const sbyte ARG_MAX = 0x60;
        public const sbyte ARG_ZERO = 0x41;
        //  public const sbyte ARG_ACK = 0x20;   // to read more status arguments
        public const sbyte ARG_ACK = 0x20;   // to read more status arguments

        /** Module identification number (firmware version) */

        public enum ModuleId
        {
            VRBOT, //*< Identifies a VRbot module
            EASYVR, //*< Identifies an EasyVR module
            EASYVR2, //*< Identifies an EasyVR module version 2
            EASYVR2_3, //*< Identifies an EasyVR module version 2, firmware revision 3
            EASYVR3 = 8, //*< Identifies an EasyVR module version 3, firmware revision 0
            EASYVR3_1, //*< Identifies an EasyVR module version 3, firmware revision 1
            EASYVR3_2, //*< Identifies an EasyVR module version 3, firmware revision 2
            EASYVR3_3, //*< Identifies an EasyVR module version 3, firmware revision 3
            EASYVR3_4, //*< Identifies an EasyVR module version 3, firmware revision 4
        }

        /** Language to use for recognition of built-in words */

        public enum Language
        {
            ENGLISH = 0, //*< Uses the US English word sets
            ITALIAN, //*< Uses the Italian word sets
            JAPANESE, //*< Uses the Japanese word sets
            GERMAN, //*< Uses the German word sets
            SPANISH, //*< Uses the Spanish word sets
            FRENCH, //*< Uses the French word sets
        }

        /** Special group numbers for recognition of custom commands */

        public enum Group
        {
            TRIGGER = 0, //*< The trigger group (shared with built-in trigger word)
            PASSWORD = 16, //*< The password group (uses speaker verification technology)
        }

        /** Index of built-in word sets */

        public enum Wordset
        {
            TRIGGER_SET, //*< The built-in trigger word set
            ACTION_SET, //*< The built-in action word set
            DIRECTION_SET, //*< The built-in direction word set
            NUMBER_SET, //*< The built-in number word set
        }

        /** Microphone distance from the user's mouth,
        used by all recognition technologies */

        public enum Distance
        {
            HEADSET = 1, //*< Nearest range (around 5cm)
            ARMS_LENGTH, //*< Medium range (from about 50cm to 1m)
            FAR_MIC, //*< Farthest range (up to 3m)
        }

        /** Confidence thresholds for the knob settings,
        used for recognition of built-in words or custom grammars
        (not used for the mixed trigger group) */

        public enum Knob
        {
            LOOSER, //*< Lowest threshold, most results reported
            LOOSE, //*< Lower threshold, more results reported
            TYPICAL, //*< Typical threshold (default)
            STRICT, //*< Higher threshold, fewer results reported
            STRICTER, //*< Highest threshold, fewest results reported
        }

        /** Strictness values for the level settings,
        used for recognition of custom commands
        (not used for the mixed trigger group) */

        public enum Level
        {
            EASY = 1, //*< Lowest value, most results reported
            NORMAL, //*< Typical value (default)
            HARD, //*< Slightly higher value, fewer results reported
            HARDER, //*< Higher value, fewer results reported
            HARDEST, //*< Highest value, fewest results reported
        }

        /** Trailing silence settings used for recognition of built-in words or
        custom grammars (including the mixed trigger group), in a range from
        100ms to 875ms in steps of 25ms. */

        public enum TrailingSilence
        {
            TRAILING_MIN = 0, //*< Lowest value (100ms), minimum latency
            TRAILING_DEF = 12, //*< Default value (400ms) after power on or reset
            TRAILING_MAX = 31, //*< Highest value (875ms), maximum latency
            TRAILING_100MS = 0, //*< Silence duration is 100ms
            TRAILING_200MS = 4, //*< Silence duration is 200ms
            TRAILING_300MS = 8, //*< Silence duration is 300ms
            TRAILING_400MS = 12, //*< Silence duration is 400ms
            TRAILING_500MS = 16, //*< Silence duration is 500ms
            TRAILING_600MS = 20, //*< Silence duration is 600ms
            TRAILING_700MS = 24, //*< Silence duration is 700ms
            TRAILING_800MS = 28, //*< Silence duration is 800ms
        }

        /** Latency settings used for recognition of custom commands or passwords
        (excluding the mixed trigger group) */

        public enum CommandLatency
        {
            MODE_NORMAL, //*< Normal settings (default), higher latency
            MODE_FAST, //*< Fast settings, better response time
        }

        /** Constants to use for baudrate settings */

        public enum Baudrate
        {
            B115200 = 1, //*< 115200 bps
            B57600 = 2, //*< 57600 bps
            B38400 = 3, //*< 38400 bps
            B19200 = 6, //*< 19200 bps
            B9600 = 12, //*< 9600 bps (default)
        }

        /** Constants for choosing wake-up method in sleep mode */

        public enum WakeMode
        {
            WAKE_ON_CHAR = 0, //*< Wake up on any character received
            WAKE_ON_WHISTLE = 1, //*< Wake up on whistle or any character received
            WAKE_ON_LOUDSOUND = 2, //*< Wake up on a loud sound or any character received
            WAKE_ON_2CLAPS_LOW = 3, //*< Wake up on double hands-clap or any character received (Low Sensitivity)
            WAKE_ON_2CLAPS_MID = 4, //*< Wake up on double hands-clap or any character received (Medium Sensitivity)
            WAKE_ON_2CLAPS_HIGH = 5, //*< Wake up on double hands-clap or any character received (High Sensitivity)
            WAKE_ON_3CLAPS_LOW = 6, //*< Wake up on triple hands-clap or any character received (Low Sensitivity)
            WAKE_ON_3CLAPS_MID = 7, //*< Wake up on triple hands-clap or any character received (Medium Sensitivity)
            WAKE_ON_3CLAPS_HIGH = 8, //*< Wake up on triple hands-clap or any character received (High Sensitivity)
        }

        /** Hands-clap sensitivity for wakeup from sleep mode.
        Use in combination with #WAKE_ON_2CLAPS or #WAKE_ON_3CLAPS */

        public enum ClapSense
        {
            CLAP_SENSE_LOW = 0, //*< Lowest threshold
            CLAP_SENSE_MID = 1, //*< Typical threshold
            CLAP_SENSE_HIGH = 2, //*< Highest threshold
        }

        /** Pin configuration options for the extra I/O connector */

        public enum PinConfig
        {
            OUTPUT_LOW, //*< Pin is a low output (0V)
            OUTPUT_HIGH, //*< Pin is a high output (3V)
            INPUT_HIZ, //*< Pin is an high impedance input
            INPUT_STRONG, //*< Pin is an input with strong pull-up (~10K)
            INPUT_WEAK, //*< Pin is an input with weak pull-up (~200K)
        }

        /** Available pin numbers on the extra I/O connector */

        public enum PinNumber
        {
            IO1 = 1, //*< Identifier of pin IO1
            IO2 = 2, //*< Identifier of pin IO2
            IO3 = 3, //*< Identifier of pin IO3
            IO4 = 4, //*< Identifier of pin IO4 (only EasyVR3)
            IO5 = 5, //*< Identifier of pin IO5 (only EasyVR3)
            IO6 = 6, //*< Identifier of pin IO6 (only EasyVR3)
        }

        /** Some quick volume settings for the sound playback functions
        (any value in the range 0-31 can be used) */

        public enum SoundVolume
        {
            VOL_MIN = 0, //*< Lowest volume (almost mute)
            VOL_HALF = 7, //*< Half scale volume (softer)
            VOL_FULL = 15, //*< Full scale volume (normal)
            VOL_DOUBLE = 31, //*< Double gain volume (louder)
        }

        /** Special sound index values, always available even when no soundtable is present */

        public enum SoundIndex
        {
            BEEP = 0, //*< Beep sound
        }

        /** Flags used by custom grammars */

        public enum GrammarFlag
        {
            GF_TRIGGER = 0x10, //*< A bit mask that indicate grammar is a trigger (opposed to commands)
        }

        /** Noise rejection level for SonicNet token detection (higher value, fewer results) */

        public enum RejectionLevel
        {
            REJECTION_MIN, //*< Lowest noise rejection, highest sensitivity
            REJECTION_AVG, //*< Medium noise rejection, medium sensitivity
            REJECTION_MAX, //*< Highest noise rejection, lowest sensitivity
        }

        /** Playback speed for recorded messages */

        public enum MessageSpeed
        {
            SPEED_NORMAL, //*< Normal playback speed
            SPEED_FASTER, //*< Faster playback speed
        }

        /** Playback attenuation for recorded messages */

        public enum MessageAttenuation
        {
            ATTEN_NONE, //*< No attenuation (normalized volume)
            ATTEN_2DB2, //*< Attenuation of -2.2dB
            ATTEN_4DB5, //*< Attenuation of -4.5dB
            ATTEN_6DB7, //*< Attenuation of -6.7dB
        }

        /** Type of recorded message */

        public enum MessageType
        {
            MSG_EMPTY = 0, //*< Empty message slot
            MSG_8BIT = 8, //*< Message recorded with 8-bits PCM
        }

        /** Threshold for real-time lip-sync */

        public enum LipsyncThreshold
        {
            RTLS_THRESHOLD_DEF = 270, //*< Default threshold
            RTLS_THRESHOLD_MAX = 1023, //*< Maximum threshold
        }

        /** Error codes used by various functions */

        public enum ErrorCode
        {
            //-- 0x: Data collection errors (patgen, wordspot, t2si)
            ERR_DATACOL_TOO_LONG = 0x02, //*< too long (memory overflow)
            ERR_DATACOL_TOO_NOISY = 0x03, //*< too noisy
            ERR_DATACOL_TOO_SOFT = 0x04, //*< spoke too soft
            ERR_DATACOL_TOO_LOUD = 0x05, //*< spoke too loud
            ERR_DATACOL_TOO_SOON = 0x06, //*< spoke too soon
            ERR_DATACOL_TOO_CHOPPY = 0x07, //*< too many segments/too complex
            ERR_DATACOL_BAD_WEIGHTS = 0x08, //*< invalid SI weights
            ERR_DATACOL_BAD_SETUP = 0x09, //*< invalid setup

            //-- 1x: Recognition errors (si, sd, sv, train, t2si)
            ERR_RECOG_FAIL = 0x11, //*< recognition failed
            ERR_RECOG_LOW_CONF = 0x12, //*< recognition result doubtful
            ERR_RECOG_MID_CONF = 0x13, //*< recognition result maybe
            ERR_RECOG_BAD_TEMPLATE = 0x14, //*< invalid SD/SV template
            ERR_RECOG_BAD_WEIGHTS = 0x15, //*< invalid SI weights
            ERR_RECOG_DURATION = 0x17, //*< incompatible pattern durations

            //-- 2x: T2si errors (t2si)
            ERR_T2SI_EXCESS_STATES = 0x21, //*< state structure is too big
            ERR_T2SI_BAD_VERSION = 0x22, //*< RSC code version/Grammar ROM dont match
            ERR_T2SI_OUT_OF_RAM = 0x23, //*< reached limit of available RAM
            ERR_T2SI_UNEXPECTED = 0x24, //*< an unexpected error occurred
            ERR_T2SI_OVERFLOW = 0x25, //*< ran out of time to process
            ERR_T2SI_PARAMETER = 0x26, //*< bad macro or grammar parameter

            ERR_T2SI_NN_TOO_BIG = 0x29, //*< layer size out of limits
            ERR_T2SI_NN_BAD_VERSION = 0x2A, //*< net structure incompatibility
            ERR_T2SI_NN_NOT_READY = 0x2B, //*< initialization not complete
            ERR_T2SI_NN_BAD_LAYERS = 0x2C, //*< not correct number of layers

            ERR_T2SI_TRIG_OOV = 0x2D, //*< trigger recognized Out Of Vocabulary
            ERR_T2SI_TOO_SHORT = 0x2F, //*< utterance was too short

            //-- 3x: Record and Play errors (standard RP and messaging)
            ERR_RP_BAD_LEVEL = 0x31, //*<  play - illegal compression level
            ERR_RP_NO_MSG = 0x38, //*<  play, erase, copy - msg doesn't exist
            ERR_RP_MSG_EXISTS = 0x39, //*<  rec, copy - msg already exists

            //-- 4x: Synthesis errors (talk, sxtalk)
            ERR_SYNTH_BAD_VERSION = 0x4A, //*< bad release number in speech file
            ERR_SYNTH_ID_NOT_SET = 0x4B, //*< (obsolete) bad sentence structure
            ERR_SYNTH_TOO_MANY_TABLES = 0x4C, //*< (obsolete) too many talk tables
            ERR_SYNTH_BAD_SEN = 0x4D, //*< (obsolete) bad sentence number
            ERR_SYNTH_BAD_MSG = 0x4E, //*< bad message data or SX technology files missing

            //-- 8x: Custom errors
            ERR_CUSTOM_NOTA = 0x80, //*< none of the above (out of grammar)
            ERR_CUSTOM_INVALID = 0x81, //*< invalid data (for memory check)

            //-- Cx: Internal errors (all)
            ERR_SW_STACK_OVERFLOW = 0xC0, //*< no room left in software stack
            ERR_INTERNAL_T2SI_BAD_SETUP = 0xCC, //*< T2SI test mode error
        }

        /** Type of Bridge mode requested */

        public enum BridgeMode
        {
            BRIDGE_NONE, //*< Bridge mode has not been requested
            BRIDGE_NORMAL, //*< Normal bridge mode (EasyVR baudrate 9600)
            BRIDGE_BOOT, //*< Bridge mode for EasyVR bootloader (baudrate 115200)
        }

        public enum BitNumber
        {
            BITS_4 = 4,
            BITS_8 = 8
        }
    }
}
