
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

using NAudio.Wave;
using System;
using System.Runtime.InteropServices;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    public partial class DosBoxOPL : IOPL, IWaveProvider
    {
        [StructLayout(LayoutKind.Explicit)]
        struct Reg
        {
            [FieldOffset(0)]
            public byte Dual1;

            [FieldOffset(1)]
            public byte Dual2;

            [FieldOffset(0)]
            public ushort Normal;
        }


        public DosBoxOPL(OPLType type)
        {
            _type = type;
        }

        #region IOpl implementation

        public bool Init(int rate)
        {
            free();

            _reg = new Reg();
            for (int i = 0; i < _chip.Length; i++)
            {
                _chip[i] = new OPLChip();
            }
            _emulator = new DosBoxOPL.Chip();

            InitTables();

            _emulator.Setup(rate);

            if (_type == OPLType.DualOpl2)
            {
                // Setup opl3 mode in the hander
                _emulator.WriteReg(0x105, 1);
            }

            _rate = rate;
            return true;
        }

        void reset()
        {
            Init(_rate);
        }
               
        public void WriteReg(int r, int v)
        {
            int tempReg = 0;
            switch (_type)
            {
                case OPLType.Opl2:
                case OPLType.DualOpl2:
                case OPLType.Opl3:
                    // We can't use _handler->writeReg here directly, since it would miss timer changes.

                    // Backup old setup register
                    tempReg = _reg.Normal;

                    // We directly allow writing to secondary OPL3 registers by using
                    // register values >= 0x100.
                    if (_type == OPLType.Opl3 && r >= 0x100)
                    {
                        // We need to set the register we want to write to via port 0x222,
                        // since we want to write to the secondary register set.
                        write(0x222, r);
                        // Do the real writing to the register
                        write(0x223, v);
                    }
                    else
                    {
                        // We need to set the register we want to write to via port 0x388
                        write(0x388, r);
                        // Do the real writing to the register
                        write(0x389, v);
                    }

                    // Restore the old register
                    if (_type == OPLType.Opl3 && tempReg >= 0x100)
                    {
                        write(0x222, tempReg & ~0x100);
                    }
                    else
                    {
                        write(0x388, tempReg);
                    }
                    break;
            }
        }

        public void ReadBuffer(short[] buffer, int pos, int length)
        {
            // For stereo OPL cards, we divide the sample count by 2,
            // to match stereo AudioStream behavior.
            if (_type != OPLType.Opl2)
                length >>= 1;

            const uint bufferLength = 512;
            var tempBuffer = new int[bufferLength * 2];

            if (_emulator.opl3Active != 0)
            {
                while (length > 0)
                {
                    uint readSamples = (uint)Math.Min(length, bufferLength);

                    _emulator.GenerateBlock3(readSamples, tempBuffer);

                    for (uint i = 0; i < (readSamples << 1); ++i)
                        buffer[pos + i] = (short)tempBuffer[i];

                    pos += (int)(readSamples << 1);
                    length -= (int)readSamples;
                }
            }
            else
            {
                while (length > 0)
                {
                    uint readSamples = (uint)Math.Min(length, bufferLength << 1);

                    _emulator.GenerateBlock2(readSamples, tempBuffer);

                    for (var i = 0; i < readSamples; ++i)
                        buffer[pos + i] = (short)tempBuffer[i];

                    pos += (int)readSamples;
                    length -= (int)readSamples;
                }
            }
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int _offset = offset;
            int _count = count / 2;

            // For stereo OPL cards, we divide the sample count by 2,
            // to match stereo AudioStream behavior.
            //if (_type != OPLType.Opl2)
            //    _count >>= 1;

            const uint bufferLength = 512;
            var tempBuffer = new int[bufferLength * 2];

            if (_emulator.opl3Active != 0)
            {
                while (_count > 0)
                {
                    uint readSamples = (uint)Math.Min(_count, bufferLength);

                    _emulator.GenerateBlock3(readSamples, tempBuffer);

                    int x = 0;
                    for (uint i = 0; i < (readSamples << 1); ++i)
                    {
                        //buffer[_offset + x ] = (byte)(tempBuffer[i] & 0xFF);
                        //buffer[_offset + x + 1] = (byte)(tempBuffer[i] >> 8 & 0xFF);
                        buffer[_offset + x] = (byte)x;
                        buffer[_offset + x + 1] = (byte)(x + 1);
                        x = x + 2;
                    }

                    _offset += (int)(readSamples * 2 << 1);
                    _count -= (int)readSamples;
                }
            }
            else
            {
                while (_count > 0)
                {
                    uint readSamples = (uint)Math.Min(_count, bufferLength << 1);

                    _emulator.GenerateBlock2(readSamples, tempBuffer);


                    int x = 0;
                    for (var i = 0; i < readSamples; i++)
                    {
                        buffer[_offset + x] = (byte)(tempBuffer[i] & 0xFF);
                        buffer[_offset + x + 1] = (byte)(tempBuffer[i] >> 8 & 0xFF);
                        x = x + 2;
                    }

                    _offset += (int)readSamples * 2;
                    _count -= (int)readSamples;
                }
            }

            return count;
        }
                
        public bool IsStereo
        {
            get
            {
                return _type != OPLType.Opl2;
            }
        }

        public WaveFormat WaveFormat
        {
            get
            {
                return new WaveFormat(_rate, 16, _type != OPLType.Opl2 ? 2 : 1);
            }
        }

        #endregion
        
        void dualWrite(byte index, byte reg, byte val)
        {
            // Make sure you don't use opl3 features
            // Don't allow write to disable opl3
            if (reg == 5)
                return;

            // Only allow 4 waveforms
            if (reg >= 0xE0 && reg <= 0xE8)
                val &= 3;

            // Write to the timer?
            if (_chip[index].Write(reg, val))
                return;

            // Enabling panning
            if (reg >= 0xC0 && reg <= 0xC8)
            {
                val &= 15;
                val |= (byte)(index != 0 ? 0xA0 : 0x50);
            }

            uint fullReg = (uint)(reg + (index != 0 ? 0x100 : 0));
            _emulator.WriteReg(fullReg, val);
        }

        public byte ReadRegister(int port)
        {
            switch (_type)
            {
                case OPLType.Opl2:
                    if ((port & 1) == 0)
                        //Make sure the low bits are 6 on opl2
                        return (byte)(_chip[0].read() | 0x6);
                    break;
                case OPLType.Opl3:
                    if ((port & 1) == 0)
                        return (byte)(_chip[0].read());
                    break;
                case OPLType.DualOpl2:
                    // Only return for the lower ports
                    if ((port & 1) == 0)
                        return 0xff;
                    // Make sure the low bits are 6 on opl2
                    return (byte)(_chip[(port >> 1) & 1].read() | 0x6);
            }
            return 0;
        }

        void write(int port, int val)
        {
            if ((port & 1) != 0)
            {
                switch (_type)
                {
                    case OPLType.Opl2:
                    case OPLType.Opl3:
                        if (!_chip[0].Write(_reg.Normal, (byte)val))
                            _emulator.WriteReg(_reg.Normal, (byte)val);
                        break;
                    case OPLType.DualOpl2:
                        // Not a 0x??8 port, then write to a specific port
                        if ((port & 0x8) == 0)
                        {
                            byte index = (byte)((port & 2) >> 1);
                            dualWrite(index, index == 0 ? _reg.Dual1 : _reg.Dual2, (byte)val);
                        }
                        else
                        {
                            //Write to both ports
                            dualWrite(0, _reg.Dual1, (byte)val);
                            dualWrite(1, _reg.Dual2, (byte)val);
                        }
                        break;
                }
            }
            else
            {
                // Ask the handler to write the address
                // Make sure to clip them in the right range
                switch (_type)
                {
                    case OPLType.Opl2:
                        _reg.Normal = (ushort)(_emulator.WriteAddr((uint)port, (byte)val) & 0xff);
                        break;
                    case OPLType.Opl3:
                        _reg.Normal = (ushort)(_emulator.WriteAddr((uint)port, (byte)val) & 0x1ff);
                        break;
                    case OPLType.DualOpl2:
                        // Not a 0x?88 port, when write to a specific side
                        if (0 == (port & 0x8))
                        {
                            byte index = (byte)((port & 2) >> 1);
                            if (index == 0)
                            {
                                _reg.Dual1 = (byte)(val & 0xff);
                            }
                            else
                            {
                                _reg.Dual2 = (byte)(val & 0xff);
                            }
                        }
                        else
                        {
                            _reg.Dual1 = (byte)(val & 0xff);
                            _reg.Dual2 = (byte)(val & 0xff);
                        }
                        break;
                }
            }
        }

        void free()
        {
            _emulator = null;
        }

        /*
        void generateSamples(short[] buffer, int length)
        {
            // For stereo OPL cards, we divide the sample count by 2,
            // to match stereo AudioStream behavior.
            if (_type != OPLType.Opl2)
                length >>= 1;

            const uint bufferLength = 512;
            int tempBuffer[bufferLength * 2];

            if (_emulator->opl3Active)
            {
                while (length > 0)
                {
                    const uint readSamples = MIN<uint>(length, bufferLength);

                    _emulator->GenerateBlock3(readSamples, tempBuffer);

                    for (uint i = 0; i < (readSamples << 1); ++i)
                        buffer[i] = tempBuffer[i];

                    buffer += (readSamples << 1);
                    length -= readSamples;
                }
            }
            else
            {
                while (length > 0)
                {
                    const uint readSamples = MIN<uint>(length, bufferLength << 1);

                    _emulator->GenerateBlock2(readSamples, tempBuffer);

                    for (uint i = 0; i < readSamples; ++i)
                        buffer[i] = tempBuffer[i];

                    buffer += readSamples;
                    length -= readSamples;
                }
            }
        }
        */
        OPLType _type;
        int _rate;

        DosBoxOPL.Chip _emulator;
        OPLChip[] _chip = new OPLChip[2];
        Reg _reg;
    }
} 

