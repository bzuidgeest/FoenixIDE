using System;
using System.Collections.Generic;
using System.Text;

namespace FoenixIDE.Simulator.Devices.Audio.SoftSynth.OPL.DOSBox
{
    public class OPLTimer
    {
        public double startTime;
        public double delay;
        public bool enabled, overflow, masked;
        public byte counter;

        public OPLTimer()
        {
            masked = false;
            overflow = false;
            enabled = false;
            counter = 0;
            delay = 0;
        }

        public void update(double time)
        {
            if (!enabled || delay == 0)
                return;

            double deltaStart = time - startTime;
            
            // Only set the overflow flag when not masked
            if (deltaStart >= 0 && !masked)
                overflow = true;
        }

        public void reset(double time)
        {
            overflow = false;
            if (delay == 0 || !enabled)
                return;
            double delta = (time - startTime);
            double rem = delta % delay;
            double next = delay - rem;
            startTime = time + next;
        }

        public void stop()
        {
            enabled = false;
        }

        public void start(double time, int scale)
        {
            //Don't enable again
            if (enabled)
                return;
            enabled = true;
            delay = 0.001 * (256 - counter) * scale;
            startTime = time + delay;
        }

    }
}
