using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    partial class DosBoxOPL
    {
        public class Channel
        {
            public Chip Chip { get; private set; }

            public int ChannelNum { get; private set; }

            public Operator[] Ops { get; private set; }

            //SynthHandler synthHandler;

            uint chanData;        //Frequency/octave and derived values
            int[] old = new int[2];          //Old data for feedback

            byte feedback;         //Feedback shift
            byte regB0;            //Register values to check for changes
            byte regC0;

            sbyte maskLeft;     //Sign extended values for both channel's panning
            sbyte maskRight;

            public SynthMode SynthMode { get; set; }

            //This should correspond with reg104, bit 6 indicates a Percussion channel, bit 7 indicates a silent channel
            public byte FourMask { get; set; }

            public void WriteA0(Chip chip, byte val)
            {
                byte fourOp = (byte)(chip.reg104 & chip.opl3Active & FourMask);
                //Don't handle writes to silent fourop channels
                if (fourOp > 0x80)
                    return;
                uint change = (chanData ^ val) & 0xff;
                if (change != 0)
                {
                    chanData ^= change;
                    UpdateFrequency(chip, fourOp);
                }
            }

            public void WriteB0(Chip chip, byte val)
            {
                byte fourOp = (byte)(chip.reg104 & chip.opl3Active & FourMask);
                //Don't handle writes to silent fourop channels
                if (fourOp > 0x80)
                    return;
                uint change = (uint)((chanData ^ (val << 8)) & 0x1f00);
                if (change != 0)
                {
                    chanData ^= change;
                    UpdateFrequency(chip, fourOp);
                }
                //Check for a change in the keyon/off state
                if (((val ^ regB0) & 0x20) == 0)
                    return;
                regB0 = val;
                if ((val & 0x20) != 0)
                {
                    Op(0).KeyOn(0x1);
                    Op(1).KeyOn(0x1);
                    if ((fourOp & 0x3f) != 0)
                    {
                        chip.Channels[ChannelNum + 1].Op(0).KeyOn(1);
                        chip.Channels[ChannelNum + 1].Op(1).KeyOn(1);
                    }
                }
                else
                {
                    Op(0).KeyOff(0x1);
                    Op(1).KeyOff(0x1);
                    if ((fourOp & 0x3f) != 0)
                    {
                        chip.Channels[ChannelNum + 1].Op(0).KeyOff(1);
                        chip.Channels[ChannelNum + 1].Op(1).KeyOff(1);
                    }
                }
            }

            public void WriteC0(Chip chip, byte val)
            {
                byte change = (byte)(val ^ regC0);
                if (change == 0)
                    return;
                regC0 = val;
                feedback = (byte)((val >> 1) & 7);
                if (feedback != 0)
                {
                    //We shift the input to the right 10 bit wave index value
                    feedback = (byte)(9 - feedback);
                }
                else
                {
                    feedback = 31;
                }
                //Select the new synth mode
                if (chip.opl3Active != 0)
                {
                    //4-op mode enabled for this channel
                    if (((chip.reg104 & FourMask) & 0x3f) != 0)
                    {
                        Channel chan0, chan1;
                        //Check if it's the 2nd channel in a 4-op
                        if ((FourMask & 0x80) == 0)
                        {
                            chan0 = this;
                            chan1 = chip.Channels[ChannelNum + 1];
                        }
                        else
                        {
                            chan0 = chip.Channels[ChannelNum - 1];
                            chan1 = this;
                        }

                        byte synth = (byte)(((chan0.regC0 & 1) << 0) | ((chan1.regC0 & 1) << 1));
                        switch (synth)
                        {
                            case 0:
                                chan0.SynthMode = SynthMode.sm3FMFM;
                                break;
                            case 1:
                                chan0.SynthMode = SynthMode.sm3AMFM;
                                break;
                            case 2:
                                chan0.SynthMode = SynthMode.sm3FMAM;
                                break;
                            case 3:
                                chan0.SynthMode = SynthMode.sm3AMAM;
                                break;
                        }
                        //Disable updating percussion channels
                    }
                    else if (((FourMask & 0x40) != 0) && ((chip.regBD & 0x20) != 0))
                    {

                        //Regular dual op, am or fm
                    }
                    else if ((val & 1) != 0)
                    {
                        SynthMode = SynthMode.sm3AM;
                    }
                    else
                    {
                        SynthMode = SynthMode.sm3FM;
                    }
                    maskLeft = (sbyte)((val & 0x10) != 0 ? -1 : 0);
                    maskRight = (sbyte)((val & 0x20) != 0 ? -1 : 0);
                    //opl2 active
                }
                else
                {
                    //Disable updating percussion channels
                    if (((FourMask & 0x40) != 0) && ((chip.regBD & 0x20) != 0))
                    {
                        //Regular dual op, am or fm
                    }
                    else if ((val & 1) != 0)
                    {
                        SynthMode = SynthMode.sm2AM;
                    }
                    else
                    {
                        SynthMode = SynthMode.sm2FM;
                    }
                }
            }

            public void ResetC0(Chip chip)
            {
                byte val = regC0;
                regC0 ^= 0xff;
                WriteC0(chip, val);
            }

            public Channel SynthHandler(Chip chip, uint samples, int[] output, int pos)
            {
                return BlockTemplate(SynthMode, chip, samples, output, pos);
            }

            public Channel(Chip chip, int index)
            {
                this.Chip = chip;
                ChannelNum = index;

                old = new int[2];
                Ops = new Operator[2];
                for (int i = 0; i < Ops.Length; i++)
                {
                    Ops[i] = new Operator();
                }

                chanData = 0;
                regB0 = 0;
                regC0 = 0;
                maskLeft = -1;
                maskRight = -1;
                feedback = 31;
                SynthMode = SynthMode.sm2FM;
            }

            /// <summary>
            /// Generate blocks of data in specific modes.
            /// </summary>
            /// <returns>The template.</returns>
            /// <param name="mode">Mode.</param>
            /// <param name="chip">Chip.</param>
            /// <param name="samples">Samples.</param>
            /// <param name="output">Output.</param>
            /// <param name="pos">Position.</param>
            Channel BlockTemplate(SynthMode mode, Chip chip, uint samples, int[] output, int pos)
            {
                switch (mode)
                {
                    case SynthMode.sm2AM:
                    case SynthMode.sm3AM:
                        if (Op(0).Silent() && Op(1).Silent())
                        {
                            old[0] = old[1] = 0;
                            return chip.Channels[ChannelNum + 1];
                        }
                        break;
                    case SynthMode.sm2FM:
                    case SynthMode.sm3FM:
                        if (Op(1).Silent())
                        {
                            old[0] = old[1] = 0;
                            return chip.Channels[ChannelNum + 1];
                        }
                        break;
                    case SynthMode.sm3FMFM:
                        if (Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.sm3AMFM:
                        if (Op(0).Silent() && Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.sm3FMAM:
                        if (Op(1).Silent() && Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.sm3AMAM:
                        if (Op(0).Silent() && Op(2).Silent() && Op(3).Silent())
                        {
                            old[0] = old[1] = 0;
                            return chip.Channels[ChannelNum + 2];
                        }
                        break;
                    case SynthMode.sm2Percussion:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.sm3Percussion:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.sm4Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.sm6Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                }
                //Init the operators with the the current vibrato and tremolo values
                Op(0).Prepare(chip);
                Op(1).Prepare(chip);
                if (mode > SynthMode.sm4Start)
                {
                    Op(2).Prepare(chip);
                    Op(3).Prepare(chip);
                }
                if (mode > SynthMode.sm6Start)
                {
                    Op(4).Prepare(chip);
                    Op(5).Prepare(chip);
                }
                for (int i = 0; i < samples; i++)
                {
                    //Early out for percussion handlers
                    if (mode == SynthMode.sm2Percussion)
                    {
                        GeneratePercussion(false, chip, output, pos + i);
                        continue;   //Prevent some unitialized value bitching
                    }
                    else if (mode == SynthMode.sm3Percussion)
                    {
                        GeneratePercussion(true, chip, output, pos + i * 2);
                        continue;   //Prevent some unitialized value bitching
                    }

                    //Do unsigned shift so we can shift out all bits but still stay in 10 bit range otherwise
                    int mod = (int)((uint)((old[0] + old[1])) >> feedback);
                    old[0] = old[1];
                    old[1] = Op(0).GetSample(mod);
                    int sample = 0;
                    int out0 = old[0];
                    if (mode == SynthMode.sm2AM || mode == SynthMode.sm3AM)
                    {
                        sample = out0 + Op(1).GetSample(0);
                    }
                    else if (mode == SynthMode.sm2FM || mode == SynthMode.sm3FM)
                    {
                        sample = Op(1).GetSample(out0);
                    }
                    else if (mode == SynthMode.sm3FMFM)
                    {
                        int next = Op(1).GetSample(out0);
                        next = Op(2).GetSample(next);
                        sample = Op(3).GetSample(next);
                    }
                    else if (mode == SynthMode.sm3AMFM)
                    {
                        sample = out0;
                        int next = Op(1).GetSample(0);
                        next = Op(2).GetSample(next);
                        sample += Op(3).GetSample(next);
                    }
                    else if (mode == SynthMode.sm3FMAM)
                    {
                        sample = Op(1).GetSample(out0);
                        int next = Op(2).GetSample(0);
                        sample += Op(3).GetSample(next);
                    }
                    else if (mode == SynthMode.sm3AMAM)
                    {
                        sample = out0;
                        int next = Op(1).GetSample(0);
                        sample += Op(2).GetSample(next);
                        sample += Op(3).GetSample(0);
                    }
                    switch (mode)
                    {
                        case SynthMode.sm2AM:
                        case SynthMode.sm2FM:
                            output[pos + i] += sample;
                            break;
                        case SynthMode.sm3AM:
                        case SynthMode.sm3FM:
                        case SynthMode.sm3FMFM:
                        case SynthMode.sm3AMFM:
                        case SynthMode.sm3FMAM:
                        case SynthMode.sm3AMAM:
                            output[pos + i * 2 + 0] += sample & maskLeft;
                            output[pos + i * 2 + 1] += sample & maskRight;
                            break;
                        case SynthMode.sm2Percussion:
                            // This case was not handled in the DOSBox code either
                            // thus we leave this blank.
                            // TODO: Consider checking this.
                            break;
                        case SynthMode.sm3Percussion:
                            // This case was not handled in the DOSBox code either
                            // thus we leave this blank.
                            // TODO: Consider checking this.
                            break;
                        case SynthMode.sm4Start:
                            // This case was not handled in the DOSBox code either
                            // thus we leave this blank.
                            // TODO: Consider checking this.
                            break;
                        case SynthMode.sm6Start:
                            // This case was not handled in the DOSBox code either
                            // thus we leave this blank.
                            // TODO: Consider checking this.
                            break;
                    }
                }
                switch (mode)
                {
                    case SynthMode.sm2AM:
                    case SynthMode.sm2FM:
                    case SynthMode.sm3AM:
                    case SynthMode.sm3FM:
                        return chip.Channels[ChannelNum + 1];
                    case SynthMode.sm3FMFM:
                    case SynthMode.sm3AMFM:
                    case SynthMode.sm3FMAM:
                    case SynthMode.sm3AMAM:
                        return chip.Channels[ChannelNum + 2];
                    case SynthMode.sm2Percussion:
                    case SynthMode.sm3Percussion:
                        return chip.Channels[ChannelNum + 3];
                    case SynthMode.sm4Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                    case SynthMode.sm6Start:
                        // This case was not handled in the DOSBox code either
                        // thus we leave this blank.
                        // TODO: Consider checking this.
                        break;
                }
                return null;
            }


            public Operator Op(uint index)
            {
                return Chip.Channels[ChannelNum + (index >> 1)].Ops[index & 1];
            }

            //Forward the channel data to the operators of the channel
            public void SetChanData(Chip chip, uint data)
            {
                uint change = chanData ^ data;
                chanData = data;
                Op(0).chanData = data;
                Op(1).chanData = data;
                //Since a frequency update triggered this, always update frequency
                Op(0).UpdateFrequency();
                Op(1).UpdateFrequency();
                if ((change & (0xff << (int)Shift.KSLBASE)) != 0)
                {
                    Op(0).UpdateAttenuation();
                    Op(1).UpdateAttenuation();
                }
                if ((change & (0xff << (int)Shift.KEYCODE)) != 0)
                {
                    Op(0).UpdateRates(chip);
                    Op(1).UpdateRates(chip);
                }
            }
            //Change in the chandata, check for new values and if we have to forward to operators
            public void UpdateFrequency(Chip chip, byte fourOp)
            {
                //Extrace the frequency bits
                uint data = chanData & 0xffff;
                uint kslBase = KslTable[data >> 6];
                uint keyCode = (data & 0x1c00) >> 9;
                if ((chip.reg08 & 0x40) != 0)
                {
                    keyCode |= (data & 0x100) >> 8; /* notesel == 1 */
                }
                else
                {
                    keyCode |= (data & 0x200) >> 9; /* notesel == 0 */
                }
                //Add the keycode and ksl into the highest bits of chanData
                data |= (keyCode << (int)Shift.KEYCODE) | (kslBase << (int)Shift.KSLBASE);
                SetChanData(chip, data);
                if ((fourOp & 0x3f) != 0)
                {
                    chip.Channels[ChannelNum + 1].SetChanData(chip, data);
                }
            }

            void GeneratePercussion(bool opl3Mode, Chip chip, int[] output, int pos)
            {
                Channel chan = this;

                //BassDrum
                int mod = (int)((uint)((old[0] + old[1])) >> feedback);
                old[0] = old[1];
                old[1] = Op(0).GetSample(mod);

                //When bassdrum is in AM mode first operator is ignoed
                if ((chan.regC0 & 1) != 0)
                {
                    mod = 0;
                }
                else
                {
                    mod = old[0];
                }
                int sample = Op(1).GetSample(mod);


                //Precalculate stuff used by other outputs
                uint noiseBit = chip.ForwardNoise() & 0x1;
                uint c2 = Op(2).ForwardWave();
                uint c5 = Op(5).ForwardWave();
                uint phaseBit = (uint)((((c2 & 0x88) ^ ((c2 << 5) & 0x80)) | ((c5 ^ (c5 << 2)) & 0x20)) != 0 ? 0x02 : 0x00);

                //Hi-Hat
                uint hhVol = Op(2).ForwardVolume();
                if (!ENV_SILENT((int)hhVol))
                {
                    uint hhIndex = (uint)((phaseBit << 8) | (0x34 << (int)(phaseBit ^ (noiseBit << 1))));
                    sample += Op(2).GetWave(hhIndex, hhVol);
                }
                //Snare Drum
                uint sdVol = Op(3).ForwardVolume();
                if (!ENV_SILENT((int)sdVol))
                {
                    uint sdIndex = (0x100 + (c2 & 0x100)) ^ (noiseBit << 8);
                    sample += Op(3).GetWave(sdIndex, sdVol);
                }
                //Tom-tom
                sample += Op(4).GetSample(0);

                //Top-Cymbal
                uint tcVol = Op(5).ForwardVolume();
                if (!ENV_SILENT((int)tcVol))
                {
                    uint tcIndex = (1 + phaseBit) << 8;
                    sample += Op(5).GetWave(tcIndex, tcVol);
                }
                sample <<= 1;
                if (opl3Mode)
                {
                    output[pos] += sample;
                    output[pos + 1] += sample;
                }
                else
                {
                    output[pos] += sample;
                }
            }

        }
    }
}





