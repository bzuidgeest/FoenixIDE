/* ScummVM - Graphic Adventure Engine
*
* ScummVM is the legal property of its developers, whose names
* are too numerous to list here. Please refer to the COPYRIGHT
* file distributed with this source distribution.
*
* This program is free software; you can redistribute it and/or
* modify it under the terms of the GNU General Public License
* as published by the Free Software Foundation; either version 2
* of the License, or (at your option) any later version.
*
* This program is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with this program; if not, write to the Free Software
* Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301, USA.
*
*/


using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace FoenixIDE.Simulator.Devices.Audio.HardSynth.OPL.OPLXLPT
{
    public class OPLXLPT : IOPL
    {
        LPTport port = LPTport.LPT1;

        OPLType type;

        public bool IsStereo
        {
            get
            {
                return type != OPLType.Opl2;
            }
        }

        public OPLXLPT(OPLType type, LPTport port)
        {
            this.type = type;
            this.port = port;
            Init(0);
        }

        ~OPLXLPT()
        {
            Reset();
        }

        public bool Init(int rate)
        {
            bool result = LPT.Init();
            Reset();
            return result;
        }

        public void Reset()
        {
            ushort Reg;
            //float FnlVolBak;

            //FnlVolBak = FinalVol;
            //FinalVol = 1.0f;
            //memset(OPLRegForce, 0x01, 0x200);

            WriteReg(0x105, 0x01); // OPL3 Enable
            WriteReg(0x001, 0x20); // Test Register
            WriteReg(0x002, 0x00); // Timer 1
            WriteReg(0x003, 0x00); // Timer 2
            WriteReg(0x004, 0x00); // IRQ Mask Clear
            WriteReg(0x104, 0x00); // 4-Op-Mode Disable
            WriteReg(0x008, 0x00); // Keyboard Split

            // make sure all internal calulations finish sound generation
            for (Reg = 0x00; Reg < 0x09; Reg++)
            {
                WriteReg(0x0C0 | Reg, 0x00);   // silence all notes (OPL3)
                WriteReg(0x1C0 | Reg, 0x00);
            }
            for (Reg = 0x00; Reg < 0x16; Reg++)
            {
                if ((Reg & 0x07) >= 0x06)
                    continue;
                WriteReg(0x040 | Reg, 0x3F);   // silence all notes (OPL2)
                WriteReg(0x140 | Reg, 0x3F);

                WriteReg(0x080 | Reg, 0xFF);   // set Sustain/Release Rate to FASTEST
                WriteReg(0x180 | Reg, 0xFF);
                WriteReg(0x060 | Reg, 0xFF);
                WriteReg(0x160 | Reg, 0xFF);

                WriteReg(0x020 | Reg, 0x00);   // NULL the rest
                WriteReg(0x120 | Reg, 0x00);

                WriteReg(0x0E0 | Reg, 0x00);
                WriteReg(0x1E0 | Reg, 0x00);
            }
            WriteReg(0x0BD, 0x00); // Rhythm Mode
            for (Reg = 0x00; Reg < 0x09; Reg++)
            {
                WriteReg(0x0B0 | Reg, 0x00);   // turn all notes off (-> Release Phase)
                WriteReg(0x1B0 | Reg, 0x00);
                WriteReg(0x0A0 | Reg, 0x00);
                WriteReg(0x1A0 | Reg, 0x00);
            }

            // although this would be a more proper reset, it sometimes produces clicks
            /*for (Reg = 0x020; Reg <= 0x0FF; Reg ++)
                WriteReg(Reg, 0x00);
            for (Reg = 0x120; Reg <= 0x1FF; Reg ++)
                WriteReg(Reg, 0x00);*/

            // Now do a proper reset of all other registers.
            for (Reg = 0x040; Reg < 0x0A0; Reg++)
            {
                if ((Reg & 0x07) >= 0x06 || (Reg & 0x1F) >= 0x18)
                    continue;
                WriteReg(0x000 | Reg, 0x00);
                WriteReg(0x100 | Reg, 0x00);
            }
            for (Reg = 0x00; Reg < 0x09; Reg++)
            {
                WriteReg(0x0C0 | Reg, 0x30);   // must be 30 to make OPL2 VGMs sound on OPL3
                WriteReg(0x1C0 | Reg, 0x30);   // if they don't send the C0 reg
            }

            //memset(OPLRegForce, 0x01, 0x200);
            //FinalVol = FnlVolBak;

            return;
        }

        public byte ReadRegister(int port)
        {
            return LPT.inportb((short)port);
        }

        public void WriteReg(int reg, int data)
        {
            if (type == OPLType.Opl2)
            {
                OPL2LPTWrite((ushort)reg, (byte)data);
            }
            else
            {
                OPL3LPTWrite((ushort)reg, (byte)data);
            }
        }

        void OPL2LPTWrite(ushort reg, byte data)
        {
            short lpt_data;
            short lpt_ctrl;
            if (port == 0)
            {
                return;
            }

            lpt_data = (short)port;
            lpt_ctrl = (short)(port + 2);

            /* Select OPL2 register */
            LPT.outportb(lpt_data, (short)reg);
            LPT.outportb(lpt_ctrl, 13);
            LPT.outportb(lpt_ctrl, 9);
            LPT.outportb(lpt_ctrl, 13);

            /* Wait at least 3.3 microseconds */
            //for (i = 0; i < 6; i++)
            //for (i = 0; i < 1; i++)
            //{
            //    LPT.inportb(lpt_ctrl);
            //}

            /* Set value */
            LPT.outportb(lpt_data, data);
            LPT.outportb(lpt_ctrl, 12);
            LPT.outportb(lpt_ctrl, 8);
            LPT.outportb(lpt_ctrl, 12);

            /* Wait at least 23 microseconds */
            //for (i = 0; i < 35; i++)
            //for (i = 0; i < 1; i++)
            //{
            //    LPT.inportb(lpt_ctrl);
            //}
        }

        void OPL3LPTWrite(ushort reg, byte data)
        {
            int i;
            short lpt_data;
            short lpt_ctrl;
            if (port == 0)
            {
                return;
            }
            lpt_data = (short)port;
            lpt_ctrl = (short)(port + 2);

            /* Select OPL3 register */
            LPT.outportb(lpt_data, (short)(reg & 0xFF));
            if (reg < 0x100)
            {
                LPT.outportb(lpt_ctrl, 13);
                LPT.outportb(lpt_ctrl, 9);
                LPT.outportb(lpt_ctrl, 13);
            }
            else
            {
                LPT.outportb(lpt_ctrl, 5);
                LPT.outportb(lpt_ctrl, 1);
                LPT.outportb(lpt_ctrl, 5);
            }

            /* Wait at least 3.3 microseconds */
            for (i = 0; i < 6; i++)
            {
                LPT.inportb(lpt_ctrl);
            }

            /* Set value */
            LPT.outportb(lpt_data, data);
            LPT.outportb(lpt_ctrl, 12);
            LPT.outportb(lpt_ctrl, 8);
            LPT.outportb(lpt_ctrl, 12);

            /* 3.3 microseconds is sufficient here as well for OPL3 */
            for (i = 0; i < 6; i++)
            {
                LPT.inportb(lpt_ctrl);
            }
        }

        public void ReadBuffer(short[] buffer, int pos, int length)
        {
            throw new Exception("ReadBuffer not supported on Hardware OPL");
        }
    }
}

