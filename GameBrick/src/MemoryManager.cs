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
        public byte[] cartridgeRom; //Full cartridge rom image from file
        public byte[] workingRam; //Internal C000-DFFF ram
        public byte[] extendedRam;
        public byte[] zeroPageRam;

        public byte interruptEnable;
        public byte interruptFlags;

        public byte cartridgeType;
        public ushort romOffset; //Offset into cartridgeRom based on bank we are switched into

        public bool memoryBank_ramOn; //(Write to 0000-1FFF) Is cartridge RAM read/write enabled (write 0x0A to enable, anything else to disable)
        public byte memoryBank_romBank; //(Write to 2000-3FFF) Map the selected ROM bank to 4000-7FFF (0 and 1 both map BANK1)
        public byte memoryBank_ramBank; //(Write to 4000-5FFF) Map the selected RAM bank to A000-BFFF (if in 16/8 mode, set the two most significant ROM address lines (effectively multiplying memoryBank_romBank by 0-3)
        public bool memoryBank_mode; //(Write to 6000-7FFF) false=16Mbit ROM/8KB RAM, true=4Mbit ROM/32KB RAM


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
        private Timer _timer;

        private IVisualizer _visualizer;

        public MemoryManager()
        {
            _visualizer = new FakeVisualizer();
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

        public void SetTimer(Timer timer)
        {
            _timer = timer;
        }

        public void SetVisualizer(IVisualizer visualizer)
        {
            _visualizer = visualizer;
        }

        public void Reset()
        {
            workingRam = new byte[0x2000];
            extendedRam = new byte[0x2000];
            zeroPageRam = new byte[0x80];
            inBios = true;
            interruptEnable = 0;
            interruptFlags = 0;

            cartridgeType = 0;
            romOffset = 0x4000;

            memoryBank_ramOn = true;
            memoryBank_romBank = 1;
            memoryBank_ramBank = 0;
            memoryBank_mode = false;

            _visualizer.SetROMBank(1);
        }

        public byte ReadByte(ushort address)
        {
            _visualizer.ReadByte(address);
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
                    return cartridgeRom[address + romOffset*memoryBank_romBank - 0x4000];
                    break;
                    //TODO: Add 16/8 mode switching

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
                    //TODO: Add switching

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
                                                //Input buttons
                                            case 0:
                                                return _input.ReadByte();
                                                break;
                                                //Serial IO
                                            case 1:
                                            case 2:
                                                //throw new NotImplementedException();
                                                break;
                                                //Timer
                                            case 4:
                                                return _timer.Divider;
                                            case 5:
                                                return _timer.Counter;
                                            case 6:
                                                return _timer.Modulo;
                                            case 7:
                                                return _timer.Control;
                                                //8-E - Unused
                                            case 8:
                                            case 9:
                                            case 10:
                                            case 11:
                                            case 12:
                                            case 13:
                                            case 14:
                                                throw new NotImplementedException();
                                                break;
                                                //Interrupt status
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
            _visualizer.WriteByte(address, value);
            switch (address & 0xF000)
            {
                //ROM addresses, memory bank switching instructions are intercepted here
                case 0x0000:
                case 0x1000:
                    memoryBank_ramOn = (value & 0xF) == 0xA;
                    break;
                case 0x2000:
                case 0x3000:
                    memoryBank_romBank = value;
                    Console.WriteLine("Switched ROM to " + value);
                    break;
                case 0x4000:
                case 0x5000:
                    memoryBank_ramBank = value;
                    break;
                case 0x6000:
                case 0x7000:
                    memoryBank_mode = value > 0;
                    break;

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
                                                _timer.Divider = 0; //Reset divTimer
                                                break;
                                            case 5:
                                                _timer.Counter = value;
                                                break;
                                            case 6:
                                                _timer.Modulo = value;
                                                break;
                                            case 7:
                                                _timer.Control = value;
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

        //Return a block of A0 bytes for use with DMA transfers.
        public byte[] ReadDMABlock(byte target)
        {
            _visualizer.DMA(target);
            ushort address = (ushort)(target * 0x100);
            byte[] result = new byte[0xA0];
            if (address < 0x4000) //ROM0
            {
                Array.Copy(cartridgeRom, address, result, 0, 0xA0);
            }
            else if (address < 0x8000) //ROM(switched bank)
            {
                Array.Copy(cartridgeRom, address + romOffset * memoryBank_romBank - 0x4000, result, 0, 0xA0);
            }
            else if (address < 0xA000) //Graphics ROM
            {
                Array.Copy(_gpu.graphicsMemory, address - 0x8000, result, 0, 0xA0);
            }
            else if (address < 0xC000)
            {
                Array.Copy(extendedRam, address - 0xA000, result, 0, 0xA0);
            }
            else if (address < 0xF000)
            {
                Array.Copy(workingRam, address - 0xC000, result, 0, 0xA0);
            }
            else
            {
                throw new NotImplementedException();
            }

            return result;
        }

        public void LoadCartridge(string file)
        {
            cartridgeRom = System.IO.File.ReadAllBytes(file);
            cartridgeType = cartridgeRom[0x0147];
        }
    }
}
