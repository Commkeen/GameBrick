using System.Threading;
using System.Threading.Tasks;
using SdlDotNet.Core;
using SdlDotNet.Graphics;
using SdlDotNet.Input;
using System.Runtime.InteropServices;
using SdlDotNet.Graphics.Sprites;
using System;

namespace Cavernlore.GameBrick
{
    class RenderWindow
    {
        Cpu _cpu;
        GPU _gpu;
        Input _input;
        Surface _window;
        Surface _gbRender;
        TextSprite _statusText;
        TextSprite _tilemapText;
        TextSprite _inputText;
        TextSprite _registerText;
        System.Drawing.Point _tilemapDrawPoint;
        Font _textFont;
        System.Drawing.Point _textDrawPoint;


        public RenderWindow()
        {
            _textFont = new Font(@"..\assets\visitor1.ttf", 20);
            _tilemapDrawPoint = new System.Drawing.Point(0, 0);
            _tilemapText = new TextSprite(_textFont);
            _inputText = new TextSprite(_textFont);
            _registerText = new TextSprite(_textFont);
        }

        public void SetCPU(Cpu cpu)
        {
            _cpu = cpu;
        }

        public void SetGPU(GPU gpu)
        {
            _gpu = gpu;
        }

        public void SetInput(Input input)
        {
            _input = input;
        }

        public void InitWindow()
        {
            _window = Video.SetVideoMode(800, 600, 32);
            Video.WindowCaption = "Game Boy";
            _gbRender = new Surface(160, 144, 32);
            _statusText = new TextSprite("ready", _textFont);
            Events.TargetFps = 50;
            Events.Tick += (TickEventHandler);
        }

        [STAThread]
        public void Run()
        {
            Events.Run();
        }

        public void TickEventHandler(object sender, TickEventArgs args)
        {
            UpdateInput();

            _window.Fill(System.Drawing.Color.Black);
            _gbRender.Update();
            _window.Blit(_gbRender);
            //RenderTilemapIndices();
            RenderInputs();
            RenderRegisters();
            _window.Blit(_statusText, new System.Drawing.Point(200, 10));
            _window.Update();
        }

        public void RenderFrameFromGPU()
        {
            Marshal.Copy(_gpu.screenData, 0, _gbRender.Pixels, 160 * 144 * 4);
        }

        public void RenderTilemapIndices()
        {
            _tilemapDrawPoint.X = 5;
            _tilemapDrawPoint.Y = 150;

            for (int i = 0; i < 32; i++)
            {
                for (int k = 0; k < 32; k++)
                {
                    int tilemapIndex = _gpu.graphicsMemory[0x1800 + k + i * 32];
                    if (tilemapIndex > 0)
                    {
                        //n
                    }
                    _tilemapText.Text = tilemapIndex.ToString();
                    _window.Blit(_tilemapText, _tilemapDrawPoint);
                    _tilemapDrawPoint.X += 20;
                }
                _tilemapDrawPoint.X = 5;
                _tilemapDrawPoint.Y += 20;
            }
        }

        public void RenderInputs()
        {
            var xOrigin = 150;
            var yOrigin = 150;

            string[] inputTexts = {"P14", "P15", "Right", "A", "Left", "B", "Up", "Select", "Down", "Start" };
            bool[] inputsOff = { _input.activatedColumn.GetBit(4), _input.activatedColumn.GetBit(5),
                                _input.keys[0].GetBit(0), _input.keys[1].GetBit(0),
                                _input.keys[0].GetBit(1), _input.keys[1].GetBit(1),
                                _input.keys[0].GetBit(2), _input.keys[1].GetBit(2),
                                _input.keys[0].GetBit(3), _input.keys[1].GetBit(3)
                              };
            for (int i = 0; i < inputTexts.Length; i++)
            {
                _textDrawPoint.X = xOrigin + (i % 2) * 60;
                _textDrawPoint.Y = yOrigin + (i / 2) * 25;
                _inputText.Text = inputTexts[i];
                _inputText.Color = inputsOff[i] ? System.Drawing.Color.Gray : System.Drawing.Color.Cyan;
                _window.Blit(_inputText, _textDrawPoint);
            }
        }

        public void RenderRegisters()
        {
            var xOrigin = 300;
            var yOrigin = 100;

            string[] registerNames = { "A", "F", "B", "C", "D", "E", "H", "L" };
            byte[] registerValues = { _cpu.registerA, _cpu.registerF, _cpu.registerB, _cpu.registerC, _cpu.registerD, _cpu.registerE, _cpu.registerH, _cpu.registerL };

            for (int i = 0; i < registerNames.Length; i++)
            {
                _textDrawPoint.X = xOrigin + (i % 2) * 90;
                _textDrawPoint.Y = yOrigin + (i / 2) * 25;
                _registerText.Text = registerNames[i] + ": " + registerValues[i].ToString("x2");
                _window.Blit(_registerText, _textDrawPoint);
            }

            _textDrawPoint.X = 300;
            _textDrawPoint.Y = 300;
            _registerText.Text = "SP: " + _cpu.stackPointer.ToString("x4");
            _window.Blit(_registerText, _textDrawPoint);
        }

        private void UpdateInput()
        {
            _input.SetKey(Keys.LEFT, Keyboard.IsKeyPressed(Key.LeftArrow));
            _input.SetKey(Keys.RIGHT, Keyboard.IsKeyPressed(Key.RightArrow));
            _input.SetKey(Keys.UP, Keyboard.IsKeyPressed(Key.UpArrow));
            _input.SetKey(Keys.DOWN, Keyboard.IsKeyPressed(Key.DownArrow));
            _input.SetKey(Keys.A, Keyboard.IsKeyPressed(Key.A));
            _input.SetKey(Keys.B, Keyboard.IsKeyPressed(Key.B));
            _input.SetKey(Keys.START, Keyboard.IsKeyPressed(Key.Return));
            _input.SetKey(Keys.SELECT, Keyboard.IsKeyPressed(Key.RightShift));
        }
    }
}
