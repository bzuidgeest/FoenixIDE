using FoenixIDE.MemoryLocations;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    /**
     * This class will automatically call other methods immediately after writes
     */
    public class MathCoproRegisters : IMemoryMappedDevice
    {
        private Memory<byte> data;

        public int BaseAddress { get; }
        public string Name { get { return this.GetType().ToString(); } }
        public int Size { get { return 47; } }

        public MathCoproRegisters(int baseAddress)
        {
            this.BaseAddress = baseAddress;
        }

        public void SetMemory(Memory<byte> memory)
        {
            this.data = memory;
        }

        /// <summary>
        /// Reads a byte from memory
        /// </summary>
        /// <param name="Address"></param>
        /// <returns></returns>
        public virtual byte ReadByte(int address)
        {
            return data.Span[address];
        }

        public void WriteByte(int address, byte Value)
        {
            data.Span[address] = Value;
            int block = address >> 3;
            switch (block)
            {
                case 0:
                    MathCoproUnsignedMultiplier(0);
                    break;
                case 1:
                    MathCoproSignedMultiplier(8);
                    break;
                case 2:
                    MathCoproUnsignedDivider(0x10);
                    break;
                case 3:
                    MathCoproSignedDivider(0x18);
                    break;
                case 4:
                    MathCoproSignedAdder(0x20);
                    break;
                case 5:
                    MathCoproSignedAdder(0x20);
                    break;
            }
        }

        void MathCoproUnsignedMultiplier(int baseAddr)
        {
            uint acc1 = (uint)((data.Span[baseAddr + 1] << 8) + data.Span[baseAddr]);
            uint acc2 = (uint)((data.Span[baseAddr + 3] << 8) + data.Span[baseAddr + 2]);
            uint result = acc1 * acc2;
            data.Span[baseAddr + 4] = (byte)(result & 0xFF);
            data.Span[baseAddr + 5] = (byte)(result >> 8 & 0xFF);
            data.Span[baseAddr + 6] = (byte)(result >> 16 & 0xFF);
            data.Span[baseAddr + 7] = (byte)(result >> 24 & 0xFF);
        }

        void MathCoproSignedMultiplier(int baseAddr)
        {
            int acc1 = (data.Span[baseAddr + 1] << 8) + data.Span[baseAddr];
            int acc2 = (data.Span[baseAddr + 3] << 8) + data.Span[baseAddr + 2];
            int result = acc1 * acc2;
            data.Span[baseAddr + 4] = (byte)(result & 0xFF);
            data.Span[baseAddr + 5] = (byte)(result >> 8 & 0xFF);
            data.Span[baseAddr + 6] = (byte)(result >> 16 & 0xFF);
            data.Span[baseAddr + 7] = (byte)(result >> 24 & 0xFF);
        }

        void MathCoproUnsignedDivider(int baseAddr)
        {
            uint acc1 = (uint)((data.Span[baseAddr + 1] << 8) + data.Span[baseAddr]);
            uint acc2 = (uint)((data.Span[baseAddr + 3] << 8) + data.Span[baseAddr + 2]);
            uint result = 0;
            uint remainder = 0;
            if (acc1 != 0)
            {
                result = acc2 / acc1;
                remainder = acc2 % acc1;
            }
            data.Span[baseAddr + 4] = (byte)(result & 0xFF);
            data.Span[baseAddr + 5] = (byte)(result >> 8 & 0xFF);
            data.Span[baseAddr + 6] = (byte)(remainder & 0xFF);
            data.Span[baseAddr + 7] = (byte)(remainder >> 8 & 0xFF);
        }

        void MathCoproSignedDivider(int baseAddr)
        {
            int acc1 = (data.Span[baseAddr + 1] << 8) + data.Span[baseAddr];
            int acc2 = (data.Span[baseAddr + 3] << 8) + data.Span[baseAddr + 2];
            int result = 0;
            int remainder = 0;
            if (acc1 != 0)
            {
                result = acc2 / acc1;
                remainder = acc2 % acc1;
            }
            data.Span[baseAddr + 4] = (byte)(result & 0xFF);
            data.Span[baseAddr + 5] = (byte)(result >> 8 & 0xFF);
            data.Span[baseAddr + 6] = (byte)(remainder & 0xFF);
            data.Span[baseAddr + 7] = (byte)(remainder >> 8 & 0xFF);
        }

        // This function gets called whenever we write to the Math Coprocessor address space
        void MathCoproSignedAdder(int baseAddr)
        {
            int acc1 = (data.Span[baseAddr + 3] << 24) + (data.Span[baseAddr + 2] << 16) + (data.Span[baseAddr + 1] << 8) + data.Span[baseAddr];
            int acc2 = (data.Span[baseAddr + 7] << 24) + (data.Span[baseAddr + 6] << 16) + (data.Span[baseAddr + 5] << 8) + data.Span[baseAddr + 4];
            int result = acc1 + acc2;
            data.Span[baseAddr + 8] = (byte)(result & 0xFF);
            data.Span[baseAddr + 9] = (byte)(result >> 8 & 0xFF);
            data.Span[baseAddr + 10] = (byte)(result >> 16 & 0xFF);
            data.Span[baseAddr + 11] = (byte)(result >> 24 & 0xFF);
        }
    }
}
