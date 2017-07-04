using System;

namespace Cavernlore.GameBrick
{
    public class MemoryManager
    {

        //Flag indicating that we are in BIOS
        //Once CPU reaches 0x00FF, BIOS is unmapped

        public bool inBios;

        //Memory pools
        public byte[] bios = 
        {
            0x31, 0xFE, 0xFF, 0xAF, 0x21, 0xFF, 0x9F, 0x32, 0xCB, 0x7C, 0x20, 0xFB, 0x21, 0x26, 0xFF, 0x0E,
            0x11, 0x3E, 0x80, 0x32, 0xE2, 0x0C, 0x3E, 0xF3, 0xE2, 0x32, 0x3E, 0x77, 0x77, 0x3E, 0xFC, 0xE0,
            0x47, 0x11, 0x04, 0x01, 0x21, 0x10, 0x80, 0x1A, 0xCD, 0x95, 0x00, 0xCD, 0x96, 0x00, 0x13, 0x7B,
            0xFE, 0x34, 0x20, 0xF3, 0x11, 0xD8, 0x00, 0x06, 0x08, 0x1A, 0x13, 0x22, 0x23, 0x05, 0x20, 0xF9,
            0x3E, 0x19, 0xEA, 0x10, 0x99, 0x21, 0x2F, 0x99, 0x0E, 0x0C, 0x3D, 0x28, 0x08, 0x32, 0x0D, 0x20,
            0xF9, 0x2E, 0x0F, 0x18, 0xF3, 0x67, 0x3E, 0x64, 0x57, 0xE0, 0x42, 0x3E, 0x91, 0xE0, 0x40, 0x04,
            0x1E, 0x02, 0x0E, 0x0C, 0xF0, 0x44, 0xFE, 0x90, 0x20, 0xFA, 0x0D, 0x20, 0xF7, 0x1D, 0x20, 0xF2,
            0x0E, 0x13, 0x24, 0x7C, 0x1E, 0x83, 0xFE, 0x62, 0x28, 0x06, 0x1E, 0xC1, 0xFE, 0x64, 0x20, 0x06,
            0x7B, 0xE2, 0x0C, 0x3E, 0x87, 0xF2, 0xF0, 0x42, 0x90, 0xE0, 0x42, 0x15, 0x20, 0xD2, 0x05, 0x20,
            0x4F, 0x16, 0x20, 0x18, 0xCB, 0x4F, 0x06, 0x04, 0xC5, 0xCB, 0x11, 0x17, 0xC1, 0xCB, 0x11, 0x17,
            0x05, 0x20, 0xF5, 0x22, 0x23, 0x22, 0x23, 0xC9, 0xCE, 0xED, 0x66, 0x66, 0xCC, 0x0D, 0x00, 0x0B,
            0x03, 0x73, 0x00, 0x83, 0x00, 0x0C, 0x00, 0x0D, 0x00, 0x08, 0x11, 0x1F, 0x88, 0x89, 0x00, 0x0E,
            0xDC, 0xCC, 0x6E, 0xE6, 0xDD, 0xDD, 0xD9, 0x99, 0xBB, 0xBB, 0x67, 0x63, 0x6E, 0x0E, 0xEC, 0xCC,
            0xDD, 0xDC, 0x99, 0x9F, 0xBB, 0xB9, 0x33, 0x3E, 0x3c, 0x42, 0xB9, 0xA5, 0xB9, 0xA5, 0x42, 0x4C,
            0x21, 0x04, 0x01, 0x11, 0xA8, 0x00, 0x1A, 0x13, 0xBE, 0x20, 0xFE, 0x23, 0x7D, 0xFE, 0x34, 0x20,
            0xF5, 0x06, 0x19, 0x78, 0x86, 0x23, 0x05, 0x20, 0xFB, 0x86, 0x20, 0xFE, 0x3E, 0x01, 0xE0, 0x50
        };
        public byte[] cartridgeRom;
        public byte[] workingRam;
        public byte[] extendedRam;
        public byte[] zeroPageRam;

        public byte interruptEnable;
        public byte interruptFlags;

        public byte divTimer; //0xFF04;
        public byte timer; //0xFF05;
        public byte timerModulo; //0xFF06;
        public byte timerControl; //0xFF07;

        public byte cartridgeType;
        public ushort romOffset;

        public bool memoryBank_ramOn;
        public byte memoryBank_romBank;
        public byte memoryBank_ramBank;
        public byte memoryBank_mode;


        //Memory addresses
        public ushort graphicsRamAddress = 0x8000;
        public ushort extendedRamAddress = 0xA000;
        public ushort workingRamAddress = 0xC000;
        public ushort shadowRamAddress = 0xE000;
        public ushort spriteRamAddress = 0xFE00;
        public ushort ioRamAddress = 0xFF00;
        public ushort zeroPageRamAddress = 0xFF80;

        private GPU _gpu;
        private Input _input;


        public MemoryManager()
        {

        }

        public void SetGPU(GPU gpu)
        {
            _gpu = gpu;
            Reset();
        }

        public void SetInput(Input input)
        {
            _input = input;
        }

        public void Reset()
        {
            workingRam = new byte[0x2000];
            extendedRam = new byte[0x2000];
            zeroPageRam = new byte[0x80];
            inBios = true;
            interruptEnable = 0;
            interruptFlags = 0;

            divTimer = 0;
            timer = 0;
            timerModulo = 0;
            timerControl = 0;

            cartridgeType = 0;
            romOffset = 0x4000;
        }

        public byte ReadByte(ushort address)
        {
            switch (address & 0xF000)
            {

                    //ROM bank 0
                case 0x0000:
                    if (inBios && address < 0x0100)
                    {
                        return bios[address];
                    }
                    else
                    {
                        return cartridgeRom[address];
                    }
                    break;
                case 0x1000:
                case 0x2000:
                case 0x3000:
                    return cartridgeRom[address];
                    break;

                    //ROM bank 1
                case 0x4000:
                case 0x5000:
                case 0x6000:
                case 0x7000:
                    return cartridgeRom[address];
                    break;
                    //TODO: Add rom bank switching

                    //VRAM
                case 0x8000:
                case 0x9000:
                    return _gpu.graphicsMemory[address&0x1FFF];
                    break;
                case 0xA000: case 0xB000:
                    return extendedRam[address & 0x1FFF];
                    break;

                case 0xC000: case 0xD000: case 0xE000:
                    return workingRam[address & 0x1FFF];
                    break;

                case 0xF000:
                    switch (address & 0x0F00)
                    {
                        //Echo Ram
                        case 0x000:
                        case 0x100:
                        case 0x200:
                        case 0x300:
                        case 0x400:
                        case 0x500:
                        case 0x600:
                        case 0x700:
                        case 0x800:
                        case 0x900:
                        case 0xA00:
                        case 0xB00:
                        case 0xC00:
                        case 0xD00:
                            return workingRam[address&0x1FFF];
                            break;

                        //OAM
                        case 0xE00:
                            if ((address & 0xFF) < 0xA0)
                            {
                                return _gpu.spriteInformation[address & 0xFF];
                            }
                            else
                            {
                                return 0;
                            }
                            break;

                        //Zeropage RAM, IO, interrupts
                        case 0xF00:
                            if (address == 0xFFFF)
                            {
                                return interruptEnable;
                            }
                            else if (address > 0xFF7f)
                            {
                                return zeroPageRam[address & 0x7F];
                            }
                            else
                            {
                                switch (address & 0xF0)
                                {
                                    case 0x00:
                                        switch (address & 0xF)
                                        {
                                            case 0:
                                                _input.ReadByte();
                                                break;
                                            case 4:
                                            case 5:
                                            case 6:
                                            case 7:
                                                throw new NotImplementedException();
                                                break;
                                            case 8:
                                            case 9:
                                            case 10:
                                            case 11:
                                            case 12:
                                            case 13:
                                            case 14:
                                                throw new NotImplementedException();
                                                break;
                                            case 15:
                                                return interruptFlags;
                                                break;
                                        }
                                        break;

                                    case 0x10:
                                    case 0x20:
                                    case 0x30:
                                        return 0;
                                        break;

                                    case 0x40:
                                    case 0x50:
                                    case 0x60:
                                    case 0x70:
                                        return _gpu.ReadByte(address);
                                        break;
                                }
                            }
                            break;

                    }
                    break;

                default:
                    throw new NotImplementedException();
                    break;
            }
            return 0;
        }

        public ushort ReadWord(ushort address)
        {
            byte lowByte = ReadByte(address);
            byte highByte = ReadByte((ushort)(address + 1));
            ushort result = (ushort)(lowByte + (highByte << 8));
            return result;
        }

        public void WriteByte(ushort address, byte value)
        {
            switch (address & 0xF000)
            {

                case 0x0000:
                case 0x1000:
                    switch (cartridgeType)
                    {
                        case 1:
                            memoryBank_ramOn = (value & 0xF) == 0xA;
                            break;
                        default:
                            //throw new NotImplementedException();
                            break;
                    }
                    break;
                case 0x2000:
                case 0x3000:
                    //throw new NotImplementedException();
                    break;

                //ROM bank 1
                case 0x4000:
                case 0x5000:
                    throw new NotImplementedException();
                    break;
                case 0x6000:
                case 0x7000:
                    throw new NotImplementedException();
                    break;
                //TODO: Add rom bank switching

                //VRAM
                case 0x8000:
                case 0x9000:
                    _gpu.graphicsMemory[address&0x1FFF] = value;
                    if (_gpu.graphicsMemory[0x0007] > 0)
                    {

                    }
                    break;

                //External RAM
                case 0xA000:
                case 0xB000:
                    extendedRam[address & 0x1FFF] = value;
                    //TODO: Account for rom bank switching
                    break;

                    //Work RAM
                case 0xC000:
                case 0xD000:
                case 0xE000:
                    workingRam[address & 0x1FFF] = value;
                    break;

                case 0xF000:
                    switch (address & 0x0F00)
                    {
                            //Echo Ram
                        case 0x000:
                        case 0x100:
                        case 0x200:
                        case 0x300:
                        case 0x400:
                        case 0x500:
                        case 0x600:
                        case 0x700:
                        case 0x800:
                        case 0x900:
                        case 0xA00:
                        case 0xB00:
                        case 0xC00:
                        case 0xD00:
                            workingRam[address & 0x1FFF] = value;
                            break;

                            //OAM
                        case 0xE00:
                            if ((address & 0xFF) < 0xA0)
                            {
                                _gpu.spriteInformation[address & 0xFF] = value;
                            }
                            break;

                            //Zeropage RAM, IO, interrupts
                        case 0xF00:
                            if (address == 0xFFFF)
                            {
                                interruptEnable = value;
                            }
                            else if (address > 0xFF7f)
                            {
                                zeroPageRam[address & 0x7F] = value;
                            }
                            else
                            {
                                switch (address&0xF0)
                                {
                                    case 0x00:
                                        switch (address & 0xF)
                                        {
                                            case 0:
                                                _input.WriteByte(value);
                                                break;
                                            case 1:
                                            case 2:
                                                //throw new NotImplementedException(); //Serial IO
                                                break;
                                            case 4:
                                                divTimer = 0; //Reset divTimer
                                                break;
                                            case 5:
                                                timer = value;
                                                break;
                                            case 6:
                                                timerModulo = value;
                                                break;
                                            case 7:
                                                timerControl = value;
                                                break;
                                            case 8:
                                            case 9:
                                            case 10:
                                            case 11:
                                            case 12:
                                            case 13:
                                            case 14:
                                                throw new NotImplementedException();
                                                break;
                                            case 15:
                                                interruptFlags = value;
                                                break;
                                        }
                                        break;

                                    case 0x10: case 0x20: case 0x30:
                                        //Sound
                                        break;

                                    case 0x40: case 0x50:
                                    case 0x60: case 0x70:
                                        _gpu.WriteByte(address, value);
                                        break;
                            }
                            }
                            break;

                    }
                    break;


                default:
                    break;
            }
        }

        public void WriteWord(ushort address, ushort value)
        {
            WriteByte(address, (byte)(value & 255));
            WriteByte((ushort)(address + 1), (byte)(value >> 8));
        }

        public void LoadCartridge(string file)
        {
            cartridgeRom = System.IO.File.ReadAllBytes(file);
            cartridgeType = cartridgeRom[0x0147];
        }
    }
}
