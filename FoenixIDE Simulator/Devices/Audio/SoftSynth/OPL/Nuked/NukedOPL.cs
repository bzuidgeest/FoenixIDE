using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.Nuked
{
    public class NukedOPL : IOPL, IWaveProvider
    {
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

        OPLType _type;
        int _rate;
        opl3_chip chip;
        uint[] address = new uint[2];

        public NukedOPL(OPLType type)
        {
            _type = type;
            _rate = 0;
        }

        ~NukedOPL()
        {
            //stop();
        }

        public bool Init(int rate)
        {
            _rate = rate;
            //chip = new opl3_chip();
            Nuked.OPL3_Reset(ref chip, _rate);

            if (_type == OPLType.DualOpl2)
            {
                Nuked.OPL3_WriteReg(ref chip, 0x105, 0x01);
            }

            return true;
        }

        void reset()
        {
            //chip = new opl3_chip();
            //Nuked.OPL3_Reset(chip, _rate);
            Init(_rate);
        }

        void write(int port, int val)
        {
            if ((port & 1) != 0)
            {
                switch (_type)
                {
                    case OPLType.Opl2:
                    case OPLType.Opl3:
                        Nuked.OPL3_WriteRegBuffered(ref chip, (ushort)address[0], (byte)val);
                        break;
                    case OPLType.DualOpl2:
                        // Not a 0x??8 port, then write to a specific port
                        if ((port & 0x8) == 0)
                        {
                            byte index = (byte)((port & 2) >> 1);
                            dualWrite(index, (byte)address[index], (byte)val);
                        }
                        else
                        {
                            //Write to both ports
                            dualWrite(0, (byte)address[0], (byte)val);
                            dualWrite(1, (byte)address[1], (byte)val);
                        }
                        break;
                }
            }
            else
            {
                switch (_type)
                {
                    case OPLType.Opl2:
                        address[0] = (uint)(val & 0xff);
                        break;
                    case OPLType.DualOpl2:
                        // Not a 0x?88 port, when write to a specific side
                        if ((port & 0x8) == 0)
                        {
                            byte index = (byte)((port & 2) >> 1);
                            address[index] = (uint)(val & 0xff);
                        }
                        else
                        {
                            address[0] = (uint)(val & 0xff);
                            address[1] = (uint)(val & 0xff);
                        }
                        break;
                    case OPLType.Opl3:
                        address[0] = (uint)((val & 0xff) | ((port << 7) & 0x100));
                        break;
                }
            }
        }


        public void WriteReg(int r, int v)
        {
            Nuked.OPL3_WriteRegBuffered(ref chip, (ushort)r, (byte)v);
        }

        private void dualWrite(byte index, byte reg, byte val)
        {
            // Make sure you don't use opl3 features
            // Don't allow write to disable opl3
            if (reg == 5)
                return;

            // Only allow 4 waveforms
            if (reg >= 0xE0 && reg <= 0xE8)
                val &= 3;

            // Enabling panning
            if (reg >= 0xC0 && reg <= 0xC8)
            {
                val &= 15;
                val |= (byte)(index != 0 ? 0xA0 : 0x50);
            }

            uint fullReg = (uint)(reg + (index != 0 ? 0x100 : 0));
            Nuked.OPL3_WriteRegBuffered(ref chip, (ushort)fullReg, (byte)val);
        }

        public byte Read(int port)
        {
            return 0;
        }

        //void generateSamples(short[] buffer, int length)
        //{
        //    Nuked.OPL3_GenerateStream(chip, buffer, (ushort)(length / 2), 0);
        //}

        public void ReadBuffer(short[] buffer, int pos, int length)
        {
            Nuked.OPL3_GenerateStream(ref chip, buffer, (ushort)(length / 2), pos);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int sampleCount = count;
            short[] tempBuffer = new short[sampleCount / 2];

            Nuked.OPL3_GenerateStream(ref chip, tempBuffer, (ushort)(sampleCount / 2), 0);

            int pos = 0;
            for (int i = 0; i < tempBuffer.Length; i++)
            {
                buffer[pos ] = (byte)(tempBuffer[i] & 0xFF);
                buffer[pos + 1] = (byte)(tempBuffer[i] >> 8 & 0xFF);
                pos += 2;
            }

            return count;
        }
    }
}
