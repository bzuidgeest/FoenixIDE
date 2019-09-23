using FoenixIDE.Simulator.Devices.Audio;
using FoenixIDE.Simulator.Devices.Audio.HardSynth.OPL.OPLXLPT;
using FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox;
using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace FoenixIDE.Simulator.Devices
{
    public class OPL2 : MemoryLocations.MemoryRAM, IWaveProvider
    {
        private OPLSystem oPLSystem = Configuration.Current.OPLSystem;
        private int parallelPort = Configuration.Current.OPLParallelPort;
        private MemoryStream stream = new MemoryStream();
        private Queue<byte> sampleQueue = new Queue<byte>();

        private IOPL oPL;
        public byte[] shadowOPL = new byte[512];

        private WaveOutEvent soundOutput;
        public WaveFormat WaveFormat { get; private set; }

        public event EventHandler<BasicRegisterEvent> OnRead;
        public event EventHandler<BasicRegisterEvent> OnWrite;

        public OPL2(int StartAddress, int Length) : base(StartAddress, Length)
        {
            switch (oPLSystem)
            {
                case OPLSystem.DOSBox:
                    DosBoxOPL o = new DosBoxOPL(OPLType.Opl3);
                    o.Init(44100);
                    this.oPL = o;

                    WaveFormat = new WaveFormat(44100, 16, 1);
                    //Thread t = new Thread(SampleReader);
                    //t.Start();
                    soundOutput = new WaveOutEvent();
                    //soundOutput.Init(this);

                    soundOutput.Init(o);
                    soundOutput.Play();
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


            for (int i = 0; i < 512; i++)
            {
                shadowOPL[i] = oPL.ReadRegister(i);
            }

            oPL.Init(44100);
        }


        private void SampleReader()
        {
            int counter = 0;
            short[] buffer = new short[1];
            while(true)
            {
                if (FoenixSystem.Current.CPU.CycleCounter >= 317)
                {
                    oPL.ReadBuffer(buffer, 0, 1);
                    //stream.Write(MemoryMarshal.Cast<short, byte>(buffer).ToArray(), 0, 2);
                    
                    sampleQueue.Enqueue((byte)(buffer[0]));
                    sampleQueue.Enqueue((byte)(buffer[0] >> 8));
                    counter -= 317;
                    if (soundOutput.PlaybackState == PlaybackState.Stopped && sampleQueue.Count > 30000)
                    {
                        soundOutput.Play();
                    }
                }
            }
        }

        // Function to read from shadow registers without disturbing the emulation
        // For the emulation this read does not exist.
        public byte ReadShadowByte(int address)
        {
            return (shadowOPL[address]);
        }

        public override byte ReadByte(int address)
        {
            byte data = base.ReadByte(address);
            OnRead?.Invoke(this, new BasicRegisterEvent(address, data));

            return data;
        }

        public override void WriteByte(int address, byte value)
        {
            shadowOPL[address] = value;

            base.WriteByte(address, value);

            OnWrite?.Invoke(this, new BasicRegisterEvent(address, value));

            oPL.WriteReg(address, value);
        }

        public int Read(byte[] buffer, int offset, int count)
        {
            int samplesReady = Math.Min(sampleQueue.Count, count);
            byte[] x = sampleQueue.DequeueChunk<byte>(samplesReady).ToArray();
            x.CopyTo(buffer, 0);
            return x.Length;
        }
    }
}
