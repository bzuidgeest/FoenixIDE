using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using FoenixIDE.Simulator.Devices;

namespace FoenixIDE.Simulator.UI
{
    public enum OPLDisplayMode
    {
        //display modes
        NOTES = 0,
        REGS = 1
    }

    public partial class OPLWindow : Form
    {
        public OPLDisplayMode DisplayMode { get; set; } = OPLDisplayMode.NOTES;

        private byte[] shadow_opl = new byte[256];
        private bool[] shadow_opl_written = new bool[256];

        private char[] VGA = new char[80 * 50];
        private Brush[] VGAColor = new Brush[80 * 50];
        private int[] lastnotes = new int[9];
        private char[] hex = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
        private string[] dec = new string[] { " 0", " 1", " 2", " 3", " 4", " 5", " 6", " 7", " 8", " 9", "10", "11", "12", "13", "14", "15" };

        private SolidBrush C_LOW = new SolidBrush(Color.FromArgb(252, 36, 36)); //low-intensity color
        private SolidBrush C_NORM = new SolidBrush(Color.FromArgb(72, 216, 252)); //normal color
        private SolidBrush C_HI = new SolidBrush(Color.FromArgb(252, 252, 252)); //highlight color
        private SolidBrush C_ACT = new SolidBrush(Color.FromArgb(216, 216, 36)); //active channel color
        private SolidBrush C_NACT = new SolidBrush(Color.FromArgb(36, 36, 252)); //inactive channel color

        public OPLWindow()
        {
            InitializeComponent();

            //FoenixSystem.Current.Memory.OPL2.OnRead += OPL2_OnRead;
            FoenixSystem.Current.Memory.OPL2.OnWrite += OPL2_OnWrite;

            screen_init();

            PictureBoxOPLStatus.Refresh();
        }

        private void OPL2_OnWrite(object sender, BasicRegisterEvent e)
        {
            shadow_opl[e.Address] = e.Value;
            shadow_opl_written[e.Address] = true;

            print_cmd((byte)e.Address, e.Value);

            this.Invoke(new MethodInvoker(delegate ()
            {
                PictureBoxOPLStatus.Refresh();
            }));
        }

        //private void OPL2_OnRead(object sender, BasicRegisterEvent e)
        //{
            
        //}

        private void PictureBoxOPLStatus_Paint(object sender, PaintEventArgs e)
        {
            for (int i = 0; i < 50; i++)
            {
                //TextRenderer.DrawText(e.Graphics, )
                e.Graphics.DrawString(VGA.AsSpan(i * 80, 80).ToString(), PictureBox.DefaultFont, new SolidBrush(PictureBox.DefaultForeColor), 20, i * 10);
            }
        }


        public static int CHAN_Y = 2;       //channel data table Y offset
        public static int ICL = -1; //invalid channel line

        public int XY(int x, int y)
        {
            //return (2 * ((x) + 80 * (y)));
            return ((x) + 80 * (y));
        }



        private void puts_xy(string text, int x, int y, Brush color)
        {
            text.AsMemory().TryCopyTo(new Memory<char>(VGA, XY(x, y), text.Length));
            //new Memory<char>(VGAColor, XY(x, y), text.Length).;
            //int vgaaddr = XY(x, y);
            //while (*text)
            //{
            //    VGA[vgaaddr++] = *text;
            //    VGA[vgaaddr++] = color;
            //    text++;
            //}
        }

        private float get_freq(int reg)
        {
            float fn = ((shadow_opl[reg + 0x10] & 0x03) << 8) | shadow_opl[reg];
            int block = (shadow_opl[reg + 0x10] >> 2) & 0x07;
            return fn * (1 << block) * 1.4320f / 2.88f / 5.24288f;
        }

        private byte get_note2(float freq)
        {
            if (freq == 0)
                return 0;
            return (byte)(Math.Floor((24.0f * Math.Log(freq / 440.0f) / Math.Log(2.0f) + 2 * 69) + 0.5f));
        }

        private void print_freq(int channel)
        {
            string buffer;
            float f = get_freq(0xA0 + channel);
            int n = get_note2(f) - 72;

            if (n < 0)
                n = 0;
            if (n > 158)
                n = 158;

            buffer = String.Format("%7.2f Hz %5.2f", f, n / 2.0f);
            puts_xy(buffer, 54, 2 * channel + CHAN_Y, C_NORM);

            if (DisplayMode == OPLDisplayMode.NOTES)
            {
                puts_xy("  ", lastnotes[channel] / 2, 20 + channel + CHAN_Y, C_NORM);
                lastnotes[channel] = n;
                if ((shadow_opl[0xB0 + channel] & 0x20) > 0)
                {
                    puts_xy((n & 1) > 0 ? "\xde\xdd" : "\xdb", n / 2, 20 + channel + CHAN_Y, C_HI);
                }
            }
        }

        private void print_mute(int channel, bool muted)
        {
            Brush color = muted ? C_NACT : C_LOW;
            puts_xy("-", 53, CHAN_Y + 1 + 2 * channel, color);
            puts_xy("----", 56, CHAN_Y + 1 + 2 * channel, color);
            puts_xy("---", 62, CHAN_Y + 1 + 2 * channel, color);
            puts_xy(dec[channel + 1], 51, CHAN_Y + 2 * channel, muted ? C_NACT : C_ACT);
            print_freq(channel);
        }

        private void print_cmd(byte reg, byte data)
        {
            int[] channel_xlat = { 0, 2, 4, 1, 3, 5, ICL, ICL, 6, 8, 10, 7, 9, 11, ICL, ICL, 12, 14, 16, 13, 15, 17, ICL, ICL, ICL, ICL, ICL, ICL, ICL, ICL, ICL, ICL };
            string[] mult = { "0.5", " 1 ", " 2 ", " 3 ", " 4 ", " 5 ", " 6 ", " 7 ", " 8 ", " 9 ", " 10", " 10", " 12", " 12", " 15", " 15" };
            string[] ksl = { " - ", "1.5", " 3 ", " 6 " };
            string[] wave = { " ^v ", " ^- ", " ^^ ", " // " };
            string buffer;

            int ch_line = channel_xlat[reg & 0x1F];
            int mch_line = reg & 0x0F;


            //raw data
            if (DisplayMode == OPLDisplayMode.REGS)
            {
                VGA[XY(3 * (reg & 0x0F) + 0, 22 + (reg >> 4))] = hex[data >> 4];
                VGA[XY(3 * (reg & 0x0F) + 1, 22 + (reg >> 4))] = hex[data & 0x0F];
            }


            //decoded data
            switch (reg & 0xF0)
            {
                case 0x20:
                case 0x30:
                    if (ch_line == ICL)
                        break;

                    puts_xy("AM", 2, ch_line + CHAN_Y, (data & 0x80) > 0 ? C_HI : C_LOW);
                    puts_xy("VIB", 5, ch_line + CHAN_Y, (data & 0x40) > 0 ? C_HI : C_LOW);
                    puts_xy("EGT", 9, ch_line + CHAN_Y, (data & 0x20) > 0 ? C_HI : C_LOW);
                    puts_xy("KSR", 13, ch_line + CHAN_Y, (data & 0x10) > 0 ? C_HI : C_LOW);
                    puts_xy(mult[data & 0x0F], 18, ch_line + CHAN_Y, C_NORM);
                    break;

                case 0x40:
                case 0x50:  //total level
                    if (ch_line == ICL)
                        break;

                    puts_xy(ksl[data >> 6], 24, ch_line + CHAN_Y, C_NORM);
                    buffer = String.Format("%5.2f",
                                24.0f * ((data & 0x20) >> 5) +
                                12.0f * ((data & 0x10) >> 4) +
                                6.0f * ((data & 0x08) >> 3) +
                                3.0f * ((data & 0x04) >> 2) +
                                1.5f * ((data & 0x02) >> 1) +
                                0.75f * (data & 0x01));
                    puts_xy(buffer, 30, ch_line + CHAN_Y, C_NORM);
                    break;

                case 0x60:
                case 0x70:  //AD
                    if (ch_line == ICL)
                        break;

                    puts_xy(dec[data >> 4], 38, ch_line + CHAN_Y, C_NORM);
                    puts_xy(dec[data & 0x0F], 41, ch_line + CHAN_Y, C_NORM);
                    break;

                case 0x80:
                case 0x90:  //SR
                    if (ch_line == ICL)
                        break;

                    puts_xy(dec[data >> 4], 44, ch_line + CHAN_Y, C_NORM);
                    puts_xy(dec[data & 0x0F], 47, ch_line + CHAN_Y, C_NORM);
                    break;

                case 0xA0:  //F-number
                    if (mch_line > 8)
                        break;

                    buffer = String.Format("%4d", ((shadow_opl[reg + 0x10] & 0x03) << 8) | data);
                    puts_xy(buffer, 56, 2 * mch_line + 1 + CHAN_Y, C_NORM);
                    print_freq(mch_line);
                    break;

                case 0xB0:  //block, F-number
                    if (mch_line > 8)
                        break;

                    puts_xy("KEY", 62, 2 * mch_line + 1 + CHAN_Y, (data & 0x20) > 0 ? C_HI : C_LOW);
                    buffer = string.Format("%1d", (data >> 2) & 0x07);
                    puts_xy(buffer, 53, 2 * mch_line + 1 + CHAN_Y, C_NORM);
                    buffer = string.Format("%4d", ((data & 0x03) << 8) | shadow_opl[reg - 0x10]);
                    puts_xy(buffer, 56, 2 * mch_line + 1 + CHAN_Y, C_NORM);
                    print_freq(mch_line);
                    break;

                case 0xC0:  //feedback
                    if (mch_line > 8)
                        break;

                    buffer = string.Format("%1d", (data >> 1) & 0x07);
                    puts_xy(buffer, 67, 2 * mch_line + 1 + CHAN_Y, C_NORM);
                    puts_xy("CNT", 70, 2 * mch_line + 1 + CHAN_Y, (data & 0x01) > 0 ? C_HI : C_LOW);
                    break;

                case 0xE0:
                case 0xF0:
                    if (ch_line == ICL)
                        break;

                    puts_xy(wave[data & 0x03], 75, ch_line + CHAN_Y, C_NORM);
                    break;
            }
        }

        void print_delay(ushort delay)
        {
            VGA[XY(16, 39)] = hex[(delay >> 12) & 0x0F];
            VGA[XY(17, 39)] = hex[(delay >> 8) & 0x0F];
            VGA[XY(18, 39)] = hex[(delay >> 4) & 0x0F];
            VGA[XY(19, 39)] = hex[delay & 0x0F];
        }

        void print_rem_delay(ushort rem)
        {
            VGA[XY(16, 40)] = hex[(rem >> 12) & 0x0F];
            VGA[XY(17, 40)] = hex[(rem >> 8) & 0x0F];
            VGA[XY(18, 40)] = hex[(rem >> 4) & 0x0F];
            VGA[XY(19, 40)] = hex[rem & 0x0F];
        }

        //void draw_logo(int x, int y)
        //{
        //    int r = 0, c = 0;
        //    char* text = logo;
        //    while (*text)
        //    {
        //        if (*text == '\n')
        //        {
        //            r++;
        //            c = 0;
        //        }
        //        else
        //        {
        //            VGA[XY(x + c, y + r)] = *text != ' ' ? 219 : ' ';
        //            VGA[XY(x + c++, y + r) + 1] = 15;
        //        }

        //        text++;
        //    }

        //    puts_xy("> imfplay "VERSION" by kvee", x + 15, y + 8, 10);
        //}

        void draw_clear(int l1, int l2)
        {
            //int i;

            VGA.AsSpan(l1 * 80, (l2 - l1) * 80).Fill(' ');
            VGAColor.AsSpan(l1 * 80, (l2 - l1) * 80).Fill(C_NORM);
            //for (i = 0; i < (l2 - l1) * 80; i++)
            //{
            //    VGA[XY(0, l1) + 0 + 2 * i] = ' ';
            //    VGAColor[XY(0, l1) + 1 + 2 * i] = C_NORM;
            //}
        }

        void dec_init(int channels)
        {
            int i;
            draw_clear(0, 2 + 2 * channels);
            puts_xy("20+", 2, 0, C_LOW);
            puts_xy("40+", 24, 0, C_LOW);
            puts_xy("60+", 38, 0, C_LOW);
            puts_xy("80+", 44, 0, C_LOW);
            puts_xy("B0:A0+", 53, 0, C_LOW);
            puts_xy("C0+", 67, 0, C_LOW);
            puts_xy("E0+", 75, 0, C_LOW);
            puts_xy("multi scale  level   A  D  S  R     o F-num      fbk      wave", 17, 1, C_LOW);

            for (i = 0; i < 2 * channels; i++)
            {
                puts_xy((i & 1) > 0 ? "2" : "1",
                    0, i + CHAN_Y,
                    C_LOW);
                puts_xy("AM VIB EGT KSR [0.5] [ - ] [ 0.00]" +



                    " [ 0  0  0  0]",
                    2, i + CHAN_Y,
                    C_LOW);
                if ((i & 1) > 0)
                    puts_xy(" [ ][    ] KEY [ ] CNT [    ]", 51, i + CHAN_Y, C_LOW);
                else
                {
                    puts_xy(dec[i / 2 + 1], 51, i + CHAN_Y, C_ACT);
                    puts_xy("[    ]", 74, i + CHAN_Y, C_LOW);
                }
            }
        }

        void reg_init()
        {
            int reg;

            draw_clear(21, 38);
            for (reg = 0; reg < 256; reg++)
            {
                if (shadow_opl_written[reg])
                {
                    int data = shadow_opl[reg];
                    VGA[XY(3 * (reg & 0x0F) + 0, 22 + (reg >> 4))] = hex[data >> 4];
                    VGA[XY(3 * (reg & 0x0F) + 1, 22 + (reg >> 4))] = hex[data & 0xF];
                }
                else
                {
                    VGA[XY(3 * (reg & 0x0F) + 0, 22 + (reg >> 4))] = (char)249;
                    VGA[XY(3 * (reg & 0x0F) + 1, 22 + (reg >> 4))] = (char)249;
                }
            }
            puts_xy("OPL2 register map", 0, 21, C_LOW);
        }

        void note_init()
        {
            draw_clear(21, 38);
            puts_xy("note map", 0, 21, C_LOW);
        }

        void screen_set_mode(OPLDisplayMode mode)
        {
            DisplayMode = mode;
            if (DisplayMode == OPLDisplayMode.REGS)
                reg_init();
            else
                note_init();
        }

        void screen_init()
        {
            int i;

            //           //init video mode
            //           gettextinfo((struct text_info*)video_mode);
            //textmode(C4350);
            //       clrscr();


            //       //prepare screen
            //       draw_logo(41, 39);
            dec_init(9);
            screen_set_mode(DisplayMode);
        }

        //void screen_restore()
        //{
        //    textmode(video_mode[6]);
        //}
    }
}
