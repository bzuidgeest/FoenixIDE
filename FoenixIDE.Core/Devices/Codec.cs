using FoenixIDE.MemoryLocations;
using FoenixIDE.Simulator.FileFormat;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class Codec : IMemoryMappedDevice
    {
        private Memory<byte> data;

        public int BaseAddress { get; }
        public string Name { get { return this.GetType().ToString(); } }
        public int Size { get { return 4; } }

        public Codec(int baseAddress)
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
            data.Span[Address] = Value;
            //await Task.Delay(200);
            data.Span[2] = 0;
        }
    }
}
