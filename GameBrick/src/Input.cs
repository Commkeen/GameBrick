
namespace Cavernlore.GameBrick
{
    // A bit set to 0 means a button is pressed/a column is active
    public class Input
    {
        public byte[] keys = { 0x0F, 0x0F }; //Lower 4 bits, swaps out based on which input column is activated
        public byte activatedColumn = 0x00;  //Upper 2 bits, this should be the only thing that gets written to by the gb

        private MemoryManager _mmu;

        public Input()
        {

        }

        public void SetMMU(MemoryManager mmu)
        {
            _mmu = mmu;
        }

        public void Reset()
        {
            keys = new byte[] { 0x0F, 0x0F };
            activatedColumn = 0x00;
        }

        public byte ReadByte()
        {
            if (!activatedColumn.GetBit(4))
            {
                return (byte)(keys[0] | activatedColumn);
            }
            if (!activatedColumn.GetBit(5))
            {
                return (byte)(keys[1] | activatedColumn);
            }
            return activatedColumn;
        }

        public void WriteByte(byte value)
        {
            activatedColumn = value;
        }

        public void KeyDown(Keys key)
        {
            switch (key)
            {
                case Keys.A:      keys[1] = keys[1].ResetBit(0); break;
                case Keys.B:      keys[1] = keys[1].ResetBit(1); break;
                case Keys.SELECT: keys[1] = keys[1].ResetBit(2); break;
                case Keys.START:  keys[1] = keys[1].ResetBit(3); break;
                case Keys.RIGHT:  keys[0] = keys[0].ResetBit(0); break;
                case Keys.LEFT:   keys[0] = keys[0].ResetBit(1); break;
                case Keys.UP:     keys[0] = keys[0].ResetBit(2); break;
                case Keys.DOWN:   keys[0] = keys[0].ResetBit(3); break;
            }
            _mmu.interruptFlags.SetBit(4);
        }

        public void KeyUp(Keys key)
        {
            switch (key)
            {
                case Keys.A: keys[1] =      keys[1].SetBit(0); break;
                case Keys.B: keys[1] =      keys[1].SetBit(1); break;
                case Keys.SELECT: keys[1] = keys[1].SetBit(2); break;
                case Keys.START: keys[1] =  keys[1].SetBit(3); break;
                case Keys.RIGHT: keys[0] =  keys[0].SetBit(0); break;
                case Keys.LEFT: keys[0] =   keys[0].SetBit(1); break;
                case Keys.UP: keys[0] =     keys[0].SetBit(2); break;
                case Keys.DOWN: keys[0] =   keys[0].SetBit(3); break;
            }
        }

        public void SetKey(Keys key, bool isDown)
        {
            if (isDown) { KeyDown(key); } else { KeyUp(key); }
        }

    }
}
