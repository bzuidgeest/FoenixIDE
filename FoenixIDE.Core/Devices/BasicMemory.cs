using FoenixIDE.Simulator.FileFormat;
using FoenixIDE.MemoryLocations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class BasicMemory : IMemoryMappedDevice
    {
        private Memory<byte> data;

        //private readonly int length;
        //private readonly int endAddress;

        public int BaseAddress { get; }
        //public string Name { get { return this.GetType().ToString(); } }
        public string Name { get; }
        public int Size { get; }

        public BasicMemory(string name, int baseAddress, int size)
        {
            this.Name = name;
            this.BaseAddress = baseAddress;
            this.Size = size;
        }

        public void SetMemory(Memory<byte> memory)
        {
            this.data = memory;
        }

        public BasicMemory()
        {
            Name = "Default";
            data = new byte[0xFF_FFFF];
            Size = 0xFF_FFFF;
        }

        /// <summary>
        /// Clear all the bytes in the memory array.
        /// </summary>
        public void Zero()
        {
            data.Span.Fill(0);
        }

        /// <summary>
        /// Reads a byte from memory
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public virtual byte ReadByte(int Address)
        {
            
            return data.Span[Address];
        }

        /// <summary>
        /// Reads a 16-bit word from memory
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public int ReadWord(int Address)
        {
            return ReadByte(Address) + (ReadByte(Address + 1) << 8);
        }

        /// <summary>
        /// Read a 24-bit long from memory
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        internal int ReadLong(int Address)
        {
            return ReadByte(Address) + (ReadByte(Address + 1) << 8) + (ReadByte(Address + 2) << 16);
        }

        internal void Load(byte[] SourceData, int SrcStart, int DestStart, int length)
        {
            for (int i = 0; i < length; i++)
            {
                this.data.Span[DestStart + i] = SourceData[SrcStart + i];
            }
        }

        public virtual void WriteByte(int Address, byte Value)
        {
            data.Span[Address] = Value;
        }

        public void WriteWord(int Address, int Value)
        {
            WriteByte(Address, (byte)(Value & 0xff));
            WriteByte(Address + 1, (byte)(Value >> 8 & 0xff));
        }

       
    }
}
