using System.ComponentModel;
using EasyVRLibrary.Properties;

namespace EasyVRLibrary
{
    public enum ArgumentCode
    {
        ArgNegOne = '@',
        Arg0 = 'A',
        Arg1 = 'B',
        Arg2 = 'C',
        Arg3 = 'D',
        Arg4 = 'E',
        Arg5 = 'F',
        Arg6 = 'G',
        Arg7 = 'H',
        Arg8 = 'I',
        Arg9 = 'J',
        Arg10 = 'K',
        Arg11 = 'L',
        Arg12 = 'M',
        Arg13 = 'N',
        Arg14 = 'O',
        Arg15 = 'P',
        Arg16 = 'Q',
        Arg17 = 'R',
        Arg18 = 'S',
        Arg19 = 'T',
        Arg20 = 'U',
        Arg21 = 'V',
        Arg22 = 'W',
        Arg23 = 'X',
        Arg24 = 'Y',
        Arg25 = 'Z',
        Arg26 = '^',
        Arg27 = '[',
        Arg28 = '\\',
        Arg29 = ']',
        Arg30 = '_',
        Arg31 = '`'
    }

    public class ArgumentEncoding
    {
        public static int ConvertArgumentCode(char argumentCode)
        {
            switch ((ArgumentCode)argumentCode)
            {
                case ArgumentCode.ArgNegOne:
                    return -1;
                case ArgumentCode.Arg0:
                    return 0;
                case ArgumentCode.Arg1:
                    return 1;
                case ArgumentCode.Arg2:
                    return 2;
                case ArgumentCode.Arg3:
                    return 3;
                case ArgumentCode.Arg4:
                    return 4;
                case ArgumentCode.Arg5:
                    return 5;
                case ArgumentCode.Arg6:
                    return 6;
                case ArgumentCode.Arg7:
                    return 7;
                case ArgumentCode.Arg8:
                    return 8;
                case ArgumentCode.Arg9:
                    return 9;
                case ArgumentCode.Arg10:
                    return 10;
                case ArgumentCode.Arg11:
                    return 11;
                case ArgumentCode.Arg12:
                    return 12;
                case ArgumentCode.Arg13:
                    return 13;
                case ArgumentCode.Arg14:
                    return 14;
                case ArgumentCode.Arg15:
                    return 15;
                case ArgumentCode.Arg16:
                    return 16;
                case ArgumentCode.Arg17:
                    return 17;
                case ArgumentCode.Arg18:
                    return 18;
                case ArgumentCode.Arg19:
                    return 19;
                case ArgumentCode.Arg20:
                    return 20;
                case ArgumentCode.Arg21:
                    return 21;
                case ArgumentCode.Arg22:
                    return 22;
                case ArgumentCode.Arg23:
                    return 23;
                case ArgumentCode.Arg24:
                    return 24;
                case ArgumentCode.Arg25:
                    return 25;
                case ArgumentCode.Arg26:
                    return 26;
                case ArgumentCode.Arg27:
                    return 27;
                case ArgumentCode.Arg28:
                    return 28;
                case ArgumentCode.Arg29:
                    return 29;
                case ArgumentCode.Arg30:
                    return 30;
                case ArgumentCode.Arg31:
                    return 31;
                
            }

            throw new InvalidEnumArgumentException(Resources.ArgumentEncoding_ConvertArgumentCode_Out_of_range);
        }



        public static ArgumentCode IntToArgumentCode(int integer)
        {
            switch (integer)
            {
                case -1:
                    return ArgumentCode.ArgNegOne;
                case 0:
                    return ArgumentCode.Arg0;
                case 1:
                    return ArgumentCode.Arg1;
                case 2:
                    return ArgumentCode.Arg2;
                case 3:
                    return ArgumentCode.Arg3;
                case 4:
                    return ArgumentCode.Arg4;
                case 5:
                    return ArgumentCode.Arg5;
                case 6:
                    return ArgumentCode.Arg6;
                case 7:
                    return ArgumentCode.Arg7;
                case 8:
                    return ArgumentCode.Arg8;
                case 9:
                    return ArgumentCode.Arg9;
                case 10:
                    return ArgumentCode.Arg10;
                case 11:
                    return ArgumentCode.Arg11;
                case 12:
                    return ArgumentCode.Arg12;
                case 13:
                    return ArgumentCode.Arg13;
                case 14:
                    return ArgumentCode.Arg14;
                case 15:
                    return ArgumentCode.Arg15;
                case 16:
                    return ArgumentCode.Arg16;
                case 17:
                    return ArgumentCode.Arg17;
                case 18:
                    return ArgumentCode.Arg18;
                case 19:
                    return ArgumentCode.Arg19;
                case 20 :
                    return ArgumentCode.Arg20;
                case 21:
                    return ArgumentCode.Arg21;
                case 22:
                    return ArgumentCode.Arg22;
                case 23:
                    return ArgumentCode.Arg23;
                case 24:
                    return ArgumentCode.Arg24;
                case 25:
                    return ArgumentCode.Arg25;
                case 26:
                    return ArgumentCode.Arg26;
                case 27:
                    return ArgumentCode.Arg27;
                case 28:
                    return ArgumentCode.Arg28;
                case 29:
                    return ArgumentCode.Arg29;
                case 30 :
                    return ArgumentCode.Arg30;
                case 31:
                    return ArgumentCode.Arg31;

            }

            throw new InvalidEnumArgumentException(Resources.ArgumentEncoding_ConvertArgumentCode_Out_of_range);
        }

        public static string IntToArgumentString(int integer)
        {
            var tempChar = (char) IntToArgumentCode(integer);
            return tempChar.ToString();
        }
    }
}