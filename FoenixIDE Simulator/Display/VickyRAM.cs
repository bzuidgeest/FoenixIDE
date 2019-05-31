﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FoenixIDE.Simulator.Basic;

namespace FoenixIDE
{
    /// <summary>
    /// Emulation of SuperIO chip which is interface with GAVIN's 
    /// but is ampped into VICKY's address space.
    /// Reads are direct, Writes work like they do on actual hardware
    /// </summary>
    public class Vicky_RAM : MemoryRAM
    {
        #region locals
        private int startAddress; // Starting address and
        private int length;       // address length in memory map
        private FoenixSystem kernel = null;

        #endregion locals

        #region const defs


        #endregion const defs

        /// <summary>
        /// Creates an instance of the SuperIO controller
        /// </summary>
        public Vicky_RAM(int StartAddress, int Length) : base(StartAddress, Length)
        {
            this.startAddress = StartAddress;
            this.length = Length;
            data = new byte[Length];
        }

        /// <summary>
        /// Does a direct read of device RAM/REGISTER 
        /// </summary>
        /// <param name="Address">Register address in device memory space</param>  
        public override byte ReadByte(int Address)
        {
            return data[Address];
        }

        /// <summary>
        /// Provides direct write functionaly to memory or if a postWrite
        /// method has been defined will raise an event to call postWrite
        /// </summary>
        /// <param name="Address">Register address in device memory space</param>
        /// <param name="Value">Value to write into memeory</param> 
        public override void WriteByte(int Address, byte Value)
        {
            if (postWrite != null)
            {
                byte old = data[Address];
                data[Address] = Value;
                postWrite.Invoke(Address, old, Value);
            }
            else
            {
                data[Address] = Value;
            }

        }


        /// <summary>
        /// Sets a reference to the kernel created in main window
        /// </summary>
        /// <param name="kernel">Source of interrupt</param>  
        public void setKernel(FoenixSystem kernel)
        {
            this.kernel = kernel;
        }

    }
}
