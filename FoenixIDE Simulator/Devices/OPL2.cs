using FoenixIDE.MemoryLocations;
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
    public class OPL2 : IMemoryMappedDevice, IWaveProvider
    {
        private Memory<byte> data;

        public int BaseAddress { get; }
        public string Name { get { return this.GetType().ToString(); } }
        public int Size { get { return 256; } } // 512 for opl3

        private OPLSystem oPLSystem = Configuration.Current.OPLSystem;
        private int parallelPort = Configuration.Current.OPLParallelPort;
        private MemoryStream stream = new MemoryStream();
        private Queue<byte> sampleQueue = new Queue<byte>();

        private IOPL oPL;
        //public byte[] shadowOPL = new byte[512];

        private WaveOutEvent soundOutput;
        public WaveFormat WaveFormat { get; private set; }

        public event EventHandler<BasicRegisterEvent> OnRead;
        public event EventHandler<BasicRegisterEvent> OnWrite;

        public OPL2(int baseAddress)
        {
            this.BaseAddress = baseAddress;

            switch (oPLSystem)
            {
                case OPLSystem.DOSBox:
                    DosBoxOPL o = new DosBoxOPL(OPLType.Opl3);
                    o.Init(44100);
                    this.oPL = o;

                    WaveFormat = new WaveFormat(44100, 16, 1);
                    Thread t = new Thread(SampleReader);
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


            

            oPL.Init(44100);
        }

        public void SetMemory(Memory<byte> memory)
        {
            this.data = memory;

            for (int i = 0; i < Size; i++)
            {
                //shadowOPL[i] = oPL.ReadRegister(i);
                data.Span[i] = oPL.ReadRegister(i);
            }
        }

        private void SampleReader()
        {
            int counter = 0, prevCounter = 0;
            byte[] buffer = new byte[1024];
            while (true)
            {
                counter = FoenixSystem.Current.CPU.CycleCounter;
                //if (counter - prevCounter >= 317)
                if (counter - prevCounter >= 324608)
                {
                    oPL.Read(buffer, 0, 1024);

                    //sampleQueue.Enqueue(buffer[0]);
                    //sampleQueue.Enqueue(buffer[1]);
                    buffer.ToList().ForEach(x => sampleQueue.Enqueue(x));

                    //counter -= 317;

                    if (soundOutput.PlaybackState == PlaybackState.Stopped && sampleQueue.Count > 30000)
                    {
                        soundOutput.Play();
                    }
                    prevCounter = counter;
                }
            }
        }

        // Function to read from shadow registers without disturbing the emulation
        // For the emulation this read does not exist.
        public byte ReadShadowByte(int address)
        {
            return (data.Span[address]);
        }

        public byte ReadByte(int address)
        {
            byte datax = data.Span[address];
            OnRead?.Invoke(this, new BasicRegisterEvent(address, datax));

            return datax;
        }

        public void WriteByte(int address, byte value)
        {
            data.Span[address] = value;

            //base.WriteByte(address, value);

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
