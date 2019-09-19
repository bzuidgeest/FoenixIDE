using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class BasicRegisterEvent : EventArgs
    {
        public int Address { get; }
        public byte Value { get; }

        public BasicRegisterEvent(int address, byte value)
        {
            this.Address = address;
            this.Value = value;
        }
    }

    
}
