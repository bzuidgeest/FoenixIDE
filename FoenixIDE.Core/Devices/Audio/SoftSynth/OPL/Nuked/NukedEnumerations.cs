using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.Nuked
{

    // Channel types
    public enum ChannelType
    {
        ch_2op = 0,
        ch_4op = 1,
        ch_4op2 = 2,
        ch_drum = 3
    };

    // Envelope key types
    public enum EnvelopeKeyType
    {
        norm = 0x01,
        drum = 0x02
    };

    public enum envelope_gen_num
    {
        attack = 0,
        decay = 1,
        sustain = 2,
        release = 3
    };

    }
