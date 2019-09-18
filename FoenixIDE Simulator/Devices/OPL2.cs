using FoenixIDE.Simulator.Devices.Audio;
using FoenixIDE.Simulator.Devices.Audio.HardSynth.OPL.OPLXLPT;
using FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class OPL2 : MemoryLocations.MemoryRAM
    {
        OPLSystem oPLSystem = Configuration.Current.OPLSystem;
        int parallelPort = Configuration.Current.OPLParallelPort;

        IOPL oPL;

        public OPL2(int StartAddress, int Length) : base(StartAddress, Length)
        {
            switch (oPLSystem)
            {
                case OPLSystem.DOSBox:
                    oPL = new DosBoxOPL(OPLType.Opl3);
                    break;
                case OPLSystem.Nuked:
                    // not yet ready
                    break;
                case OPLSystem.OPL2LPT:
                    oPL = new OPLXLPT(OPLType.Opl2, (LPTport)0xC020);
                    break;
                case OPLSystem.OPL3LPT:
                    oPL = new OPLXLPT(OPLType.Opl3, (LPTport)0xC020);
                    break;
                default:
                    // Maybe throw tantrum
                    break;
            }

            oPL.Init(44100);
        }

        public override byte ReadByte(int Address)
        {
            return base.ReadByte(Address);
        }

        public override void WriteByte(int Address, byte Value)
        {
            base.WriteByte(Address, Value);

            oPL.WriteReg(Address, Value);
        }
    }
}
