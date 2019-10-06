using FoenixIDE.MemoryLocations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class MPU401 : IMemoryMappedDevice
    {
        private Memory<byte> data;

        public int BaseAddress { get; }
        public string Name { get { return this.GetType().ToString(); } }
        public int Size { get { return 2; } }

        public MPU401(int baseAddress)
        {
            this.BaseAddress = baseAddress;
        }

        public void SetMemory(Memory<byte> memory)
        {
            this.data = memory;
        }

        public byte ReadByte(int address)
        {
            return data.Span[address];
        }

        public void WriteByte(int Address, byte Value)
        {
            // MPU401 only accepts two commands $3f and $ff, when this is received, acknowledge the command
            if (Address == 1)
            {
                data.Span[0] = 0xFE;
                data.Span[1] = 0x80;
            }
        }
    }
}
