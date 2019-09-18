/*
 *  Copyright (C) 2002-2011  The DOSBox Team
 *
 *  This program is free software; you can redistribute it and/or modify
 *  it under the terms of the GNU General Public License as published by
 *  the Free Software Foundation; either version 2 of the License, or
 *  (at your option) any later version.
 *
 *  This program is distributed in the hope that it will be useful,
 *  but WITHOUT ANY WARRANTY; without even the implied warranty of
 *  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 *  GNU General Public License for more details.
 *
 *  You should have received a copy of the GNU General Public License
 *  along with this program; if not, write to the Free Software
 *  Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA 02111-1307, USA.
 */

/*
	DOSBox implementation of a combined Yamaha YMF262 and Yamaha YM3812 emulator.
	Enabling the opl3 bit will switch the emulator to stereo opl3 output instead of regular mono opl2
	Except for the table generation it's all integer math
	Can choose different types of generators, using muls and bigger tables, try different ones for slower platforms
	The generation was based on the MAME implementation but tried to have it use less memory and be faster in general
	MAME uses much bigger envelope tables and this will be the biggest cause of it sounding different at times

	//TODO Don't delay first operator 1 sample in opl3 mode
	//TODO Maybe not use class method pointers but a regular function pointers with operator as first parameter
	//TODO Fix panning for the Percussion channels, would any opl3 player use it and actually really change it though?
	//TODO Check if having the same accuracy in all frequency multipliers sounds better or not

	//DUNNO Keyon in 4op, switch to 2op without keyoff.
*/

// Last synch with DOSBox SVN trunk r3752

//#define WAVE_HANDLER //10
//#define WAVE_TABLEMUL //11
//#define WAVE_TABLELOG //12

using System;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    partial class DosBoxOPL
    {


        public const double OPLRATE = ((double)(14318180.0 / 288.0));
        public const int TREMOLO_TABLE = 52;

        //Try to use most precision for frequencies
        //Else try to keep different waves in synch
        //#define WAVE_PRECISION	1

#if !WAVE_PRECISION
        //Wave bits available in the top of the 32bit range
        //Original adlib uses 10.10, we use 10.22
        public const int WAVE_BITS = 10;
#else
        //Need some extra bits at the top to have room for octaves and frequency multiplier
        //We support to 8 times lower rate
        //128 * 15 * 8 = 15350, 2^13.9, so need 14 bits
        public const int WAVE_BITS = 14;
#endif

        public const int WAVE_SH = (32 - WAVE_BITS);
        public const int WAVE_MASK = ((1 << WAVE_SH) - 1);

        //Use the same accuracy as the waves
        public const int LFO_SH = (WAVE_SH - 10);
        //LFO is controlled by our tremolo 256 sample limit
        public const int LFO_MAX = (256 << (LFO_SH));

        //Maximum amount of attenuation bits
        //Envelope goes to 511, 9 bits

        //Uses the value directly
        public const int ENV_BITS = 9;


        //Limits of the envelope with those bits and when the envelope goes silent
        public const int ENV_MIN = 0;
        public const int ENV_EXTRA = (ENV_BITS - 9);
        public const int ENV_MAX = (511 << ENV_EXTRA);
        public const int ENV_LIMIT = ((12 * 256) >> (3 - ENV_EXTRA));

        public static bool ENV_SILENT(int x)
        {
            return x >= ENV_LIMIT;
        }

        //Attack/decay/release rate counter shift
        public const int RATE_SH = 24;
        public const int RATE_MASK = ((1 << RATE_SH) - 1);

        //Has to fit within 16bit lookuptable
        public const int MUL_SH = 16;

        delegate int VolumeHandler();

        delegate Channel SynthHandler(Chip chip, uint samples, int[] output, int pos);

#if WAVE_HANDLER || WAVE_TABLELOG
        static ushort[] ExpTable = new ushort[256];
#endif

#if WAVE_HANDLER
        //PI table used by WAVEHANDLER
        static ushort[] SinTable = new ushort[512];
#endif

#if WAVE_TABLEMUL
        static ushort[] MulTable = new ushort[384];
#endif




#if WAVE_TABLELOG || WAVE_TABLEMUL
        //Layout of the waveform table in 512 entry intervals
        //With overlapping waves we reduce the table to half it's size

        //	|    |//\\|____|WAV7|//__|/\  |____|/\/\|
        //	|\\//|    |    |WAV7|    |  \/|    |    |
        //	|06  |0126|17  |7   |3   |4   |4 5 |5   |

        //6 is just 0 shifted and masked

        static short[] WaveTable = new short[8 * 512];
        //Distance into WaveTable the wave starts
        static readonly ushort[] WaveBaseTable = new ushort[]{
            0x000, 0x200, 0x200, 0x800,
            0xa00, 0xc00, 0x100, 0x400,
        };

        //Where to start the counter on at keyon
        static readonly ushort[] WaveStartTable = new ushort[] {
            512, 0, 0, 0,
            0, 512, 512, 256,
        };

        //Mask the counter with this
        static readonly ushort[] WaveMaskTable = new ushort[] {
            1023, 1023, 511, 511,
            1023, 1023, 512, 1023,
        };
                
#endif

        public static byte[] KslTable = new byte[8 * 16];

        //How much to substract from the base value for the final attenuation
        public static readonly byte[] KslCreateTable = new byte[16] 
        {
	        //0 will always be be lower than 7 * 8
	        64, 32, 24, 19,
            16, 12, 11, 10,
             8,  6,  5,  4,
             3,  2,  1,  0,
        };

        public static byte[] TremoloTable = new byte[TREMOLO_TABLE];
        //Start of a channel behind the chip struct start
        static Func<Chip,Channel>[] chanOffsetTable = new Func<Chip,Channel>[32];
        //Start of an operator behind the chip struct start
        static Func<Chip,Operator>[] opOffsetTable = new Func<Chip,Operator>[64];

        static byte M(double x)
        {
            return (byte)(x * 2);
        }

        static readonly byte[] FreqCreateTable =
            {
                M(0.5), M(1), M(2), M(3), M(4), M(5), M(6), M(7),
                M(8), M(9), M(10), M(10), M(12), M(12), M(15), M(15)
            };

         //Generate a table index and table shift value using input value from a selected rate
        static void EnvelopeSelect(byte val, out byte index, out byte shift )
        {
            if (val < 13 * 4)
            {               //Rate 0 - 12
                shift = (byte)(12 - (val >> 2));
                index = (byte)(val & 3);
            }
            else if (val < 15 * 4)
            {       //rate 13 - 14
                shift = 0;
                index = (byte)(val - 12 * 4);
            }
            else
            {                           //rate 15 and up
                shift = 0;
                index = 12;
            }
        }

        //On a real opl these values take 8 samples to reach and are based upon larger tables
        public static readonly byte[] EnvelopeIncreaseTable = new byte[13] {
            4,  5,  6,  7,
            8, 10, 12, 14,
            16, 20, 24, 28,
            32,
        };

        //We're not including the highest attack rate, that gets a special value
        public static readonly byte[] AttackSamplesTable = new byte[13] {
            69, 55, 46, 40,
            35, 29, 23, 20,
            19, 15, 11, 10,
            9
        };

        static bool doneTables = false;

        static void InitTables()
        {
            if (doneTables)
                return;
            doneTables = true;
#if WAVE_HANDLER || WAVE_TABLELOG
            //Exponential volume table, same as the real adlib
            for (int i = 0; i < 256; i++)
            {
                //Save them in reverse
                ExpTable[i] = (int)(0.5 + (pow(2.0, (255 - i) * (1.0 / 256)) - 1) * 1024);
                ExpTable[i] += 1024; //or remove the -1 oh well :)
                                     //Preshift to the left once so the final volume can shift to the right
                ExpTable[i] *= 2;
            }
#endif
#if WAVE_HANDLER
            //Add 0.5 for the trunc rounding of the integer cast
            //Do a PI sinetable instead of the original 0.5 PI
            for (int i = 0; i < 512; i++)
            {
                SinTable[i] = (short)(0.5 - log10(sin((i + 0.5) * (M_PI / 512.0))) / log10(2.0) * 256);
            }
#endif
#if (WAVE_TABLEMUL)
            //Multiplication based tables
            for (int i = 0; i < 384; i++)
            {
                int s = i * 8;
                //TODO maybe keep some of the precision errors of the original table?
                double val = (0.5 + (Math.Pow(2.0, -1.0 + (255 - s) * (1.0 / 256))) * (1 << MUL_SH));
                MulTable[i] = (ushort)(val);
            }

            //Sine Wave Base
            for (int i = 0; i < 512; i++)
            {
                WaveTable[0x0200 + i] = (short)(Math.Sin((i + 0.5) * (Math.PI / 512.0)) * 4084);
                WaveTable[0x0000 + i] = (short)-WaveTable[0x200 + i];
            }
            //Exponential wave
            for (int i = 0; i < 256; i++)
            {
                WaveTable[0x700 + i] = (short)(0.5 + (Math.Pow(2.0, -1.0 + (255 - i * 8) * (1.0 / 256))) * 4085);
                WaveTable[0x6ff - i] = (short)-WaveTable[0x700 + i];
            }
#endif
#if WAVE_TABLELOG
            //Sine Wave Base
            for (int i = 0; i < 512; i++)
            {
                WaveTable[0x0200 + i] = (short)(0.5 - log10(sin((i + 0.5) * (M_PI / 512.0))) / log10(2.0) * 256);
                WaveTable[0x0000 + i] = ((short)0x8000) | WaveTable[0x200 + i];
            }
            //Exponential wave
            for (int i = 0; i < 256; i++)
            {
                WaveTable[0x700 + i] = i * 8;
                WaveTable[0x6ff - i] = ((short)0x8000) | i * 8;
            }
#endif

            //	|    |//\\|____|WAV7|//__|/\  |____|/\/\|
            //	|\\//|    |    |WAV7|    |  \/|    |    |
            //	|06  |0126|27  |7   |3   |4   |4 5 |5   |

#if WAVE_TABLELOG || WAVE_TABLEMUL
            for (int i = 0; i < 256; i++)
            {
                //Fill silence gaps
                WaveTable[0x400 + i] = WaveTable[0];
                WaveTable[0x500 + i] = WaveTable[0];
                WaveTable[0x900 + i] = WaveTable[0];
                WaveTable[0xc00 + i] = WaveTable[0];
                WaveTable[0xd00 + i] = WaveTable[0];
                //Replicate sines in other pieces
                WaveTable[0x800 + i] = WaveTable[0x200 + i];
                //double speed sines
                WaveTable[0xa00 + i] = WaveTable[0x200 + i * 2];
                WaveTable[0xb00 + i] = WaveTable[0x000 + i * 2];
                WaveTable[0xe00 + i] = WaveTable[0x200 + i * 2];
                WaveTable[0xf00 + i] = WaveTable[0x200 + i * 2];
            }
#endif

            //Create the ksl table
            for (int oct = 0; oct < 8; oct++)
            {
                int _base = oct * 8;
                for (int i = 0; i < 16; i++)
                {
                    int val = _base - KslCreateTable[i];
                    if (val < 0)
                        val = 0;
                    //*4 for the final range to match attenuation range
                    KslTable[oct * 16 + i] = (byte)(val * 4);
                }
            }
            //Create the Tremolo table, just increase and decrease a triangle wave
            for (byte i = 0; i < TREMOLO_TABLE / 2; i++)
            {
                byte val = (byte)(i << ENV_EXTRA);
                TremoloTable[i] = val;
                TremoloTable[TREMOLO_TABLE - 1 - i] = val;
            }
            //Create a table with offsets of the channels from the start of the chip
            Chip chip = new Chip();
            for (uint i = 0; i < 32; i++)
            {
                uint index = i & 0xf;
                if (index >= 9)
                {
                    chanOffsetTable[i] = null;
                    continue;
                }
                //Make sure the four op channels follow eachother
                if (index < 6)
                {
                    index = (index % 3) * 2 + (index / 3);
                }
                //Add back the bits for highest ones
                if (i >= 16)
                    index += 9;
                chanOffsetTable[i] = new Func<Chip, Channel>(c => c.Channels[index]);
            }
            //Same for operators
            for (uint i = 0; i < 64; i++)
            {
                if (i % 8 >= 6 || ((i / 8) % 4 == 3))
                {
                    opOffsetTable[i] = null;
                    continue;
                }
                uint chNum = (i / 8) * 3 + (i % 8) % 3;
                //Make sure we use 16 and up for the 2nd range to match the chanoffset gap
                if (chNum >= 12)
                    chNum += 16 - 12;
                uint opNum = (i % 8) / 3;

                opOffsetTable[i] = new Func<Chip, Operator>(c => chanOffsetTable[chNum](c).Ops[opNum]);
            }
        }

#if WAVE_HANDLER
        /*
            Generate the different waveforms out of the sine/exponetial table using handlers
        */
        static inline int MakeVolume(uint wave, uint volume)
        {
            uint total = wave + volume;
            uint index = total & 0xff;
            uint sig = ExpTable[index];
            uint exp = total >> 8;
            return (sig >> exp);
        }

        static int DB_FASTCALL WaveForm0(uint i, uint volume)
        {
            int neg = 0 - ((i >> 9) & 1);//Create ~0 or 0
            uint wave = SinTable[i & 511];
            return (MakeVolume(wave, volume) ^ neg) - neg;
        }
        static int DB_FASTCALL WaveForm1(uint i, uint volume)
        {
            Bit32u wave = SinTable[i & 511];
            wave |= (((i ^ 512) & 512) - 1) >> (32 - 12);
            return MakeVolume(wave, volume);
        }
        static int DB_FASTCALL WaveForm2(uint i, uint volume)
        {
            uint wave = SinTable[i & 511];
            return MakeVolume(wave, volume);
        }
        static int DB_FASTCALL WaveForm3(uint i, uint volume)
        {
            uint wave = SinTable[i & 255];
            wave |= (((i ^ 256) & 256) - 1) >> (32 - 12);
            return MakeVolume(wave, volume);
        }
        static int DB_FASTCALL WaveForm4(uint i, uint volume)
        {
            //Twice as fast
            i <<= 1;
            int neg = 0 - ((i >> 9) & 1);//Create ~0 or 0
            uint wave = SinTable[i & 511];
            wave |= (((i ^ 512) & 512) - 1) >> (32 - 12);
            return (MakeVolume(wave, volume) ^ neg) - neg;
        }
        static int DB_FASTCALL WaveForm5(uint i, uint volume)
        {
            //Twice as fast
            i <<= 1;
            uint wave = SinTable[i & 511];
            wave |= (((i ^ 512) & 512) - 1) >> (32 - 12);
            return MakeVolume(wave, volume);
        }
        static int DB_FASTCALL WaveForm6(uint i, uint volume)
        {
            int neg = 0 - ((i >> 9) & 1);//Create ~0 or 0
            return (MakeVolume(0, volume) ^ neg) - neg;
        }
        static int DB_FASTCALL WaveForm7(uint i, uint volume)
        {
            //Negative is reversed here
            int neg = ((i >> 9) & 1) - 1;
            uint wave = (i << 3);
            //When negative the volume also runs backwards
            wave = ((wave ^ neg) - neg) & 4095;
            return (MakeVolume(wave, volume) ^ neg) - neg;
        }

        static const WaveHandler[] WaveHandlerTable = new WaveHandler[8] {
            WaveForm0, WaveForm1, WaveForm2, WaveForm3,
            WaveForm4, WaveForm5, WaveForm6, WaveForm7
};

#endif


        //The lower bits are the shift of the operator vibrato value
        //The highest bit is right shifted to generate -1 or 0 for negation
        //So taking the highest input value of 7 this gives 3, 7, 3, 0, -3, -7, -3, 0
        public static readonly sbyte[] VibratoTable = new sbyte[8] {
            1 - 0x00, 0 - 0x00, 1 - 0x00, 30 - 0x00,
            1 - 0x80, 0 - 0x80, 1 - 0x80, 30 - 0x80
        };

        //Shift strength for the ksl value determined by ksl strength
        readonly static byte[] KslShiftTable = new byte[4] { 31, 1, 2, 0 };
    }
}

