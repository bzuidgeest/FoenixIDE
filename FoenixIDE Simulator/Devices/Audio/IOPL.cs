using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio
{
    public interface IOPL
    {
        bool Init(int rate);

        void WriteReg(int r, int v);

        void ReadBuffer(short[] buffer, int pos, int length);

        bool IsStereo { get; }
    }
}
