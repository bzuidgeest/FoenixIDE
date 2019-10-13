using FoenixIDE.Basic;
using FoenixIDE.MemoryLocations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class KeyboardRegister : IMemoryMappedDevice
    {
        private Memory<byte> data;

        public int BaseAddress { get; }
        public string Name { get { return this.GetType().ToString(); } }
        public int Size { get { return 5; } }

        public KeyboardRegister(int baseAddress)
        {
            this.BaseAddress = baseAddress;
        }

        public void SetMemory(Memory<byte> memory)
        {
            this.data = memory;
        }

        // This is used to simulate the Keyboard Register
        public void WriteByte(int address, byte Value)
        {
            // In order to avoid an infinite loop, we write to the device directly
            data.Span[address] = Value;
            switch (address)
            {
                case 0:
                    byte command = data.Span[0];
                    switch (command)
                    {
                        case 0x69:
                            data.Span[4] = 1;
                            break;
                        case 0xEE: // echo command
                            data.Span[4] = 1;
                            break;
                        case 0xF4:
                            data.Span[0] = 0xFA;
                            data.Span[4] = 1;
                            break;
                        case 0xF6:
                            data.Span[4] = 1;
                            break;
                    }
                    break;
                case 4:
                    byte reg = data.Span[4];
                    switch (reg)
                    {
                        case 0x20:
                            data.Span[4] = 1;
                            break;
                        case 0x60:
                            data.Span[4] = 0;
                            break;
                        case 0xAA:
                            data.Span[0] = 0x55;
                            data.Span[4] = 1;
                            break;
                        case 0xA8:
                            data.Span[4] = 1;
                            break;
                        case 0xA9:
                            data.Span[0] = 0;
                            data.Span[4] = 1;
                            break;
                        case 0xAB:
                            data.Span[0] = 0;
                            break;
                        case 0xD4:
                            data.Span[4] = 1;
                            break;
                    }
                    break;
            }
        }

        public byte ReadByte(int address)
        {
            // Whenever the buffer is read, set the buffer to empty.
            if (address == 0)
            {
                data.Span[4] = 0;
            }
            return data.Span[address];
        }
        public void WriteKey(FoenixSystem kernel, ScanCode key)
        {
            // Check if the Keyboard interrupt is allowed
            byte mask = FoenixSystem.Current.MemoryManager.ReadByte(MemoryMap.INT_MASK_REG1);
            if ((~mask & 1) == 1)
            {
                FoenixSystem.Current.MemoryManager.WriteByte(MemoryMap.KBD_DATA_BUF, (byte)key);
                FoenixSystem.Current.MemoryManager.WriteByte(MemoryMap.KBD_DATA_BUF + 4, 0);
                // Set the Keyboard Interrupt
                byte IRQ1 = FoenixSystem.Current.MemoryManager.ReadByte(MemoryMap.INT_PENDING_REG1);
                IRQ1 |= 1;
                FoenixSystem.Current.MemoryManager.WriteByte(MemoryMap.INT_PENDING_REG1, IRQ1);
                kernel.CPU.Pins.IRQ = true;
            }
        }
    }
}
