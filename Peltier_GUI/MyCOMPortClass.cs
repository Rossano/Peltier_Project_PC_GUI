using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Threading;


namespace MyCOMPort
{
    public class MyCOMPortClass
	{
	protected bool _isConnected;
	protected static SerialPort comport;
	protected String name = "COM1";
	protected static String message;
	protected Thread readThread;
	protected Queue<Int32> val;
	public List<String> log;
		
		/*--------------------------------------------
			Only to use for Standalone Debug
		--------------------------------------------*/
		public MyCOMPortClass ()
		{
			//NewAcquisition = false;
			comport = new SerialPort();
			val = new Queue<Int32>();
			log = new List<String>();
			// Quick way do not ask for parameters

            comport.PortName = SetPortName (comport.PortName);
			comport.BaudRate = (int)SetPortBaudRate(19200);
			comport.Parity = SetPortParity(Parity.None);
			comport.DataBits = SetPortDataBit(8);
			comport.StopBits = SetPortStopBits(StopBits.One);
			comport.Handshake = SetPortHandshake(Handshake.None);
	
			comport.ReadTimeout = 3000;
			comport.WriteTimeout = 500;
        }

        public MyCOMPortClass(String name, int baud, Parity parity, int bits, StopBits stop, Handshake hand)
		{			
			comport = new SerialPort();
			val = new Queue<Int32>();
			log = new List<String>();
			// Set port parameters
			comport.PortName = name; //SetPortName (comport.PortName);
			comport.BaudRate = (int)SetPortBaudRate(baud);
			comport.Parity = SetPortParity(parity);
			comport.DataBits = SetPortDataBit(bits);
			comport.StopBits = SetPortStopBits(stop);
			comport.Handshake = SetPortHandshake(hand);
			// Set up timeouts
			comport.ReadTimeout = 500;
			comport.WriteTimeout = 500;
		}

        public void Disconnect()
		{
			_isConnected = false;
			Thread.Sleep(500);
			//readThread.Join();
			comport.Close();			
			//comport.Finalize();
		}

        //public bool getConnectionStatus()
        //{
        //    return isConnected;
        //}
        public bool isConnected
        {
            get
            {
                return _isConnected;
            }
            set
            {
                _isConnected = value;
            }
        }

		//void Disconnect()
		//{
		//	isConnected = false;
		//}

        public void send(String msg)
		{
			try
			{
				comport.WriteLine (msg);
			}
			catch (Exception e)
			{
				throw new Exception (e.Message);
			}
		}

		String get ()
		{
			try
			{
				return comport.ReadLine();
			}
			catch (Exception e)
			{
				throw new Exception (e.Message);
			}
		}

		public Int32 getData ()
		{
			try
			{
				return val.Dequeue();
			}		
			catch (InvalidOperationException e)
			{
				throw e;
			}
		}

		public List<String> getLog ()
		{
			return log;
		}

		long SetPortBaudRate(int baud)
		{
			return baud;
		}

		Parity SetPortParity(Parity parity)
		{
			return parity;
		}

		int SetPortDataBit(int bits)
		{
			return bits;
		}
		
		StopBits SetPortStopBits(StopBits stop)
		{
			return stop;
		}
		
		Handshake SetPortHandshake(Handshake hand)
		{
			return hand;
		}

        String SetPortName (String name)
        {
            return name;
        }
	};
}