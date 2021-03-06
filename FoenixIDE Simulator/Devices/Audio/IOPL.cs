﻿using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio
{
    public interface IOPL
    {
        bool Init(int rate);

        void WriteReg(int r, int v);

        byte ReadRegister(int register);

        //void ReadBuffer(short[] buffer, int pos, int length);
        int Read(byte[] buffer, int offset, int count);

        bool IsStereo { get; }
    }
}
