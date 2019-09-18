using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.Nuked
{
    public static partial class Nuked
    {
        public static int RSM_FRAC = 10;
        public static int OPL_WRITEBUF_SIZE = 1024;
        public static int OPL_WRITEBUF_DELAY = 2;

        //
        // Envelope generator
        //

        public static short OPL3_EnvelopeCalcExp(uint level)
        {
            if (level > 0x1fff)
            {
                level = 0x1fff;
            }
            return (short)((exprom[level & 0xff] << 1) >> (int)(level >> 8));
        }

        public static short OPL3_EnvelopeCalcSin0(ushort phase, ushort envelope)
        {
            ushort _out = 0;
            ushort neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                neg = 0xffff;
            }
            if ((phase & 0x100) != 0)
            {
                _out = logsinrom[(phase & 0xff) ^ 0xff];
            }
            else
            {
                _out = logsinrom[phase & 0xff];
            }
            return (short)(OPL3_EnvelopeCalcExp((uint)(_out + (envelope << 3))) ^ neg);
        }

        public static short OPL3_EnvelopeCalcSin1(ushort phase, ushort envelope)
        {
            ushort _out = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                _out = 0x1000;
            }
            else if ((phase & 0x100) != 0)
            {
                _out = logsinrom[(phase & 0xff) ^ 0xff];
            }
            else
            {
                _out = logsinrom[phase & 0xff];
            }
            return OPL3_EnvelopeCalcExp((uint)(_out + (envelope << 3)));
        }

        public static short OPL3_EnvelopeCalcSin2(ushort phase, ushort envelope)
        {
            ushort _out = 0;
            phase &= 0x3ff;
            if ((phase & 0x100) != 0)
            {
                _out = logsinrom[(phase & 0xff) ^ 0xff];
            }
            else
            {
                _out = logsinrom[phase & 0xff];
            }
            return OPL3_EnvelopeCalcExp((uint)(_out + (envelope << 3)));
        }

        public static short OPL3_EnvelopeCalcSin3(ushort phase, ushort envelope)
        {
            ushort _out = 0;
            phase &= 0x3ff;
            if ((phase & 0x100) != 0)
            {
                _out = 0x1000;
            }
            else
            {
                _out = logsinrom[phase & 0xff];
            }
            return OPL3_EnvelopeCalcExp((uint)(_out + (envelope << 3)));
        }

        public static short OPL3_EnvelopeCalcSin4(ushort phase, ushort envelope)
        {
            ushort _out = 0;
            ushort neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x300) == 0x100)
            {
                neg = 0xffff;
            }
            if ((phase & 0x200) != 0)
            {
                _out = 0x1000;
            }
            else if ((phase & 0x80) != 0)
            {
                _out = logsinrom[((phase ^ 0xff) << 1) & 0xff];
            }
            else
            {
                _out = logsinrom[(phase << 1) & 0xff];
            }
            return (short)(OPL3_EnvelopeCalcExp((uint)(_out + (envelope << 3))) ^ neg);
        }

        public static short OPL3_EnvelopeCalcSin5(ushort phase, ushort envelope)
        {
            ushort _out = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                _out = 0x1000;
            }
            else if ((phase & 0x80) != 0)
            {
                _out = logsinrom[((phase ^ 0xff) << 1) & 0xff];
            }
            else
            {
                _out = logsinrom[(phase << 1) & 0xff];
            }
            return OPL3_EnvelopeCalcExp((uint)(_out + (envelope << 3)));
        }

        public static short OPL3_EnvelopeCalcSin6(ushort phase, ushort envelope)
        {
            ushort neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                neg = 0xffff;
            }
            return (short)(OPL3_EnvelopeCalcExp((uint)(envelope << 3)) ^ neg);
        }

        static short OPL3_EnvelopeCalcSin7(ushort phase, ushort envelope)
        {
            ushort _out = 0;
            ushort neg = 0;
            phase &= 0x3ff;
            if ((phase & 0x200) != 0)
            {
                neg = 0xffff;
                phase = (ushort)((phase & 0x1ff) ^ 0x1ff);
            }
            _out = (ushort)(phase << 3);
            return (short)(OPL3_EnvelopeCalcExp((uint)(_out + (envelope << 3))) ^ neg);
        }

        //public static const envelope_sinfunc[] envelope_sin = {//[8]
        public static readonly Func<ushort, ushort, short>[] envelope_sin = {//[8]
            OPL3_EnvelopeCalcSin0,
            OPL3_EnvelopeCalcSin1,
            OPL3_EnvelopeCalcSin2,
            OPL3_EnvelopeCalcSin3,
            OPL3_EnvelopeCalcSin4,
            OPL3_EnvelopeCalcSin5,
            OPL3_EnvelopeCalcSin6,
            OPL3_EnvelopeCalcSin7
        };


        public static void OPL3_EnvelopeUpdateKSL(ref opl3_slot slot)
        {
            short ksl = (short)((kslrom[slot.channel.f_num >> 6] << 2) - ((0x08 - slot.channel.block) << 5));
            if (ksl < 0)
            {
                ksl = 0;
            }
            slot.eg_ksl = (byte)ksl;
        }

        public static void OPL3_EnvelopeCalc(ref opl3_slot slot)
        {
            byte nonzero;
            byte rate;
            byte rate_hi;
            byte rate_lo;
            byte reg_rate = 0;
            byte ks;
            byte eg_shift, shift;
            ushort eg_rout;
            short eg_inc;
            byte eg_off;
            byte reset = 0;
            //unsafe
            //{
            //    slot.eg_out = (short)(slot.eg_rout + (slot.reg_tl << 2) + (slot.eg_ksl >> kslshift[slot.reg_ksl]) + *slot.trem);
            //}
            slot.eg_out = (short)(slot.eg_rout + (slot.reg_tl << 2) + (slot.eg_ksl >> kslshift[slot.reg_ksl]) + (slot.tremZero == false ? slot.chip.tremolo : 0));
            if (slot.key != 0 && slot.eg_gen == envelope_gen_num.release)
            {
                reset = 1;
                reg_rate = slot.reg_ar;
            }
            else
            {
                switch (slot.eg_gen)
                {
                    case envelope_gen_num.attack:
                        reg_rate = slot.reg_ar;
                        break;
                    case envelope_gen_num.decay:
                        reg_rate = slot.reg_dr;
                        break;
                    case envelope_gen_num.sustain:
                        unsafe
                        {
                            if (slot.reg_type == 0)
                            {
                                reg_rate = slot.reg_rr;
                            }
                        }
                        break;
                    case envelope_gen_num.release:
                        reg_rate = slot.reg_rr;
                        break;
                }
            }
            slot.pg_reset = reset;
            ks = (byte)(slot.channel.ksv >> ((slot.reg_ksr ^ 1) << 1));
            nonzero = (byte)((reg_rate != 0) ? 1 : 0);
            rate = (byte)(ks + (reg_rate << 2));
            rate_hi = (byte)(rate >> 2);
            rate_lo = (byte)(rate & 0x03);
            if ((rate_hi & 0x10) != 0)
            {
                rate_hi = 0x0f;
            }
            eg_shift = (byte)(rate_hi + slot.chip.eg_add);
            shift = 0;
            if (nonzero != 0)
            {
                if (rate_hi < 12)
                {
                    if (slot.chip.eg_state != 0)
                    {
                        switch (eg_shift)
                        {
                            case 12:
                                shift = 1;
                                break;
                            case 13:
                                shift = (byte)((rate_lo >> 1) & 0x01);
                                break;
                            case 14:
                                shift = (byte)(rate_lo & 0x01);
                                break;
                            default:
                                break;
                        }
                    }
                }
                else
                {
                    shift = (byte)((rate_hi & 0x03) + eg_incstep[rate_lo][slot.chip.timer & 0x03]);
                    if ((shift & 0x04) != 0)
                    {
                        shift = 0x03;
                    }
                    if (shift == 0)
                    {
                        shift = slot.chip.eg_state;
                    }
                }
            }
            eg_rout = (ushort)slot.eg_rout;
            eg_inc = 0;
            eg_off = 0;
            // Instant attack
            if (reset != 0 && rate_hi == 0x0f)
            {
                eg_rout = 0x00;
            }
            // Envelope off
            if ((slot.eg_rout & 0x1f8) == 0x1f8)
            {
                eg_off = 1;
            }
            if (slot.eg_gen != envelope_gen_num.attack && reset == 0 && eg_off != 0)
            {
                eg_rout = 0x1ff;
            }
            switch (slot.eg_gen)
            {
                case envelope_gen_num.attack:
                    if (slot.eg_rout == 0)
                    {
                        slot.eg_gen = envelope_gen_num.decay;
                    }
                    else if (slot.key != 0 && shift > 0 && rate_hi != 0x0f)
                    {
                        eg_inc = (short)(((~slot.eg_rout) << shift) >> 4);
                    }
                    break;
                case envelope_gen_num.decay:
                    if ((slot.eg_rout >> 4) == slot.reg_sl)
                    {
                        slot.eg_gen = envelope_gen_num.sustain;
                    }
                    else if (eg_off == 0 && reset == 0 && shift > 0)
                    {
                        eg_inc = (short)(1 << (shift - 1));
                    }
                    break;
                case envelope_gen_num.sustain:
                case envelope_gen_num.release:
                    if (eg_off == 0 && reset == 0 && shift > 0)
                    {
                        eg_inc = (short)(1 << (shift - 1));
                    }
                    break;
            }
            slot.eg_rout = (short)((eg_rout + eg_inc) & 0x1ff);
            // Key off
            if (reset != 0)
            {
                slot.eg_gen = envelope_gen_num.attack;
            }
            if (slot.key == 0)
            {
                slot.eg_gen = envelope_gen_num.release;
            }
        }

        static void OPL3_EnvelopeKeyOn(ref opl3_slot slot, EnvelopeKeyType type)
        {
            slot.key |= (byte)type;
        }

        public static void OPL3_EnvelopeKeyOff(ref opl3_slot slot, EnvelopeKeyType type)
        {
            slot.key &= (byte)~type;
        }

        //
        // Phase Generator
        //

        public static void OPL3_PhaseGenerate(ref opl3_slot slot)
        {
            opl3_chip chip;
            ushort f_num;
            uint basefreq;
            byte rm_xor, n_bit;
            uint noise;
            ushort phase;

            chip = slot.chip;
            f_num = slot.channel.f_num;
            if (slot.reg_vib != 0)
            {
                sbyte range;
                byte vibpos;

                range = (sbyte)((f_num >> 7) & 7);
                vibpos = slot.chip.vibpos;

                if ((vibpos & 3) == 0)
                {
                    range = 0;
                }
                else if ((vibpos & 1) != 0)
                {
                    range >>= 1;
                }
                range >>= slot.chip.vibshift;

                if ((vibpos & 4) != 0)
                {
                    range = (sbyte)-range;
                }
                f_num += (ushort)range;
            }
            basefreq = (uint)((f_num << slot.channel.block) >> 1);
            phase = (ushort)(slot.pg_phase >> 9);
            if (slot.pg_reset != 0)
            {
                slot.pg_phase = 0;
            }
            slot.pg_phase += (basefreq * mt[slot.reg_mult]) >> 1;
            // Rhythm mode
            noise = chip.noise;
            slot.pg_phase_out = phase;
            if (slot.slot_num == 13) // hh
            {
                chip.rm_hh_bit2 = (byte)((phase >> 2) & 1);
                chip.rm_hh_bit3 = (byte)((phase >> 3) & 1);
                chip.rm_hh_bit7 = (byte)((phase >> 7) & 1);
                chip.rm_hh_bit8 = (byte)((phase >> 8) & 1);
            }
            if (slot.slot_num == 17 && (chip.rhy & 0x20) != 0) // tc
            {
                chip.rm_tc_bit3 = (byte)((phase >> 3) & 1);
                chip.rm_tc_bit5 = (byte)((phase >> 5) & 1);
            }
            if ((chip.rhy & 0x20) != 0)
            {
                rm_xor = (byte)((chip.rm_hh_bit2 ^ chip.rm_hh_bit7) | (chip.rm_hh_bit3 ^ chip.rm_tc_bit5) | (chip.rm_tc_bit3 ^ chip.rm_tc_bit5));
                switch (slot.slot_num)
                {
                    case 13: // hh
                        slot.pg_phase_out = (ushort)(rm_xor << 9);
                        if ((rm_xor ^ (noise & 1)) != 0)
                        {
                            slot.pg_phase_out |= 0xd0;
                        }
                        else
                        {
                            slot.pg_phase_out |= 0x34;
                        }
                        break;
                    case 16: // sd
                        slot.pg_phase_out = (ushort)((chip.rm_hh_bit8 << 9) | ((chip.rm_hh_bit8 ^ (noise & 1)) << 8));
                        break;
                    case 17: // tc
                        slot.pg_phase_out = (ushort)((rm_xor << 9) | 0x80);
                        break;
                    default:
                        break;
                }
            }
            n_bit = (byte)(((noise >> 14) ^ noise) & 0x01);
            chip.noise = (byte)((noise >> 1) | (n_bit << 22));
        }

        //
        // Slot
        //

        public static void OPL3_SlotWrite20(ref opl3_slot slot, byte data)
        {
            unsafe
            {
                if (((data >> 7) & 0x01) != 0)
                {
                    //slot.trem = slot.chip.tremolo;
                    slot.tremZero = false;
                }
                else
                {
                    //slot.trem = (byte)slot.chip.zeromod;
                    slot.tremZero = true;
                }
                slot.reg_vib = (byte)((data >> 6) & 0x01);
                slot.reg_type = (byte)((data >> 5) & 0x01);
                slot.reg_ksr = (byte)((data >> 4) & 0x01);
                slot.reg_mult = (byte)(data & 0x0f);
            }
        }

        static void OPL3_SlotWrite40(ref opl3_slot slot, byte data)
        {
            slot.reg_ksl = (byte)((data >> 6) & 0x03);
            slot.reg_tl = (byte)(data & 0x3f);
            OPL3_EnvelopeUpdateKSL(ref slot);
        }

        static void OPL3_SlotWrite60(ref opl3_slot slot, byte data)
        {
            slot.reg_ar = (byte)((data >> 4) & 0x0f);
            slot.reg_dr = (byte)(data & 0x0f);
        }

        public static void OPL3_SlotWrite80(ref opl3_slot slot, byte data)
        {
            slot.reg_sl = (byte)((data >> 4) & 0x0f);
            if (slot.reg_sl == 0x0f)
            {
                slot.reg_sl = 0x1f;
            }
            slot.reg_rr = (byte)(data & 0x0f);
        }

        public static void OPL3_SlotWriteE0(ref opl3_slot slot, byte data)
        {
            slot.reg_wf = (byte)(data & 0x07);
            if (slot.chip.newm == 0x00)
            {
                slot.reg_wf &= 0x03;
            }
        }

        public static void OPL3_SlotGenerate(ref opl3_slot slot)
        {
            unsafe
            {
                slot._out = envelope_sin[slot.reg_wf]((ushort)(slot.pg_phase_out + *slot.mod), (ushort)(slot.eg_out));
            }
        }

        public static void OPL3_SlotCalcFB(ref opl3_slot slot)
        {
            if (slot.channel.fb != 0x00)
            {
                slot.fbmod = (short)((slot.prout + slot._out) >> (0x09 - slot.channel.fb));
            }
            else
            {
                slot.fbmod = 0;
            }
            slot.prout = slot._out;
        }

        //
        // Channel
        //


        public static void OPL3_ChannelUpdateRhythm(ref opl3_chip chip, byte data)
        {
            opl3_channel channel6;
            opl3_channel channel7;
            opl3_channel channel8;
            byte chnum;

            chip.rhy = (byte)(data & 0x3f);
            if ((chip.rhy & 0x20) != 0)
            {
                channel6 = chip.channel[6];
                channel7 = chip.channel[7];
                channel8 = chip.channel[8];
                channel6._out[0] = channel6.slots[1]._out;
                channel6._out[1] = channel6.slots[1]._out;
                channel6._out[2] = chip.zeromod;
                channel6._out[3] = chip.zeromod;
                channel7._out[0] = channel7.slots[0]._out;
                channel7._out[1] = channel7.slots[0]._out;
                channel7._out[2] = channel7.slots[1]._out;
                channel7._out[3] = channel7.slots[1]._out;
                channel8._out[0] = channel8.slots[0]._out;
                channel8._out[1] = channel8.slots[0]._out;
                channel8._out[2] = channel8.slots[1]._out;
                channel8._out[3] = channel8.slots[1]._out;
                for (chnum = 6; chnum < 9; chnum++)
                {
                    chip.channel[chnum].chtype = ChannelType.ch_drum;
                }
                OPL3_ChannelSetupAlg(ref channel6);
                OPL3_ChannelSetupAlg(ref channel7);
                OPL3_ChannelSetupAlg(ref channel8);
                //hh
                if ((chip.rhy & 0x01) != 0)
                {
                    OPL3_EnvelopeKeyOn(ref channel7.slots[0], EnvelopeKeyType.drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(ref channel7.slots[0], EnvelopeKeyType.drum);
                }
                //tc
                if ((chip.rhy & 0x02) != 0)
                {
                    OPL3_EnvelopeKeyOn(ref channel8.slots[1], EnvelopeKeyType.drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(ref channel8.slots[1], EnvelopeKeyType.drum);
                }
                //tom
                if ((chip.rhy & 0x04) != 0)
                {
                    OPL3_EnvelopeKeyOn(ref channel8.slots[0], EnvelopeKeyType.drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(ref channel8.slots[0], EnvelopeKeyType.drum);
                }
                //sd
                if ((chip.rhy & 0x08) != 0)
                {
                    OPL3_EnvelopeKeyOn(ref channel7.slots[1], EnvelopeKeyType.drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(ref channel7.slots[1], EnvelopeKeyType.drum);
                }
                //bd
                if ((chip.rhy & 0x10) != 0)
                {
                    OPL3_EnvelopeKeyOn(ref channel6.slots[0], EnvelopeKeyType.drum);
                    OPL3_EnvelopeKeyOn(ref channel6.slots[1], EnvelopeKeyType.drum);
                }
                else
                {
                    OPL3_EnvelopeKeyOff(ref channel6.slots[0], EnvelopeKeyType.drum);
                    OPL3_EnvelopeKeyOff(ref channel6.slots[1], EnvelopeKeyType.drum);
                }
            }
            else
            {
                for (chnum = 6; chnum < 9; chnum++)
                {
                    chip.channel[chnum].chtype = ChannelType.ch_2op;
                    OPL3_ChannelSetupAlg(ref chip.channel[chnum]);
                    OPL3_EnvelopeKeyOff(ref chip.channel[chnum].slots[0], EnvelopeKeyType.drum);
                    OPL3_EnvelopeKeyOff(ref chip.channel[chnum].slots[1], EnvelopeKeyType.drum);
                }
            }
        }

        public static void OPL3_ChannelWriteA0(ref opl3_channel channel, byte data)
        {
            if (channel.chip.newm != 0 && channel.chtype == ChannelType.ch_4op2)
            {
                return;
            }
            channel.f_num = (ushort)((channel.f_num & 0x300) | data);
            channel.ksv = (byte)((channel.block << 1) | ((channel.f_num >> (0x09 - channel.chip.nts)) & 0x01));
            OPL3_EnvelopeUpdateKSL(ref channel.slots[0]);
            OPL3_EnvelopeUpdateKSL(ref channel.slots[1]);
            if (channel.chip.newm != 0 && channel.chtype == ChannelType.ch_4op)
            {
                channel.pair.f_num = channel.f_num;
                channel.pair.ksv = channel.ksv;
                OPL3_EnvelopeUpdateKSL(ref channel.pair.slots[0]);
                OPL3_EnvelopeUpdateKSL(ref channel.pair.slots[1]);
            }
        }

        public static void OPL3_ChannelWriteB0(ref opl3_channel channel, byte data)
        {
            if (channel.chip.newm != 0 && channel.chtype == ChannelType.ch_4op2)
            {
                return;
            }
            channel.f_num = (ushort)((channel.f_num & 0xff) | ((data & 0x03) << 8));
            channel.block = (byte)((data >> 2) & 0x07);
            channel.ksv = (byte)((channel.block << 1) | ((channel.f_num >> (0x09 - channel.chip.nts)) & 0x01));
            OPL3_EnvelopeUpdateKSL(ref channel.slots[0]);
            OPL3_EnvelopeUpdateKSL(ref channel.slots[1]);
            if (channel.chip.newm != 0 && channel.chtype == ChannelType.ch_4op)
            {
                channel.pair.f_num = channel.f_num;
                channel.pair.block = channel.block;
                channel.pair.ksv = channel.ksv;
                OPL3_EnvelopeUpdateKSL(ref channel.pair.slots[0]);
                OPL3_EnvelopeUpdateKSL(ref channel.pair.slots[1]);
            }
        }

        public static void OPL3_ChannelSetupAlg(ref opl3_channel channel)
        {
            unsafe
            {
                if (channel.chtype == ChannelType.ch_drum)
                {
                    if (channel.ch_num == 7 || channel.ch_num == 8)
                    {
                        fixed (short* p = &channel.chip.zeromod)
                        {
                            channel.slots[0].mod = p;
                            channel.slots[1].mod = p;
                        }
                        return;
                    }
                    switch (channel.alg & 0x01)
                    {
                        case 0x00:
                            fixed (short* f = &channel.slots[0].fbmod)
                            {
                                fixed (short* o = &channel.slots[0]._out)
                                {
                                    channel.slots[0].mod = f;
                                    channel.slots[1].mod = o;
                                }
                            }
                            break;
                        case 0x01:
                            fixed (short* f = &channel.slots[0].fbmod)
                            {
                                fixed (short* o = &channel.chip.zeromod)
                                {
                                    channel.slots[0].mod = f;
                                    channel.slots[1].mod = o;
                                }
                            }
                            break;
                    }
                    return;
                }
                if ((channel.alg & 0x08) != 0)
                {
                    return;
                }
                if ((channel.alg & 0x04) != 0)
                {
                    channel.pair._out[0] = channel.chip.zeromod;
                    channel.pair._out[1] = channel.chip.zeromod;
                    channel.pair._out[2] = channel.chip.zeromod;
                    channel.pair._out[3] = channel.chip.zeromod;
                    switch (channel.alg & 0x03)
                    {
                        case 0x00:
                            fixed (short* f = &channel.pair.slots[0].fbmod)
                            {
                                fixed (short* o = &channel.pair.slots[0]._out)
                                {
                                    channel.pair.slots[0].mod = f;
                                    channel.pair.slots[1].mod = o;
                                }
                            }
                            fixed (short* f = &channel.pair.slots[1]._out)
                            {
                                fixed (short* o = &channel.slots[0]._out)
                                {
                                    channel.slots[0].mod = f;
                                    channel.slots[1].mod = o;
                                }
                            }
                            channel._out[0] = channel.slots[1]._out;
                            channel._out[1] = channel.chip.zeromod;
                            channel._out[2] = channel.chip.zeromod;
                            channel._out[3] = channel.chip.zeromod;
                            break;
                        case 0x01:
                            //    channel.pair.slots[0].mod = channel.pair.slots[0].fbmod;
                            //    channel.pair.slots[1].mod = channel.pair.slots[0]._out;
                            //    channel.slots[0].mod = channel.chip.zeromod;
                            //    channel.slots[1].mod = channel.slots[0]._out;
                            //    channel._out[0] = channel.pair.slots[1]._out;
                            //    channel._out[1] = channel.slots[1]._out;
                            //    channel._out[2] = channel.chip.zeromod;
                            //    channel._out[3] = channel.chip.zeromod;
                            break;
                        case 0x02:
                        //    channel.pair.slots[0].mod = channel.pair.slots[0].fbmod;
                        //    channel.pair.slots[1].mod = channel.chip.zeromod;
                        //    channel.slots[0].mod = channel.pair.slots[1]._out;
                        //    channel.slots[1].mod = channel.slots[0]._out;
                        //    channel._out[0] = channel.pair.slots[0]._out;
                        //    channel._out[1] = channel.slots[1]._out;
                        //    channel._out[2] = channel.chip.zeromod;
                        //    channel._out[3] = channel.chip.zeromod;
                        //    break;
                        case 0x03:
                            //    channel.pair.slots[0].mod = channel.pair.slots[0].fbmod;
                            //    channel.pair.slots[1].mod = channel.chip.zeromod;
                            //    channel.slots[0].mod = channel.pair.slots[1]._out;
                            //    channel.slots[1].mod = channel.chip.zeromod;
                            //    channel._out[0] = channel.pair.slots[0]._out;
                            //    channel._out[1] = channel.slots[0]._out;
                            //    channel._out[2] = channel.slots[1]._out;
                            //    channel._out[3] = channel.chip.zeromod;
                            break;
                    }
                }
                else
                {
                    switch (channel.alg & 0x01)
                    {
                        case 0x00:
                            fixed (short* f = &channel.slots[0].fbmod)
                            {
                                fixed (short* o = &channel.slots[0]._out)
                                {
                                    channel.slots[0].mod = f;
                                    channel.slots[1].mod = o;
                                }
                            }
                            channel._out[0] = channel.slots[1]._out;
                            channel._out[1] = channel.chip.zeromod;
                            channel._out[2] = channel.chip.zeromod;
                            channel._out[3] = channel.chip.zeromod;
                            break;
                        case 0x01:
                            //channel.slots[0].mod = channel.slots[0].fbmod;
                            //channel.slots[1].mod = channel.chip.zeromod;

                            channel._out[0] = channel.slots[0]._out;
                            channel._out[1] = channel.slots[1]._out;
                            channel._out[2] = channel.chip.zeromod;
                            channel._out[3] = channel.chip.zeromod;
                            break;
                    }
                }
            }
        }

        public static void OPL3_ChannelWriteC0(ref opl3_channel channel, byte data)
        {
            channel.fb = (byte)((data & 0x0e) >> 1);
            channel.con = (byte)(data & 0x01);
            channel.alg = channel.con;
            if ((channel.chip.newm) != 0)
            {
                if (channel.chtype == ChannelType.ch_4op)
                {
                    channel.pair.alg = (byte)(0x04 | (channel.con << 1) | (channel.pair.con));
                    channel.alg = 0x08;
                    OPL3_ChannelSetupAlg(ref channel.pair);
                }
                else if (channel.chtype == ChannelType.ch_4op2)
                {
                    channel.alg = (byte)(0x04 | (channel.pair.con << 1) | (channel.con));
                    channel.pair.alg = 0x08;
                    OPL3_ChannelSetupAlg(ref channel);
                }
                else
                {
                    OPL3_ChannelSetupAlg(ref channel);
                }
            }
            else
            {
                OPL3_ChannelSetupAlg(ref channel);
            }
            if (channel.chip.newm != 0)
            {
                channel.cha = (ushort)(((data >> 4) & 0x01) != 0 ? ~0 : 0);
                channel.chb = (ushort)(((data >> 5) & 0x01) != 0 ? ~0 : 0);
            }
            else
            {
                channel.cha = channel.chb = unchecked((ushort)~0);
            }
        }

        public static void OPL3_ChannelKeyOn(ref opl3_channel channel)
        {
            if (channel.chip.newm != 0)
            {
                if (channel.chtype == ChannelType.ch_4op)
                {
                    OPL3_EnvelopeKeyOn(ref channel.slots[0], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOn(ref channel.slots[1], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOn(ref channel.pair.slots[0], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOn(ref channel.pair.slots[1], EnvelopeKeyType.norm);
                }
                else if (channel.chtype == ChannelType.ch_2op || channel.chtype == ChannelType.ch_drum)
                {
                    OPL3_EnvelopeKeyOn(ref channel.slots[0], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOn(ref channel.slots[1], EnvelopeKeyType.norm);
                }
            }
            else
            {
                OPL3_EnvelopeKeyOn(ref channel.slots[0], EnvelopeKeyType.norm);
                OPL3_EnvelopeKeyOn(ref channel.slots[1], EnvelopeKeyType.norm);
            }
        }

        public static void OPL3_ChannelKeyOff(ref opl3_channel channel)
        {
            if (channel.chip.newm != 0)
            {
                if (channel.chtype == ChannelType.ch_4op)
                {
                    OPL3_EnvelopeKeyOff(ref channel.slots[0], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOff(ref channel.slots[1], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOff(ref channel.pair.slots[0], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOff(ref channel.pair.slots[1], EnvelopeKeyType.norm);
                }
                else if (channel.chtype == ChannelType.ch_2op || channel.chtype == ChannelType.ch_drum)
                {
                    OPL3_EnvelopeKeyOff(ref channel.slots[0], EnvelopeKeyType.norm);
                    OPL3_EnvelopeKeyOff(ref channel.slots[1], EnvelopeKeyType.norm);
                }
            }
            else
            {
                OPL3_EnvelopeKeyOff(ref channel.slots[0], EnvelopeKeyType.norm);
                OPL3_EnvelopeKeyOff(ref channel.slots[1], EnvelopeKeyType.norm);
            }
        }

        public static void OPL3_ChannelSet4Op(ref opl3_chip chip, byte data)
        {
            byte bit;
            byte chnum;
            for (bit = 0; bit < 6; bit++)
            {
                chnum = bit;
                if (bit >= 3)
                {
                    chnum += 9 - 3;
                }
                if (((data >> bit) & 0x01) != 0)
                {
                    chip.channel[chnum].chtype = ChannelType.ch_4op;
                    chip.channel[chnum + 3].chtype = ChannelType.ch_4op2;
                }
                else
                {
                    chip.channel[chnum].chtype = ChannelType.ch_2op;
                    chip.channel[chnum + 3].chtype = ChannelType.ch_2op;
                }
            }
        }

        static short OPL3_ClipSample(int sample)
        {
            if (sample > 32767)
            {
                sample = 32767;
            }
            else if (sample < -32768)
            {
                sample = -32768;
            }
            return (short)sample;
        }

        public static void OPL3_Generate(ref opl3_chip chip, short[] buf)
        {
            byte ii;
            byte jj;
            short accm;
            byte shift = 0;

            buf[1] = OPL3_ClipSample(chip.mixbuff[1]);

            for (ii = 0; ii < 15; ii++)
            {
                OPL3_SlotCalcFB(ref chip.slot[ii]);
                OPL3_EnvelopeCalc(ref chip.slot[ii]);
                OPL3_PhaseGenerate(ref chip.slot[ii]);
                OPL3_SlotGenerate(ref chip.slot[ii]);
            }

            chip.mixbuff[0] = 0;
            for (ii = 0; ii < 18; ii++)
            {
                accm = 0;
                for (jj = 0; jj < 4; jj++)
                {
                    accm += chip.channel[ii]._out[jj];
                }
                chip.mixbuff[0] += (short)(accm & chip.channel[ii].cha);
            }

            for (ii = 15; ii < 18; ii++)
            {
                OPL3_SlotCalcFB(ref chip.slot[ii]);
                OPL3_EnvelopeCalc(ref chip.slot[ii]);
                OPL3_PhaseGenerate(ref chip.slot[ii]);
                OPL3_SlotGenerate(ref chip.slot[ii]);
            }

            buf[0] = OPL3_ClipSample(chip.mixbuff[0]);

            for (ii = 18; ii < 33; ii++)
            {
                OPL3_SlotCalcFB(ref chip.slot[ii]);
                OPL3_EnvelopeCalc(ref chip.slot[ii]);
                OPL3_PhaseGenerate(ref chip.slot[ii]);
                OPL3_SlotGenerate(ref chip.slot[ii]);
            }

            chip.mixbuff[1] = 0;
            for (ii = 0; ii < 18; ii++)
            {
                accm = 0;
                for (jj = 0; jj < 4; jj++)
                {
                    accm += chip.channel[ii]._out[jj];
                }
                chip.mixbuff[1] += (short)(accm & chip.channel[ii].chb);
            }

            for (ii = 33; ii < 36; ii++)
            {
                OPL3_SlotCalcFB(ref chip.slot[ii]);
                OPL3_EnvelopeCalc(ref chip.slot[ii]);
                OPL3_PhaseGenerate(ref chip.slot[ii]);
                OPL3_SlotGenerate(ref chip.slot[ii]);
            }

            if ((chip.timer & 0x3f) == 0x3f)
            {
                chip.tremolopos = (byte)((chip.tremolopos + 1) % 210);
            }
            if (chip.tremolopos < 105)
            {
                chip.tremolo = (byte)(chip.tremolopos >> chip.tremoloshift);
            }
            else
            {
                chip.tremolo = (byte)((210 - chip.tremolopos) >> chip.tremoloshift);
            }

            if ((chip.timer & 0x3ff) == 0x3ff)
            {
                chip.vibpos = (byte)((chip.vibpos + 1) & 7);
            }

            chip.timer++;

            chip.eg_add = 0;
            if (chip.eg_timer != 0)
            {
                while (shift < 36 && ((chip.eg_timer >> shift) & 1) == 0)
                {
                    shift++;
                }
                if (shift > 12)
                {
                    chip.eg_add = 0;
                }
                else
                {
                    chip.eg_add = (byte)(shift + 1);
                }
            }

            if (chip.eg_timerrem != 0 || chip.eg_state != 0)
            {
                if (chip.eg_timer == 0xfffffffff)
                {
                    chip.eg_timer = 0;
                    chip.eg_timerrem = 1;
                }
                else
                {
                    chip.eg_timer++;
                    chip.eg_timerrem = 0;
                }
            }

            chip.eg_state ^= 1;

            while (chip.writebuf[chip.writebuf_cur].time <= chip.writebuf_samplecnt)
            {
                if ((chip.writebuf[chip.writebuf_cur].reg & 0x200) == 0)
                {
                    break;
                }
                chip.writebuf[chip.writebuf_cur].reg &= 0x1ff;
                OPL3_WriteReg(ref chip, chip.writebuf[chip.writebuf_cur].reg, chip.writebuf[chip.writebuf_cur].data);
                chip.writebuf_cur = (uint)((chip.writebuf_cur + 1) % OPL_WRITEBUF_SIZE);
            }
            chip.writebuf_samplecnt++;
        }

        public static void OPL3_GenerateResampled(ref opl3_chip chip, short[] buf, int pos)
        {
            while (chip.samplecnt >= chip.rateratio)
            {
                chip.oldsamples[0] = chip.samples[0];
                chip.oldsamples[1] = chip.samples[1];
                Nuked.OPL3_Generate(ref chip, chip.samples);
                chip.samplecnt -= chip.rateratio;
            }
            buf[pos] = (short)((chip.oldsamples[0] * (chip.rateratio - chip.samplecnt) + chip.samples[0] * chip.samplecnt) / chip.rateratio);
            buf[pos + 1] = (short)((chip.oldsamples[1] * (chip.rateratio - chip.samplecnt) + chip.samples[1] * chip.samplecnt) / chip.rateratio);
            chip.samplecnt += 1 << RSM_FRAC;
        }

        public static void OPL3_Reset(ref opl3_chip chip, int samplerate)
        {
            byte slotnum;
            byte channum;

            chip = new opl3_chip();
            for (slotnum = 0; slotnum < 36; slotnum++)
            {
                chip.slot[slotnum] = new opl3_slot();
                chip.slot[slotnum].chip = chip;
                unsafe
                {
                    fixed (short* p = &chip.zeromod)
                    {
                        chip.slot[slotnum].mod = p;
                    }
                }
                chip.slot[slotnum].eg_rout = 0x1ff;
                chip.slot[slotnum].eg_out = 0x1ff;
                chip.slot[slotnum].eg_gen = envelope_gen_num.release;
                //chip.slot[slotnum].trem = (byte)chip.zeromod;
                chip.slot[slotnum].tremZero = true;
                chip.slot[slotnum].slot_num = slotnum;
            }
            for (channum = 0; channum < 18; channum++)
            {
                chip.channel[channum] = new opl3_channel();
                chip.channel[channum].slots[0] = chip.slot[ch_slot[channum]];
                chip.channel[channum].slots[1] = chip.slot[ch_slot[channum] + 3];
                chip.slot[ch_slot[channum]].channel = chip.channel[channum];
                chip.slot[ch_slot[channum] + 3].channel = chip.channel[channum];
                if ((channum % 9) < 3)
                {
                    chip.channel[channum].pair = chip.channel[channum + 3];
                }
                else if ((channum % 9) < 6)
                {
                    chip.channel[channum].pair = chip.channel[channum - 3];
                }
                chip.channel[channum].chip = chip;
                chip.channel[channum]._out[0] = chip.zeromod;
                chip.channel[channum]._out[1] = chip.zeromod;
                chip.channel[channum]._out[2] = chip.zeromod;
                chip.channel[channum]._out[3] = chip.zeromod;
                chip.channel[channum].chtype = ChannelType.ch_2op;
                chip.channel[channum].cha = 0xffff;
                chip.channel[channum].chb = 0xffff;
                chip.channel[channum].ch_num = channum;
                OPL3_ChannelSetupAlg(ref chip.channel[channum]);
            }
            chip.noise = 1;
            chip.rateratio = (int)((samplerate << RSM_FRAC) / 49716);
            chip.tremoloshift = 4;
            chip.vibshift = 1;
        }

        public static void OPL3_WriteReg(ref opl3_chip chip, ushort reg, byte v)
        {
            byte high = (byte)((reg >> 8) & 0x01);
            byte regm = (byte)(reg & 0xff);
            switch (regm & 0xf0)
            {
                case 0x00:
                    if (high != 0)
                    {
                        switch (regm & 0x0f)
                        {
                            case 0x04:
                                OPL3_ChannelSet4Op(ref chip, v);
                                break;
                            case 0x05:
                                chip.newm = (byte)(v & 0x01);
                                break;
                        }
                    }
                    else
                    {
                        switch (regm & 0x0f)
                        {
                            case 0x08:
                                chip.nts = (byte)((v >> 6) & 0x01);
                                break;
                        }
                    }
                    break;
                case 0x20:
                case 0x30:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite20(ref chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0x40:
                case 0x50:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite40(ref chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0x60:
                case 0x70:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite60(ref chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0x80:
                case 0x90:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWrite80(ref chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0xe0:
                case 0xf0:
                    if (ad_slot[regm & 0x1f] >= 0)
                    {
                        OPL3_SlotWriteE0(ref chip.slot[18 * high + ad_slot[regm & 0x1f]], v);
                    }
                    break;
                case 0xa0:
                    if ((regm & 0x0f) < 9)
                    {
                        OPL3_ChannelWriteA0(ref chip.channel[(int)(9 * high + (regm & 0x0f))], v);
                    }
                    break;
                case 0xb0:
                    if (regm == 0xbd && high == 0)
                    {
                        chip.tremoloshift = (byte)((((v >> 7) ^ 1) << 1) + 2);
                        chip.vibshift = (byte)(((v >> 6) & 0x01) ^ 1);
                        OPL3_ChannelUpdateRhythm(ref chip, v);
                    }
                    else if ((regm & 0x0f) < 9)
                    {
                        OPL3_ChannelWriteB0(ref chip.channel[9 * high + (regm & 0x0f)], v);
                        if ((v & 0x20) != 0)
                        {
                            OPL3_ChannelKeyOn(ref chip.channel[9 * high + (regm & 0x0f)]);
                        }
                        else
                        {
                            OPL3_ChannelKeyOff(ref chip.channel[9 * high + (regm & 0x0f)]);
                        }
                    }
                    break;
                case 0xc0:
                    if ((regm & 0x0f) < 9)
                    {
                        OPL3_ChannelWriteC0(ref chip.channel[9 * high + (regm & 0x0f)], v);
                    }
                    break;
            }
        }

        public static void OPL3_WriteRegBuffered(ref opl3_chip chip, ushort reg, byte v)
        {
            ulong time1, time2;

            if ((chip.writebuf[chip.writebuf_last].reg & 0x200) != 0)
            {
                OPL3_WriteReg(ref chip, (ushort)(chip.writebuf[chip.writebuf_last].reg & 0x1ff), chip.writebuf[chip.writebuf_last].data);

                chip.writebuf_cur = (uint)((chip.writebuf_last + 1) % OPL_WRITEBUF_SIZE);
                chip.writebuf_samplecnt = chip.writebuf[chip.writebuf_last].time;
            }

            chip.writebuf[chip.writebuf_last].reg = (ushort)(reg | 0x200);
            chip.writebuf[chip.writebuf_last].data = v;
            time1 = chip.writebuf_lasttime + (ulong)OPL_WRITEBUF_DELAY;
            time2 = chip.writebuf_samplecnt;

            if (time1 < time2)
            {
                time1 = time2;
            }

            chip.writebuf[chip.writebuf_last].time = time1;
            chip.writebuf_lasttime = time1;
            chip.writebuf_last = (chip.writebuf_last + 1) % (uint)OPL_WRITEBUF_SIZE;
        }

        public static void OPL3_GenerateStream(ref opl3_chip chip, short[] sndptr, uint numsamples, int pos)
        {

            int x = 0;
            for (int i = 0; i < numsamples / 2; i++)
            {
                OPL3_GenerateResampled(ref chip, sndptr, x);
                x += 2;
                //sndptr += 2;
            }
        }


    }
}
