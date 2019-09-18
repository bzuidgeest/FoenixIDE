using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    public class OPLChip
    {
       readonly OPLTimer[] timer = new OPLTimer[2]
       {
            new OPLTimer(),
            new OPLTimer()
       };

        public bool Write(uint reg, byte val)
        {
            switch (reg)
            {
                case 0x02:
                    timer[0].counter = val;
                    return true;
                case 0x03:
                    timer[1].counter = val;
                    return true;
                case 0x04:
                    double time = Environment.TickCount / 1000.0;

                    if ((val & 0x80) != 0)
                    {
                        timer[0].reset(time);
                        timer[1].reset(time);
                    }
                    else
                    {
                        timer[0].update(time);
                        timer[1].update(time);

                        if ((val & 0x1) != 0)
                            timer[0].start(time, 80);
                        else
                            timer[0].stop();

                        timer[0].masked = (val & 0x40) > 0;

                        if (timer[0].masked)
                            timer[0].overflow = false;

                        if ((val & 0x2) != 0)
                            timer[1].start(time, 320);
                        else
                            timer[1].stop();

                        timer[1].masked = (val & 0x20) > 0;

                        if (timer[1].masked)
                            timer[1].overflow = false;
                    }
                    return true;
            }
            return false;
        }

        public byte read()
        {
            double time = Environment.TickCount / 1000.0;

            timer[0].update(time);
            timer[1].update(time);

            byte ret = 0;
            // Overflow won't be set if a channel is masked
            if (timer[0].overflow)
            {
                ret |= 0x40;
                ret |= 0x80;
            }
            if (timer[1].overflow)
            {
                ret |= 0x20;
                ret |= 0x80;
            }
            return ret;
        }
              
    }
}
