using System;

namespace Cavernlore.GameBrick
{
    public class Cpu
    {
        private MemoryManager _mmu;
        private Timer _timer;

        public int machineClock;

        public int lastMachineClock;
        public int lastCycleClock;

        //8 bit registers
        public byte registerA; //Accumulator
        public byte registerF; //Flag register
        public byte registerB; //Commonly used as 8-bit counter
        public byte registerC; //Used for interfacing with hardware?
        public byte registerD; //Usually used with E
        public byte registerE;
        public byte registerH; //Usually used with L, often for addressing memory
        public byte registerL;

        //16 bit registers
        public ushort programCounter;
        public ushort stackPointer;

        public bool interruptMasterFlag;

        public bool halt;

        public bool showDebug = false;

        private delegate void Instruction();

        private Instruction[] _instructionMap;
        private Instruction[] _cbInstructionMap;

        public Cpu()
        {
            ConstructInstructionMap();
            ResetState();
            programCounter = 0x0100;
            //showDebug = true;
        }

        public void SetMemoryManager(MemoryManager memManager)
        {
            _mmu = memManager;
        }

        public void SetTimer(Timer timer)
        {
            _timer = timer;
        }

        public void Execute()
        {
            if (programCounter == 0x0100)
                _mmu.inBios = false;

            if (showDebug && !halt)
            {
                WriteDebugLine();
            }


            if (halt)
            {
                lastMachineClock = 1;
            }
            else
            {
                byte instruction = _mmu.ReadByte(programCounter++);
                Instruction func = _instructionMap[instruction];
                func();
                if (programCounter == 0x00FC)
                {
                    //Console.WriteLine("here");
                }
            }
            lastCycleClock = lastMachineClock * 4;
            IncrementTimers(lastMachineClock);
        }

        private void IncrementTimers(int lastMachineClock)
        {
            if (_timer.IncrementTimers(lastMachineClock))
            {
                _mmu.interruptFlags = _mmu.interruptFlags.SetBit(2);
            }
        }

        private void WriteDebugLine()
        {
            byte thisByte = _mmu.ReadByte(programCounter);
            string thisFunc = _instructionMap[thisByte].Method.Name;
            byte nextByte = _mmu.ReadByte((ushort)(programCounter + 1));
            Console.Write(programCounter.ToString("x4") + " -- ");
            Console.Write(thisByte.ToString("x2") + " " + thisFunc + " " + nextByte.ToString("x2"));
            Console.Write("  A={0} F={3} BC={4} DE={5} HL={1} SP={2} T={6}",
                registerA.ToString("x2"), registerHL.ToString("x4"), stackPointer.ToString("x4"), registerF.ToString("x2"),
                registerBC.ToString("x4"), registerDE.ToString("x4"), _timer.Counter.ToString("x2"));
            Console.WriteLine();
        }

        public void CheckInterrupts()
        {
            
            byte interruptFired = (byte)(_mmu.interruptEnable & _mmu.interruptFlags);
            if (interruptFired == 0) { return; }

            halt = false;
            if (!interruptMasterFlag)
                return;
            interruptMasterFlag = false;
            if ((interruptFired & 1) > 0) { _mmu.interruptFlags &= 0xFE; RST40(); } //vblank
            else if ((interruptFired & 2) > 0) { _mmu.interruptFlags &= 0xFD; RST48(); } //lcdc
            else if ((interruptFired & 4) > 0) { _mmu.interruptFlags &= 0xFB; RST50(); } //timer
            else if ((interruptFired & 8) > 0) { _mmu.interruptFlags &= 0xF7; RST58(); } //serial
            else if ((interruptFired & 16) > 0) { _mmu.interruptFlags &= 0xEF; RST60(); } //input
        }

        private void ResetState()
        {
            machineClock = 0;
            lastMachineClock = 0;
            lastCycleClock = 0;

            registerA = 0;
            registerB = 0;
            registerC = 0;
            registerD = 0;
            registerE = 0;
            registerF = 0;
            registerH = 0;
            registerL = 0;

            programCounter = 0;
            stackPointer = 0;

            interruptMasterFlag = true;

            halt = false;
        }

        private void ConstructInstructionMap()
        {
            _instructionMap = new Instruction[]
            {
                // 0x00
                new Instruction(NOP),
                new Instruction(LDBCnn),
                new Instruction(LDBCmA),
                new Instruction(INCBC),
                new Instruction(INCr_b),
                new Instruction(DECr_b),
                new Instruction(LDrn_b),
                new Instruction(RLCA),
                new Instruction(LDmmSP),
                new Instruction(ADDHLBC),
                new Instruction(LDABCm),
                new Instruction(DECBC),
                new Instruction(INCr_c),
                new Instruction(DECr_c),
                new Instruction(LDrn_c),
                new Instruction(RRCA),
                
                //0x10
                new Instruction(DJNZn),
                new Instruction(LDDEnn),
                new Instruction(LDDEmA),
                new Instruction(INCDE),
                new Instruction(INCr_d),
                new Instruction(DECr_d),
                new Instruction(LDrn_d),
                new Instruction(RLA),
                new Instruction(JRn),
                new Instruction(ADDHLDE),
                new Instruction(LDADEm),
                new Instruction(DECDE),
                new Instruction(INCr_e),
                new Instruction(DECr_e),
                new Instruction(LDrn_e),
                new Instruction(RRA),
                
                //0x20
                new Instruction(JRNZn),
                new Instruction(LDHLnn),
                new Instruction(LDHLIA),
                new Instruction(INCHL),
                new Instruction(INCr_h),
                new Instruction(DECr_h),
                new Instruction(LDrn_h),
                new Instruction(DAA),
                new Instruction(JRZn),
                new Instruction(ADDHLHL),
                new Instruction(LDAHLI),
                new Instruction(DECHL),
                new Instruction(INCr_l),
                new Instruction(DECr_l),
                new Instruction(LDrn_l),
                new Instruction(CPL),
                

                //0x30
                new Instruction(JRNCn),
                new Instruction(LDSPnn),
                new Instruction(LDHLDA),
                new Instruction(INCSP),
                new Instruction(INCHLm),
                new Instruction(DECHLm),
                new Instruction(LDHLmn),
                new Instruction(SCF),
                new Instruction(JRCn),
                new Instruction(ADDHLSP),
                new Instruction(LDAHLD),
                new Instruction(DECSP),
                new Instruction(INCr_a),
                new Instruction(DECr_a),
                new Instruction(LDrn_a),
                new Instruction(CCF),
                
                //0x40
                new Instruction(LDrr_bb),
                new Instruction(LDrr_bc),
                new Instruction(LDrr_bd),
                new Instruction(LDrr_be),
                new Instruction(LDrr_bh),
                new Instruction(LDrr_bl),
                new Instruction(LDrHLm_b),
                new Instruction(LDrr_ba),
                new Instruction(LDrr_cb),
                new Instruction(LDrr_cc),
                new Instruction(LDrr_cd),
                new Instruction(LDrr_ce),
                new Instruction(LDrr_ch),
                new Instruction(LDrr_cl),
                new Instruction(LDrHLm_c),
                new Instruction(LDrr_ca),
                

                //0x50
                new Instruction(LDrr_db),
                new Instruction(LDrr_dc),
                new Instruction(LDrr_dd),
                new Instruction(LDrr_de),
                new Instruction(LDrr_dh),
                new Instruction(LDrr_dl),
                new Instruction(LDrHLm_d),
                new Instruction(LDrr_da),
                new Instruction(LDrr_eb),
                new Instruction(LDrr_ec),
                new Instruction(LDrr_ed),
                new Instruction(LDrr_ee),
                new Instruction(LDrr_eh),
                new Instruction(LDrr_el),
                new Instruction(LDrHLm_e),
                new Instruction(LDrr_ea),
                

                //0x60
                new Instruction(LDrr_hb),
                new Instruction(LDrr_hc),
                new Instruction(LDrr_hd),
                new Instruction(LDrr_he),
                new Instruction(LDrr_hh),
                new Instruction(LDrr_hl),
                new Instruction(LDrHLm_h),
                new Instruction(LDrr_ha),
                new Instruction(LDrr_lb),
                new Instruction(LDrr_lc),
                new Instruction(LDrr_ld),
                new Instruction(LDrr_le),
                new Instruction(LDrr_lh),
                new Instruction(LDrr_ll),
                new Instruction(LDrHLm_l),
                new Instruction(LDrr_la),
                
                //0x70
                new Instruction(LDHLmr_b),
                new Instruction(LDHLmr_c),
                new Instruction(LDHLmr_d),
                new Instruction(LDHLmr_e),
                new Instruction(LDHLmr_h),
                new Instruction(LDHLmr_l),
                new Instruction(HALT),
                new Instruction(LDHLmr_a),
                new Instruction(LDrr_ab),
                new Instruction(LDrr_ac),
                new Instruction(LDrr_ad),
                new Instruction(LDrr_ae),
                new Instruction(LDrr_ah),
                new Instruction(LDrr_al),
                new Instruction(LDrHLm_a),
                new Instruction(LDrr_aa),
                
                //0x80
                new Instruction(ADDr_b),
                new Instruction(ADDr_c),
                new Instruction(ADDr_d),
                new Instruction(ADDr_e),
                new Instruction(ADDr_h),
                new Instruction(ADDr_l),
                new Instruction(ADDHL),
                new Instruction(ADDr_a),
                new Instruction(ADCr_b),
                new Instruction(ADCr_c),
                new Instruction(ADCr_d),
                new Instruction(ADCr_e),
                new Instruction(ADCr_h),
                new Instruction(ADCr_l),
                new Instruction(ADCHL),
                new Instruction(ADCr_a),
                
                //0x90
                new Instruction(SUBr_b),
                new Instruction(SUBr_c),
                new Instruction(SUBr_d),
                new Instruction(SUBr_e),
                new Instruction(SUBr_h),
                new Instruction(SUBr_l),
                new Instruction(SUBHL),
                new Instruction(SUBr_a),
                new Instruction(SBCr_b),
                new Instruction(SBCr_c),
                new Instruction(SBCr_d),
                new Instruction(SBCr_e),
                new Instruction(SBCr_h),
                new Instruction(SBCr_l),
                new Instruction(SBCHL),
                new Instruction(SBCr_a),
                
                //0xA0
                new Instruction(ANDr_b),
                new Instruction(ANDr_c),
                new Instruction(ANDr_d),
                new Instruction(ANDr_e),
                new Instruction(ANDr_h),
                new Instruction(ANDr_l),
                new Instruction(ANDHL),
                new Instruction(ANDr_a),
                new Instruction(XORr_b),
                new Instruction(XORr_c),
                new Instruction(XORr_d),
                new Instruction(XORr_e),
                new Instruction(XORr_h),
                new Instruction(XORr_l),
                new Instruction(XORHL),
                new Instruction(XORr_a),
                
                //0xB0
                new Instruction(ORr_b),
                new Instruction(ORr_c),
                new Instruction(ORr_d),
                new Instruction(ORr_e),
                new Instruction(ORr_h),
                new Instruction(ORr_l),
                new Instruction(ORHL),
                new Instruction(ORr_a),
                new Instruction(CPr_b),
                new Instruction(CPr_c),
                new Instruction(CPr_d),
                new Instruction(CPr_e),
                new Instruction(CPr_h),
                new Instruction(CPr_l),
                new Instruction(CPHL),
                new Instruction(CPr_a),
                
                //0xC0
                new Instruction(RETNZ),
                new Instruction(POPBC),
                new Instruction(JPNZnn),
                new Instruction(JPnn),
                new Instruction(CALLNZnn),
                new Instruction(PUSHBC),
                new Instruction(ADDn),
                new Instruction(RST00),
                new Instruction(RETZ),
                new Instruction(RET),
                new Instruction(JPZnn),
                new Instruction(MAPcb),
                new Instruction(CALLZnn),
                new Instruction(CALLnn),
                new Instruction(ADCn),
                new Instruction(RST08),
                
                //0xD0
                new Instruction(RETNC),
                new Instruction(POPDE),
                new Instruction(JPNCnn),
                new Instruction(XX),
                new Instruction(CALLNCnn),
                new Instruction(PUSHDE),
                new Instruction(SUBn),
                new Instruction(RST10),
                new Instruction(RETC),
                new Instruction(RETI),
                new Instruction(JPCnn),
                new Instruction(XX),
                new Instruction(CALLCnn),
                new Instruction(XX),
                new Instruction(SBCn),
                new Instruction(RST18),
                
                //0xE0
                new Instruction(LDIOnA),
                new Instruction(POPHL),
                new Instruction(LDIOCA),
                new Instruction(XX),
                new Instruction(XX),
                new Instruction(PUSHHL),
                new Instruction(ANDn),
                new Instruction(RST20),
                new Instruction(ADDSPn),
                new Instruction(JPHL),
                new Instruction(LDmmA),
                new Instruction(XX),
                new Instruction(XX),
                new Instruction(XX),
                new Instruction(XORn),
                new Instruction(RST28),
                
                //0xF0
                new Instruction(LDAIOn),
                new Instruction(POPAF),
                new Instruction(LDAIOC),
                new Instruction(DI),
                new Instruction(XX),
                new Instruction(PUSHAF),
                new Instruction(ORn),
                new Instruction(RST30),
                new Instruction(LDHLSPn),
                new Instruction(LDSPHL),
                new Instruction(LDAmm),
                new Instruction(EI),
                new Instruction(XX),
                new Instruction(XX),
                new Instruction(CPn),
                new Instruction(RST38)
            };

            _cbInstructionMap = new Instruction[]
            {
                // CB00
                new Instruction(RLCr_b),
                new Instruction(RLCr_c),
                new Instruction(RLCr_d),
                new Instruction(RLCr_e),
                new Instruction(RLCr_h),
                new Instruction(RLCr_l),
                new Instruction(RLCHL),
                new Instruction(RLCr_a),
                new Instruction(RRCr_b),
                new Instruction(RRCr_c),
                new Instruction(RRCr_d),
                new Instruction(RRCr_e),
                new Instruction(RRCr_h),
                new Instruction(RRCr_l),
                new Instruction(RRCHL),
                new Instruction(RRCr_a),

  // CB10
                new Instruction(RLr_b),
                new Instruction(RLr_c),
                new Instruction(RLr_d),
                new Instruction(RLr_e),
                new Instruction(RLr_h),
                new Instruction(RLr_l),
                new Instruction(RLHL),
                new Instruction(RLr_a),
                new Instruction(RRr_b),
                new Instruction(RRr_c),
                new Instruction(RRr_d),
                new Instruction(RRr_e),
                new Instruction(RRr_h),
                new Instruction(RRr_l),
                new Instruction(RRHL),
                new Instruction(RRr_a),

  // CB20
                new Instruction(SLAr_b),
                new Instruction(SLAr_c),
                new Instruction(SLAr_d),
                new Instruction(SLAr_e),
                new Instruction(SLAr_h),
                new Instruction(SLAr_l),
                new Instruction(SLAHL),
                new Instruction(SLAr_a),
                new Instruction(SRAr_b),
                new Instruction(SRAr_c),
                new Instruction(SRAr_d),
                new Instruction(SRAr_e),
                new Instruction(SRAr_h),
                new Instruction(SRAr_l),
                new Instruction(SRAHL),
                new Instruction(SRAr_a),

  // CB30
                new Instruction(SWAPr_b),
                new Instruction(SWAPr_c),
                new Instruction(SWAPr_d),
                new Instruction(SWAPr_e),
                new Instruction(SWAPr_h),
                new Instruction(SWAPr_l),
                new Instruction(SWAPHL),
                new Instruction(SWAPr_a),
                new Instruction(SRLr_b),
                new Instruction(SRLr_c),
                new Instruction(SRLr_d),
                new Instruction(SRLr_e),
                new Instruction(SRLr_h),
                new Instruction(SRLr_l),
                new Instruction(SRLHL),
                new Instruction(SRLr_a),

  // CB40
                new Instruction(BIT0b),
                new Instruction(BIT0c),
                new Instruction(BIT0d),
                new Instruction(BIT0e),
                new Instruction(BIT0h),
                new Instruction(BIT0l),
                new Instruction(BIT0m),
                new Instruction(BIT0a),
                new Instruction(BIT1b),
                new Instruction(BIT1c),
                new Instruction(BIT1d),
                new Instruction(BIT1e),
                new Instruction(BIT1h),
                new Instruction(BIT1l),
                new Instruction(BIT1m),
                new Instruction(BIT1a),

  // CB50
                new Instruction(BIT2b),
                new Instruction(BIT2c),
                new Instruction(BIT2d),
                new Instruction(BIT2e),
                new Instruction(BIT2h),
                new Instruction(BIT2l),
                new Instruction(BIT2m),
                new Instruction(BIT2a),
                new Instruction(BIT3b),
                new Instruction(BIT3c),
                new Instruction(BIT3d),
                new Instruction(BIT3e),
                new Instruction(BIT3h),
                new Instruction(BIT3l),
                new Instruction(BIT3m),
                new Instruction(BIT3a),

  // CB60
                new Instruction(BIT4b),
                new Instruction(BIT4c),
                new Instruction(BIT4d),
                new Instruction(BIT4e),
                new Instruction(BIT4h),
                new Instruction(BIT4l),
                new Instruction(BIT4m),
                new Instruction(BIT4a),
                new Instruction(BIT5b),
                new Instruction(BIT5c),
                new Instruction(BIT5d),
                new Instruction(BIT5e),
                new Instruction(BIT5h),
                new Instruction(BIT5l),
                new Instruction(BIT5m),
                new Instruction(BIT5a),

  // CB70
                new Instruction(BIT6b),
                new Instruction(BIT6c),
                new Instruction(BIT6d),
                new Instruction(BIT6e),
                new Instruction(BIT6h),
                new Instruction(BIT6l),
                new Instruction(BIT6m),
                new Instruction(BIT6a),
                new Instruction(BIT7b),
                new Instruction(BIT7c),
                new Instruction(BIT7d),
                new Instruction(BIT7e),
                new Instruction(BIT7h),
                new Instruction(BIT7l),
                new Instruction(BIT7m),
                new Instruction(BIT7a),

  // CB80
                new Instruction(()=> { RESr(0, ref registerB); }),
                new Instruction(()=> { RESr(0, ref registerC); }),
                new Instruction(()=> { RESr(0, ref registerD); }),
                new Instruction(()=> { RESr(0, ref registerE); }),
                new Instruction(()=> { RESr(0, ref registerH); }),
                new Instruction(()=> { RESr(0, ref registerL); }),
                new Instruction(()=> { RESm(0); }),
                new Instruction(()=> { RESr(0, ref registerA); }),
                new Instruction(()=> { RESr(1, ref registerB); }),
                new Instruction(()=> { RESr(1, ref registerC); }),
                new Instruction(()=> { RESr(1, ref registerD); }),
                new Instruction(()=> { RESr(1, ref registerE); }),
                new Instruction(()=> { RESr(1, ref registerH); }),
                new Instruction(()=> { RESr(1, ref registerL); }),
                new Instruction(()=> { RESm(1); }),
                new Instruction(()=> { RESr(1, ref registerA); }),

  // CB90
                new Instruction(()=> { RESr(2, ref registerB); }),
                new Instruction(()=> { RESr(2, ref registerC); }),
                new Instruction(()=> { RESr(2, ref registerD); }),
                new Instruction(()=> { RESr(2, ref registerE); }),
                new Instruction(()=> { RESr(2, ref registerH); }),
                new Instruction(()=> { RESr(2, ref registerL); }),
                new Instruction(()=> { RESm(2); }),
                new Instruction(()=> { RESr(2, ref registerA); }),
                new Instruction(()=> { RESr(3, ref registerB); }),
                new Instruction(()=> { RESr(3, ref registerC); }),
                new Instruction(()=> { RESr(3, ref registerD); }),
                new Instruction(()=> { RESr(3, ref registerE); }),
                new Instruction(()=> { RESr(3, ref registerH); }),
                new Instruction(()=> { RESr(3, ref registerL); }),
                new Instruction(()=> { RESm(3); }),
                new Instruction(()=> { RESr(3, ref registerA); }),

  // CBA0
                new Instruction(()=> { RESr(4, ref registerB); }),
                new Instruction(()=> { RESr(4, ref registerC); }),
                new Instruction(()=> { RESr(4, ref registerD); }),
                new Instruction(()=> { RESr(4, ref registerE); }),
                new Instruction(()=> { RESr(4, ref registerH); }),
                new Instruction(()=> { RESr(4, ref registerL); }),
                new Instruction(()=> { RESm(4); }),
                new Instruction(()=> { RESr(4, ref registerA); }),
                new Instruction(()=> { RESr(5, ref registerB); }),
                new Instruction(()=> { RESr(5, ref registerC); }),
                new Instruction(()=> { RESr(5, ref registerD); }),
                new Instruction(()=> { RESr(5, ref registerE); }),
                new Instruction(()=> { RESr(5, ref registerH); }),
                new Instruction(()=> { RESr(5, ref registerL); }),
                new Instruction(()=> { RESm(5); }),
                new Instruction(()=> { RESr(5, ref registerA); }),

  // CBB0
                new Instruction(()=> { RESr(6, ref registerB); }),
                new Instruction(()=> { RESr(6, ref registerC); }),
                new Instruction(()=> { RESr(6, ref registerD); }),
                new Instruction(()=> { RESr(6, ref registerE); }),
                new Instruction(()=> { RESr(6, ref registerH); }),
                new Instruction(()=> { RESr(6, ref registerL); }),
                new Instruction(()=> { RESm(6); }),
                new Instruction(()=> { RESr(6, ref registerA); }),
                new Instruction(()=> { RESr(7, ref registerB); }),
                new Instruction(()=> { RESr(7, ref registerC); }),
                new Instruction(()=> { RESr(7, ref registerD); }),
                new Instruction(()=> { RESr(7, ref registerE); }),
                new Instruction(()=> { RESr(7, ref registerH); }),
                new Instruction(()=> { RESr(7, ref registerL); }),
                new Instruction(()=> { RESm(7); }),
                new Instruction(()=> { RESr(7, ref registerA); }),

 // CBC0
                new Instruction(()=> { SETr(0, ref registerB); }),
                new Instruction(()=> { SETr(0, ref registerC); }),
                new Instruction(()=> { SETr(0, ref registerD); }),
                new Instruction(()=> { SETr(0, ref registerE); }),
                new Instruction(()=> { SETr(0, ref registerH); }),
                new Instruction(()=> { SETr(0, ref registerL); }),
                new Instruction(()=> { SETm(0); }),
                new Instruction(()=> { SETr(0, ref registerA); }),
                new Instruction(()=> { SETr(1, ref registerB); }),
                new Instruction(()=> { SETr(1, ref registerC); }),
                new Instruction(()=> { SETr(1, ref registerD); }),
                new Instruction(()=> { SETr(1, ref registerE); }),
                new Instruction(()=> { SETr(1, ref registerH); }),
                new Instruction(()=> { SETr(1, ref registerL); }),
                new Instruction(()=> { SETm(1); }),
                new Instruction(()=> { SETr(1, ref registerA); }),

  // CBD0
                new Instruction(()=> { SETr(2, ref registerB); }),
                new Instruction(()=> { SETr(2, ref registerC); }),
                new Instruction(()=> { SETr(2, ref registerD); }),
                new Instruction(()=> { SETr(2, ref registerE); }),
                new Instruction(()=> { SETr(2, ref registerH); }),
                new Instruction(()=> { SETr(2, ref registerL); }),
                new Instruction(()=> { SETm(2); }),
                new Instruction(()=> { SETr(2, ref registerA); }),
                new Instruction(()=> { SETr(3, ref registerB); }),
                new Instruction(()=> { SETr(3, ref registerC); }),
                new Instruction(()=> { SETr(3, ref registerD); }),
                new Instruction(()=> { SETr(3, ref registerE); }),
                new Instruction(()=> { SETr(3, ref registerH); }),
                new Instruction(()=> { SETr(3, ref registerL); }),
                new Instruction(()=> { SETm(3); }),
                new Instruction(()=> { SETr(3, ref registerA); }),

  // CBE0
                new Instruction(()=> { SETr(4, ref registerB); }),
                new Instruction(()=> { SETr(4, ref registerC); }),
                new Instruction(()=> { SETr(4, ref registerD); }),
                new Instruction(()=> { SETr(4, ref registerE); }),
                new Instruction(()=> { SETr(4, ref registerH); }),
                new Instruction(()=> { SETr(4, ref registerL); }),
                new Instruction(()=> { SETm(4); }),
                new Instruction(()=> { SETr(4, ref registerA); }),
                new Instruction(()=> { SETr(5, ref registerB); }),
                new Instruction(()=> { SETr(5, ref registerC); }),
                new Instruction(()=> { SETr(5, ref registerD); }),
                new Instruction(()=> { SETr(5, ref registerE); }),
                new Instruction(()=> { SETr(5, ref registerH); }),
                new Instruction(()=> { SETr(5, ref registerL); }),
                new Instruction(()=> { SETm(5); }),
                new Instruction(()=> { SETr(5, ref registerA); }),

  // CBF0
                new Instruction(()=> { SETr(6, ref registerB); }),
                new Instruction(()=> { SETr(6, ref registerC); }),
                new Instruction(()=> { SETr(6, ref registerD); }),
                new Instruction(()=> { SETr(6, ref registerE); }),
                new Instruction(()=> { SETr(6, ref registerH); }),
                new Instruction(()=> { SETr(6, ref registerL); }),
                new Instruction(()=> { SETm(6); }),
                new Instruction(()=> { SETr(6, ref registerA); }),
                new Instruction(()=> { SETr(7, ref registerB); }),
                new Instruction(()=> { SETr(7, ref registerC); }),
                new Instruction(()=> { SETr(7, ref registerD); }),
                new Instruction(()=> { SETr(7, ref registerE); }),
                new Instruction(()=> { SETr(7, ref registerH); }),
                new Instruction(()=> { SETr(7, ref registerL); }),
                new Instruction(()=> { SETm(7); }),
                new Instruction(()=> { SETr(7, ref registerA); }),
            };
        }

        #region Operators

        #region Load/save

        //register-to-register copy
        private void LDrr_bb() { registerB = registerB; lastMachineClock = 1; }
        private void LDrr_bc() { registerB = registerC; lastMachineClock = 1; }
        private void LDrr_bd() { registerB = registerD; lastMachineClock = 1; }
        private void LDrr_be() { registerB = registerE; lastMachineClock = 1; }
        private void LDrr_bh() { registerB = registerH; lastMachineClock = 1; }
        private void LDrr_bl() { registerB = registerL; lastMachineClock = 1; }
        private void LDrr_ba() { registerB = registerA; lastMachineClock = 1; }
        private void LDrr_cb() { registerC = registerB; lastMachineClock = 1; }
        private void LDrr_cc() { registerC = registerC; lastMachineClock = 1; }
        private void LDrr_cd() { registerC = registerD; lastMachineClock = 1; }
        private void LDrr_ce() { registerC = registerE; lastMachineClock = 1; }
        private void LDrr_ch() { registerC = registerH; lastMachineClock = 1; }
        private void LDrr_cl() { registerC = registerL; lastMachineClock = 1; }
        private void LDrr_ca() { registerC = registerA; lastMachineClock = 1; }
        private void LDrr_db() { registerD = registerB; lastMachineClock = 1; }
        private void LDrr_dc() { registerD = registerC; lastMachineClock = 1; }
        private void LDrr_dd() { registerD = registerD; lastMachineClock = 1; }
        private void LDrr_de() { registerD = registerE; lastMachineClock = 1; }
        private void LDrr_dh() { registerD = registerH; lastMachineClock = 1; }
        private void LDrr_dl() { registerD = registerL; lastMachineClock = 1; }
        private void LDrr_da() { registerD = registerA; lastMachineClock = 1; }
        private void LDrr_eb() { registerE = registerB; lastMachineClock = 1; }
        private void LDrr_ec() { registerE = registerC; lastMachineClock = 1; }
        private void LDrr_ed() { registerE = registerD; lastMachineClock = 1; }
        private void LDrr_ee() { registerE = registerE; lastMachineClock = 1; }
        private void LDrr_eh() { registerE = registerH; lastMachineClock = 1; }
        private void LDrr_el() { registerE = registerL; lastMachineClock = 1; }
        private void LDrr_ea() { registerE = registerA; lastMachineClock = 1; }
        private void LDrr_hb() { registerH = registerB; lastMachineClock = 1; }
        private void LDrr_hc() { registerH = registerC; lastMachineClock = 1; }
        private void LDrr_hd() { registerH = registerD; lastMachineClock = 1; }
        private void LDrr_he() { registerH = registerE; lastMachineClock = 1; }
        private void LDrr_hh() { registerH = registerH; lastMachineClock = 1; }
        private void LDrr_hl() { registerH = registerL; lastMachineClock = 1; }
        private void LDrr_ha() { registerH = registerA; lastMachineClock = 1; }
        private void LDrr_lb() { registerL = registerB; lastMachineClock = 1; }
        private void LDrr_lc() { registerL = registerC; lastMachineClock = 1; }
        private void LDrr_ld() { registerL = registerD; lastMachineClock = 1; }
        private void LDrr_le() { registerL = registerE; lastMachineClock = 1; }
        private void LDrr_lh() { registerL = registerH; lastMachineClock = 1; }
        private void LDrr_ll() { registerL = registerL; lastMachineClock = 1; }
        private void LDrr_la() { registerL = registerA; lastMachineClock = 1; }
        private void LDrr_ab() { registerA = registerB; lastMachineClock = 1; }
        private void LDrr_ac() { registerA = registerC; lastMachineClock = 1; }
        private void LDrr_ad() { registerA = registerD; lastMachineClock = 1; }
        private void LDrr_ae() { registerA = registerE; lastMachineClock = 1; }
        private void LDrr_ah() { registerA = registerH; lastMachineClock = 1; }
        private void LDrr_al() { registerA = registerL; lastMachineClock = 1; }
        private void LDrr_aa() { registerA = registerA; lastMachineClock = 1; }


        //Read mem byte addressed in HL register
        private void LDrHLm_b() { registerB = _mmu.ReadByte(registerHL); lastMachineClock = 2; }
        private void LDrHLm_c() { registerC = _mmu.ReadByte(registerHL); lastMachineClock = 2; }
        private void LDrHLm_d() { registerD = _mmu.ReadByte(registerHL); lastMachineClock = 2; }
        private void LDrHLm_e() { registerE = _mmu.ReadByte(registerHL); lastMachineClock = 2; }
        private void LDrHLm_h() { registerH = _mmu.ReadByte(registerHL); lastMachineClock = 2; }
        private void LDrHLm_l() { registerL = _mmu.ReadByte(registerHL); lastMachineClock = 2; }
        private void LDrHLm_a() { registerA = _mmu.ReadByte(registerHL); lastMachineClock = 2; }

        //Write to mem byte addressed in HL register
        private void LDHLmr_b() { _mmu.WriteByte(registerHL, registerB); lastMachineClock = 2; }
        private void LDHLmr_c() { _mmu.WriteByte(registerHL, registerC); lastMachineClock = 2; }
        private void LDHLmr_d() { _mmu.WriteByte(registerHL, registerD); lastMachineClock = 2; }
        private void LDHLmr_e() { _mmu.WriteByte(registerHL, registerE); lastMachineClock = 2; }
        private void LDHLmr_h() { _mmu.WriteByte(registerHL, registerH); lastMachineClock = 2; }
        private void LDHLmr_l() { _mmu.WriteByte(registerHL, registerL); lastMachineClock = 2; }
        private void LDHLmr_a() { _mmu.WriteByte(registerHL, registerA); lastMachineClock = 2; }

        //Load immediate value to register and increment program counter
        private void LDrn_b() { registerB = _mmu.ReadByte(programCounter); programCounter++; lastMachineClock = 2; }
        private void LDrn_c() { registerC = _mmu.ReadByte(programCounter); programCounter++; lastMachineClock = 2; }
        private void LDrn_d() { registerD = _mmu.ReadByte(programCounter); programCounter++; lastMachineClock = 2; }
        private void LDrn_e() { registerE = _mmu.ReadByte(programCounter); programCounter++; lastMachineClock = 2; }
        private void LDrn_h() { registerH = _mmu.ReadByte(programCounter); programCounter++; lastMachineClock = 2; }
        private void LDrn_l() { registerL = _mmu.ReadByte(programCounter); programCounter++; lastMachineClock = 2; }
        private void LDrn_a() { registerA = _mmu.ReadByte(programCounter); programCounter++; lastMachineClock = 2; }

        //Write current instruction to mem byte addressed in HL register and increment program counter
        private void LDHLmn() { _mmu.WriteByte(registerHL, _mmu.ReadByte(programCounter)); programCounter++; lastMachineClock = 3; }

        //Write from register A to mem byte addressed in BC/DE register
        private void LDBCmA() { _mmu.WriteByte(registerBC, registerA); lastMachineClock = 2; }
        private void LDDEmA() { _mmu.WriteByte(registerDE, registerA); lastMachineClock = 2; }

        //Write from register A to mem byte addressed in program counter and increment program counter
        private void LDmmA() { _mmu.WriteByte(_mmu.ReadWord(programCounter), registerA); programCounter += 2; lastMachineClock = 4; }

        //Read from mem byte addressed in BC/DE register to register A
        private void LDABCm() { registerA = _mmu.ReadByte(registerBC); lastMachineClock = 2; lastCycleClock = 8; }
        private void LDADEm() { registerA = _mmu.ReadByte(registerDE); lastMachineClock = 2; lastCycleClock = 8; }

        //Read from mem byte addressed in program counter to register A and increment program counter
        private void LDAmm() { registerA = _mmu.ReadByte(_mmu.ReadWord(programCounter)); programCounter += 2; lastMachineClock = 3; lastCycleClock = 12; }

        //Read word addressed in program counter and increment program counter
        private void LDBCnn() { registerC = _mmu.ReadByte(programCounter); registerB = _mmu.ReadByte((ushort)(programCounter + 1)); programCounter += 2; lastMachineClock = 3; lastCycleClock = 12; }
        private void LDDEnn() { registerE = _mmu.ReadByte(programCounter); registerD = _mmu.ReadByte((ushort)(programCounter + 1)); programCounter += 2; lastMachineClock = 3; lastCycleClock = 12; }
        private void LDHLnn() { registerL = _mmu.ReadByte(programCounter); registerH = _mmu.ReadByte((ushort)(programCounter + 1)); programCounter += 2; lastMachineClock = 3; lastCycleClock = 12; }
        private void LDSPnn() { stackPointer = _mmu.ReadWord(programCounter); programCounter += 2; lastMachineClock = 3; lastCycleClock = 12; }

        //MAYBE write from stack pointer to mem byte addressed in program counter, and increment program counter
        private void LDmmSP() { _mmu.WriteWord(_mmu.ReadWord(programCounter), stackPointer); programCounter += 2; lastMachineClock = 3; lastCycleClock = 12; }

        //?
        //private void LDHLmm() { }
        //private void LDmmHL() { }

        //Read/Write between register A and memory addressed in HL, and increment/decrement HL
        private void LDHLIA() { _mmu.WriteByte(registerHL, registerA); IncrementHL(); lastMachineClock = 2; }
        private void LDAHLI() { registerA = _mmu.ReadByte(registerHL); IncrementHL(); lastMachineClock = 2; }
        private void LDHLDA() { _mmu.WriteByte(registerHL, registerA); DecrementHL(); lastMachineClock = 2; }
        private void LDAHLD() { registerA = _mmu.ReadByte(registerHL); DecrementHL(); lastMachineClock = 2; }

        //Read/Write between register A and IO memory at immediate value
        private void LDAIOn() { registerA = _mmu.ReadByte((ushort)(0xFF00 + _mmu.ReadByte(programCounter))); programCounter++; lastMachineClock = 3; }
        private void LDIOnA() { _mmu.WriteByte((ushort)(0xFF00 + _mmu.ReadByte(programCounter)), registerA); programCounter++; lastMachineClock = 3; }
        private void LDAIOC() { registerA = _mmu.ReadByte((ushort)(0xFF00 + registerC)); lastMachineClock += 2; }
        private void LDIOCA() { _mmu.WriteByte((ushort)(0xFF00 + registerC), registerA); lastMachineClock += 2; }

        //Put SP + n effective address into HL
        private void LDHLSPn() { ushort result = AdditionHelperSPn(stackPointer, _mmu.ReadByte(programCounter)); registerH = (byte)((result >> 8) & 255); registerL = (byte)(result & 255); programCounter++; lastMachineClock = 3; }

        //Put HL into SP
        private void LDSPHL() { stackPointer = registerHL; lastMachineClock = 2; }

        //Swap value in register with value in memory addressed by HL
        private byte SWAPHelper(byte value) { byte result = (byte)(((value & 0xF) << 4) | (((value & 0xF0) >> 4))); registerF = 0; fZ = result == 0; return result; }
        private void SWAPr_b() { registerB = SWAPHelper(registerB); lastMachineClock = 2; }
        private void SWAPr_c() { registerC = SWAPHelper(registerC); lastMachineClock = 2; }
        private void SWAPr_d() { registerD = SWAPHelper(registerD); lastMachineClock = 2; }
        private void SWAPr_e() { registerE = SWAPHelper(registerE); lastMachineClock = 2; }
        private void SWAPr_h() { registerH = SWAPHelper(registerH); lastMachineClock = 2; }
        private void SWAPr_l() { registerL = SWAPHelper(registerL); lastMachineClock = 2; }
        private void SWAPr_a() { registerA = SWAPHelper(registerA); lastMachineClock = 2; }
        private void SWAPHL() { _mmu.WriteByte(registerHL, SWAPHelper(_mmu.ReadByte(registerHL))); lastMachineClock = 4; }

        //TODO

        #endregion

        #region Arithmetic/logic operations

        //Addition
        private byte AdditionHelper(byte a, byte b, bool useCarryFlag=false)
        {
            int carry = useCarryFlag && fC ? 1 : 0;
            int bigResult = a + b + carry;
            registerF = 0;
            fZ = bigResult == 256 || bigResult == 0; //Check for 0
            fH = a.LowerNibble() + b.LowerNibble() + carry > 15;
            fC = bigResult > 255; //Check for carry
            return (byte)(bigResult % 256);
        }

        private ushort AdditionHelperWord(ushort target, ushort value, bool useCarryFlag=false)
        {
            int carry = useCarryFlag && fC ? 1 : 0;
            int bigResult = target + value + carry;
            //fZ = bigResult == 65535 || bigResult == 0; //Check for 0 (not used on 16 bit add)
            fN = false;
            fH = (((target&0xfff)+(value&0xfff))&0x1000) > 0;
            fC = bigResult > 65535;
            return (ushort)(bigResult % 65536);
        }

        private ushort AdditionHelperSPn(ushort target, byte value) //Value is signed
        {
            registerF = 0;
            int signedValue = value.ToSigned();
            int bigResult = target + signedValue;
            ushort finalResult = (ushort)(bigResult % 65536);
            fH = ((target ^ signedValue ^ finalResult) & 0x10) > 0;
            fC = ((target ^ signedValue ^ finalResult) & 0x100) > 0;
            return finalResult;
        }

        private void ADDr_b() { registerA = AdditionHelper(registerA, registerB); lastMachineClock = 1; lastCycleClock = 4; }
        private void ADDr_c() { registerA = AdditionHelper(registerA, registerC); lastMachineClock = 1; lastCycleClock = 4; }
        private void ADDr_d() { registerA = AdditionHelper(registerA, registerD); lastMachineClock = 1; lastCycleClock = 4; }
        private void ADDr_e() { registerA = AdditionHelper(registerA, registerE); lastMachineClock = 1; lastCycleClock = 4; }
        private void ADDr_h() { registerA = AdditionHelper(registerA, registerH); lastMachineClock = 1; lastCycleClock = 4; }
        private void ADDr_l() { registerA = AdditionHelper(registerA, registerL); lastMachineClock = 1; lastCycleClock = 4; }
        private void ADDr_a() { registerA = AdditionHelper(registerA, registerA); lastMachineClock = 1; lastCycleClock = 4; }
        private void ADDHL() { registerA = AdditionHelper(registerA, _mmu.ReadByte(registerHL)); lastMachineClock = 2; lastCycleClock = 8; }
        private void ADDn() { registerA = AdditionHelper(registerA, _mmu.ReadByte(programCounter)); programCounter++; lastMachineClock = 2; lastCycleClock = 8; }
        private void ADDHLBC() { ushort result = AdditionHelperWord(registerHL, registerBC); registerH = (byte)((result >> 8) & 255); registerL = (byte)(result & 255); lastMachineClock = 3; }
        private void ADDHLDE() { ushort result = AdditionHelperWord(registerHL, registerDE); registerH = (byte)((result >> 8) & 255); registerL = (byte)(result & 255); lastMachineClock = 3; }
        private void ADDHLHL() { ushort result = AdditionHelperWord(registerHL, registerHL); registerH = (byte)((result >> 8) & 255); registerL = (byte)(result & 255); lastMachineClock = 3; }
        private void ADDHLSP() { ushort result = AdditionHelperWord(registerHL, stackPointer); registerH = (byte)((result >> 8) & 255); registerL = (byte)(result & 255); lastMachineClock = 3; }
        private void ADDSPn() { ushort result = AdditionHelperSPn(stackPointer, _mmu.ReadByte(programCounter)); stackPointer = result; programCounter++; lastMachineClock = 4; }
        //Is this supposed to be signed?

        private void ADCr_b() { registerA = AdditionHelper(registerA, registerB, true); lastMachineClock = 1; }
        private void ADCr_c() { registerA = AdditionHelper(registerA, registerC, true); lastMachineClock = 1; }
        private void ADCr_d() { registerA = AdditionHelper(registerA, registerD, true); lastMachineClock = 1; }
        private void ADCr_e() { registerA = AdditionHelper(registerA, registerE, true); lastMachineClock = 1; }
        private void ADCr_h() { registerA = AdditionHelper(registerA, registerH, true); lastMachineClock = 1; }
        private void ADCr_l() { registerA = AdditionHelper(registerA, registerL, true); lastMachineClock = 1; }
        private void ADCr_a() { registerA = AdditionHelper(registerA, registerA, true); lastMachineClock = 1; }
        private void ADCHL() { registerA = AdditionHelper(registerA, _mmu.ReadByte(registerHL), true); lastMachineClock = 2; }
        private void ADCn() { registerA = AdditionHelper(registerA, _mmu.ReadByte(programCounter), true); programCounter++; lastMachineClock = 2; }

        //Subtraction
        private byte SubtractionHelper(byte a, byte b, bool useCarryFlag=false)
        {
            int carry = useCarryFlag && fC ? 1 : 0;
            int signedResult = a - b - carry;
            fN = true;
            fC = false;
            if (signedResult < 0)
            {
                signedResult = 256 + signedResult;
                fC = true;
            }
            fZ = signedResult == 0; //Check for 0
            fH = (a.LowerNibble() - b.LowerNibble() - carry < 0);
            return (byte)signedResult;
        }

        private void SUBr_b() { registerA = SubtractionHelper(registerA, registerB); lastMachineClock = 1; }
        private void SUBr_c() { registerA = SubtractionHelper(registerA, registerC); lastMachineClock = 1; }
        private void SUBr_d() { registerA = SubtractionHelper(registerA, registerD); lastMachineClock = 1; }
        private void SUBr_e() { registerA = SubtractionHelper(registerA, registerE); lastMachineClock = 1; }
        private void SUBr_h() { registerA = SubtractionHelper(registerA, registerH); lastMachineClock = 1; }
        private void SUBr_l() { registerA = SubtractionHelper(registerA, registerL); lastMachineClock = 1; }
        private void SUBr_a() { registerA = SubtractionHelper(registerA, registerA); lastMachineClock = 1; }
        private void SUBHL() { registerA = SubtractionHelper(registerA, _mmu.ReadByte(registerHL)); lastMachineClock = 2; }
        private void SUBn() { registerA = SubtractionHelper(registerA, _mmu.ReadByte(programCounter)); programCounter++; lastMachineClock = 2; }
        //...

        private void SBCr_b() { registerA = SubtractionHelper(registerA, registerB, true); lastMachineClock = 1; }
        private void SBCr_c() { registerA = SubtractionHelper(registerA, registerC, true); lastMachineClock = 1; }
        private void SBCr_d() { registerA = SubtractionHelper(registerA, registerD, true); lastMachineClock = 1; }
        private void SBCr_e() { registerA = SubtractionHelper(registerA, registerE, true); lastMachineClock = 1; }
        private void SBCr_h() { registerA = SubtractionHelper(registerA, registerH, true); lastMachineClock = 1; }
        private void SBCr_l() { registerA = SubtractionHelper(registerA, registerL, true); lastMachineClock = 1; }
        private void SBCr_a() { registerA = SubtractionHelper(registerA, registerA, true); lastMachineClock = 1; }
        private void SBCHL() { registerA = SubtractionHelper(registerA, _mmu.ReadByte(registerHL), true); lastMachineClock = 2; }
        private void SBCn() { registerA = SubtractionHelper(registerA, _mmu.ReadByte(programCounter), true); programCounter++; lastMachineClock = 2; }
        //...

        private void CPr_b() { SubtractionHelper(registerA, registerB); lastMachineClock = 1; }
        private void CPr_c() { SubtractionHelper(registerA, registerC); lastMachineClock = 1; }
        private void CPr_d() { SubtractionHelper(registerA, registerD); lastMachineClock = 1; }
        private void CPr_e() { SubtractionHelper(registerA, registerE); lastMachineClock = 1; }
        private void CPr_h() { SubtractionHelper(registerA, registerH); lastMachineClock = 1; }
        private void CPr_l() { SubtractionHelper(registerA, registerL); lastMachineClock = 1; }
        private void CPr_a() { SubtractionHelper(registerA, registerA); lastMachineClock = 1; }
        private void CPHL() { int i = registerA; int m = _mmu.ReadByte(registerHL); i -= m; registerF = (byte)((i < 0) ? 0x50 : 0x40); i &= 255; if (i == 0) { registerF |= 0x80; } if (((registerA ^ i ^ m) & 0x10) > 0) { registerF |= 0x20; } lastMachineClock = 2; }
        private void CPn() { int i = registerA; int m = _mmu.ReadByte(programCounter); i -= m; programCounter++; registerF = (byte)((i < 0) ? 0x50 : 0x40); i &= 255; if (i == 0) { registerF |= 0x80; } if (((registerA ^ i ^ m) & 0x10) > 0) { registerF |= 0x20; } lastMachineClock = 2; }
        //...

        private void ANDr_b() { registerA &= registerB; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 1; }
        private void ANDr_c() { registerA &= registerC; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 1; }
        private void ANDr_d() { registerA &= registerD; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 1; }
        private void ANDr_e() { registerA &= registerE; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 1; }
        private void ANDr_h() { registerA &= registerH; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 1; }
        private void ANDr_l() { registerA &= registerL; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 1; }
        private void ANDr_a() { registerA &= registerA; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 1; }
        private void ANDHL() { registerA &= _mmu.ReadByte(registerHL); registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 2; }
        private void ANDn() { registerA &= _mmu.ReadByte(programCounter); programCounter++; registerF = 0; fH = true; fZ = registerA == 0; lastMachineClock = 2; }

        //...

        private void ORr_b() { registerA |= registerB; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void ORr_c() { registerA |= registerC; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void ORr_d() { registerA |= registerD; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void ORr_e() { registerA |= registerE; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void ORr_h() { registerA |= registerH; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void ORr_l() { registerA |= registerL; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void ORr_a() { registerA |= registerA; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void ORHL() { registerA |= _mmu.ReadByte(registerHL); registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 2; }
        private void ORn() { registerA |= _mmu.ReadByte(programCounter); programCounter++; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 2; }

        //...

        private void XORr_b() { registerA ^= registerB; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void XORr_c() { registerA ^= registerC; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void XORr_d() { registerA ^= registerD; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void XORr_e() { registerA ^= registerE; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void XORr_h() { registerA ^= registerH; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void XORr_l() { registerA ^= registerL; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void XORr_a() { registerA ^= registerA; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 1; }
        private void XORHL() { registerA ^= _mmu.ReadByte(registerHL); registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 2; }
        private void XORn() { registerA ^= _mmu.ReadByte(programCounter); programCounter++; registerF = (byte)(registerA > 0 ? 0 : 0x80); lastMachineClock = 2; }
        //...

        private void INCr_b() { registerB++; fZ = registerB == 0; fN = false; fH = registerB.LowerNibble() == 0; lastMachineClock = 1; }
        private void INCr_c() { registerC++; fZ = registerC == 0; fN = false; fH = registerC.LowerNibble() == 0; lastMachineClock = 1; }
        private void INCr_d() { registerD++; fZ = registerD == 0; fN = false; fH = registerD.LowerNibble() == 0; lastMachineClock = 1; }
        private void INCr_e() { registerE++; fZ = registerE == 0; fN = false; fH = registerE.LowerNibble() == 0; lastMachineClock = 1; }
        private void INCr_h() { registerH++; fZ = registerH == 0; fN = false; fH = registerH.LowerNibble() == 0; lastMachineClock = 1; }
        private void INCr_l() { registerL++; fZ = registerL == 0; fN = false; fH = registerL.LowerNibble() == 0; lastMachineClock = 1; }
        private void INCr_a() { registerA++; fZ = registerA == 0; fN = false; fH = registerA.LowerNibble() == 0; lastMachineClock = 1; }
        private void INCHLm() { byte i = _mmu.ReadByte(registerHL); i++; _mmu.WriteByte(registerHL, i); fZ = i == 0; fN = false; fH = i.LowerNibble() == 0; lastMachineClock = 3; }
        //...

        private void DECr_b() { registerB--; fZ = registerB == 0; fN = true; fH = registerB.LowerNibble() == 0xF; lastMachineClock = 1; }
        private void DECr_c() { registerC--; fZ = registerC == 0; fN = true; fH = registerC.LowerNibble() == 0xF; lastMachineClock = 1; }
        private void DECr_d() { registerD--; fZ = registerD == 0; fN = true; fH = registerD.LowerNibble() == 0xF; lastMachineClock = 1; }
        private void DECr_e() { registerE--; fZ = registerE == 0; fN = true; fH = registerE.LowerNibble() == 0xF; lastMachineClock = 1; }
        private void DECr_h() { registerH--; fZ = registerH == 0; fN = true; fH = registerH.LowerNibble() == 0xF; lastMachineClock = 1; }
        private void DECr_l() { registerL--; fZ = registerL == 0; fN = true; fH = registerL.LowerNibble() == 0xF; lastMachineClock = 1; }
        private void DECr_a() { registerA--; fZ = registerA == 0; fN = true; fH = registerA.LowerNibble() == 0xF; lastMachineClock = 1; }
        private void DECHLm() { byte i = _mmu.ReadByte(registerHL); i--; _mmu.WriteByte(registerHL, i); fZ = i == 0; fN = true; fH = i.LowerNibble() == 0xF; lastMachineClock = 3; }
        //...

        private void INCBC() { IncrementBC(); lastMachineClock = 2; }
        private void INCDE() { IncrementDE(); lastMachineClock = 2; }
        private void INCHL() { IncrementHL(); lastMachineClock = 2; }
        private void INCSP() { stackPointer++; lastMachineClock = 2; }

        private void DECBC() { DecrementBC(); lastMachineClock = 2; }
        private void DECDE() { DecrementDE(); lastMachineClock = 2; }
        private void DECHL() { DecrementHL(); lastMachineClock = 2; }
        private void DECSP() { stackPointer--; lastMachineClock = 2; }



        #endregion

        #region Bitwise operations

        private void BIT(byte value, byte position) { fN = false; fH = true; fZ = !value.GetBit(position); lastMachineClock = 2; }

        private void BIT0b() { BIT(registerB, 0); }
        private void BIT0c() { BIT(registerC, 0); }
        private void BIT0d() { BIT(registerD, 0); }
        private void BIT0e() { BIT(registerE, 0); }
        private void BIT0h() { BIT(registerH, 0); }
        private void BIT0l() { BIT(registerL, 0); }
        private void BIT0a() { BIT(registerA, 0); }
        private void BIT0m() { BIT(_mmu.ReadByte(registerHL), 0); lastMachineClock += 2; }

        private void BIT1b() { BIT(registerB, 1); }
        private void BIT1c() { BIT(registerC, 1); }
        private void BIT1d() { BIT(registerD, 1); }
        private void BIT1e() { BIT(registerE, 1); }
        private void BIT1h() { BIT(registerH, 1); }
        private void BIT1l() { BIT(registerL, 1); }
        private void BIT1a() { BIT(registerA, 1); }
        private void BIT1m() { BIT(_mmu.ReadByte(registerHL), 1); lastMachineClock += 2; }

        private void BIT2b() { BIT(registerB, 2); }
        private void BIT2c() { BIT(registerC, 2); }
        private void BIT2d() { BIT(registerD, 2); }
        private void BIT2e() { BIT(registerE, 2); }
        private void BIT2h() { BIT(registerH, 2); }
        private void BIT2l() { BIT(registerL, 2); }
        private void BIT2a() { BIT(registerA, 2); }
        private void BIT2m() { BIT(_mmu.ReadByte(registerHL), 2); lastMachineClock += 2; }

        private void BIT3b() { BIT(registerB, 3); }
        private void BIT3c() { BIT(registerC, 3); }
        private void BIT3d() { BIT(registerD, 3); }
        private void BIT3e() { BIT(registerE, 3); }
        private void BIT3h() { BIT(registerH, 3); }
        private void BIT3l() { BIT(registerL, 3); }
        private void BIT3a() { BIT(registerA, 3); }
        private void BIT3m() { BIT(_mmu.ReadByte(registerHL), 3); lastMachineClock += 2; }

        private void BIT4b() { BIT(registerB, 4); }
        private void BIT4c() { BIT(registerC, 4); }
        private void BIT4d() { BIT(registerD, 4); }
        private void BIT4e() { BIT(registerE, 4); }
        private void BIT4h() { BIT(registerH, 4); }
        private void BIT4l() { BIT(registerL, 4); }
        private void BIT4a() { BIT(registerA, 4); }
        private void BIT4m() { BIT(_mmu.ReadByte(registerHL), 4); lastMachineClock += 2; }

        private void BIT5b() { BIT(registerB, 5); }
        private void BIT5c() { BIT(registerC, 5); }
        private void BIT5d() { BIT(registerD, 5); }
        private void BIT5e() { BIT(registerE, 5); }
        private void BIT5h() { BIT(registerH, 5); }
        private void BIT5l() { BIT(registerL, 5); }
        private void BIT5a() { BIT(registerA, 5); }
        private void BIT5m() { BIT(_mmu.ReadByte(registerHL), 5); lastMachineClock += 2; }

        private void BIT6b() { BIT(registerB, 6); }
        private void BIT6c() { BIT(registerC, 6); }
        private void BIT6d() { BIT(registerD, 6); }
        private void BIT6e() { BIT(registerE, 6); }
        private void BIT6h() { BIT(registerH, 6); }
        private void BIT6l() { BIT(registerL, 6); }
        private void BIT6a() { BIT(registerA, 6); }
        private void BIT6m() { BIT(_mmu.ReadByte(registerHL), 6); lastMachineClock += 2; }

        private void BIT7b() { BIT(registerB, 7); }
        private void BIT7c() { BIT(registerC, 7); }
        private void BIT7d() { BIT(registerD, 7); }
        private void BIT7e() { BIT(registerE, 7); }
        private void BIT7h() { BIT(registerH, 7); }
        private void BIT7l() { BIT(registerL, 7); }
        private void BIT7a() { BIT(registerA, 7); }
        private void BIT7m() { BIT(_mmu.ReadByte(registerHL), 7); lastMachineClock += 2; }

        private void RESr(byte bit, ref byte register) { register = register.ResetBit(bit); lastMachineClock = 2; }
        private void RESm(byte bit) { byte i = _mmu.ReadByte(registerHL).ResetBit(bit); _mmu.WriteByte(registerHL, i); lastMachineClock = 4; }

        /*
        private void RES0b() { registerB = registerB.ResetBit(0); lastMachineClock = 2; }
        private void RES0c() { registerC = registerC.ResetBit(0); lastMachineClock = 2; }
        private void RES0d() { registerD = registerD.ResetBit(0); lastMachineClock = 2; }
        private void RES0e() { registerE = registerE.ResetBit(0); lastMachineClock = 2; }
        private void RES0h() { registerH = registerH.ResetBit(0); lastMachineClock = 2; }
        private void RES0l() { registerL = registerL.ResetBit(0); lastMachineClock = 2; }
        private void RES0a() { registerA = registerA.ResetBit(0); lastMachineClock = 2; }
        private void RES0m() { byte i = _mmu.ReadByte(registerHL).ResetBit(0); _mmu.WriteByte(registerHL, i); lastMachineClock = 4; }

        private void RES1b() { registerB = registerB.ResetBit(1); lastMachineClock = 2; }
        private void RES1c() { registerC = registerC.ResetBit(1); lastMachineClock = 2; }
        private void RES1d() { registerD = registerD.ResetBit(1); lastMachineClock = 2; }
        private void RES1e() { registerE = registerE.ResetBit(1); lastMachineClock = 2; }
        private void RES1h() { registerH = registerH.ResetBit(1); lastMachineClock = 2; }
        private void RES1l() { registerL = registerL.ResetBit(1); lastMachineClock = 2; }
        private void RES1a() { registerA = registerA.ResetBit(1); lastMachineClock = 2; }
        private void RES1m() { byte i = _mmu.ReadByte(registerHL).ResetBit(1); _mmu.WriteByte(registerHL, i); lastMachineClock = 4; }
        */

        private void SETr(byte bit, ref byte register) { register = register.SetBit(bit); lastMachineClock = 2; }
        private void SETm(byte bit) { byte i = _mmu.ReadByte(registerHL).SetBit(bit); _mmu.WriteByte(registerHL, i); lastMachineClock = 4; }

        /*
        private void SET0b() { registerB = registerB.SetBit(0); lastMachineClock = 2; }
        private void SET0c() { registerC = registerC.SetBit(0); lastMachineClock = 2; }
        private void SET0d() { registerD = registerD.SetBit(0); lastMachineClock = 2; }
        private void SET0e() { registerE = registerE.SetBit(0); lastMachineClock = 2; }
        private void SET0h() { registerH = registerH.SetBit(0); lastMachineClock = 2; }
        private void SET0l() { registerL = registerL.SetBit(0); lastMachineClock = 2; }
        private void SET0a() { registerA = registerA.SetBit(0); lastMachineClock = 2; }
        private void SET0m() { byte i = _mmu.ReadByte(registerHL).SetBit(0); _mmu.WriteByte(registerHL, i); lastMachineClock = 4; }
        */

        //Rotations and shifts

        //Documentation does not say this, but fZ resets to false on these
        private void RLA() { RLr_a(); fZ = false; lastMachineClock = 1; }
        private void RLCA() { RLCr_a(); fZ = false; lastMachineClock = 1; }
        private void RRA() { RRr_a(); fZ = false; lastMachineClock = 1; }
        private void RRCA() { RRCr_a(); fZ = false; lastMachineClock = 1; }

        private byte RLHelper(byte value) { bool oldC = fC; registerF = 0; fC = value.GetBit(7); value = value.ShiftLeft(); if (oldC) { value = value.SetBit(0); } fZ = (value == 0); lastMachineClock = 2; return value; }
        private void RLr_b() { registerB = RLHelper(registerB); }
        private void RLr_c() { registerC = RLHelper(registerC); }
        private void RLr_d() { registerD = RLHelper(registerD); }
        private void RLr_e() { registerE = RLHelper(registerE); }
        private void RLr_h() { registerH = RLHelper(registerH); }
        private void RLr_l() { registerL = RLHelper(registerL); }
        private void RLr_a() { registerA = RLHelper(registerA); }
        private void RLHL() { _mmu.WriteByte(registerHL, RLHelper(_mmu.ReadByte(registerHL))); lastMachineClock += 2; }

        private byte RLCHelper(byte value) { registerF = 0; fC = value.GetBit(7); value = value.ShiftLeft(); if (fC) { value = value.SetBit(0); } fZ = (value == 0); lastMachineClock = 2; return value; }
        private void RLCr_b() { registerB = RLCHelper(registerB); }
        private void RLCr_c() { registerC = RLCHelper(registerC); }
        private void RLCr_d() { registerD = RLCHelper(registerD); }
        private void RLCr_e() { registerE = RLCHelper(registerE); }
        private void RLCr_h() { registerH = RLCHelper(registerH); }
        private void RLCr_l() { registerL = RLCHelper(registerL); }
        private void RLCr_a() { registerA = RLCHelper(registerA); }
        private void RLCHL() { _mmu.WriteByte(registerHL, RLCHelper(_mmu.ReadByte(registerHL))); lastMachineClock += 2; }

        private byte RRHelper(byte value) { bool oldC = fC; registerF = 0; fC = value.GetBit(0); value = value.ShiftRight(); if (oldC) { value = value.SetBit(7); } fZ = (value == 0); lastMachineClock = 2; return value; }
        private void RRr_b() { registerB = RRHelper(registerB); }
        private void RRr_c() { registerC = RRHelper(registerC); }
        private void RRr_d() { registerD = RRHelper(registerD); }
        private void RRr_e() { registerE = RRHelper(registerE); }
        private void RRr_h() { registerH = RRHelper(registerH); }
        private void RRr_l() { registerL = RRHelper(registerL); }
        private void RRr_a() { registerA = RRHelper(registerA); }
        private void RRHL() { _mmu.WriteByte(registerHL, RRHelper(_mmu.ReadByte(registerHL))); lastMachineClock += 2; }

        private byte RRCHelper(byte value) { registerF = 0; fC = value.GetBit(0); value = value.ShiftRight(); if (fC) { value = value.SetBit(7); } fZ = (value == 0); lastMachineClock = 2; return value; }
        private void RRCr_b() { registerB = RRCHelper(registerB); }
        private void RRCr_c() { registerC = RRCHelper(registerC); }
        private void RRCr_d() { registerD = RRCHelper(registerD); }
        private void RRCr_e() { registerE = RRCHelper(registerE); }
        private void RRCr_h() { registerH = RRCHelper(registerH); }
        private void RRCr_l() { registerL = RRCHelper(registerL); }
        private void RRCr_a() { registerA = RRCHelper(registerA); }
        private void RRCHL() { _mmu.WriteByte(registerHL, RRCHelper(_mmu.ReadByte(registerHL))); lastMachineClock += 2; }

        private byte SLAHelper(byte value) { registerF = 0; fC = value.GetBit(7); value = value.ShiftLeft(); fZ = (value == 0); lastMachineClock = 2; return value; }
        private void SLAr_b() { registerB = SLAHelper(registerB); }
        private void SLAr_c() { registerC = SLAHelper(registerC); }
        private void SLAr_d() { registerD = SLAHelper(registerD); }
        private void SLAr_e() { registerE = SLAHelper(registerE); }
        private void SLAr_h() { registerH = SLAHelper(registerH); }
        private void SLAr_l() { registerL = SLAHelper(registerL); }
        private void SLAr_a() { registerA = SLAHelper(registerA); }
        private void SLAHL() { _mmu.WriteByte(registerHL, SLAHelper(_mmu.ReadByte(registerHL))); lastMachineClock += 2; }

        private byte SRAHelper(byte value) { bool oldMSB = value.GetBit(7); registerF = 0; fC = value.GetBit(0); value = value.ShiftRight(); if (oldMSB) { value = value.SetBit(7); } fZ = (value == 0); lastMachineClock = 2; return value; }
        private void SRAr_b() { registerB = SRAHelper(registerB); }
        private void SRAr_c() { registerC = SRAHelper(registerC); }
        private void SRAr_d() { registerD = SRAHelper(registerD); }
        private void SRAr_e() { registerE = SRAHelper(registerE); }
        private void SRAr_h() { registerH = SRAHelper(registerH); }
        private void SRAr_l() { registerL = SRAHelper(registerL); }
        private void SRAr_a() { registerA = SRAHelper(registerA); }
        private void SRAHL() { _mmu.WriteByte(registerHL, SRAHelper(_mmu.ReadByte(registerHL))); lastMachineClock += 2; }

        private byte SRLHelper(byte value) { registerF = 0; fC = value.GetBit(0); value = value.ShiftRight(); fZ = (value == 0); lastMachineClock = 2; return value; }
        private void SRLr_b() { registerB = SRLHelper(registerB); }
        private void SRLr_c() { registerC = SRLHelper(registerC); }
        private void SRLr_d() { registerD = SRLHelper(registerD); }
        private void SRLr_e() { registerE = SRLHelper(registerE); }
        private void SRLr_h() { registerH = SRLHelper(registerH); }
        private void SRLr_l() { registerL = SRLHelper(registerL); }
        private void SRLr_a() { registerA = SRLHelper(registerA); }
        private void SRLHL() { _mmu.WriteByte(registerHL, SRLHelper(_mmu.ReadByte(registerHL))); lastMachineClock += 2; }

        
        private void CPL() { registerA ^= 255; fN = true; fH = true; lastMachineClock = 1; }
        private void CCF() { fN = false; fH = false; fC = !fC; lastMachineClock = 1; }
        private void SCF() { fN = false; fH = false; fC = true; lastMachineClock = 1; }

        private void DAA()
        {
            int result = registerA;
            if (!fN)
            {
                if (fH || registerA.LowerNibble() > 9) { result += 6; }
                if (fC || result > 0x9F) { result += 0x60; }
            }
            else
            {
                if (fH) { result = (result - 6) & 0xFF; }
                if (fC) { result -= 0x60; }
            }
            fH = false;
            fC = fC || (result & 0x100)>0;
            result &= 0xFF;
            fZ = result == 0;
            registerA = (byte)result;
            lastMachineClock = 1;
        }

        #endregion

        #region Stack functions

        private void PUSHBC() { stackPointer--; _mmu.WriteByte(stackPointer, registerB); stackPointer--; _mmu.WriteByte(stackPointer, registerC); lastMachineClock = 3; lastCycleClock = 12; }
        private void PUSHDE() { stackPointer--; _mmu.WriteByte(stackPointer, registerD); stackPointer--; _mmu.WriteByte(stackPointer, registerE); lastMachineClock = 3; lastCycleClock = 12; }
        private void PUSHHL() { stackPointer--; _mmu.WriteByte(stackPointer, registerH); stackPointer--; _mmu.WriteByte(stackPointer, registerL); lastMachineClock = 3; lastCycleClock = 12; }
        private void PUSHAF() { stackPointer--; _mmu.WriteByte(stackPointer, registerA); stackPointer--; _mmu.WriteByte(stackPointer, registerF); lastMachineClock = 3; lastCycleClock = 12; }

        private void POPBC() { registerC = _mmu.ReadByte(stackPointer); stackPointer++; registerB = _mmu.ReadByte(stackPointer); stackPointer++; lastMachineClock = 3; lastCycleClock = 12; }
        private void POPDE() { registerE = _mmu.ReadByte(stackPointer); stackPointer++; registerD = _mmu.ReadByte(stackPointer); stackPointer++; lastMachineClock = 3; lastCycleClock = 12; }
        private void POPHL() { registerL = _mmu.ReadByte(stackPointer); stackPointer++; registerH = _mmu.ReadByte(stackPointer); stackPointer++; lastMachineClock = 3; lastCycleClock = 12; }
        private void POPAF() { registerF = (byte)(_mmu.ReadByte(stackPointer) & 0xF0); stackPointer++; registerA = _mmu.ReadByte(stackPointer); stackPointer++; lastMachineClock = 3; lastCycleClock = 12; } //only upper 4 regF bits are usable

        #endregion

        #region Jump functions

        //Jump to immediate address
        private void JPnn() { programCounter = _mmu.ReadWord(programCounter); lastMachineClock = 3; lastCycleClock = 12; }

        //Jump to address in register HL
        private void JPHL() { programCounter = registerHL; lastMachineClock = 1; lastCycleClock = 4; }

        //Jump to immediate address if last result was/was not carry/zero
        private void JPNZnn() { lastMachineClock = 3; lastCycleClock = 12; if (!fZ) { programCounter = _mmu.ReadWord(programCounter); lastMachineClock++; lastCycleClock += 4; } else { programCounter += 2; } }
        private void JPZnn() { lastMachineClock = 3; lastCycleClock = 12; if (fZ) { programCounter = _mmu.ReadWord(programCounter); lastMachineClock++; lastCycleClock += 4; } else { programCounter += 2; } }
        private void JPNCnn() { lastMachineClock = 3; lastCycleClock = 12; if (!fC) { programCounter = _mmu.ReadWord(programCounter); lastMachineClock++; lastCycleClock += 4; } else { programCounter += 2; } }
        private void JPCnn() { lastMachineClock = 3; lastCycleClock = 12; if (fC) { programCounter = _mmu.ReadWord(programCounter); lastMachineClock++; lastCycleClock += 4; } else { programCounter += 2; } }

        //Relative jumps
        private void JRn() { int i = _mmu.ReadByte(programCounter).ToSigned(); programCounter++; lastMachineClock = 2; programCounter = (ushort)(programCounter + i); lastMachineClock++; }
        private void JRNZn() { int i = _mmu.ReadByte(programCounter).ToSigned(); programCounter++; lastMachineClock = 2; if ((registerF & 0x80) == 0x00) { programCounter = (ushort)(programCounter+i); lastMachineClock++; } }
        private void JRZn() { int i = _mmu.ReadByte(programCounter).ToSigned(); programCounter++; lastMachineClock = 2; if ((registerF & 0x80) == 0x80) { programCounter = (ushort)(programCounter + i); lastMachineClock++; } }
        private void JRNCn() { int i = _mmu.ReadByte(programCounter).ToSigned(); programCounter++; lastMachineClock = 2; if ((registerF & 0x10) == 0x00) { programCounter = (ushort)(programCounter + i); lastMachineClock++; } }
        private void JRCn() { int i = _mmu.ReadByte(programCounter).ToSigned(); programCounter++; lastMachineClock = 2; if ((registerF & 0x10) == 0x10) { programCounter = (ushort)(programCounter + i); lastMachineClock++; } }

        //? Stop?
        private void DJNZn() { int i = _mmu.ReadByte(programCounter); if (i > 127) { i = -((~i + 1) & 255); } programCounter++; lastMachineClock = 2; registerB--; if (registerB > 0) { programCounter += (byte)i; lastMachineClock++; } }

        //Call routine at position
        private void CALLnn() { stackPointer -= 2; _mmu.WriteWord(stackPointer, (ushort)(programCounter + 2)); programCounter = _mmu.ReadWord(programCounter); lastMachineClock = 5; }
        private void CALLNZnn() { if (!fZ) { CALLnn(); } else { programCounter += 2; } }
        private void CALLZnn() { if (fZ) { CALLnn(); } else { programCounter += 2; } }
        private void CALLNCnn() { if (!fC) { CALLnn(); } else { programCounter += 2; } }
        private void CALLCnn() { if (fC) { CALLnn(); } else { programCounter += 2; } }

        //Return to calling routine
        private void RET()   { programCounter = _mmu.ReadWord(stackPointer); stackPointer += 2; lastMachineClock = 3; }
        private void RETI()  { interruptMasterFlag = true; RET(); }
        private void RETNZ() { lastMachineClock = 1; if (!fZ) { programCounter = _mmu.ReadWord(stackPointer); stackPointer += 2; lastMachineClock += 2; } }
        private void RETZ()  { lastMachineClock = 1; if (fZ) { programCounter = _mmu.ReadWord(stackPointer); stackPointer += 2; lastMachineClock += 2; } }
        private void RETNC() { lastMachineClock = 1; if (!fC) { programCounter = _mmu.ReadWord(stackPointer); stackPointer += 2; lastMachineClock += 2; } }
        private void RETC()  { lastMachineClock = 1; if (fC) { programCounter = _mmu.ReadWord(stackPointer); stackPointer += 2; lastMachineClock += 2; } }

        //Reset to position
        private void RST00() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x00; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST08() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x08; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST10() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x10; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST18() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x18; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST20() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x20; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST28() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x28; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST30() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x30; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST38() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x38; lastMachineClock = 3; lastCycleClock = 12; }
        private void RST40() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x40; lastMachineClock = 3; lastCycleClock = 12; } //Vblank
        private void RST48() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x48; lastMachineClock = 3; lastCycleClock = 12; } //LCDC
        private void RST50() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x50; lastMachineClock = 3; lastCycleClock = 12; } //Timer
        private void RST58() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x58; lastMachineClock = 3; lastCycleClock = 12; } //Serial Transfer
        private void RST60() { stackPointer -= 2; _mmu.WriteWord(stackPointer, programCounter); programCounter = 0x60; lastMachineClock = 3; lastCycleClock = 12; } //Input

        #endregion

        private void NOP() { lastMachineClock = 1; }
        private void HALT() { halt = true; lastMachineClock = 1; }

        private void MAPcb()
        {
            byte i = _mmu.ReadByte(programCounter);
            programCounter++;
            _cbInstructionMap[i]();
        }

        //Disable/enable interrupts
        private void DI() { interruptMasterFlag = false; lastMachineClock = 1; lastCycleClock = 4; }
        private void EI() { interruptMasterFlag = true; lastMachineClock = 1; lastCycleClock = 4; }

        //Undefined
        private void XX() { NOP(); throw new NotImplementedException(); }

        #endregion

        #region Helpers

        #region Register F accessor properties

        //True if math op returns zero or CP instruction = true
        private bool fZ
        {
            get
            {
                return registerF.GetBit(7);
            }
            set
            {
                if (value) { registerF = registerF.SetBit(7); } else { registerF = registerF.ResetBit(7); }
            }
        }

        //True if subtraction was performed in last math instruction
        private bool fN
        {
            get
            {
                return registerF.GetBit(6);
            }
            set
            {
                if (value) { registerF = registerF.SetBit(6); } else { registerF = registerF.ResetBit(6); }
            }
        }

        //True if carry from lower nibble occured in last operation
        private bool fH
        {
            get
            {
                return registerF.GetBit(5);
            }
            set
            {
                if (value) { registerF = registerF.SetBit(5); } else { registerF = registerF.ResetBit(5); }
            }
        }

        //True if carry occurred from last operation or regA < when executing CP
        private bool fC
        {
            get
            {
                return registerF.GetBit(4);
            }
            set
            {
                if (value) { registerF = registerF.SetBit(4); } else { registerF = registerF.ResetBit(4); }
            }
        }

        #endregion

        #region Word accessor properties

        private ushort registerBC
        {
            get
            {
                ushort result = registerC;
                result += (ushort)((ushort)registerB << 8);
                return result;
            }
        }

        private void IncrementBC()
        {
            registerC++;
            if (registerC == 0)
                registerB++;
        }

        private void DecrementBC()
        {
            registerC--;
            if (registerC == 255)
                registerB--;
        }

        private ushort registerDE
        {
            get
            {
                ushort result = registerE;
                result += (ushort)((ushort)registerD << 8);
                return result;
            }
        }

        private void IncrementDE()
        {
            registerE++;
            if (registerE == 0)
                registerD++;
        }

        private void DecrementDE()
        {
            registerE--;
            if (registerE == 255)
                registerD--;
        }

        private ushort registerHL
        {
            get
            {
                ushort result = registerL;
                result += (ushort)((ushort)registerH << 8);
                return result;
            }
        }

        private void IncrementHL()
        {
            registerL++;
            if (registerL == 0)
                registerH++;
        }

        private void DecrementHL()
        {
            registerL--;
            if (registerL == 255)
                registerH--;
        }

        #endregion

        #endregion
    }
}
