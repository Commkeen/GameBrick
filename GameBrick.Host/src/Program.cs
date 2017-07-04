﻿using System;
using System.Threading;
using System.Threading.Tasks;

namespace Cavernlore.GameBrick
{
    class Program
    {
        public static void Main()
        {
            MemoryManager mem = new MemoryManager();
            Cpu z80 = new Cpu();
            GPU gpu = new GPU();
            Input input = new Input();

            z80.SetMemoryManager(mem);
            mem.SetGPU(gpu);
            mem.SetInput(input);
            gpu.SetMMU(mem);
            input.SetMMU(mem);

            mem.LoadCartridge(@"..\assets\individual\07-jr,jp,call,ret,rst.gb");

            RenderWindow window = new RenderWindow();
            window.SetCPU(z80);
            window.SetGPU(gpu);
            window.SetInput(input);
            window.InitWindow();

            Task.Run(() => RunEmulator(mem, z80, gpu, input, window));

            window.Run();
        }

        public static void RunEmulator(MemoryManager mem, Cpu z80, GPU gpu, Input input, RenderWindow window)
        {
            int frameClock = 0;

            float frameRateCap = 30.0f;
            float millisecondsPerFrame = (1.0f / frameRateCap) * 1000;

            while (true)
            {
                DateTime startTime = DateTime.Now;
                while (frameClock < 70224)
                {
                    int programCounter = z80.programCounter;
                    int instruction = mem.ReadByte(z80.programCounter);
                    z80.Execute();
                    z80.CheckInterrupts();
                    gpu.Step(z80.lastCycleClock);
                    frameClock += z80.lastCycleClock;
                }
                frameClock -= 70224;
                DateTime endTime = DateTime.Now;
                float timeSpan = (float)(endTime - startTime).TotalMilliseconds;
                if (timeSpan < millisecondsPerFrame)
                {
                    Thread.Sleep((int)(millisecondsPerFrame - timeSpan));
                }
                window.RenderFrameFromGPU();
            }
        }
       
    }
}
