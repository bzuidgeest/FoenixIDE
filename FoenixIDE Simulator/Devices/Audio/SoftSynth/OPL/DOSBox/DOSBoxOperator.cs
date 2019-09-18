using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    partial class DosBoxOPL
    {
        public class Operator
        {

            //Masks for operator 20 values


            VolumeHandler volHandler;

#if (WAVE_HANDLER)
            WaveHandler waveHandler;    //Routine that generate a wave
#else
            short[] waveBase;
	        uint waveMask;
	        uint waveStart;
#endif
            uint waveIndex;           //WAVE_BITS shifted counter of the frequency index
            uint waveAdd;             //The base frequency without vibrato
            uint waveCurrent;         //waveAdd + vibratao

            public uint chanData;            //Frequency/octave and derived data coming from whatever channel controls this
            uint freqMul;             //Scale channel frequency with this, TODO maybe remove?
            uint vibrato;             //Scaled up vibrato strength
            int sustainLevel;        //When stopping at sustain level stop here
            int totalLevel;          //totalLevel is added to every generated volume
            uint currentLevel;        //totalLevel + tremolo
            int volume;              //The currently active volume

            uint attackAdd;           //Timers for the different states of the envelope
            uint decayAdd;
            uint releaseAdd;
            uint rateIndex;           //Current position of the evenlope

            byte rateZero;             //int for the different states of the envelope having no changes
            byte keyOn;                //Bitmask of different values that can generate keyon
                                       //Registers, also used to check for changes
            byte reg20, reg40, reg60, reg80, regE0;
            //Active part of the envelope we're in
            State state;
            //0xff when tremolo is enabled
            byte tremoloMask;
            //Strength of the vibrato
            byte vibStrength;
            //Keep track of the calculated KSR so we can check for changes
            byte ksr;


            public void UpdateAttenuation()
            {
                byte kslBase = (byte)((chanData >> (int)Shift.KSLBASE) & 0xff);
                uint tl = (uint)(reg40 & 0x3f);
                byte kslShift = KslShiftTable[reg40 >> 6];
                //Make sure the attenuation goes to the right bits
                totalLevel = (int)(tl << (ENV_BITS - 7));  //Total level goes 2 bits below max
                totalLevel += (kslBase << ENV_EXTRA) >> kslShift;
            }

            public void UpdateRates(Chip chip)
            {
                //Mame seems to reverse this where enabling ksr actually lowers
                //the rate, but pdf manuals says otherwise?
                byte newKsr = (byte)((chanData >> (int)Shift.KEYCODE) & 0xff);
                if ((reg20 & (int)Mask.KSR) == 0)
                {
                    newKsr >>= 2;
                }
                if (ksr == newKsr)
                    return;
                ksr = newKsr;
                UpdateAttack(chip);
                UpdateDecay(chip);
                UpdateRelease(chip);
            }

            public void UpdateFrequency()
            {
                uint freq = chanData & ((1 << 10) - 1);
                uint block = (chanData >> 10) & 0xff;
#if WAVE_PRECISION
            block = 7 - block;
            waveAdd = (freq * freqMul) >> block;
#else
                waveAdd = (freq << (int)block) * freqMul;
#endif
                if ((reg20 & (int)Mask.VIBRATO) != 0)
                {
                    vibStrength = (byte)(freq >> 7);

#if WAVE_PRECISION
                vibrato = (vibStrength * freqMul) >> block;
#else
                    vibrato = (uint)((vibStrength << (int)block) * freqMul);
#endif
                }
                else
                {
                    vibStrength = 0;
                    vibrato = 0;
                }
            }

            public void Write20(Chip chip, byte val)
            {
                byte change = (byte)(reg20 ^ val);
                if (change == 0)
                    return;
                reg20 = val;
                //Shift the tremolo bit over the entire register, saved a branch, YES!
                tremoloMask = (byte)((sbyte)(val) >> 7);
                tremoloMask &= unchecked((byte)~((1 << ENV_EXTRA) - 1));
                //Update specific features based on changes
                if ((change & (int)Mask.KSR) != 0)
                {
                    UpdateRates(chip);
                }
                //With sustain enable the volume doesn't change
                if ((reg20 & (int)Mask.SUSTAIN) != 0 || (releaseAdd == 0))
                {
                    rateZero |= (1 << (int)State.SUSTAIN);
                }
                else
                {
                    rateZero &= unchecked((byte)~(1 << (int)State.SUSTAIN));
                }
                //Frequency multiplier or vibrato changed
                if ((change & (0xf | (int)Mask.VIBRATO)) != 0)
                {
                    freqMul = chip.freqMul[val & 0xf];
                    UpdateFrequency();
                }
            }

            public void Write40(Chip chip, byte val)
            {
                if ((reg40 ^ val) == 0)
                    return;
                reg40 = val;
                UpdateAttenuation();
            }

            public void Write60(Chip chip, byte val)
            {
                byte change = (byte)(reg60 ^ val);
                reg60 = val;
                if ((change & 0x0f) != 0)
                {
                    UpdateDecay(chip);
                }
                if ((change & 0xf0) != 0)
                {
                    UpdateAttack(chip);
                }
            }

            public void Write80(Chip chip, byte val)
            {
                byte change = (byte)(reg80 ^ val);
                if (change == 0)
                    return;
                reg80 = val;
                byte sustain = (byte)(val >> 4);
                //Turn 0xf into 0x1f
                sustain |= (byte)((sustain + 1) & 0x10);
                sustainLevel = sustain << (ENV_BITS - 5);
                if ((change & 0x0f) != 0)
                {
                    UpdateRelease(chip);
                }
            }

            public void WriteE0(Chip chip, byte val)
            {
                if ((regE0 ^ val) == 0)
                    return;
                //in opl3 mode you can always selet 7 waveforms regardless of waveformselect
                byte waveForm = (byte)(val & ((0x3 & chip.waveFormMask) | (0x7 & chip.opl3Active)));
                regE0 = val;
#if WAVE_HANDLER
                waveHandler = WaveHandlerTable[waveForm];
#else
                waveBase = new short[WaveTable.Length - WaveBaseTable[waveForm]];
                Array.Copy(WaveTable, WaveBaseTable[waveForm], waveBase, 0, WaveTable.Length - WaveBaseTable[waveForm]);
	            //waveBase = WaveTable + WaveBaseTable[ waveForm ];
	            waveStart = (uint)(WaveStartTable[ waveForm ] << WAVE_SH);
	            waveMask = WaveMaskTable[ waveForm ];
#endif
            }

            public bool Silent()
            {
                if (!ENV_SILENT(totalLevel + volume))
                    return false;
                if ((rateZero & (1 << (int)state)) == 0)
                    return false;
                return true;
            }
            
            public void Prepare(Chip chip)
            {
                currentLevel = (uint)(totalLevel + (chip.tremoloValue & tremoloMask));
                waveCurrent = waveAdd;
                if ((vibStrength >> chip.vibratoShift) != 0)
                {
                    int add = (int)(vibrato >> chip.vibratoShift);
                    //Sign extend over the shift value
                    int neg = chip.vibratoSign;
                    //Negate the add with -1 or 0
                    add = (add ^ neg) - neg;
                    waveCurrent += (uint)add;
                }

            }

            public void KeyOn(byte mask)
            {
                if (keyOn == 0)
                {
                    //Restart the frequency generator
#if WAVE_TABLEMUL || WAVE_TABLELOG
		waveIndex = waveStart;
#else
                    waveIndex = 0;
#endif
                    rateIndex = 0;
                    SetState(State.ATTACK);
                }
                keyOn |= mask;
            }

            public void KeyOff(byte mask)
            {
                keyOn &= (byte)~mask;
                if (keyOn == 0)
                {
                    if (state != State.OFF)
                    {
                        SetState(State.RELEASE);
                    }
                }
            }
            
            public int TemplateVolume(State state)
            {
                int vol = volume;
                int change;
                switch (state)
                {
                    case State.OFF:
                        return ENV_MAX;
                    case State.ATTACK:
                        change = RateForward(attackAdd);
                        if (change == 0)
                            return vol;
                        vol += ((~vol) * change) >> 3;
                        if (vol < ENV_MIN)
                        {
                            volume = ENV_MIN;
                            rateIndex = 0;
                            SetState(State.DECAY);
                            return ENV_MIN;
                        }
                        break;
                    case State.DECAY:
                        vol += RateForward(decayAdd);
                        if (vol >= sustainLevel)
                        {
                            //Check if we didn't overshoot max attenuation, then just go off
                            if (vol >= ENV_MAX)
                            {
                                volume = ENV_MAX;
                                SetState(State.OFF);
                                return ENV_MAX;
                            }
                            //Continue as sustain
                            rateIndex = 0;
                            SetState(State.SUSTAIN);
                        }
                        break;
                    case State.SUSTAIN:
                        if ((reg20 & (int)Mask.SUSTAIN) != 0)
                        {
                            return vol;
                        }
                        //In sustain phase, but not sustaining, do regular release
                        //fall through
                        goto case State.RELEASE;
                    case State.RELEASE:
                        vol += RateForward(releaseAdd);
                        if (vol >= ENV_MAX)
                        {
                            volume = ENV_MAX;
                            SetState(State.OFF);
                            return ENV_MAX;
                        }
                        break;
                }
                volume = vol;
                return vol;
            }

            public int RateForward(uint add)
            {
                rateIndex += add;
                int ret = (int)(rateIndex >> RATE_SH);
                rateIndex = rateIndex & RATE_MASK;
                return ret;
            }


            public uint ForwardWave()
            {
                waveIndex += waveCurrent;
                return waveIndex >> WAVE_SH;
            }

            public uint ForwardVolume()
            {
                return (uint)(currentLevel + volHandler());
            }

            public int GetSample(int modulation)
            {
                uint vol = ForwardVolume();
                if (ENV_SILENT((int)vol))
                {
                    //Simply forward the wave
                    waveIndex += waveCurrent;
                    return 0;
                }
                else
                {
                    uint index = ForwardWave();
                    index += (uint)modulation;
                    return GetWave(index, vol);
                }
            }

            public int GetWave(uint index, uint vol)
            {
#if (WAVE_HANDLER)
                return waveHandler(index, vol << (3 - ENV_EXTRA));
#elif (WAVE_TABLEMUL)
	            return (waveBase[ index & waveMask ] * MulTable[ vol >> ENV_EXTRA ]) >> MUL_SH;
#elif (WAVE_TABLELOG)
	            int wave = waveBase[ index & waveMask ];
	            uint total = ( wave & 0x7fff ) + vol << ( 3 - ENV_EXTRA );
	            int sig = ExpTable[ total & 0xff ];
	            uint exp = total >> 8;
	            int neg = wave >> 16;
	            return ((sig ^ neg) - neg) >> exp;
#else
                #error "No valid wave routine"
#endif
            }
            public Operator()
            {
                chanData = 0;
                freqMul = 0;
                waveIndex = 0;
                waveAdd = 0;
                waveCurrent = 0;
                keyOn = 0;
                ksr = 0;
                reg20 = 0;
                reg40 = 0;
                reg60 = 0;
                reg80 = 0;
                regE0 = 0;
                SetState(State.OFF);
                rateZero = (1 << (int)State.OFF);
                sustainLevel = ENV_MAX;
                currentLevel = ENV_MAX;
                totalLevel = ENV_MAX;
                volume = ENV_MAX;
                releaseAdd = 0;

            }

            static bool HasFlag(byte value, Mask mask)
            {
                return (value & (byte)mask) != 0;
            }
            protected void SetState(State s)
            {
                state = s;
                volHandler = () => TemplateVolume(s);
            }

            protected void UpdateAttack(Chip chip)
            {
                byte rate = (byte)(reg60 >> 4);
                if (rate != 0)
                {
                    byte val = (byte)((rate << 2) + ksr);
                    attackAdd = chip.attackRates[val];
                    rateZero &= unchecked((byte)~(1 << (int)State.ATTACK));
                }
                else
                {
                    attackAdd = 0;
                    rateZero |= (1 << (int)State.ATTACK);
                }
            }

            protected void UpdateRelease(Chip chip)
            {
                byte rate = (byte)(reg80 & 0xf);
                if (rate != 0)
                {
                    byte val = (byte)((rate << 2) + ksr);
                    releaseAdd = chip.linearRates[val];
                    rateZero &= unchecked((byte)~(1 << (int)State.RELEASE));
                    if ((reg20 & (byte)Mask.SUSTAIN) == 0)
                    {
                        rateZero &= unchecked((byte)~(1 << (int)State.SUSTAIN));
                    }
                }
                else
                {
                    rateZero |= (1 << (int)State.RELEASE);
                    releaseAdd = 0;
                    if ((reg20 & (byte)Mask.SUSTAIN) == 0)
                    {
                        rateZero |= (1 << (int)State.SUSTAIN);
                    }
                }
            }
            protected void UpdateDecay(Chip chip)
            {
                byte rate = (byte)(reg60 & 0xf);
                if (rate != 0)
                {
                    byte val = (byte)((rate << 2) + ksr);
                    decayAdd = chip.linearRates[val];
                    rateZero &= unchecked((byte)~(1 << (int)State.DECAY));
                }
                else
                {
                    decayAdd = 0;
                    rateZero |= (1 << (int)State.DECAY);
                }
            }

           

        }
    }


    /*
    static const VolumeHandler VolumeHandlerTable[5] = {
        &TemplateVolume< OFF >,
        &TemplateVolume< RELEASE >,
        &TemplateVolume< SUSTAIN >,
        &TemplateVolume< DECAY >,
        &TemplateVolume< ATTACK >
    };
    */




}