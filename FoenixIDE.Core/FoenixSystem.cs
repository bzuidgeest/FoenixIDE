using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FoenixIDE.Processor;
using FoenixIDE.Display;
using System.Threading;
using FoenixIDE.MemoryLocations;
using FoenixIDE.Simulator.Devices;
using FoenixIDE.Simulator.FileFormat;
using FoenixIDE.UI;
using FoenixIDE.Simulator.Devices.SDCard;
using FoenixIDE.Simulator;

namespace FoenixIDE
{
    public sealed class FoenixSystem
    {
        public MemoryManager MemoryManager { get; set; }
        
        /* should replace this fixed access points with somthing dynamix */
        //public BasicMemory RAM;
        public BasicMemory VICKY;
        public BasicMemory VIDEO;
        public BasicMemory FLASH;
        public BasicMemory BEATRIX;
        public MathCoproRegisters MATH = null;
        public Codec CODEC = null;
        public KeyboardRegister KEYBOARD = null;
        public SDCardRegister SDCARD = null;
        public InterruptController INTERRUPT = null;
        public UART UART1 = null;
        public UART UART2 = null;
        public OPL2 OPL2 = null;
        public MPU401 MPU401 = null;

        public Processor.CPU CPU = null;
        private Gpu gpu = null;

        public DeviceEnum InputDevice = DeviceEnum.Keyboard;
        public DeviceEnum OutputDevice = DeviceEnum.Screen;

        public Thread CPUThread = null;
        //private String defaultKernel = @"ROMs\kernel.hex";

        public ResourceChecker Resources;
        public Processor.Breakpoints Breakpoints;
        public ListFile lstFile;


        //https://csharpindepth.com/articles/singleton 
        private static FoenixSystem instance = null;
        private static readonly object padlock = new object();
        public static FoenixSystem Current
        {
            get
            {
                lock (padlock)
                {
                    if (instance == null)
                    {
                        instance = new FoenixSystem();
                    }
                    return instance;
                }
            }
        }

        public Gpu GPU
        {
            get
            {
                return gpu;
            }
        }

        private FoenixSystem()
        {
            gpu = new Gpu();

            MemoryManager = new MemoryManager();


            //RAM = new BasicMemory("Ram", MemoryMap.RAM_START, MemoryMap.RAM_SIZE);   // 2MB RAM - extensible to 4MB
            //MemoryManager.AddDevice(RAM);
            VICKY = new BasicMemory("Vicky", MemoryMap.VICKY_START, MemoryMap.VICKY_SIZE);   // 60K
            MemoryManager.AddDevice(VICKY);
            VIDEO = new BasicMemory("Video", MemoryMap.VIDEO_START, MemoryMap.VIDEO_SIZE - 1); // 4MB Video
            MemoryManager.AddDevice(VIDEO);
            FLASH = new BasicMemory("Flash", MemoryMap.FLASH_START, MemoryMap.FLASH_SIZE); // 8MB RAM
            MemoryManager.AddDevice(FLASH);
            BEATRIX = new BasicMemory("Beatrix", MemoryMap.BEATRIX_START, MemoryMap.BEATRIX_SIZE); // 4K 
            MemoryManager.AddDevice(BEATRIX);

            // Special devices
            MATH = new MathCoproRegisters(MemoryMap.MATH_START);
            MemoryManager.AddDevice(MATH); // 47 bytes

            // This register is only a single byte but we allow writing a word
            CODEC = new Codec(MemoryMap.CODEC_START);
            MemoryManager.AddDevice(CODEC); // 4 bytes
            KEYBOARD = new KeyboardRegister(MemoryMap.KBD_DATA_BUF);
            MemoryManager.AddDevice(KEYBOARD); // 5 bytes
            SDCARD = new SDCardRegister(MemoryMap.SDCARD_DATA);
            MemoryManager.AddDevice(SDCARD); // 2 bytes
            INTERRUPT = new InterruptController(MemoryMap.INT_PENDING_REG0);
            MemoryManager.AddDevice(INTERRUPT); // 3 bytes
            UART1 = new UART(1, MemoryMap.UART1_REGISTERS);
            MemoryManager.AddDevice(UART1); // 8 bytes
            UART2 = new UART(2, MemoryMap.UART2_REGISTERS);
            MemoryManager.AddDevice(UART2); // 8 bytes
            OPL2 = new OPL2(MemoryMap.OPL2_S_BASE);
            //MemoryManager.AddDevice(OPL2); // 256 bytes
            MPU401 = new MPU401(MemoryMap.MPU401_REGISTERS);
            MemoryManager.AddDevice(MPU401); // 2 bytes

            this.CPU = new CPU(MemoryManager);
            this.CPU.SimulatorCommand += CPU_SimulatorCommand;

            gpu.VRAM = VIDEO;
            //gpu.RAM = RAM; 
            gpu.VICKY = VICKY;
            
            // This fontset is loaded just in case the kernel doesn't provide one.
            gpu.LoadFontSet("Foenix", @"Resources\Bm437_PhoenixEGA_8x8.bin", 0, CharacterSet.CharTypeCodes.ASCII_PET, CharacterSet.SizeCodes.Size8x8);
        }

        private void CPU_SimulatorCommand(int EventID)
        {
            switch (EventID)
            {
                case SimulatorCommands.RefreshDisplay:
                    gpu.RefreshTimer = 0;
                    break;
                default:
                    break;
            }
        }

        public void ResetCPU(bool ResetMemory)
        {
            CPU.Halt();

            gpu.Refresh();

            // This fontset is loaded just in case the kernel doesn't provide one.
            gpu.LoadFontSet("Foenix", @"Resources\Bm437_PhoenixEGA_8x8.bin", 0, CharacterSet.CharTypeCodes.ASCII_PET, CharacterSet.SizeCodes.Size8x8);

            if (Configuration.Current.StartUpHexFile.EndsWith(".fnxml", true, null))
            {
                FoeniXmlFile fnxml = new FoeniXmlFile(MemoryManager, Resources, CPUWindow.Instance.breakpoints);
                fnxml.Load(Configuration.Current.StartUpHexFile);
            }
            else
            {
                Configuration.Current.StartUpHexFile = HexFile.Load(MemoryManager, Configuration.Current.StartUpHexFile);
                if (Configuration.Current.StartUpHexFile != null)
                {
                    if (ResetMemory)
                    {
                        lstFile = new ListFile(Configuration.Current.StartUpHexFile);
                    }
                    else
                    {
                        // TODO: We should really ensure that there are no duplicated PC in the list
                        ListFile tempList = new ListFile(Configuration.Current.StartUpHexFile);
                        lstFile.Lines.InsertRange(0, tempList.Lines);
                    }
                }
            }

            // If the reset vector is not set in Bank 0, but it is set in Bank 18, then copy bank 18 into bank 0.
            if (MemoryManager.ReadLong(0xFFE0) == 0 && MemoryManager.ReadLong(0x18_FFE0) != 0)
            {
                MemoryManager.Copy(0x180000, MemoryMap.RAM_START, MemoryMap.PAGE_SIZE);
                // See if lines of code exist in the 0x18_0000 to 0x18_FFFF block
                List<DebugLine> copiedLines = new List<DebugLine>();
                if (lstFile.Lines.Count > 0)
                {
                    List<DebugLine> tempLines = new List<DebugLine>();
                    foreach (DebugLine line in lstFile.Lines)
                    {
                        if (line.PC >= 0x18_0000 && line.PC < 0x19_0000)
                        {
                            DebugLine dl = (DebugLine)line.Clone();
                            dl.PC -= 0x18_0000;
                            copiedLines.Add(dl);
                        }
                    }
                }
                if (copiedLines.Count > 0)
                {
                    lstFile.Lines.InsertRange(0, copiedLines);
                }
            }
            CPU.Reset();
        }

        public void SetKernel(String value)
        {
            Configuration.Current.StartUpHexFile = value;
        }
    }
}
