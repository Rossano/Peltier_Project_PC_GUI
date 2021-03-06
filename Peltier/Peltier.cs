﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AVR_Device;
using System.Threading;

namespace Peltier_GUI
{
    public class Peltier
    {
        #region Constants
        // 
        //  Board Configurations
        //
        const uint Peltier_PWM = 4;//2;                 //  Peltier Cell PWM channel
        const uint Room_Temperature_ADC = 3;            //  ADC channel measuring the Room Temperature
        const uint Peltier_Temperature_ADC = 30;//2;    //  ADC channel measuring the Peltier Cell temperature
        const uint AVR_BandGap_ADC = 30;                //  ADC Bandgap channel
        const uint AVR_GND_ADC = 31;                    //  ADC ground channel
        const double ADC_Ref = 5.0;                     //  ADC Reference voltage used in conversion

        #endregion

        #region Members

        public double room_temperature { get; set; }    //  Member storing the Room Temperature
        public double peltier_temperature { get; set; } //  Member storing the Peltier Cell Temperature
        public uint pwm_level { get; set; }             //  Peltier Cell PWM channel level
        public AVRDevice _avr;                          //  AVR Object
        private Queue<string> avrResult;                //  FIFO storing the Strings received from AVR
        public double alpha = 1.0;                      //  ADC Calibration Gain factor
        public double beta = 0;                         //  ADC Calibration Offset factor

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="Peltier"/> class.
        /// Creates all the data structures but nothing else.
        /// AVR object is NOT INITIALIZED
        /// </summary>
        public Peltier()
        {
            //  AVR NOT INITIALIZED
            _avr = null;
            room_temperature = -100;
            peltier_temperature = -100;
            pwm_level = 0;
            //  Initialize the Input FIFO
            avrResult = new Queue<string>();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Peltier"/> class.
        /// </summary>
        /// <param name="com">The AVR Serial Port Name.</param>
        public Peltier(string com)
        {
            room_temperature = -100;
            peltier_temperature = -100;
            pwm_level = 0;
            //  Initialize the Input FIFO
            avrResult = new Queue<string>();
            try
            {
                //  Initialize the AVR object
                _avr = new AVRDevice(com);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="Peltier"/> class.
        /// </summary>
        /// <param name="com">The AVR Serial Port Nale.</param>
        /// <param name="flag">if set to <c>true</c> Tells the application that AVR109 bootloader has to be used.</param>
        public Peltier(string com, bool flag)
        {
            room_temperature = -100;
            peltier_temperature = -100;
            pwm_level = 0;
            //  Initialize the Input FIFO
            avrResult = new Queue<string>();
            try
            {
                //  Initialize the AVR object
                _avr = new AVRDevice(com, flag);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }
        
        #endregion

        #region Methods

        /// <summary>
        /// Method to Clips the input level.
        /// </summary>
        /// <param name="level">The level to be clipped on 16 bits.</param>
        /// <returns></returns>
        private uint pwm_clip(uint level)
        {
            //  If level is < 0 returns 0, else if it is greater than 65535(2^16-1) returns 65535
            //  else return the level value
            if (level < 0) return 0;
            else if (level > 65535) return 65535;
            else return level;
        }

        /// <summary>
        /// Connects the Application to the AVR instance.
        /// </summary>
        /// <exception cref="System.NullReferenceException">AVR Objcet not referenced in Peltier class</exception>
        public void Connect()
        {
            //  If the AVR instance has already been instantied launch its connect method, else throw an exception
            try
            {
                if (_avr != null) _avr.Connect();
                else throw new NullReferenceException("AVR Objcet not referenced in Peltier class");
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Sends a command to the AVR instance and checks the operations is fine.
        /// </summary>
        /// <param name="cmd">The command to be sent to AVR.</param>
        /// <returns></returns>
        public bool SendCommand(string cmd)
        {
            //  Create the request string
            string request = cmd + "\r\n";
            //
            //  For robustness reasons the request is sent 10 times before sending an exception
            //
            uint count = 10;
            do
            {
                //  Send the request to the AVR instance
                _avr.Send(request);
                //  Waits the AVR to complete the operation and read from the AVR instance
                Thread.Sleep(100);
                string result = _avr.getCOMData();
                //  If the result contains "OK" 
                if (result.Contains("OK"))
                {
                    //  Push the received string into the Input FIFO and return
                    avrResult.Enqueue(result);
                    return true;
                }
                else if (result.Contains("Error:"))
                {
                    //  If string contains Error decrease the counter and send twice 
                    //  a CR LF string reading the output.
                    //  Then the string is send again to the AVR instance
                    count--;
                    _avr.Send("\r\n");
                    Thread.Sleep(100);
                    _avr.Send("\r\n");
                    Thread.Sleep(100);
                    _avr.getCOMData();
                }
                else
                {
                    //  It shouldn't come here, but in case push the received string into the FIFO and returns
                    avrResult.Enqueue(result);
                    return true;
                }

            }
            //  If counter is not expired send the command again
            while (count > 0);
            //  Counter has expired, operation failed
            return false;
        }

        /// <summary>
        /// PInitialize the Peltier Instance.
        /// </summary>        
        public string PeltierInit()
        //public void PeltierInit()
        {
            string result;
            try
            {
                //  Send the command to initialize the PWM channel connected to the Peltier Cell
                result = InitPwm(Peltier_PWM);
                //  Initialize the ADC channels connected to the Thermal Sensors
                result += InitSensors();
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Method to Initialie the Peltier Cell PWM channel.
        /// </summary>
        /// <param name="pwm">The PWM channel.</param>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Invalid PWM channel</exception>        
        protected string InitPwm(uint pwm)
        //protected void InitPwm(uint pwm)
        {
            //  Initially check if the pwm channel is possible
            if ((pwm >= 0) && (pwm < 5))
            {
                try
                {
                    //  Creates the PWM initialization channel in the AVR Firmware format
                    string cmd = string.Format("pwm_init {0}", pwm);
                    string result;
                    //  Send the command to the AVR instance, if the result is true (AVR instance responded "OK")
                    //  Read the result from the FIFO to clean it up.
                    //  Else send an exception using the resulting string as message
                    if (SendCommand(cmd) == true) result = avrResult.Dequeue();
                    else throw new Exception(avrResult.Dequeue());
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else throw new ArgumentOutOfRangeException("Invalid PWM channel");
        }

        /// <summary>
        /// Initialize the ADC channel connected to the Temperature sensors.
        /// </summary>
        /// <exception cref="System.Exception"></exception>
        protected string InitSensors()
        //protected void InitSensors()
        {
            try
            {
                //  Create the ADC initialization command in AVR Firmware version
                string cmd = "adc_init";
                string result;
                //  Send the command to the AVR instance, if the result is true (AVR instance responded "OK")
                //  Read the result from the FIFO to clean it up.
                //  Else send an exception using the resulting string as message
                if (SendCommand(cmd)) result = avrResult.Dequeue();
                else throw new Exception(avrResult.Dequeue());
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Set the level of the PWM channel.
        /// </summary>
        /// <param name="channel">The PWM channel.</param>
        /// <param name="level">The new PWM level.</param>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Invalid PWM channel</exception>
        public string PwmSet (uint channel, uint level)
        // public void PwmSet (uint channel, uint level)
        {
            //  Firstly check if the PWM channel is coherent
            if ((channel >= 0) && (channel < 5))
            {
                try
                {
                    //  Create the PWM set command in the AVR Firmware format
                    string cmd = string.Format("pwm_set {0} {1}", channel, level);                    
                    string result;
                    //  Send the command to the AVR instance, if the result is true (AVR instance responded "OK")
                    //  Read the result from the FIFO to clean it up.
                    //  Else send an exception using the resulting string as message.
                    if (SendCommand(cmd)) result = avrResult.Dequeue();
                    else throw new Exception(avrResult.Dequeue());
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else throw new ArgumentOutOfRangeException("Invalid PWM channel");
        }

        /// <summary>
        /// Start a PWMs channel.
        /// </summary>
        /// <param name="channel">The PWM channel.</param>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Invalid PWM channel</exception>
        public string PwmStart(uint channel)
        // public void PwmStart(uint channel)
        {
            //  Firstly check if the PWM channel is coherent
            if ((channel >= 0) && (channel < 5))
            {
                try
                {
                    //  Create the PWM start command in the AVR Firmware format
                    string cmd = string.Format("pwm_start {0}", channel);                    
                    string result;
                    //  Send the command to the AVR instance, if the result is true (AVR instance responded "OK")
                    //  Read the result from the FIFO to clean it up.
                    //  Else send an exception using the resulting string as message.
                    if (SendCommand(cmd)) result = avrResult.Dequeue();
                    else throw new Exception(avrResult.Dequeue());
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else throw new ArgumentOutOfRangeException("Invalid PWM channel");
        }

        /// <summary>
        /// Stop a PWMs channel.
        /// </summary>
        /// <param name="channel">The PWM channel.</param>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Invalid PWM channel</exception>
        public string PwmStop(uint channel)
        // public void PwmStop(uint channel)
        {
            //  Firstly check if the PWM channel is coherent
            if ((channel >= 0) && (channel < 5))
            {
                try
                {
                    //  Create the PWM stop command in the AVR Firmware format
                    string cmd = string.Format("pwm_stop {0}", channel);
                    string result;
                    //  Send the command to the AVR instance, if the result is true (AVR instance responded "OK")
                    //  Read the result from the FIFO to clean it up.
                    //  Else send an exception using the resulting string as message.
                    if (SendCommand(cmd)) result = avrResult.Dequeue();
                    else throw new Exception(avrResult.Dequeue());
                    return result;
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else throw new ArgumentOutOfRangeException("Invalid PWM channel");
        }

        /// <summary>
        /// Method to Calibrate the ADC.
        /// </summary>
        /// <exception cref="System.Exception">
        /// </exception>
        public void ADCCalibration()
        {
            //
            //  The Method is measuring two known voltages on the ADC (the ground and the Bandgap)
            //  And from the result and the exptected result is carrying out the gain and offset corrections.
            //
            try
            {
                //  Read the ADC ground channel
                string cmd=string.Format("adc_get  {0}", AVR_GND_ADC);
                string result;
                uint A0, A1;
                //  Send the command and reads out the result
                if (SendCommand(cmd))
                {
                    Thread.Sleep(100);
                    //  Read the response from FIFO
                    result = avrResult.Dequeue();
                    //  Split the string in its different words (tokens)
                    char[] delim = { ' ', ':', ';', '\r', '\n' };
                    string[] tokens = result.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    uint i = 0;
                    //  Looks into the tokens for "ADC->"  the value is the next one
                    foreach (string s in tokens)
                    {
                        if (s.Equals("ADC->")) break;
                        i++;
                    }
                    //  Convert and store the ADC value
                    A0 = Convert.ToUInt16(tokens[i + 1]);
                }
                else throw new Exception(avrResult.Dequeue());
                //  Read teh ADC Bandgap channel
                cmd = string.Format("adc_get {0}", AVR_BandGap_ADC);
                //  Send the command and read out the result
                if (SendCommand(cmd))
                {
                    Thread.Sleep(100);
                    //  Read the response from the FIFO
                    result = avrResult.Dequeue();
                    //  Split the string in its different words (tokens)
                    char[] delim = { ' ', ':', ';', '\r', '\n' };
                    string[] tokens = result.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                    uint i = 0;
                    //  Looks into the tokens for "ADC->" the value conversion is the next one
                    foreach (string s in tokens)
                    {
                        if (s.Equals("ADC->")) break;
                        i++;
                    }
                    //  Convert and store the ADC value
                    A1 = Convert.ToUInt16(tokens[i + 1]);
                }
                else throw new Exception(avrResult.Dequeue());
                //  Carry out the offset and gain correction factors
                beta = (double)A0 / 1023 * ADC_Ref;
                alpha = ((A1 - A0) * ADC_Ref) / 1023 / 1.1;
            }
            catch(Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Gets the temperature from a sensor.
        /// This method read from an ADC channel and returns the conversion result. It is an ADC reading function.
        /// </summary>
        /// <param name="channel">The ADC channel.</param>
        /// <returns></returns>
        /// <exception cref="System.Exception"></exception>
        /// <exception cref="System.ArgumentOutOfRangeException">Invalid ADC channel</exception>
        public uint GetTemperature(uint channel)
        {            
            //  Firstly check if the channel are coherent with the Arduino Analog Inputs, or the Bandgap Channel
            if((channel>=0)&&(channel<=5)||((channel>=9)&&(channel<=10)) || (channel==AVR_BandGap_ADC))
            {
                try
                {
                    //  Create the ADC reading string in the AVR FIrmware format.
                    string cmd = string.Format("adc_get {0}", channel);
                    string result;
                    //  Send the command to the AVR instance and read the result
                    if (SendCommand(cmd))
                    {
                        do
                        {
                            //  Get the AVR result from FIFO
                            result = avrResult.Dequeue();
                            //  Split the result into the words (the tokens)
                            char[] delim = { ' ', ':', ';', '\r', '\n' };
                            string[] tokens = result.Split(delim, StringSplitOptions.RemoveEmptyEntries);
                            uint i = 0;
                            //  Parse the tokens to look for "ADC->"
                            foreach (string s in tokens)
                            {
                                if (s.Equals("ADC->")) break;
                                i++;
                            }
                            //  the Conversion value is the next one
                            return Convert.ToUInt16(tokens[i + 1]);
                        }
                        while (avrResult.Count > 0);
                    }
                    else throw new Exception(avrResult.Dequeue());
                }
                catch (Exception ex)
                {
                    throw ex;
                }
            }
            else throw new ArgumentOutOfRangeException("Invalid ADC channel");
            return 0;
        }

        /// <summary>
        /// Gets the Peltier Cell and Room temperatures.
        /// This method reads the ADC channels connected to the temperature sensors and convert the result as temperatures.
        /// </summary>
        public string GetTemperatures()
        //public void GetTemperatures()
        {
            string result;
            try
            {
                //  Read the Peltier Cell temperature and convert it into voltage
                double VADC_Peltier = GetTemperature(Peltier_Temperature_ADC) * ADC_Ref / 1023;
//                System.Threading.Thread.Sleep(100);
                //  Read the Room temperature and convert is into voltage
                double VADC_Room = GetTemperature(Room_Temperature_ADC) * ADC_Ref / 1023;
                //  Convert the ADV voltage values into temperature
                room_temperature = (VADC_Room - 0.5) / 0.01;
                peltier_temperature = (VADC_Peltier - 0.5) / 0.01;
                result = string.Format("Peltier T={0}\nRoom T={1}\n", peltier_temperature, room_temperature);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Gets the Peltier Cell and Room temperatures.
        /// This method reads the ADC channels connected to the temperature sensors and convert the result as temperatures.
        /// </summary>
        public string GetTemperatures(uint samples)
        //public void GetTemperatures(uint samples)
        {
            string result;
            try
            {
                double foo1 = 0.0;
                double foo2 = 0.0;
                for (uint i = 0; i < samples; i++)
                {
                    //  Read the Peltier Cell temperature and convert it into voltage
                    double VADC_Peltier = GetTemperature(Peltier_Temperature_ADC) * ADC_Ref / 1023;                    
                    //                System.Threading.Thread.Sleep(100);
                    //  Read the Room temperature and convert is into voltage
                    double VADC_Room = GetTemperature(Room_Temperature_ADC) * ADC_Ref / 1023;
                    //  Convert the ADV voltage values into temperature
                    foo1 += (VADC_Room - 0.5) / 0.01;
                    foo2 += (VADC_Peltier - 0.5) / 0.01;
                }
                room_temperature = foo1 / samples;
                peltier_temperature = foo2 / samples;
                result = string.Format("Peltier T={0}\nRoom T={1}", peltier_temperature, room_temperature);
                return result;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Gets the AVR Firmware version.
        /// </summary>
        /// <returns>The Firmware Version String</returns>
        /// <exception cref="System.Exception"></exception>
        public string GetVersion()
        {
            try
            {
                //  Prepare the AVR Firmware Version string command
                string cmd = "info\r\n";
                //  Send the command to the AVR instance
                _avr.Send(cmd);
                Thread.Sleep(500);
                //  Reads back the answer from the AVR and check if it is consistent
                string res = _avr.getCOMData();
                //  If it contains "OK" returns the string from AVR
                //  Else throw an exception
                if (res.Contains("OK")) return res;
                else if (res.Contains("Error:")) throw new Exception(res);
            }
            catch (Exception ex)
            {
                throw ex;
            }
            return string.Empty;
        }

        /// <summary>
        /// Gets a ACK from the AVR.
        /// </summary>
        /// <returns>True if ACK is received, false if not</returns>
        public bool GetAck()
        {
            try
            {
                //  Prepare the AVR Firmware command to request the ACK
                string cmd = "get_ACK\r\n";
                //  Send the command and read the answer
                _avr.Send(cmd);
                Thread.Sleep(100);
                //  Reads back the answer from the AVR and checks if it is consisten
                string res = _avr.getCOMData();
                //  Return TRUE if the ACK is received, else return FALSE
                if (res.Contains("ACK")) return true;
                else if (res.Contains("Error:")) return false;
            }
            catch (Exception ex)
            {
                //throw ex;
                return false;
            }
            return false;
        }

        #endregion
    }
}
