﻿using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LEDBlasterNet
{
    public class RGBLed
    {
        private Thread _runner;
        public enum ColorChangeMode
        {
            Instant = 0,
            Fade = 1,
            FadeOverBlack = 2
        }

        public Color CurrentColor { get; set; }
        public ColorChangeMode CurrentMode { get; set; }

        public int PinRed { get; set; }
        public int PinGreen { get; set; }
        public int PinBlue { get; set; }

        public float ValRed { get; set; }
        public float ValGreen { get; set; }
        public float ValBlue { get; set; }

        public RGBLed(int red, int green, int blue)
        {
            CurrentMode = ColorChangeMode.Fade;

            PinRed = red;
            PinGreen = green;
            PinBlue = blue;

            AllOff();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AllOff()
        {
            CancelCurrentTask();
            FadeTo(Color.Black, 300);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void AllOn()
        {
            CancelCurrentTask();
            FadeTo(Color.White, 300);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CancelCurrentTask()
        {
            if (_runner == null) return;
            _runner.Interrupt();
            _runner = null;
        }


        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetColor(float r, float g, float b)
        {
            var rInt = (int)(r * 255);
            var gInt = (int)(g * 255);
            var bInt = (int)(b * 255);

            SetColor(rInt, gInt, bInt);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetColor(int r, int g, int b)
        {
            var color = Color.FromArgb(r, g, b);
            SetColor(color);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetColor(string htmlColor)
        {
            Console.WriteLine(htmlColor);
            CancelCurrentTask();
            SetColor(ColorTranslator.FromHtml(htmlColor));
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Run(Action action)
        {
            _runner = new Thread(new ThreadStart(action));
            _runner.Start();
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void SetColor(Color color, double luminance = 1)
        {
            switch (CurrentMode)
            {
                case ColorChangeMode.Fade:
                    FadeTo(color, 300);
                    break;
                case ColorChangeMode.FadeOverBlack:
                    FadeFromBlack(color);
                    break;
                default:
                    SetColorInstant(color, luminance);
                    break;
            }

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetColorInstant(Color color, double luminance = 1, bool setCurrentColor = true)
        {
            this.CurrentColor = color;
            var rPwm = (float)(color.R * luminance) / 255.0f;
            var gPwm = (float)(color.G * luminance) / 255.0f;
            var bPwm = (float)(color.B * luminance) / 255.0f;

            PiBlaster.Set(PinRed, rPwm);
            PiBlaster.Set(PinGreen, gPwm);
            PiBlaster.Set(PinBlue, bPwm);
            ValRed = rPwm;
            ValGreen = gPwm;
            ValBlue = bPwm;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Colorfade(int ms)
        {
            CancelCurrentTask();
            Run(delegate()
            {
                while (Thread.CurrentThread.IsAlive)
                {
                    try
                    {
                        FadeTo(ColorHelper.GetRandomColor(45), ms);
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Colorflash(int ms)
        {
            CancelCurrentTask();
            Run(delegate()
            {
                while (Thread.CurrentThread.IsAlive)
                {
                    SetColorInstant(ColorHelper.GetRandomColor(45));
                    try
                    {
                        Thread.Sleep(ms);
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Colorstrobe(int ms)
        {
            CancelCurrentTask();
            Run(delegate()
            {
                while (Thread.CurrentThread.IsAlive)
                {
                    SetColorInstant(ColorHelper.GetRandomColor(45));
                    try
                    {
                        Thread.Sleep(ms);
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                    PwmColor(0, 0, 0);
                    try
                    {
                        Thread.Sleep(ms);
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void Strobe(int ms)
        {
            CancelCurrentTask();
            Run(delegate()
            {
                while (Thread.CurrentThread.IsAlive)
                {
                    PwmColor(1, 1, 1);
                    try
                    {
                        Thread.Sleep(ms);
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                    PwmColor(0, 0, 0);
                    try
                    {
                        Thread.Sleep(ms);
                    }
                    catch (ThreadInterruptedException ex)
                    {
                        Console.WriteLine(ex.Message);
                        break;
                    }
                }
            });
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void FadeFromBlack(Color color)
        {
            CurrentMode = ColorChangeMode.Fade;
            AllOff();
            SetColor(color);
            CurrentMode = ColorChangeMode.FadeOverBlack;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void FadeTo(Color color, int ms, bool setCurrentColor = true)
        {
            SetColorInstant(CurrentColor);
            ms /= 10;
            for (var i = 0; i < ms; i++)
            {
                if (!Thread.CurrentThread.IsAlive) return;
                var r = CurrentColor.R + (i * (color.R - CurrentColor.R) / ms);
                var g = CurrentColor.G + (i * (color.G - CurrentColor.G) / ms);
                var b = CurrentColor.B + (i * (color.B - CurrentColor.B) / ms);
                SetColorInstant(Color.FromArgb(r, g, b));
                Thread.Sleep(10);
            }
            SetColorInstant(color);
        }

        //[MethodImpl(MethodImplOptions.Synchronized)]
        //public void FadeTo(Color color, int ms, bool setCurrentColor = true)
        //{
        //    SetColorInstant(CurrentColor);
        //    var currentHsbColor = ColorHelper.RGBtoHSB(CurrentColor);
        //    var targetHsbColor = ColorHelper.RGBtoHSB(color);

        //    for (var i = 0; i < ms; i++)
        //    {
        //        if (!Thread.CurrentThread.IsAlive) return;
        //        SetColorInstant(ColorHelper.HSBtoRGB(255, (float)(currentHsbColor.Hue + (i * (targetHsbColor.Hue - currentHsbColor.Hue) / ms)), 1, (float)(currentHsbColor.Brightness + (i * (targetHsbColor.Brightness - currentHsbColor.Brightness) / ms))));
        //        Thread.Sleep(1);
        //    }
        //    SetColorInstant(color);
        //}

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void PwmColor(float r, float g, float b)
        {
            PiBlaster.Set(PinRed, r);
            PiBlaster.Set(PinGreen, g);
            PiBlaster.Set(PinBlue, b);
            ValRed = r;
            ValGreen = g;
            ValBlue = b;
        }
    }
}
