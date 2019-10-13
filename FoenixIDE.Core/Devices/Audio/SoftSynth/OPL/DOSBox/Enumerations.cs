using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    //Different synth modes that can generate blocks of data
    public enum SynthMode
    {
        sm2AM,
        sm2FM,
        sm3AM,
        sm3FM,
        sm4Start,
        sm3FMFM,
        sm3AMFM,
        sm3FMAM,
        sm3AMAM,
        sm6Start,
        sm2Percussion,
        sm3Percussion
    }

    //Shifts for the values contained in chandata variable
    public enum Shift
    {
        KSLBASE = 16,
        KEYCODE = 24
    };

    public enum Mask
    {
        KSR = 0x10,
        SUSTAIN = 0x20,
        VIBRATO = 0x40,
        TREMOLO = 0x80
    };

    public enum State
    {
        OFF,
        RELEASE,
        SUSTAIN,
        DECAY,
        ATTACK
    }
}
