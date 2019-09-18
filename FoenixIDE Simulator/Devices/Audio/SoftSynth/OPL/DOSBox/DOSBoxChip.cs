using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    partial class DosBoxOPL
    {
        public class Chip
        {
            //Frequency scales for the different multiplications
            public uint[] freqMul = new uint[16];
            //Rates for decay and release for rate of this chip
            public uint[] linearRates = new uint[76];
            //Best match attack rates for the rate of this chip
            public uint[] attackRates = new uint[76];

            //18 channels with 2 operators each
            Channel[] chan = new Channel[18];
            public Channel[] Channels { get { return chan; } }

            public byte reg104 = 0;
            public byte reg08 = 0;
            public byte reg04 = 0;
            public byte regBD = 0;
            public byte vibratoIndex;
            public byte tremoloIndex;
            public sbyte vibratoSign;
            public byte vibratoShift;
            public byte tremoloValue;
            public byte vibratoStrength;
            public byte tremoloStrength;
            //Mask for allowed wave forms
            public byte waveFormMask;
            //0 or -1 when enabled
            public sbyte opl3Active = 0;

            public Chip()
            {
                chan = new Channel[18];
                for (int i = 0; i < chan.Length; i++)
                {
                    chan[i] = new Channel(this, i);
                }
            }

            public uint ForwardNoise()
            {
                noiseCounter += noiseAdd;
                uint count = noiseCounter >> LFO_SH;
                noiseCounter &= WAVE_MASK;
                for (; count > 0; --count)
                {
                    //Noise calculation from mame
                    noiseValue ^= (0x800302) & (0 - (noiseValue & 1));
                    noiseValue >>= 1;
                }
                return noiseValue;
            }

            public void WriteReg(uint reg, byte val)
            {
                switch ((reg & 0xf0) >> 4)
                {
                    case 0x00 >> 4:
                        if (reg == 0x01)
                        {
                            waveFormMask = (byte)(((val & 0x20) != 0) ? 0x7 : 0x0);
                        }
                        else if (reg == 0x104)
                        {
                            //Only detect changes in lowest 6 bits
                            if (((reg104 ^ val) & 0x3f) == 0)
                                return;
                            //Always keep the highest bit enabled, for checking > 0x80
                            reg104 = (byte)(0x80 | (val & 0x3f));
                        }
                        else if (reg == 0x105)
                        {
                            //MAME says the real opl3 doesn't reset anything on opl3 disable/enable till the next write in another register
                            if (((opl3Active ^ val) & 1) == 0)
                                return;
                            opl3Active = (sbyte)(((val & 1) != 0) ? 0xff : 0);
                            //Update the 0xc0 register for all channels to signal the switch to mono/stereo handlers
                            for (int i = 0; i < 18; i++)
                            {
                                chan[i].ResetC0(this);
                            }
                        }
                        else if (reg == 0x08)
                        {
                            reg08 = val;
                        }
                        goto case 0x10 >> 4;
                    case 0x10 >> 4:
                        break;
                    case 0x20 >> 4:
                    case 0x30 >> 4:
                        RegOp(reg, op => op.Write20(this, val));
                        break;
                    case 0x40 >> 4:
                    case 0x50 >> 4:
                        RegOp(reg, op => op.Write40(this, val));
                        break;
                    case 0x60 >> 4:
                    case 0x70 >> 4:
                        RegOp(reg, op => op.Write60(this, val));
                        break;
                    case 0x80 >> 4:
                    case 0x90 >> 4:
                        RegOp(reg, op => op.Write80(this, val));
                        break;
                    case 0xa0 >> 4:
                        RegChan(reg, ch => ch.WriteA0(this, val));
                        break;
                    case 0xb0 >> 4:
                        if (reg == 0xbd)
                        {
                            WriteBD(val);
                        }
                        else
                        {
                            RegChan(reg, ch => ch.WriteB0(this, val));
                        }
                        break;
                    case 0xc0 >> 4:
                        RegChan(reg, ch => ch.WriteC0(this, val));
                        goto case 0xd0 >> 4;
                    case 0xd0 >> 4:
                        break;
                    case 0xe0 >> 4:
                    case 0xf0 >> 4:
                        RegOp(reg, op => op.WriteE0(this, val));
                        break;
                }
            }

            public uint WriteAddr(uint port, byte val)
            {
                switch (port & 3)
                {
                    case 0:
                        return val;
                    case 2:
                        if (opl3Active != 0 || (val == 0x05))
                            return (uint)(0x100 | val);
                        else
                            return val;
                }
                return 0;
            }

            public void GenerateBlock2(uint total, int[] output)
            {
                int pos = 0;
                Array.Clear(output, 0, output.Length);
                while (total > 0)
                {
                    uint samples = ForwardLFO(total);
                    for (var i = 0; i < 9; i++)
                    {
                        var ch = chan[i];
                        ch = ch.SynthHandler(this, samples, output, pos);
                    }
                    total -= samples;
                    pos += (int)samples;
                }
            }

            public void GenerateBlock3(uint total, int[] output)
            {
                int pos = 0;
                Array.Clear(output, 0, output.Length);
                while (total > 0)
                {
                    uint samples = ForwardLFO(total);
                    for (var i = 0; i < 18; i++)
                    {
                        var ch = chan[i];
                        ch.SynthHandler(this, samples, output, pos);
                    }
                    total -= samples;
                    pos += (int)(samples * 2);
                }
            }

            public void Setup(int rate)
            {
                double scale = OPLRATE / (double)rate;

                //Noise counter is run at the same precision as general waves
                noiseAdd = (uint)(0.5 + scale * (1 << LFO_SH));
                noiseCounter = 0;
                noiseValue = 1; //Make sure it triggers the noise xor the first time
                //The low frequency oscillation counter
                //Every time his overflows vibrato and tremoloindex are increased
                lfoAdd = (uint)(0.5 + scale * (1 << LFO_SH));
                lfoCounter = 0;
                vibratoIndex = 0;
                tremoloIndex = 0;

                //With higher octave this gets shifted up
                //-1 since the freqCreateTable = *2
#if WAVE_PRECISION
            double freqScale = (1 << 7) * scale * (1 << (WAVE_SH - 1 - 10));
            for (int i = 0; i < 16; i++)
            {
                freqMul[i] = (uint)(0.5 + freqScale * FreqCreateTable[i]);
            }
#else
                uint freqScale = (uint)(0.5 + scale * (1 << (WAVE_SH - 1 - 10)));
                for (int i = 0; i < 16; i++)
                {
                    freqMul[i] = freqScale * FreqCreateTable[i];
                }
#endif

                //-3 since the real envelope takes 8 steps to reach the single value we supply
                for (byte i = 0; i < 76; i++)
                {
                    byte index, shift;
                    EnvelopeSelect(i, out index, out shift);
                    linearRates[i] = (uint)(scale * (EnvelopeIncreaseTable[index] << (RATE_SH + ENV_EXTRA - shift - 3)));
                }
                //Generate the best matching attack rate
                for (byte i = 0; i < 62; i++)
                {
                    byte index, shift;
                    EnvelopeSelect(i, out index, out shift);
                    //Original amount of samples the attack would take
                    int original = (int)((AttackSamplesTable[index] << shift) / scale);

                    int guessAdd = (int)(scale * (EnvelopeIncreaseTable[index] << (RATE_SH - shift - 3)));
                    int bestAdd = guessAdd;
                    uint bestDiff = 1 << 30;
                    for (uint passes = 0; passes < 16; passes++)
                    {
                        int volume = ENV_MAX;
                        int samples = 0;
                        uint count = 0;
                        while (volume > 0 && samples < original * 2)
                        {
                            count += (uint)guessAdd;
                            int change = (int)(count >> RATE_SH);
                            count &= RATE_MASK;
                            if (change != 0)
                            { // less than 1 %
                                volume += (~volume * change) >> 3;
                            }
                            samples++;

                        }
                        int diff = original - samples;
                        uint lDiff = (uint)Math.Abs(diff);
                        //Init last on first pass
                        if (lDiff < bestDiff)
                        {
                            bestDiff = lDiff;
                            bestAdd = guessAdd;
                            if (bestDiff == 0)
                                break;
                        }
                        //Below our target
                        if (diff < 0)
                        {
                            //Better than the last time
                            int mul = ((original - diff) << 12) / original;
                            guessAdd = ((guessAdd * mul) >> 12);
                            guessAdd++;
                        }
                        else if (diff > 0)
                        {
                            int mul = ((original - diff) << 12) / original;
                            guessAdd = (guessAdd * mul) >> 12;
                            guessAdd--;
                        }
                    }
                    attackRates[i] = (uint)bestAdd;
                }
                for (byte i = 62; i < 76; i++)
                {
                    //This should provide instant volume maximizing
                    attackRates[i] = 8 << RATE_SH;
                }
                //Setup the channels with the correct four op flags
                //Channels are accessed through a table so they appear linear here
                chan[0].FourMask = 0x00 | (1 << 0);
                chan[1].FourMask = 0x80 | (1 << 0);
                chan[2].FourMask = 0x00 | (1 << 1);
                chan[3].FourMask = 0x80 | (1 << 1);
                chan[4].FourMask = 0x00 | (1 << 2);
                chan[5].FourMask = 0x80 | (1 << 2);

                chan[9].FourMask = 0x00 | (1 << 3);
                chan[10].FourMask = 0x80 | (1 << 3);
                chan[11].FourMask = 0x00 | (1 << 4);
                chan[12].FourMask = 0x80 | (1 << 4);
                chan[13].FourMask = 0x00 | (1 << 5);
                chan[14].FourMask = 0x80 | (1 << 5);

                //mark the percussion channels
                chan[6].FourMask = 0x40;
                chan[7].FourMask = 0x40;
                chan[8].FourMask = 0x40;

                //Clear Everything in opl3 mode
                WriteReg(0x105, 0x1);
                for (uint i = 0; i < 512; i++)
                {
                    if (i == 0x105)
                        continue;
                    WriteReg(i, 0xff);
                    WriteReg(i, 0x0);
                }
                WriteReg(0x105, 0x0);
                //Clear everything in opl2 mode
                for (uint i = 0; i < 255; i++)
                {
                    WriteReg(i, 0xff);
                    WriteReg(i, 0x0);
                }
            }

            /// <summary>
            /// Return the maximum amount of samples before and LFO change.
            /// </summary>
            /// <returns>The maximum amount of samples before and LFO change.</returns>
            /// <param name="samples">Samples.</param>
            uint ForwardLFO(uint samples)
            {
                //Current vibrato value, runs 4x slower than tremolo
                vibratoSign = (sbyte)((VibratoTable[vibratoIndex >> 2]) >> 7);
                vibratoShift = (byte)((VibratoTable[vibratoIndex >> 2] & 7) + vibratoStrength);
                tremoloValue = (byte)(TremoloTable[tremoloIndex] >> tremoloStrength);

                //Check hom many samples there can be done before the value changes
                uint todo = LFO_MAX - lfoCounter;
                uint count = (todo + lfoAdd - 1) / lfoAdd;
                if (count > samples)
                {
                    count = samples;
                    lfoCounter += count * lfoAdd;
                }
                else
                {
                    lfoCounter += count * lfoAdd;
                    lfoCounter &= (LFO_MAX - 1);
                    //Maximum of 7 vibrato value * 4
                    vibratoIndex = (byte)((vibratoIndex + 1) & 31);
                    //Clip tremolo to the the table size
                    if (tremoloIndex + 1 < TREMOLO_TABLE)
                        ++tremoloIndex;
                    else
                        tremoloIndex = 0;
                }
                return count;
            }


            public void RegOp(uint reg, Action<Operator> action)
            {
                var op = opOffsetTable[((reg >> 3) & 0x20) | (reg & 0x1f)];
                if (op != null)
                {
                    action(op(this));
                }
            }

            void RegChan(uint reg, Action<Channel> action)
            {
                var ch = chanOffsetTable[((reg >> 4) & 0x10) | (reg & 0xf)];
                if (ch != null)
                {
                    action(ch(this));
                }
            }

            public void WriteBD(byte val)
            {
                byte change = (byte)(regBD ^ val);
                if (change == 0)
                    return;
                regBD = val;
                //TODO could do this with shift and xor?
                vibratoStrength = (val & 0x40) == 0x40 ? (byte)0x00 : (byte)0x01;
                tremoloStrength = (val & 0x80) == 0x80 ? (byte)0x00 : (byte)0x02;
                if ((val & 0x20) == 0x20)
                {
                    //Drum was just enabled, make sure channel 6 has the right synth
                    if ((change & 0x20) != 0)
                    {
                        var mode = (opl3Active != 0) ? SynthMode.sm3Percussion : SynthMode.sm2Percussion;
                        chan[6].SynthMode = mode;
                    }
                    //Bass Drum
                    if ((val & 0x10) == 0x10)
                    {
                        chan[6].Ops[0].KeyOn(0x2);
                        chan[6].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        chan[6].Ops[0].KeyOff(0x2);
                        chan[6].Ops[1].KeyOff(0x2);
                    }
                    //Hi-Hat
                    if ((val & 0x1) != 0)
                    {
                        chan[7].Ops[0].KeyOn(0x2);
                    }
                    else
                    {
                        chan[7].Ops[0].KeyOff(0x2);
                    }
                    //Snare
                    if ((val & 0x8) != 0)
                    {
                        chan[7].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        chan[7].Ops[1].KeyOff(0x2);
                    }
                    //Tom-Tom
                    if ((val & 0x4) != 0)
                    {
                        chan[8].Ops[0].KeyOn(0x2);
                    }
                    else
                    {
                        chan[8].Ops[0].KeyOff(0x2);
                    }
                    //Top Cymbal
                    if ((val & 0x2) != 0)
                    {
                        chan[8].Ops[1].KeyOn(0x2);
                    }
                    else
                    {
                        chan[8].Ops[1].KeyOff(0x2);
                    }
                    //Toggle keyoffs when we turn off the percussion
                }
                else if ((change & 0x20) != 0)
                {
                    //Trigger a reset to setup the original synth handler
                    chan[6].ResetC0(this);
                    chan[6].Ops[0].KeyOff(0x2);
                    chan[6].Ops[1].KeyOff(0x2);
                    chan[7].Ops[0].KeyOff(0x2);
                    chan[7].Ops[1].KeyOff(0x2);
                    chan[8].Ops[0].KeyOff(0x2);
                    chan[8].Ops[1].KeyOff(0x2);
                }
            }

            //This is used as the base counter for vibrato and tremolo
            uint lfoCounter;
            uint lfoAdd;


            uint noiseCounter;
            uint noiseAdd;
            uint noiseValue;
        }
    }
}
