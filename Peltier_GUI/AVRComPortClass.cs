using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;
using MyCOMPort;


namespace AVR_Peltier
{    

	public class DUTCOMPortClass: MyCOMPortClass
	{
		public const string PROMPT = "AVR>";
		public bool NewAcquisition;
		public bool Stack = false;
		public bool isReady = false;
		private string p;
		private int p_2;
		public Parity parity;
		private int p_3;
		public StopBits stopBits;
		public Handshake handshake;
		public int baudRate;
		private System.Windows.Threading.DispatcherTimer timeout;

		public DUTCOMPortClass (): base()
		{
			NewAcquisition = false;
			try
			{
				comport.Open();
				readThread = new Thread (new ThreadStart (this.Read)); 
				isConnected = true;
				readThread.Start();
			}
			catch (UnauthorizedAccessException )
			{
				throw new UnauthorizedAccessException ("Access denied on this port, COM Port already open");
			}
			catch (ArgumentOutOfRangeException )
			{
				throw new ArgumentOutOfRangeException ("Parity, DataBits, StopBits or Handshake are not valid values\n" + 
						"BaudRate, TimeOuts are less than or equal to zero");
			}
			catch (ArgumentException e)
			{
				throw new ArgumentException ("Port name does not begin with COM");
			}
			catch (IOException )
			{
				throw new IOException ("Port is in invalid state");
			}
			catch (InvalidOperationException)
			{
				throw new InvalidOperationException ("Specified Port in current instance is already open");
			}
		}

		public DUTCOMPortClass (String name, int baud, Parity parity, int bits, StopBits stop, Handshake hand): 
			base(name, baud, parity, bits, stop, hand)
		{
			//NewAcquisition = false;
			try
			{
				message = string.Empty;
				comport.Open();
				readThread = new Thread (new ThreadStart (this.Read)); 
				isConnected = true;
				timeout = new System.Windows.Threading.DispatcherTimer();
				timeout.Interval = new TimeSpan(0, 0, 10);
				timeout.Tick+=new EventHandler(timeout_Tick);
				readThread.Start();
				send("\n");                 //  To initialize Minishell
			}
			catch (UnauthorizedAccessException)
			{
				throw new UnauthorizedAccessException ("Access denied on this port, COM Port already open");
			}
			catch (ArgumentOutOfRangeException )
			{
				throw new ArgumentOutOfRangeException ("Parity, DataBits, StopBits or Handshake are not valid values\n" + 
						"BaudRate, TimeOuts are less than or equal to zero");
			}
			catch (ArgumentException e)
			{
				throw new ArgumentException ("Port name does not begin with COM");
			}
			catch (IOException )
			{
				throw new IOException ("Port is in invalid state");
			}
			catch (InvalidOperationException)
			{
				throw new InvalidOperationException ("Specified Port in current instance is already open");
			}
		}

		void Read()
		{	
			bool peekNext = false;
			//Int32 count = 0;
			bool readRegisterCommand = false;
			while (base.isConnected)
			{				
				try
				{
					message = comport.ReadLine();
					log.Add(message);
					//
					//  Write Log to Main Window
					//
					//MainWindow.COMLog.Text. (message);
					if (peekNext)
					{
						//string dummy = {' '};
						string[] str = message.Split ((string[]) null, StringSplitOptions.RemoveEmptyEntries);
						string hexstr;
						Int32 i;
						if (readRegisterCommand)
						{
							hexstr = str[1].Substring(2, 8);
							try
							{
								i = Int32.Parse(hexstr, System.Globalization.NumberStyles.HexNumber, null);
							}
							catch (Exception)
							{
								i = 0;
							}
						}
						else
						{
							hexstr = str[7];
							try
							{
								i = Int32.Parse(hexstr);
							}
							catch (Exception)
							{
								i = 0;
							}
						}                        
						val.Enqueue (i);
					}
					peekNext = message.Contains("Si49>dbg read") || message.Contains("Si49>auxadc convint");
					if (peekNext)
					{
						if (message.ToLower().Contains("dbg read")) readRegisterCommand = true;
						else readRegisterCommand = false;
						// Write Log
						log.Add(String.Format("[{0}]: {1}", name, message));							
					}
				}
				catch (TimeoutException ) 
				{
					throw new TimeoutException("AVR connection Lost!");
				}
			}	
		}

		private void timeout_Tick (object sender, EventArgs e)
		{
			throw new TimeoutException("Timeout Error when accessing AVR Controller");
		}

		public void ClearLog()
		{
			base.log.Clear();
		}

		public void Connect()
		{
			try
			{
				timeout.Start();
				while (!message.Equals(PROMPT)) ;
				isReady = true;
				timeout.Stop();
			}
			catch (Exception e)
			{                
				throw e;
			}
		}

		public void Disconnect()
		{
			base.Disconnect();
			isReady = false;
		}
	 
	};

}
	
