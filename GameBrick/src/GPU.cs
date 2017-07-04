
namespace Cavernlore.GameBrick
{
    public class GPU
    {
        //8000-87FF: Tile set #1: 0-127
        //8800-8FFF: Tile set #1: 128-255; Tile set #0: -1 to -128
        //9000-97FF: Tile set #0: 0-127
        //9800-9BFF: Tile map #0
        //9C00-9FFF: Tile map #1
        public byte[] graphicsMemory;

        //FE00-FE9F: Sprite info
        public byte[] spriteInformation;
        /*  Each sprite is 8x8 and has 4 bytes of data:
         * Byte 0 = Y coordinate of top left - 16;
         * Byte 1 = X coordinate of top left - 8;
         * Byte 2 = tile number;
         * Byte 3 = flags: 7=above/below bg; 6=Yflip; 5=Xflip; 4=palette
         */


        public byte controlFlags; //Read/write
        /*
         * Byte 0: background on/off
         * Byte 1: sprites on/off
         * Byte 2: sprite size (8x8 vs 8x16)
         * Byte 3: tilemap
         * Byte 4: tileset
         * Byte 5: Window on/off
         * Byte 6: Window tilemap
         * Byte 7: display on/off
         * 
         */

        #region Control Flag Accessors

        public bool BackgroundOn
        {
            get { return (controlFlags & 0x01) > 0; }
            set { if (value) { controlFlags |= 0x01; } else { controlFlags &= unchecked((byte)(~0x01)); } }
        }

        public bool SpritesOn
        {
            get { return (controlFlags & 0x02) > 0; }
            set { if (value) { controlFlags |= 0x02; } else { controlFlags &= unchecked((byte)(~0x02)); } }
        }

        public bool SpriteSize
        {
            get { return (controlFlags & 0x04) > 0; }
            set { if (value) { controlFlags |= 0x04; } else { controlFlags &= unchecked((byte)(~0x04)); } }
        }

        public bool BackgroundMapSet
        {
            get { return (controlFlags & 0x08) > 0; }
            set { if (value) { controlFlags |= 0x08; } else { controlFlags &= unchecked((byte)(~0x08)); } }
        }

        public bool BackgroundTileSet
        {
            get { return (controlFlags & 0x10) > 0; }
            set { if (value) { controlFlags |= 0x10; } else { controlFlags &= unchecked((byte)(~0x10)); } }
        }

        public bool ScreenOn
        {
            get { return (controlFlags & 0x80) > 0; }
            set { if (value) { controlFlags |= 0x80; } else { controlFlags &= unchecked((byte)(~0x80)); } }
        }

        #endregion


        public byte[] reg;

        public byte scrollX; //0xFF43
        public byte scrollY; //0xFF42

        public byte currentScanLine; //0xFF44, externally readonly
        public byte backgroundPalette; //0xFF47, externally writeonly

        //What's actually on the screen
        public byte[] screenData;


        

        //GPU States
        //

        enum GPU_STATE { HORIZONTAL_BLANK, VERTICAL_BLANK, OAM_SCAN, VRAM_SCAN }

        const int OAM_SCAN_TIME = 80;
        const int VRAM_SCAN_TIME = 172;
        const int H_BLANK_TIME = 204;
        const int SCANLINE_TIME = OAM_SCAN_TIME + VRAM_SCAN_TIME + H_BLANK_TIME;
        const int V_BLANK_TIME = SCANLINE_TIME*10;

        private GPU_STATE _state;
        private int _gpuClock;

        private MemoryManager _mmu;

        public GPU()
        {
            Reset();
        }

        public void SetMMU(MemoryManager mmu)
        {
            _mmu = mmu;
        }

        public void Reset()
        {
            graphicsMemory = new byte[0x2000];
            spriteInformation = new byte[0x00A0];
            screenData = new byte[160 * 144 * 4];
            reg = new byte[1024];
            for (int i = 0; i < screenData.Length; i++)
            {
                screenData[i] = 128;
            }
        }


        public void Step(int lastCycleTime)
        {
            _gpuClock += lastCycleTime;

            switch (_state)
            {
                case GPU_STATE.HORIZONTAL_BLANK:
                    if (_gpuClock >= H_BLANK_TIME)
                    {
                        if (currentScanLine == 143)
                        {
                            _state = GPU_STATE.VERTICAL_BLANK;
                            _mmu.interruptFlags |= 1;
                        }
                        else
                        {
                            _state = GPU_STATE.OAM_SCAN;
                        }
                        currentScanLine++;
                        _gpuClock -= H_BLANK_TIME;
                    }
                    break;
                case GPU_STATE.VERTICAL_BLANK:
                    if (_gpuClock >= V_BLANK_TIME)
                    {
                        _gpuClock -= V_BLANK_TIME;
                        currentScanLine++;
                        if (currentScanLine > 153)
                        {
                            currentScanLine = 0;
                            _state = GPU_STATE.OAM_SCAN;
                        }
                    }
                    break;
                case GPU_STATE.OAM_SCAN:
                    if (_gpuClock >= OAM_SCAN_TIME)
                    {
                        _gpuClock -= OAM_SCAN_TIME;
                        _state = GPU_STATE.VRAM_SCAN;
                    }
                    break;
                case GPU_STATE.VRAM_SCAN:
                    if (_gpuClock >= VRAM_SCAN_TIME)
                    {
                        _gpuClock -= VRAM_SCAN_TIME;
                        _state = 0;
                        ProcessScanline();
                    }
                    break;
                default:
                    break;
            }
        }

        public byte ReadByte(ushort address)
        {
            ushort relativeAddress = (ushort)(address - 0xFF40);

            switch (relativeAddress)
            {
                case 0:
                    return controlFlags;
                    break;
                case 1:
                    break;
                case 2:
                    return scrollY;
                case 3:
                    return scrollX;
                case 4:
                    return currentScanLine;
                case 5:
                    break;
                default:
                    return reg[relativeAddress];
            }


            return 0;
        }

        public void WriteByte(ushort address, byte value)
        {
            ushort relativeAddress = (ushort)(address - 0xFF40);
            reg[relativeAddress] = value;
            switch (relativeAddress)
            {
                case 0:
                    controlFlags = value;
                    break;
                case 1:
                    break;
                case 2:
                    scrollY = value;
                    break;
                case 3:
                    scrollX = value;
                    break;
                case 4:
                    currentScanLine = value;
                    break;
                case 5:
                    break;
                default:
                    //graphicsMemory[relativeAddress] = value;
                    break;
            }
        }


        private void ProcessScanline()
        {
            //Background Render
            if (BackgroundOn)
            {
                //First, figure out which tile we are going to start with
                

                //Figure out what line of tiles we are using, which depends on the scanline we are drawing and our y scroll value
                ushort curScanLine = currentScanLine;
                ushort scanlineInTiles = (ushort)(((currentScanLine + scrollY) & 255) >> 3);
                ushort tilemapOffset = (ushort)(scanlineInTiles*32);
                
                //Now we figure out which tile we start with, depending on windowX
                ushort tilemapXOffset = (ushort)(scrollX >> 3);

                //We also need to figure out what pixel of the tile we need to draw
                ushort pixelY = (ushort)((currentScanLine + scrollY) & 7);
                ushort pixelX = (ushort)(scrollX & 7);

                if (tilemapOffset == 8)
                {

                }

                tilemapOffset += 0x1800; //Use map 1

                int canvasOffset = currentScanLine * 160 * 4; //Where do we start drawing to screen?

                byte color;
                ushort tileAddress = graphicsMemory[tilemapOffset + tilemapXOffset];
                if (!BackgroundTileSet && tileAddress < 128)
                    tileAddress += 256;

                for (int i = 0; i < 160; i++)
                {
                    //So now we know that we are looking at the tile at the address [tileAddress].
                    //Each tile is 8x8 pixels, and each pixel can be 4 different colors, so tile data is held in 16 bytes.
                    //A single pixel is held in two bits of data, but those two bits are held in two seperate bytes.
                    //The low bit comes first, and the high bit comes second.

                    //Each row of the tile is held in two bytes, so find which two bytes we are dealing with.

                    byte lowByte = graphicsMemory[(ushort)(tileAddress*16 + pixelY*2)];
                    byte highByte = graphicsMemory[(ushort)(tileAddress*16 + pixelY*2 + 1)];

                    //Figure out which bit in the byte we are looking at, based on x position in the tile
                    byte pixelIndex = (byte)(1 << (7 - pixelX));

                    //Get the pixel value
                    byte pixelValue = (byte)((((lowByte & pixelIndex) > 0) ? 1 : 0) + (((highByte & pixelIndex) > 0) ? 2 : 0));

                    screenData[canvasOffset + 0] = (byte)(255 - (pixelValue * 96));
                    screenData[canvasOffset + 1] = (byte)(255 - (pixelValue * 96));
                    screenData[canvasOffset + 2] = (byte)(255 - (pixelValue * 96));
                    screenData[canvasOffset + 3] = (byte)(255 - (pixelValue * 96));
                    canvasOffset += 4;

                    pixelX++;
                    if (pixelX == 8)
                    {
                        pixelX = 0;
                        tilemapXOffset = (ushort)((tilemapXOffset + 1) & 31);
                        tileAddress = graphicsMemory[tilemapOffset + tilemapXOffset];
                        if (!BackgroundTileSet && tileAddress < 128)
                            tileAddress += 256;
                    }
                }

            }

            //Sprite Render
            if (SpritesOn)
            {
                //throw new NotImplementedException();
            }
        }

    }
}
