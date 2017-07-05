using System;

namespace Cavernlore.GameBrick
{
    public class Timer
    {
        int dividerTimer; //Value exposed to system is this value / 4
        int counterTimerBase;
        int counterTimer;
        byte counterModulo;

        byte controlByte;
        int[] counterSteps = { 4, 16, 64, 256 };
        int counterStep;
        bool counterActive;

        public Timer()
        {
            Reset();
        }

        public void Reset()
        {
            dividerTimer = 0;
            counterTimerBase = 0;
            counterTimer = 0;
            counterModulo = 0;
            controlByte = 0;
            counterStep = 0;
            counterActive = false;
        }

        //Returns whether an interrupt is raised by counterTimer
        public bool IncrementTimers(int machineClock)
        {
            dividerTimer = (dividerTimer + machineClock) % (256*64);

            if (counterActive)
            {
                counterTimerBase += (machineClock);
                while (counterTimerBase >= counterSteps[counterStep])
                {
                    counterTimerBase -= counterSteps[counterStep];
                    counterTimer++;
                }
                if (counterTimer > 255)
                {
                    counterTimer -= 256;
                    counterTimer += counterModulo;
                    return true;
                }
            }
            return false;
        }

        public byte Divider { get { return (byte)(dividerTimer / 64); } set { dividerTimer = 0; } }
        public byte Counter { get { return (byte)counterTimer; } set { counterTimer = value; counterTimerBase = 0; } }
        public byte Modulo { get { return counterModulo; } set { counterModulo = value; } }
        public byte Control
        {
            get
            {
                return controlByte;
            }
            set
            {
                controlByte = value;
                counterActive = controlByte.GetBit(2);
                switch(controlByte & 0x3)
                {
                    case 0: counterStep = 3; break;
                    case 1: counterStep = 0; break;
                    case 2: counterStep = 2; break;
                    case 3: counterStep = 1; break;
                    default: throw new ArgumentOutOfRangeException();
                }
            }
        }
    }
}
