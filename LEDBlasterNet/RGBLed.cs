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
        private readonly Random _random;
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
            CurrentMode = ColorChangeMode.Instant;
            _random = new Random();

            PinRed = red;
            PinGreen = green;
            PinBlue = blue;

            AllOff();
        }

        public void AllOff()
        {
            CancelCurrentTask();
            PwmColor(0, 0, 0);
        }

        public void AllOn()
        {
            CancelCurrentTask();
            PwmColor(1, 1, 1);
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void CancelCurrentTask()
        {
            if (_runner == null) return;
            _runner.Interrupt();
            _runner = null;
        }


        public void SetColor(float r, float g, float b)
        {
            var rInt = (int)(r * 255);
            var gInt = (int)(g * 255);
            var bInt = (int)(b * 255);

            SetColor(rInt, gInt, bInt);
        }
        public void SetColor(int r, int g, int b)
        {
            var color = Color.FromArgb(r, g, b);
            SetColor(color);
        }

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

        public void SetColor(Color color, double luminance = 1)
        {
            switch (CurrentMode)
            {
                case ColorChangeMode.Fade:
                    //FadeTo(color, 300);
                    break;
                case ColorChangeMode.FadeOverBlack:
                    FadeToBlack(300);
                    FadeFromBlack(color, 300);
                    break;
                default:
                    SetColorInstant(color, luminance);
                    break;
            }

        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        private void SetColorInstant(Color color, double luminance)
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
        public void FadeToBlack(int ms)
        {
            var stepSize = 1.0f / ms;
            SetColor(CurrentColor);
            for (var intensity = 1.0f; intensity > 0; intensity -= stepSize)
            {
                SetColorInstant(CurrentColor, intensity);
                Thread.Sleep(1);
            }
            AllOff();
        }

        public void Colorfade(int ms)
        {
            var stepSize = 1.0f / ms;
            SetColor(CurrentColor);
            for (var intensity = 1.0f; intensity > 0; intensity -= stepSize)
            {
                SetColor(CurrentColor, intensity);
                Thread.Sleep(1);
            }
            AllOff();
        }

        public void Colorflash(int ms)
        {
            CancelCurrentTask();
            Run(delegate()
            {
                while (Thread.CurrentThread.IsAlive)
                {
                    SetColor(GetRandomColor());
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
                AllOff();
            })
            ;
        }

        [MethodImpl(MethodImplOptions.Synchronized)]
        public void FadeFromBlack(Color color, int ms)
        {
            var stepSize = 1.0f / ms;
            AllOff();
            for (var intensity = 0f; intensity < 1f; intensity += stepSize)
            {
                SetColorInstant(color, intensity);
                Thread.Sleep(1);
            }
            SetColor(color);
        }

        public Color GetRandomColor()
        {
            return Color.FromArgb(_random.Next(255), _random.Next(255), _random.Next(255));
        }

        //public void FadeTo(Color color, int ms)
        //{
        //    var curRgb = new RGB(CurrentColor.R, CurrentColor.G, CurrentColor.B);
        //    var curCielCh = curRgb.toCIELCh();

        //    var tempCielCh = curRgb.toCIELCh();

        //    var tarRgb = new RGB(color.R, color.G, color.B);
        //    var tarCielCh = tarRgb.toCIELCh();

        //    var difference = curCielCh.h - tarCielCh.h;
        //    var stepSize = difference / ms;

        //    SetColor(CurrentColor);
        //    for (var h = curCielCh.h; Math.Abs(h - tarCielCh.h) > 0.01; h += stepSize)
        //    {
        //        tempCielCh.h = h;
        //        var tempRgb = tempCielCh.toRGB();
        //        var tempColor = Color.FromArgb(tempRgb.r, tempRgb.g, tempRgb.b);
        //        SetColor(tempColor);
        //        Thread.Sleep(1);
        //    }
        //    SetColor(color);
        //}

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