using System;

namespace Cavernlore.GameBrick
{
    public static class ByteExtensions
    {
        public static byte LowerByte(this ushort s)
        {
            return (byte)(s & 0xff);
        }
        public static byte UpperByte(this ushort s)
        {
            return (byte)(s << 8);
        }
        public static byte LowerNibble(this byte b)
        {
            return (byte)(b & 0xf);
        }
        public static byte UpperNibble(this byte b)
        {
            return (byte)(b << 4);
        }

        public static int ToSigned(this byte b)
        {
            int result = b;
            if (b > 127)
            {
                result = -((~b + 1) & 255);
            }
            return result;
        }

        public static byte SetBit(this byte b, byte index)
        {
            switch (index)
            {
                case 0: b |= 0x01; break;
                case 1: b |= 0x02; break;
                case 2: b |= 0x04; break;
                case 3: b |= 0x08; break;
                case 4: b |= 0x10; break;
                case 5: b |= 0x20; break;
                case 6: b |= 0x40; break;
                case 7: b |= 0x80; break;
                default: throw new ArgumentOutOfRangeException();
            }
            return b;
        }

        public static byte ResetBit(this byte b, byte index)
        {
            switch (index)
            {
                case 0: b &= 0xFE; break;
                case 1: b &= 0xFD; break;
                case 2: b &= 0xFB; break;
                case 3: b &= 0xF7; break;
                case 4: b &= 0xEF; break;
                case 5: b &= 0xDF; break;
                case 6: b &= 0xBF; break;
                case 7: b &= 0x7F; break;
                default: throw new ArgumentOutOfRangeException();
            }
            return b;
        }

        public static bool GetBit(this byte b, byte index)
        {
            switch (index)
            {
                case 0: return (b & 0x01) == 0x01;
                case 1: return (b & 0x02) == 0x02;
                case 2: return (b & 0x04) == 0x04;
                case 3: return (b & 0x08) == 0x08;
                case 4: return (b & 0x10) == 0x10;
                case 5: return (b & 0x20) == 0x20;
                case 6: return (b & 0x40) == 0x40;
                case 7: return (b & 0x80) == 0x80;
                default: throw new ArgumentOutOfRangeException();
            }
        }

        public static byte ShiftLeft(this byte b) { return b.ShiftLeft(1); }
        public static byte ShiftLeft(this byte b, int num)
        {
            return unchecked((byte)(b << 1));
        }

        public static byte ShiftRight(this byte b) { return b.ShiftRight(1); }
        public static byte ShiftRight(this byte b, int num)
        {
            return unchecked((byte)(b >> 1));
        }

        public static byte RotateLeft(this byte b, int num)
        {
            bool topBit = b.GetBit(7);
            byte newByte = unchecked((byte)(b << 1));
            if (topBit) { newByte = newByte.SetBit(0); }
            return newByte;
        }
    }
}
