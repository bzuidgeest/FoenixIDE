using FoenixIDE.MemoryLocations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class InterruptController : IMemoryMappedDevice
    {
        private Memory<byte> data;

        public int BaseAddress { get; }
        public string Name { get { return this.GetType().ToString(); } }
        public int Size { get { return 3; } } 

        public InterruptController(int baseAddress)
        {
            data = new byte[Size];
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

        public void WriteByte(int address, byte Value)
        {
            // Read the current byte at the address, to detect an edge
            byte old = data.Span[address];
            // If a bit gets set from 0 to 1, leave it.  If a bit gets set a second time, reset to 0.
            byte combo = (byte)(old & Value);
            if (combo > 0)
            {
                data.Span[address] = (byte)(data.Span[address] & (byte)(~combo));
            }
            else
            {
                data.Span[address] = Value;
            }
        }
    }
}
