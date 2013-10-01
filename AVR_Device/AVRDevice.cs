using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using System.Diagnostics;

namespace AVR_Device
{
	public class AVRDevice: COM_Device, IDisposable
	{
				#region Members
		/// <summary>
		/// AVR initialization state
		/// </summary>
		public bool isInitialize { get; private set; }
		public bool isArduinoBootLoader { get; private set; }
		#endregion

		#region Constructor

		/// <summary>
		/// Object Constructor.
		/// 
		/// Create a Serial Port and initialize it to false
		/// </summary>
		/// <param name="name">COM Port name</param>
		public AVRDevice(string name):
			//base(name,9600,8,System.IO.Ports.Parity.None, System.IO.Ports.StopBits.One,System.IO.Ports.Handshake.None)
			base(name,9600,8,Parity.None,StopBits.One,Handshake.None, true)
		{
			isInitialize = false;
			isArduinoBootLoader = false;
			//Send("\n");
		}

		/// <summary>
		/// Object Constructor.
		/// 
		/// Create a serial port and initialize it to false.
		/// </summary>
		/// <param name="name">Com Port name.</param>
		/// <param name="flag">Flag storing if it is an Arduino BootLoader (true) or not (false).</param>
		public AVRDevice(string name, bool flag):base(name, 9600, 8, Parity.None, StopBits.One, Handshake.None, true)
		{
			isInitialize = false;
			isArduinoBootLoader = flag;
		}

		#endregion

		#region Methods

		/// <summary>
		/// AVR Device Connects Method. 
		/// </summary>
		/// 
		/// Open the Serial Port, ask the Version and checks the answer
		/// <returns>Result String</returns>
		public string Connect()
		{
			/// If the Serial Port is not istanciated return an error			
			if (this._com == null) return "COM port not ready!";
			if (!isInitialize)
			{
				/// If the object is not initializated, send the Version Request and check
				/// if the returned string is coherent.
				/// If object is already in initialized state return an error
				try
				{
					Send("get_ACK");
					string res = getCOMData();
					if (res.StartsWith("ACK") || res.Contains("AVR>"))
					{
						/// If the returned string is cohenrent set the state and return OK
						isInitialize = true;
						_isConnected = true;						
						return "AVR Initialization OK";
					}
					else
					{
						/// Else return and error and pput the status uninitialized
						isInitialize = false;
						_isConnected = false;						
						return "AVR COM port opened but failed to complete initialization process";
					}
				}
				catch (Exception ex)
				{	
					/// In case of Error
					throw ex;
				}
			}
			else return "AVR already initialized";
		}

		/// <summary>
		/// Get  Temperature Method.
		/// </summary>
		/// <param name="area">The area index of the Temperature Request.</param>
		/// <returns>
		/// Temperature Value as String
		/// </returns>
		/// Send the Temperature Request and  Send back the result. Area can be null, in that case it is 
		/// read the main measurement area temperature.
		public string Get_Temperature(Nullable<int> adc)
		{
			try
			{
				if (adc == null)
				{
					/// Send the Read temperature Request for the main measurement area
					Send("?T");
				}
				else
				{
					/// Send the Read temperature Request for the measurement area <area>
					Send(string.Format("?T({0})", (int)adc));
				}
				/// Awaits and Read the answer from the PI-160
				string res = getCOMData();
				/// Answer cohenence check, if it pass split the string coming from IR Camera and send back
				/// to the client making the request the temperature only/
				/// If it does not pass return an error string
				if (res.StartsWith("ADC="))
				{
					char[] separators = { '!', '=', '°', '?' };
					string[] val = res.Split(separators, StringSplitOptions.RemoveEmptyEntries);
					return val[1];
				}
				else return "Error getting Temperature Area";
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		
		/// <summary>
		/// Get PI-160 Version Method.
		/// </summary>
		/// 
		/// Send the Read Version Request and  Send back the result
		/// <returns>Temperature Value as String</returns>
		public string Get_Version()
		{
			try
			{
				/// Send the Read Version Request
				Send("info");
				/// Awaits and Read the answer from the PI-160
				string res = getCOMData();
				/// Answer cohenence check, if it pass split the string coming from IR Camera and send back
				/// to the client making the request the temperature only/
				/// If it does not pass return an error string
				if (res.StartsWith("FW="))
				{
					char[] separators = { '!', '=', '°', '?' };
					string[] val = res.Split(separators, StringSplitOptions.RemoveEmptyEntries);
					return val[1];
				}
				else return "Error getting AVR Configuration";
			}
			catch (Exception ex)
			{
				throw ex;
			}
		}

		/// <summary>
		/// Sets the boot loader flag.
		/// </summary>
		/// <param name="flag">Flag storing if it is an Arduino Bootloader (true) or not (false).</param>
		public void setBootLoaderFlag(bool flag)
		{
			isArduinoBootLoader = flag;
		}

		#endregion
	}
}
