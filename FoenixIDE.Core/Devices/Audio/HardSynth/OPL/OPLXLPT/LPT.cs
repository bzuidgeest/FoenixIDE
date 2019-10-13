using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.HardSynth.OPL.OPLXLPT
{
    public enum LPTport
    {
        LPT1 = 0x3BC,
        LPT2 = 0x378,
        LPT3 = 0x278
    }

    public static class LPT
    {
        [DllImport("inpout32.dll")]
        public static extern UInt32 IsInpOutDriverOpen();

        [DllImport("inpout32.dll")]
        private static extern void Out32(short PortAddress, short Data);

        [DllImport("inpout32.dll")]
        private static extern char Inp32(short PortAddress);

        [DllImport("inpout32.dll")]
        public static extern void DlPortWritePortUshort(short PortAddress, ushort Data);

        [DllImport("inpout32.dll")]
        public static extern ushort DlPortReadPortUshort(short PortAddress);

        [DllImport("inpout32.dll")]
        public static extern void DlPortWritePortUlong(int PortAddress, uint Data);

        [DllImport("inpout32.dll")]
        public static extern uint DlPortReadPortUlong(int PortAddress);

        [DllImport("inpoutx64.dll")]
        public static extern bool GetPhysLong(ref int PortAddress, ref uint Data);

        [DllImport("inpoutx64.dll")]
        public static extern bool SetPhysLong(ref int PortAddress, ref uint Data);


        [DllImport("inpoutx64.dll", EntryPoint = "IsInpOutDriverOpen")]
        public static extern UInt32 IsInpOutDriverOpen_x64();

        [DllImport("inpoutx64.dll", EntryPoint = "Out32")]
        public static extern void Out32_x64(short PortAddress, short Data);

        [DllImport("inpoutx64.dll", EntryPoint = "Inp32")]
        public static extern char Inp32_x64(short PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUshort")]
        public static extern void DlPortWritePortUshort_x64(short PortAddress, ushort Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUshort")]
        public static extern ushort DlPortReadPortUshort_x64(short PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "DlPortWritePortUlong")]
        public static extern void DlPortWritePortUlong_x64(int PortAddress, uint Data);
        [DllImport("inpoutx64.dll", EntryPoint = "DlPortReadPortUlong")]
        public static extern uint DlPortReadPortUlong_x64(int PortAddress);

        [DllImport("inpoutx64.dll", EntryPoint = "GetPhysLong")]
        public static extern bool GetPhysLong_x64(ref int PortAddress, ref uint Data);
        [DllImport("inpoutx64.dll", EntryPoint = "SetPhysLong")]
        public static extern bool SetPhysLong_x64(ref int PortAddress, ref uint Data);


        public static bool IsLPTX64 { get; private set; } = false;

        public static bool Init()
        {
            try
            {
                uint nResult = 0;
                try
                {
                    nResult = LPT.IsInpOutDriverOpen();
                    return true;
                }
                catch (BadImageFormatException)
                {
                    nResult = LPT.IsInpOutDriverOpen_x64();
                    if (nResult != 0)
                        IsLPTX64 = true;
                    else
                        return false;
                }
            }
            catch (DllNotFoundException ex)
            {
                return false;
            }

            return false;
        }

        public static void WriteControl(LPTport port, int value)
        {
            if (IsLPTX64)
            {
                Out32_x64((short)(port + 2), (short)value);
                //Out32_x64((short)(port + 4), (short)value);
            }
            else
            {
                Out32((short)(port + 2), (short)value);
                //Out32((short)(port + 4), (short)value);
            }
        }

        public static void WriteStatus(LPTport port, int value)
        {
            if (IsLPTX64)
            {
                Out32_x64((short)(port + 1), (short)value);
            }
            else
            {
                Out32((short)(port + 1), (short)value);
            }
        }

        public static void WriteData(LPTport port, int value)
        {
            if (IsLPTX64)
            {
                Out32_x64((short)(port), (short)value);
            }
            else
            {
                Out32((short)(port), (short)value);
            }
        }


        public static void outportb(short portAddress, short data)
        {
#if DEBUG
            Debug.WriteLine($"Write: {portAddress:x}, {data:x}");
#endif
            if (IsLPTX64)
            {
                Out32_x64(portAddress, data);
            }
            else
            {
                Out32(portAddress, data);
            }
        }

        public static byte inportb(short portAddress)
        {
#if DEBUG
            Debug.WriteLine($"Read: {portAddress:x}");
#endif
            if (IsLPTX64)
            {
                return (byte)Inp32_x64(portAddress);
            }
            else
            {
                return (byte)Inp32(portAddress);
            }
            
        }
    }
}
