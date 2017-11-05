
namespace Cavernlore.GameBrick
{
    public interface IVisualizer
    {
        void ReadByte(ushort address);
        void WriteByte(ushort address, byte value);
        void SetROMBank(int newBank);
        void DMA(byte target);
    }
}
