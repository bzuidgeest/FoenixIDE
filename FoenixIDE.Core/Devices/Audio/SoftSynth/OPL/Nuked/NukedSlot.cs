using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.Nuked
{

    public class opl3_slot
    {
        public opl3_channel channel;
        public opl3_chip chip;
        public short _out;
        public short fbmod;
        unsafe public short* mod;
        public short prout;
        public short eg_rout;
        public short eg_out;
        public byte eg_inc;
        public envelope_gen_num eg_gen;
        public byte eg_rate;
        public byte eg_ksl;
        //unsafe public byte* trem;
        public bool tremZero = true;
        public byte reg_vib;
        public byte reg_type;
        public byte reg_ksr;
        public byte reg_mult;
        public byte reg_ksl;
        public byte reg_tl;
        public byte reg_ar;
        public byte reg_dr;
        public byte reg_sl;
        public byte reg_rr;
        public byte reg_wf;
        public byte key;
        public uint pg_reset;
        public uint pg_phase;
        public ushort pg_phase_out;
        public byte slot_num;
    }

    public class opl3_channel
    {
        public opl3_slot[] slots = new opl3_slot[2];
        public opl3_channel pair;
        public opl3_chip chip;
        public short[] _out = new short[4];
        public ChannelType chtype;
        public ushort f_num;
        public byte block;
        public byte fb;
        public byte con;
        public byte alg;
        public byte ksv;
        public ushort cha, chb;
        public byte ch_num;
    };

    public class opl3_writebuf
    {
        public ulong time;
        public ushort reg;
        public byte data;
    }

    public class opl3_chip
    {
        public opl3_channel[] channel;
        public opl3_slot[] slot;
        public ushort timer;
        public ulong eg_timer;
        public byte eg_timerrem;
        public byte eg_state;
        public byte eg_add;
        public byte newm;
        public byte nts;
        public byte rhy;
        public byte vibpos;
        public byte vibshift;
        public byte tremolo;
        public byte tremolopos;
        public byte tremoloshift;
        public uint noise;
        public short zeromod;
        public int[] mixbuff;
        public byte rm_hh_bit2;
        public byte rm_hh_bit3;
        public byte rm_hh_bit7;
        public byte rm_hh_bit8;
        public byte rm_tc_bit3;
        public byte rm_tc_bit5;
        //OPL3L
        public int rateratio;
        public int samplecnt;
        public short[] oldsamples;
        public short[] samples;

        public ulong writebuf_samplecnt;
        public uint writebuf_cur;
        public uint writebuf_last;
        public ulong writebuf_lasttime;
        public opl3_writebuf[] writebuf;

        public opl3_chip()
        {
            writebuf = new opl3_writebuf[Nuked.OPL_WRITEBUF_SIZE];
            for (int i = 0; i < Nuked.OPL_WRITEBUF_SIZE; i++)
            {
                writebuf[i] = new opl3_writebuf();
            }
            oldsamples = new short[2];
            samples = new short[2];
            channel = new opl3_channel[18];
            slot = new opl3_slot[36];
            mixbuff = new int[2];
        }
    };
}
