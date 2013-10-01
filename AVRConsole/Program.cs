using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using AVR_Device;
using System.Diagnostics;
using System.Threading;
using Peltier_GUI;

namespace AVRConsole
{
    class Program
    {
        static public void launchAVRDude()
        {
            ProcessStartInfo avrdude = new ProcessStartInfo();

            avrdude.CreateNoWindow = false;
            avrdude.UseShellExecute = false;
            avrdude.WindowStyle = ProcessWindowStyle.Hidden;
            avrdude.FileName = "avrdude.exe";
            avrdude.Arguments = "-c avr109 -p m32u4 -P COM7 -U lfuse:r:0xfc:m -U hfuse:r:0xd0:m -U efuse:r:0xf3:m";

            try
            {
                using (Process exeProc = Process.Start(avrdude))
                {
                    exeProc.WaitForExit();
                }
            }
            catch
            {
                throw new Exception("AVRDUDE fails, impossible to connect to AVR");
            }
        }

        static void Main(string[] args)        
        {   
           // AVRDevice _avr;
            Peltier _peltier;
            launchAVRDude();
            Thread.Sleep(3000);
            return;

            Console.WriteLine("AVR Console Started");
            string[] ports = SerialPort.GetPortNames();
            Console.WriteLine("Available Ports: ");
            foreach (string s in ports)
            {
                Console.WriteLine(s);
            }
            
            //_avr = new AVRDevice("COM8");
            //_avr.Connect();
            _peltier = new Peltier("COM19");
            _peltier.Connect();

            Console.WriteLine("Type Exit to end");
            string cmd;
            while (true)
            {                
                cmd = Console.ReadLine();
                //_avr.Send(cmd);
                //string res = _avr.getCOMData();
                //Console.Write(res);
                //if (cmd.ToLower().Equals("exit")) break;
                char[] delim = { ' ', '\r', '\n' };
                string[] tok = cmd.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                try
                {
                    if (tok[0].ToLower().StartsWith("exit")) break;
                    else if (tok[0].ToLower().StartsWith("info")) Console.WriteLine(_peltier.GetVersion());
                    else if (tok[0].ToLower().StartsWith("ack"))
                    {
                        if (_peltier.GetAck()) Console.WriteLine("ACK");
                        else Console.WriteLine("NACK");
                    }
                    else if (tok[0].ToLower().StartsWith("init"))
                    {
                        //uint channel = Convert.ToUInt16(tok[1]);
                        _peltier.PeltierInit();
                        Console.WriteLine("Init-> OK");
                    }
                    else if (tok[0].ToLower().StartsWith("pwm_start"))
                    {
                        uint channel = Convert.ToUInt16(tok[1]);
                        _peltier.PwmStart(channel);
                        Console.WriteLine("PWM Start -> OK");
                    }
                    else if (tok[0].ToLower().StartsWith("pwm_stop"))
                    {
                        uint channel = Convert.ToUInt16(tok[1]);
                        _peltier.PwmStop(channel);
                        Console.WriteLine("PWM Stop -> OK");
                    }
                    else if (tok[0].ToLower().StartsWith("pwm_set"))
                    {
                        uint channel = Convert.ToUInt16(tok[1]);
                        uint level = Convert.ToUInt16(tok[2]);
                        _peltier.PwmSet(channel, level);
                        Console.WriteLine("PWM Set -> OK");
                    }
                    else if (tok[0].ToLower().StartsWith("adc_calib"))
                    {
                        _peltier.ADCCalibration();
                        Console.WriteLine("ADC Calibration Result\nAlpha -> {0}\nBeta -> {1}", _peltier.alpha, _peltier.beta);
                    }
                    else if (tok[0].ToLower().StartsWith("get_temperature"))
                    {
                        _peltier.GetTemperatures();
                        Console.WriteLine("Room Temperature -> {0}", _peltier.room_temperature);
                        Console.WriteLine("Peltier Temperature -> {0}", _peltier.peltier_temperature);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error: " + ex.Message);
                }
            }
            
        }
    }
}
