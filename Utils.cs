﻿using System.IO.Ports;
using System.Text.RegularExpressions;
using System;
using System.Runtime.InteropServices;
using GlmSharp;


using System.Windows;
using System.Windows.Data;
using System.Windows.Input;
using System.Drawing;
// using System.Windows.Media;
using GMap.NET.WindowsForms;
using GMap.NET.MapProviders;
using GMap.NET;
using GMap.NET.Internals;
using System.Collections.Generic;
using System.Drawing.Drawing2D;
using System.Windows.Forms.DataVisualization.Charting;
using System.Windows.Forms;
using System.Linq;


namespace CanSatGUI
{
    class Utils
    {
        static Regex valid_packet_regex = new Regex(@"^([0-9\-;:\.xX]+\s+$)");

        public static string[] ParsePacket(string packet)
        {
            MatchCollection matches = valid_packet_regex.Matches(packet);
            if (matches.Count == 0) {
                Console.Write("skipping packet: invalid packet " + packet);
                return null;
            }
            //Console.Write("matched");
            /// "$$ZSM-Sat,997.27,1018,END$$$"
            string[] list = packet.Split(';');
            return list;
        }

        public static SerialPort InitSerialPort(string portName)
        {
            SerialPort port = new SerialPort();
            port.PortName = portName;
            port.BaudRate = 9600;
            port.Parity = Parity.None;
            port.DataBits = 8;
            port.StopBits = StopBits.One;
            port.NewLine = "\n";
            port.Open();
            return port;
        }

        //public bool reconnect(SerialPort serialPort, string portName, int attempts = 3)
        //{
        //    try
        //    {
        //        //if (TPumpSerialPort != null && TPumpSerialPort.IsOpen == true)
        //        if (serialPort != null)
        //        {
        //            serialPort.Close();
        //            serialPort.Dispose();
        //        }

        //        int i = 1;
        //        while (true)
        //        {
        //            Log(string.Format("Reconnecting Turbo Pump attempt {0}", i));

        //            Thread.Sleep(2000);

        //            if (tryToConnectToCOM(serialPort))
        //                break;

        //            if (i == attempts)
        //                return false;

        //            i++;
        //        }

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Log(string.Format("Could not reconnect to serial port: {0}", ex.Message));
        //        return false;
        //    }
        //}

        

        public static double WindSpeed(double hallRPM)
        {
            double radius = 0.018; // in meters
            return ((0.00418410041841 * hallRPM) + 1.7866108786611);
        }



        public static String GetTimestamp(DateTime value)
        {
            return value.ToString("yyyyMMddHHmmssfff");
        }

        //def f(db):
        //min_ = -111
        //max_ = -53
        //db += -min_
        //return db / (-53 - min_) * 100
        
        public static double SignalStrengthInPercent(double db)
        {
            double min = -120;
            double max = -30;
            db += -min;
            return db / (max - min) * 100;
        }

        //public static double SeaLevelPressure(double h, double P, double T)
        //{
        //    return P * Math.Pow(1 - (0.0065 * h / (T + 0.0065 * h + 273.15)), -5.257);
        //}

        public static double HypsometricFormula(double P0, double P, double T)
        {
            // P0 - pressure at sea level in hPa (constant)
            // P - pressure at cansat level
            // T - temperature in C at cansat level
            // returns level 
            return (Math.Pow(P0 / P, 1 / 5.257) - 1) * (T + 273.15) / 0.0065;
        }


        public static IEnumerable<Control> GetAllControls(Control container)
        {
            List<Control> controlList = new List<Control>();
            foreach (Control c in container.Controls)
            {
                controlList.AddRange(GetAllControls(c));
                controlList.Add(c);
            }
            return controlList;
        }
    }

    class GaugeChart
    {
        Series s;
        Chart chart;
        TextBox textBox;
        string outputFormat;
        double range;

        public GaugeChart(Chart chart, TextBox textbox, string outputFormat)
        {
            this.outputFormat = outputFormat;
            this.chart = chart;
            textBox = textbox;
            s = chart.Series[0];
            s.SetCustomProperty("PieStartAngle", 0 + "");
            s.SetCustomProperty("DoughnutRadius", "30");

        }

        public void Update(double val)
        {
            s = chart.Series[0];
            s.Points.Clear();
            s.Points.AddY(90);
            s.Points.AddY(0);
            s.Points.AddY(0);
            s.Points[0].Color = Color.Transparent;
            if (val < 0)
            {
                range = 500;
                val = val * -1;
                s.Points[1].Color = Color.FromArgb(0, 179, 0);
            }
            else
            {
                range = 100;
                s.Points[1].Color = Color.FromArgb(240, 100, 80); // pomaranczowy (łososiowy)
            }
            s.Points[2].Color = Color.FromArgb(120, 190, 200); // j.niebieski

            //double range = valMax - valMin;
            //double aRange = 360 - angle;
            double aRange = 360 - 90; // 270
            double f = aRange / range;


            double v1 = val * f;
            double v2 = (range - val) * f;

            s.Points[1].YValues[0] = v1;
            s.Points[2].YValues[0] = v2;

            //Console.WriteLine(s.Points[0].YValues[0]);
            //Console.WriteLine(s.Points[1].YValues[0]);
            //Console.WriteLine(s.Points[2].YValues[0]);
            //Console.WriteLine("");

            try
            {
                textBox.Text = String.Format(outputFormat, Convert.ToInt32(val));
            }
            catch { }
            chart.Refresh();

            
        }
    }
}


