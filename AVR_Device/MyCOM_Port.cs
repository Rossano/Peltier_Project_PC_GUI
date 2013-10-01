using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
//using System.Threading;
using System.Timers;

namespace AVR_Device
{
    public class COM_Device : IDisposable
    {
        #region Members

        protected SerialPort _com;
        private string _name;
        private int _baudrate;
        private int _databits;
        private Parity _parity;
        private StopBits _stopbits;
        private Handshake _handshake;
        protected bool _isConnected = false;

        private Queue<string> datain;
        private Timer timeOut;

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes a new instance of the <see cref="COM_Device"/> class.
        /// </summary>
        /// <param name="name">Serial Port name</param>
        public COM_Device(string name)
        {
            try
            {
                //  Create the serial port object
                _com = new SerialPort(name);
                //  Set-up the parameters of the port
                _name = name;
                _baudrate = 9600;
                _databits = 8;
                _stopbits = StopBits.One;
                _parity = Parity.None;
                _handshake = Handshake.None;
                //  Initialize the connection flag
                _isConnected = true;
                //  Initialize the Read/Write timeouts
                _com.ReadTimeout = 5000;
                _com.WriteTimeout = 500;
                //_isConnected = true;
                //  Define the handler for the received data from the serial port
                _com.DataReceived += new SerialDataReceivedEventHandler(RXHandler);
                //  Open the Serial port
                _com.Open();

                //  Set-up a timer used for timeout when connecting the device
                timeOut = new Timer();
                timeOut.Interval = 5000;
                timeOut.AutoReset = false;
                timeOut.Elapsed += new ElapsedEventHandler(timeOut_Elapsed);

                //  Initialize the Queue of the received data
                datain = new Queue<string>();
            }
            catch (Exception ex)
            {
                //  Throw the caught exception
                throw ex;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="COM_Device" /> class.
        /// </summary>
        /// <param name="name">The serial port name.</param>
        /// <param name="baudrate">The serial port baudrate.</param>
        /// <param name="databits">The serial port databits.</param>
        /// <param name="parity">The serial port parity.</param>
        /// <param name="stopbits">The serial port stopbits.</param>
        /// <param name="handshake">The serial port handshake.</param>
        /// <param   name="isArduino">if set to <c>true</c> [is arduino] configures the Serial port accordingly.</param>
        public COM_Device(string name, int baudrate, int databits, Parity parity, StopBits stopbits, Handshake handshake, bool isArduino)
        {
            try
            {
                //  Create the serial port object
                _com = new SerialPort(name, baudrate, parity, databits, stopbits);
                //  Set-up the serial port parameters passed to the function
                _name = name;
                _databits = databits;
                _baudrate = baudrate;
                _stopbits = stopbits;
                _parity = parity;
                _com.Handshake = handshake;
                _handshake = handshake;
                //
                //  If it is an Arduino micro-controller, the USB driver needs to control DTS/RTS
                if (isArduino)
                {
                    _com.DtrEnable = true;
                    _com.RtsEnable = true;
                }
                //  Set-up the Read/Write timeouts
                _com.ReadTimeout = 5000;
                _com.WriteTimeout = 500;
                //  Defines the Serial Port received data Handler
                _com.DataReceived += new SerialDataReceivedEventHandler(RXHandler);
                //  Opens the Serial port
                _com.Open();

                //  Set-up a timer used as Timeout when connecting the device
                timeOut = new Timer();
                timeOut.Interval = 5000;
                timeOut.AutoReset = false;
                timeOut.Elapsed += new ElapsedEventHandler(timeOut_Elapsed);

                // Initialize the Queue storing the data received from the Serial Port
                datain = new Queue<string>();
            }
            catch (Exception ex)
            {
                // throw the exception
                throw ex;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            //  Indicates the the device is now disconneted
            _isConnected = false;
            Dispose();
        }

        #endregion

        #region Handlers

        /// <summary>
        /// Serial Port Received Data Event Handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="SerialDataReceivedEventArgs"/> instance containing the event data.</param>
        private void RXHandler(object sender, SerialDataReceivedEventArgs e)
        {
            //  Read the data from the serial port
            SerialPort sp = (SerialPort)sender;
            string res = sp.ReadExisting();
            //  Store it into the FIFO queue
            datain.Enqueue(res);
        }

        /// <summary>
        /// Connection Timeout Event Handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ElapsedEventArgs"/> instance containing the event data.</param>
        /// <exception cref="System.TimeoutException"></exception>
        private void timeOut_Elapsed(object sender, ElapsedEventArgs e)
        {
            //  Throw a timeout exception
            throw new TimeoutException(string.Format("Timeout occurred reading on {0}\n", _name));
        }

        #endregion

        #region Methods

        /// <summary>
        /// Sends the specified command to the Serial port.
        /// </summary>
        /// <param name="cmd">The command to be sent.</param>
        public void Send(string cmd)
        {
            _com.Write(cmd + "\r\n");
        }

        /// <summary>
        /// Gets the COM data from the reading FIFO.
        /// </summary>
        /// <returns>The data stored into the FIFO</returns>
        public string getCOMData()
        {
            //
            //  Start the reading timeout, if there are no data into the FIFO, instead of locking the
            //  application awaiting indefinitely, it fires a timeout that throws an exception
            //
            timeOut.Start();
            do
            {
                //  If there is any data into the FIFO read it
                if (datain.Count() != 0)
                {
                    //  Stop the timer since there are some data
                    timeOut.Stop();
                    string res = "";                    
                    //  Read all the data into the FIFO
                    while (datain.Count > 0)
                    {
                        res += datain.Dequeue() + "\n";
                    }
                    return res;
                }
            }
            while (true);
        }

        public string getName()
        {
            return _name;
        }

        public int getBaudrate()
        {
            return _baudrate;
        }

        public int getDatabits()
        {
            return _databits;
        }

        public StopBits getStopbits()
        {
            return _stopbits;
        }

        public Parity getParity()
        {
            return _parity;
        }

        public Handshake getHandshake()
        {
            return _handshake;
        }

        public bool isConnected()
        {
            return _isConnected;
        }

        public void setName(string name)
        {
            _name = name;
        }

        public void setBaudrate(int baudrate)
        {
            _baudrate = baudrate;
        }

        public void setDatabits(int databits)
        {
            _databits = databits;
        }

        public void setParity(Parity parity)
        {
            _parity = parity;
        }

        public void setStopbits(StopBits stopbits)
        {
            _stopbits = stopbits;
        }

        public void setHandshake(Handshake handshake)
        {
            _handshake = handshake;
        }

        #endregion

    }
}

