
using System;
using System.Collections.Generic;

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

        //Tileset #0 is only used for background/window, it is numbered -128 to 127
        //Tileset #1 can be used for sprites as well as background/window, it is numbered 0-255.
        //Each 2 bytes represents 1 line of a tile/sprite image (8 2-bit palette values).



        //FE00-FE9F: Sprite info
        public byte[] spriteInformation;
        /*  Each sprite is 8x8 and has 4 bytes of data:
         * Byte 0 = Y coordinate of top left - 16;
         * Byte 1 = X coordinate of top left - 8;
         * Byte 2 = pattern number (aka offset from 0x8000);
         * Byte 3 = flags: 7=above/below bg; 6=Yflip; 5=Xflip; 4=palette
         */


        public byte controlFlags; //Read/write
        /*
         * Bit 0: background on/off
         * Bit 1: sprites on/off
         * Bit 2: sprite size (8x8 vs 8x16)
         * Bit 3: tilemap
         * Bit 4: tileset
         * Bit 5: Window on/off
         * Bit 6: Window tilemap
         * Bit 7: display on/off
         * 
         */

        public byte statusFlags; //Read/write
        /*
         * These flags contain the current mode (e.g. VBlank) and set what will cause the GPU to trigger an LCDC interrupt.
         * 
         * Bit 0-1: 00=Hblank, 01=VBlank, 10=OAM processing, 11=Drawing
         * Bit 2: True if currentScanLine == compareScanLine
         * Bit 3: Interrupt on HBlank
         * Bit 4: Interrupt on VBlank
         * Bit 5: Interrupt on OAM
         * Bit 6: Interrupt when currentScanLine == compareScanLine
         * 
         * 
         */

        #region Control Flag Accessors

        public bool BackgroundOn
        {
            get { return controlFlags.GetBit(0); }
            set { if (value) { controlFlags = controlFlags.SetBit(0); } else { controlFlags = controlFlags.ResetBit(0); } }
        }

        public bool SpritesOn
        {
            get { return controlFlags.GetBit(1); }
            set { if (value) { controlFlags = controlFlags.SetBit(1); } else { controlFlags = controlFlags.ResetBit(1); } }
        }

        public bool SpriteSize
        {
            get { return controlFlags.GetBit(2); }
            set { if (value) { controlFlags = controlFlags.SetBit(2); } else { controlFlags = controlFlags.ResetBit(2); } }
        }

        public bool BackgroundMapSet
        {
            get { return controlFlags.GetBit(3); }
            set { if (value) { controlFlags = controlFlags.SetBit(3); } else { controlFlags = controlFlags.ResetBit(3); } }
        }

        public bool BackgroundTileSet
        {
            get { return controlFlags.GetBit(4); }
            set { if (value) { controlFlags = controlFlags.SetBit(4); } else { controlFlags = controlFlags.ResetBit(4); } }
        }

        public bool WindowOn
        {
            get { return controlFlags.GetBit(5); }
            set { if (value) { controlFlags = controlFlags.SetBit(5); } else { controlFlags = controlFlags.ResetBit(5); } }
        }

        public bool WindowMapSet
        {
            get { return controlFlags.GetBit(6); }
            set { if (value) { controlFlags = controlFlags.SetBit(6); } else { controlFlags = controlFlags.ResetBit(6); } }
        }

        public bool ScreenOn
        {
            get { return controlFlags.GetBit(7); }
            set { if (value) { controlFlags = controlFlags.SetBit(7); } else { controlFlags = controlFlags.ResetBit(7); } }
        }

        #endregion

        #region Status Flag Accessors

        GPU_STATE CurrentMode
        {
            set
            {
                statusFlags = statusFlags.ResetBit(0);
                statusFlags = statusFlags.ResetBit(1);
                if (((byte)value).GetBit(0)) { statusFlags = statusFlags.SetBit(0); }
                if (((byte)value).GetBit(1)) { statusFlags = statusFlags.SetBit(1); }
            }
        }

        bool CoincidenceFlag
        {
            set { if (value) { statusFlags = statusFlags.SetBit(2); } else { statusFlags = statusFlags.ResetBit(2); } }
        }

        bool InterruptHBlank
        {
            get { return statusFlags.GetBit(3); }
        }

        bool InterruptVBlank
        {
            get { return statusFlags.GetBit(4); }
        }

        bool InterruptOAM
        {
            get { return statusFlags.GetBit(5); }
        }

        bool InterruptScanLine
        {
            get { return statusFlags.GetBit(6); }
        }

        #endregion

        public byte[] reg;

        public byte scrollX; //0xFF43
        public byte scrollY; //0xFF42

        public byte currentScanLine; //0xFF44, externally readonly
        public byte compareScanLine; //0xFF45
        public byte backgroundPalette; //0xFF47
        public byte spritePalette0; //0xFF48
        public byte spritePalette1; //0xFF49

        public byte windowY; //0xFF4A;
        public byte windowX; //0xFF4B;

        public byte nextWindowLine; //next line in window to draw during this frame

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

        byte[] PIXEL_VALUES = { 255, 180, 90, 0 };
        byte DEFAULT_PALETTE = 0xE4;

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
            backgroundPalette = DEFAULT_PALETTE;
            spritePalette0 = DEFAULT_PALETTE;
            spritePalette1 = DEFAULT_PALETTE;
            nextWindowLine = 0;
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
                            ChangeState(GPU_STATE.VERTICAL_BLANK);
                        }
                        else
                        {
                            ChangeState(GPU_STATE.OAM_SCAN);
                        }
                        IncrementScanLine();
                        _gpuClock -= H_BLANK_TIME;
                    }
                    break;
                case GPU_STATE.VERTICAL_BLANK:
                    if (_gpuClock >= V_BLANK_TIME)
                    {
                        _gpuClock -= V_BLANK_TIME;
                        IncrementScanLine();
                        if (currentScanLine > 153)
                        {
                            currentScanLine = 0;
                            nextWindowLine = 0;
                            ChangeState(GPU_STATE.OAM_SCAN);
                        }
                    }
                    break;
                case GPU_STATE.OAM_SCAN:
                    if (_gpuClock >= OAM_SCAN_TIME)
                    {
                        _gpuClock -= OAM_SCAN_TIME;
                        ChangeState(GPU_STATE.VRAM_SCAN);
                    }
                    break;
                case GPU_STATE.VRAM_SCAN:
                    if (_gpuClock >= VRAM_SCAN_TIME)
                    {
                        _gpuClock -= VRAM_SCAN_TIME;
                        ChangeState(GPU_STATE.HORIZONTAL_BLANK);
                        ProcessScanline();
                    }
                    break;
                default:
                    break;
            }

            CurrentMode = _state;
        }

        private void ChangeState(GPU_STATE newState)
        {
            _state = newState;
            CurrentMode = _state;
            switch (newState)
            {
                case GPU_STATE.HORIZONTAL_BLANK:
                    if (InterruptHBlank) { _mmu.interruptFlags = _mmu.interruptFlags.SetBit(1); }
                    break;
                case GPU_STATE.VERTICAL_BLANK:
                    _mmu.interruptFlags = _mmu.interruptFlags.SetBit(0);
                    if (InterruptVBlank) { _mmu.interruptFlags = _mmu.interruptFlags.SetBit(1); }
                    break;
                case GPU_STATE.OAM_SCAN:
                    if (InterruptOAM) { _mmu.interruptFlags = _mmu.interruptFlags.SetBit(1); }
                    break;
            }
        }

        private void IncrementScanLine()
        {
            currentScanLine++;
            if (currentScanLine == compareScanLine)
            {
                CoincidenceFlag = true;
                if (InterruptScanLine) { _mmu.interruptFlags = _mmu.interruptFlags.SetBit(1); }
            }
            else
            {
                CoincidenceFlag = false;
            }
        }

        public byte ReadByte(ushort address)
        {
            ushort relativeAddress = (ushort)(address - 0xFF40);

            switch (relativeAddress)
            {
                case 0:
                    return controlFlags;
                case 1:
                    return statusFlags;
                case 2:
                    return scrollY;
                case 3:
                    return scrollX;
                case 4:
                    return currentScanLine;
                case 5:
                    return compareScanLine;
                case 6:
                    //DMA transfer, no read
                    break;
                case 7:
                    //BG/Window palette data
                    return backgroundPalette;
                case 8:
                    return spritePalette0;
                case 9:
                    return spritePalette1;
                case 0xA:
                    return windowY;
                case 0xB:
                    return windowX;


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
                    statusFlags = value;
                    break;
                case 2:
                    scrollY = value;
                    break;
                case 3:
                    scrollX = value;
                    break;
                case 4:
                    currentScanLine = 0;
                    break;
                case 5:
                    compareScanLine = value;
                    CoincidenceFlag = (compareScanLine == currentScanLine);
                    break;
                case 6:
                    //DMA transfer
                    DMATransfer(value);
                    break;
                case 7:
                    backgroundPalette = value;
                    break;
                case 8:
                    spritePalette0 = value;
                    break;
                case 9:
                    spritePalette1 = value;
                    break;
                case 0xA:
                    windowY = value;
                    break;
                case 0xB:
                    windowX = value;
                    break;
                default:
                    //graphicsMemory[relativeAddress] = value;
                    break;
            }
        }

        //Copies the contents of memory from 0x[value]00-0x[value]A0 to OAM
        //Called by writing a target value to 0xFF46
        //Is supposed to take 160 microseconds in parallel with CPU
        private void DMATransfer(byte value)
        {
            spriteInformation = _mmu.ReadDMABlock(value);
        }


        private void ProcessScanline()
        {
            //Background Render
            if (BackgroundOn)
            {
                //First, figure out which tile we are going to start with
                

                //Figure out what line of tiles we are using, which depends on the scanline we are drawing and our y scroll value
                ushort scanlineInTiles = (ushort)(((currentScanLine + scrollY) & 255) >> 3);
                ushort tilemapOffset = (ushort)(scanlineInTiles*32);
                
                //Now we figure out which tile we start with, depending on scrollX
                ushort tilemapXOffset = (ushort)(scrollX >> 3);

                //We also need to figure out what pixel of the tile we need to draw
                ushort pixelY = (ushort)((currentScanLine + scrollY) & 7);
                ushort pixelX = (ushort)(scrollX & 7);
                

                tilemapOffset += 0x1800; //Use map 0
                if (BackgroundMapSet) { tilemapOffset += 0x400; } //Use map 1

                int canvasOffset = currentScanLine * 160 * 4; //Where do we start drawing to screen?

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
                    byte colorValue = GetColor(pixelValue, backgroundPalette);

                    screenData[canvasOffset + 0] = colorValue;
                    screenData[canvasOffset + 1] = colorValue;
                    screenData[canvasOffset + 2] = colorValue;
                    screenData[canvasOffset + 3] = 255;
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

            //Window Render
            if (WindowOn && currentScanLine >= windowY)
            {
                //Find the row of tiles we will be drawing from
                ushort scanlineInTiles = (ushort)(nextWindowLine >> 3);
                ushort tilemapOffset = (ushort)(scanlineInTiles * 32);
                ushort tilemapXOffset = 0; //We'll always start drawing with the first tile in the row

                //We also need to figure out what pixel of the tile to start on
                ushort pixelY = (ushort)((nextWindowLine) & 7);
                ushort pixelX = 0;
                if (windowX < 7) { pixelX = (ushort)(7 - windowX); }

                tilemapOffset += 0x1800; //Use map 0
                if (WindowMapSet) { tilemapOffset += 0x400; } //Use map 1

                int canvasOffset = currentScanLine * 160 * 4; //Where do we start drawing to screen?

                ushort tileAddress = graphicsMemory[tilemapOffset];
                if (!BackgroundTileSet && tileAddress < 128)
                    tileAddress += 256;

                for (int i = 0; i < 160; i++)
                {
                    //If we're on a pixel to the left of windowX, don't draw yet
                    if (i < windowX - 7)
                    {
                        canvasOffset += 4;
                        continue;
                    }

                    //So now we know that we are looking at the tile at the address [tileAddress].
                    //Each tile is 8x8 pixels, and each pixel can be 4 different colors, so tile data is held in 16 bytes.
                    //A single pixel is held in two bits of data, but those two bits are held in two seperate bytes.
                    //The low bit comes first, and the high bit comes second.

                    //Each row of the tile is held in two bytes, so find which two bytes we are dealing with.

                    byte lowByte = graphicsMemory[(ushort)(tileAddress * 16 + pixelY * 2)];
                    byte highByte = graphicsMemory[(ushort)(tileAddress * 16 + pixelY * 2 + 1)];

                    //Figure out which bit in the byte we are looking at, based on x position in the tile
                    byte pixelIndex = (byte)(1 << (7 - pixelX));

                    //Get the pixel value
                    byte pixelValue = (byte)((((lowByte & pixelIndex) > 0) ? 1 : 0) + (((highByte & pixelIndex) > 0) ? 2 : 0));
                    byte colorValue = GetColor(pixelValue, backgroundPalette);

                    screenData[canvasOffset + 0] = colorValue;
                    screenData[canvasOffset + 1] = colorValue;
                    screenData[canvasOffset + 2] = colorValue;
                    screenData[canvasOffset + 3] = 255;
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

                nextWindowLine++;
            }

            //Sprite Render
            if (SpritesOn)
            {
                byte spriteWidth = 8;
                byte spriteHeight = 8;
                if (SpriteSize) { spriteHeight = 16; }

                //Figure out what sprites are on this line
                //The gameboy can only draw the first 10 sprites on the line!
                byte[] spritesToDraw = new byte[10];
                byte numOfSprites = 0;
                for (byte spriteAddress = 0; spriteAddress < 0xA0 && numOfSprites < 10; spriteAddress+=4) //Each sprite has a 4-byte block of info
                {
                    int spriteY = spriteInformation[spriteAddress];

                    if (spriteY > 0)
                    {
                        //Sprite coordinates start at 8px to the left and 16px above the upper left corner of the screen
                        spriteY -= 16;

                        if (spriteY <= currentScanLine && spriteY + spriteHeight-1 >= currentScanLine)
                        {
                            spritesToDraw[numOfSprites] = spriteAddress;
                            numOfSprites++;
                        }
                    }
                }

                //Now we try to draw all the sprites we found
                for (int i = 0; i < numOfSprites; i++)
                {
                    //Get all relevant sprite info
                    byte spriteAddress = spritesToDraw[i];
                    int spriteY = spriteInformation[spriteAddress];
                    int spriteX = spriteInformation[spriteAddress + 1];
                    ushort spritePatternAddress = (ushort)(spriteInformation[spriteAddress + 2]*0x10);
                    bool priority = spriteInformation[spriteAddress + 3].GetBit(7);
                    bool yFlip = spriteInformation[spriteAddress + 3].GetBit(6);
                    bool xFlip = spriteInformation[spriteAddress + 3].GetBit(5);
                    bool spritePalette = spriteInformation[spriteAddress + 3].GetBit(4);

                    int spriteLine = currentScanLine - (spriteY - 16);
                    if (yFlip) { spriteLine = 8 - spriteLine; }
                    ushort spriteLineAddress = (ushort)(spritePatternAddress + (spriteLine * 2));
                    byte lowByte = graphicsMemory[spriteLineAddress];
                    byte highByte = graphicsMemory[spriteLineAddress + 1];

                    int startOfLineOnCanvas = currentScanLine * 160 * 4;
                    int pixelX = spriteX - 8;
                    for (int x = 0; x < 8; x++)
                    {
                        byte pixelIndex = (byte)(1 << (7 - x));
                        if (xFlip) { pixelIndex = (byte)(1 << x); }

                        //Get the pixel value
                        byte pixelValue = (byte)((((lowByte & pixelIndex) > 0) ? 1 : 0) + (((highByte & pixelIndex) > 0) ? 2 : 0));
                        byte palette = spritePalette ? spritePalette1 : spritePalette0;
                        byte colorValue = GetColor(pixelValue, palette);

                        if (pixelX >= 0 && pixelX < 160 && pixelValue != 0)
                        {
                            int canvasOffset = startOfLineOnCanvas + (pixelX * 4);
                            screenData[canvasOffset + 0] = colorValue; //R
                            screenData[canvasOffset + 1] = colorValue; //G
                            screenData[canvasOffset + 2] = colorValue; //B
                            screenData[canvasOffset + 3] = 255;        //A
                        }
                        pixelX++;
                    }
                }

                //throw new NotImplementedException();
            }
        }

        /// <summary>
        /// Given a 2-bit index into our palette, return the value to be used for R,G,B
        /// </summary>
        /// <param name="pixelValue"></param>
        /// <returns></returns>
        private byte GetColor(byte pixelValue, byte palette)
        {
            if (pixelValue > 3) { throw new ArgumentOutOfRangeException(); }

            byte paletteIndex = (byte)(pixelValue * 2);
            byte colorIndex = 0;
            if (palette.GetBit(paletteIndex)) { colorIndex = colorIndex.SetBit(0); }
            if (palette.GetBit((byte)(paletteIndex+1))) { colorIndex = colorIndex.SetBit(1); }

            return PIXEL_VALUES[colorIndex];
        }

    }
}
