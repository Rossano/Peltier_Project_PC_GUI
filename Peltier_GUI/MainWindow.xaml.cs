using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using ZedGraph;
using Peltier_Graph;
using MyCOMPort;
using AVR_Peltier;
using System.Windows.Threading;
using System.IO;
using System.IO.Ports;
using System.Diagnostics;
using Peltier_GUI;
using System.Globalization;
using System.Reflection;
using System.Resources;
using System.ComponentModel;
using Tools;

namespace Peltier_GUI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        //
        //  System Related Data & Methods
        //
        public struct Sys
        {
            
        }
        //public AVR_Peltier.DUTCOMPortClass avr;        
        //public Peltier _peltier;
        private const string revisionString = "0.1";        
        private bool isArduino;
        private bool isArduinoBootloader;
        private string AVRBootLoader_COM;
        public TimeSpan elapsedTime = new TimeSpan();
        private TimeSpan lastGraphTick = new TimeSpan();
        private double StartTime;
        private int PWM_Value;
        private int Auto_Value;
        private ZedGraph.ZedGraphControl mainGraph;
        private zedGraphUserControl Graph;
        private DispatcherTimer mainTimer = new DispatcherTimer();
        private DispatcherTimer graphTimer = new DispatcherTimer();
        private DebugWindow debugWnd = null;
        private bool isAuto = false;
        private bool debugMode = true;
        private const int MinTemp = -20;
        private const int MaxTemp = 30;
        private const uint ReadingSamples = 5;
        private string[] HelpPaths;
        private const string DefaultChm = "Peltier_GUI_Help_en_US.chm";
        private string chmFullFileName; 
//        private ResourceManager rm;
//        private Image ConnectedImage;
//        private Image DisconnectedImage;

        //public string ControlTabDef;
        //public string ManualTabDef;
        //public string DebugTabDef;
        //public string Cnt_PortName_Label;
        //public string Cnt_PortName_Grp_Def;

        //#region GUI Only Members

        //private string AVR_COM_Name;
        //private int AVR_COM_Baudrate;
        //private int AVR_COM_Databits;
        //private StopBits AVR_COM_Stopbits;
        //private Parity AVR_COM_Parity;
        //private Handshake AVR_COM_Handshake;

        //#endregion

        public MainWindow()
        {
            InitializeComponent();
            //  Initialize the graphic area
            mainGraph = new ZedGraphControl();
            Graph = new zedGraphUserControl((int)mainGraphHost.Width, (int)mainGraphHost.Height);
            mainGraphHost.Child = Graph;
            try
            {               
                //
                // Read the Configuration file
                //
                configXml = "../../config.xml";
                if (File.Exists(configXml))
                {
                    // Development Version
                    ReadConfiguration();
                }
                else
                {
                    configXml = "config.xml";
                    if (File.Exists(configXml))
                    {
                        // Released Version
                        ReadConfiguration();
                    }
                }
                //
                //  Using XML for language
                //
                //  UpdateDialogs();
                //
                //  Using Resources for language
                //
                UpdateGUI();
            }
            catch (Exception ex)
            {
                ErrDlg("Error Reading Configurations\n", ex);
            }            
            //
            //  If it is AVR Bootloader help to get into the AVR user code via avrdude
            //
            if (!isArduinoBootloader)
            {
                launchAVRDude();
                System.Threading.Thread.Sleep(3000);
            }
            this.Width = 820;
            this.Height = 540;
            PWMGroupBox.IsEnabled = !isAuto;
            autoGroupBox.IsEnabled = isAuto;
            PWM_Value = (int)PWMSlider.Value;
            autoGroupBox.Visibility = Visibility.Collapsed;
            autoGroupBox.IsEnabled = false;
            Auto_Value = convertTemp(MaxTemp);
            Graph.CreateGraph(Graph.Graph);
            //mainGraph = new ZedGraphControl();
            foreach (string s in System.IO.Ports.SerialPort.GetPortNames())
            {
                //ComPortComboBox.Items.Add(s);
                AVRCOMListBox.Items.Add(s);
            }
            //Graph = new zedGraphUserControl((int)mainGraphHost.Width, (int)mainGraphHost.Height);
                //(int)dummy.Width, (int)dummy.Height);
            //mainGraphHost.Child = Graph;
            //Graph.SetSize((int)mainGraphHost.Child.Width, (int)mainGraphHost.Child.Height);
            SolidColorBrush color = new SolidColorBrush();
            color.Color = Color.FromArgb((byte)255, (byte)255, 0, 0);
            PWMSlider.Background = color;
            mainTimer.Interval = TimeSpan.FromSeconds(1);
            mainTimer.IsEnabled = true;
            mainTimer.Tick += mainTimer_Tick;
            mainTimer.Stop();
            graphTimer.Interval = TimeSpan.FromSeconds(2);
            graphTimer.Tick += graphTimer_Tick;
            graphTimer.Stop();
            StartTime = Environment.TickCount;
            try
            {                
                ConnectionImage.Source = new BitmapImage(new Uri(@"/images/DisconnectedImg.png", UriKind.Relative));
                Connected_Label = Properties.Resources.StatusBar_Disconnected;
                ConnectioStatusLabel.Content = Connected_Label;
                //ConnectioStatusLabel.Content = Disconnected_Label;                
            }
            catch
            { }
            //  Get the Full help file name
            bool found = false;
            HelpPaths = new string[3] { "/Help/", "Help/", "../../Help/" };           
            foreach (string s in HelpPaths)
            {
                if (File.Exists(s + DefaultChm))
                {
                    found = true;
                    break;
                }
            }
            if (!found)
            {
                ErrDlg(Properties.Resources.Error_HelpFileNotFound, (Exception)null);
            }
        }       

        /// <summary>
        /// Temperature Conversion utility for the PWM slider box.
        /// </summary>
        /// <param name="temperature">Temperature integer value.</param>
        /// <returns></returns>
        private int convertTemp (int temperature)
        {
            return (int)((MaxTemp - temperature) / (MaxTemp - MinTemp) * 255);
        }

        #region Event Handlers

        /// <summary>
        /// Main Timer Tick Event Handler.
        /// It Fires the new firing interval and, it display the connection elapsed time and it checks the connection
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void mainTimer_Tick(object sender, EventArgs e)
        {
            //Sys.elapsedTime += TimeSpan(1);
            //  Set up the new firing interval
            elapsedTime += TimeSpan.FromSeconds(1);
            //  Update the elapsed time label
            elapsedTimeLabel.Content = elapsedTime.ToString();
            //  Checks up the AVR connection and plot an error if it is found disconnected
            if (!((App)(System.Windows.Application.Current))._peltier.GetAck())
            {
                ConnectioStatusLabel.Content = Disconnected_Label;
                ErrDlg("Error: AVR disconnected", (Exception)null);
            }
        }

        /// <summary>
        /// HUpdate grpahic timer event handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void graphTimer_Tick(object sender, EventArgs e)
        {
            string res = "";
            //  Read the temperature from the AVR
            res = ((App)(System.Windows.Application.Current))._peltier.GetTemperatures();//ReadingSamples);            
            double time = ((double)Environment.TickCount - StartTime) / 1000;
            //  Update the Graphics area with the new data points
            Graph.UpdateGraph(new PointPair(time,((App)(System.Windows.Application.Current))._peltier.peltier_temperature), new PointPair(time, ((App)(System.Windows.Application.Current))._peltier.room_temperature));           
            //  Update the temperatures values
            Temp1Label.Content = ((App)(System.Windows.Application.Current))._peltier.peltier_temperature.ToString();
            Temp2Label.Content = ((App)(System.Windows.Application.Current))._peltier.room_temperature.ToString();
            if (debugWnd.IsVisible) debugWnd.RXbuffer += res;
        }

        /// <summary>
        /// AVR Serial Port Selection Box Event Handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void AVRBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //  Get the last selection for the List Box
            AVR_COM_Name = (string)AVRCOMListBox.SelectedItem;
        }

        /// <summary>
        /// Serial Port Baudrate Change Event Handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SelectionChangedEventArgs"/> instance containing the event data.</param>
        private void AVRBaudrate_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                //
                //  Gets the selected Index of the Baudrate radiobox
                //  And decode the index to set the baudrate accordingly
                //
                int i = AVRBaudrateListBox.SelectedIndex;
                switch (i)
                {
                    case 0: ((App)(System.Windows.Application.Current))._peltier._avr.setBaudrate(9600); break;
                    case 1: ((App)(System.Windows.Application.Current))._peltier._avr.setBaudrate(19200); break;
                    case 2: ((App)(System.Windows.Application.Current))._peltier._avr.setBaudrate(38400); break;
                    case 3: ((App)(System.Windows.Application.Current))._peltier._avr.setBaudrate(57600); break;
                    case 4: ((App)(System.Windows.Application.Current))._peltier._avr.setBaudrate(112200); break;
                    default: ((App)(System.Windows.Application.Current))._peltier._avr.setBaudrate(9600); break;
                }

            }
            catch (Exception ex)
            {
                ErrDlg("Invalid Baudrate Selection", (Exception)null);
            }
        }

        /// <summary>
        /// Connection Button Click Event Handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void AVRConnectButton_Click(object sender, RoutedEventArgs e)
        {
            //  Initialize the PWM GUI element
            PWM_Value = (int)PWMSlider.Value;
            //
            //  Initialize the AVR element
            //
            ((App)(System.Windows.Application.Current))._peltier = new Peltier(AVR_COM_Name, isArduinoBootloader);
            //
            //  Read the Firmware Versions and show it on a MessageBox
            //
            string ver = ((App)(System.Windows.Application.Current))._peltier.GetVersion();
            //  Parse the received string to the the useful information only
            char[] delim = { ':', ' ', '\n' };
            string[] tokens = ver.Split(delim);
            int i = 0;
            foreach (string tok in tokens)
            {
                if (tok.Equals("Firmware")) break;
                i++;
            }
            //  Display the Firmware version
            ver = "";
            for (int j = 0; j < 7; j++)
            {
                ver += tokens[i + j] + " ";
            }
            //  Show the AVR Firmware version into a message box
            System.Windows.MessageBox.Show(ver, "Info", MessageBoxButton.OK, MessageBoxImage.Information);
            //
            //  Display the Control Tab into the GUI
            //
            MainTab.SelectedIndex++;
            //
            //  Start the Timers
            //
            mainTimer.Start();
            graphTimer.Start();
            //  Initialize the Peltier 
            ((App)(System.Windows.Application.Current))._peltier.PeltierInit();
            //  Change the StatusBar Icon
            ConnectionImage.Source = new BitmapImage(new Uri(@"/images/ConnectedImg.png", UriKind.Relative));
            //  Change the StatusBar labels
            ConnectioStatusLabel.Content = Properties.Resources.StatusBar_Connected;
            StatusBar_Version.Content = "FW Ver. : " + tokens[i] + " " + tokens[i + 1];
        }

        /// <summary>
        /// PWMs the slider value changed Event Handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void PWMSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //  Synchronize member and GUI element values
            PWM_Value = (int)PWMSlider.Value;
            PWMSlider.Value = PWM_Value;
            //  Update the code color with respect to the new value
            SolidColorBrush sliderColor = new SolidColorBrush();
            int red = (byte)255 - (byte)PWM_Value;
            int green;
            //if (PWM_Value < 128)
            //{
            //    green = 2 * PWM_Value;
            //}
            //else
            //{
            //    green = 255 - 2 * PWM_Value;
            //}
            int blue = PWM_Value;
            green = (int)Math.Sqrt(255 * 255 - red * red - blue * blue);
            sliderColor.Color = Color.FromArgb((byte)255, (byte)red, (byte)green, (byte)blue);
            float Brightness = 1.0f;
            float Hue = (float)((float)PWM_Value) * 2 / 3 / 255;
            float Saturation = 1.0f;
            sliderColor.Color = HSBtoRGB(Hue, Saturation, Brightness);
            PWMSlider.Background = sliderColor;
        }

        /// <summary>
        /// Conver  from HSB color space to RGB color space.
        /// </summary>
        /// <param name="hue">The hue value.</param>
        /// <param name="saturation">The saturation value.</param>
        /// <param name="brightness">The brightness value.</param>
        /// <returns>An array Containing the converted RGB color space</returns>
        public static Color HSBtoRGB(float hue, float saturation, float brightness)
        {
            int r = 0, g = 0, b = 0;
            if (saturation == 0)
            {
                r = g = b = (int)(brightness * 255.0f + 0.5f);
            }
            else
            {
                float h = (hue - (float)Math.Floor(hue)) *6.0f;
                float f = h - (float)Math.Floor(h);
                float p = brightness * (1.0f - saturation);
                float q = brightness * (1.0f - saturation * f);
                float t = brightness * (1.0f - (saturation * (1.0f - f)));
                switch ((int)h)
                {
                    case 0:
                        r = (int)(brightness * 255.0f + 0.5f);
                        g = (int)(t * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 1:
                        r = (int)(q * 255.0f + 0.5f);
                        g = (int)(brightness * 255.0f + 0.5f);
                        b = (int)(p * 255.0f + 0.5f);
                        break;
                    case 2:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(brightness * 255.0f + 0.5f);
                        b = (int)(t * 255.0f + 0.5f);
                        break;
                    case 3:
                        r = (int)(p * 255.0f + 0.5f);
                        g = (int)(q * 255.0f + 0.5f);
                        b = (int)(brightness * 255.0f + 0.5f);
                        break;
                    case 4:
                        r = (int)(t * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(brightness * 255.0f + 0.5f);
                        break;
                    case 5:
                        r = (int)(brightness * 255.0f + 0.5f);
                        g = (int)(p * 255.0f + 0.5f);
                        b = (int)(q * 255.0f + 0.5f);
                        break;
                }
            }
            return Color.FromArgb(Convert.ToByte(255), Convert.ToByte(r), Convert.ToByte(g), Convert.ToByte(b));
        }

        /// <summary>
        /// Auto PWM Slider value changed. Event Handler.
        /// </summary>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The e.</param>
        private void AutoSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            //  Synchronize member and GUI element values
            Auto_Value = (int)AutoSlider.Value;
            AutoSlider.Value = Auto_Value;
            //  Carry out the slider color code with respect to the new level value
            SolidColorBrush sliderColor = new SolidColorBrush();
            int red = (byte)255 - (byte)Auto_Value;
            int blue = Auto_Value;
            int green = (int)Math.Sqrt(255 * 255 - red * red - blue * blue);
            AutoSlider.Background = sliderColor;
        }

        /// <summary>
        /// Window Close control Event Handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="ExecutedRoutedEventArgs"/> instance containing the event data.</param>
        private void Close_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            System.Windows.Application.Current.Shutdown();
        }

        /// <summary>
        /// Graphic Window size change Event Handler.
        /// It repaint the graphic windows adapting ZedGraph
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="SizeChangedEventArgs"/> instance containing the event data.</param>
        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            Graph.SetSize((int)mainGraphHost.Child.Width, (int)mainGraphHost.Child.Height); //(int)dummy.ActualHeight);
        }

        /// <summary>
        /// PID Automatic Coefficient Update CheckBox Event Handler
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void autoCheckBox_Click(object sender, RoutedEventArgs e)
        {
            isAuto = !(bool)autoCheckBox.IsChecked;
            autoGroupBox.IsEnabled = isAuto;
            PWMGroupBox.IsEnabled = !isAuto;
        }

        /// <summary>
        /// Manual Control Radio Button Click Event Handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void manualRadioButton_Click(object sender, RoutedEventArgs e)
        {
            //  Disable the auto mode flag and arrange GUI element visibility
            isAuto = false;
            autoGroupBox.Visibility = Visibility.Collapsed;
            autoGroupBox.IsEnabled = false;
            PWMGroupBox.Visibility = Visibility.Visible;
            PWMGroupBox.IsEnabled = true;
        }

        /// <summary>
        /// Automatic Control Radio Button Click Event Handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void autoRadioButton_Click(object sender, RoutedEventArgs e)
        {
            //  Enable the auto mode flag and arrange GUI elements.
            isAuto = true;
            PWMGroupBox.Visibility = Visibility.Collapsed;
            PWMGroupBox.IsEnabled = false;
            autoGroupBox.Visibility = Visibility.Visible;
            autoGroupBox.IsEnabled = true;
        }

        /// <summary>
        /// New FW Click event handler.
        /// This function show a new custom window to download a new FW into the microcontroler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void NewFW_Click(object sender, RoutedEventArgs e)
        {
            NewFW_Download_Window wnd = new NewFW_Download_Window(isArduinoBootloader);
            wnd.ShowDialog();
        }

        private void Debug_Click(object sender, RoutedEventArgs e)
        {
            //if (debugWnd == null)
            {
                debugWnd = new DebugWindow(this);
            }
            debugWnd.Show();
        }
        
        private void AVRCOMListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AVRBaudrateListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {

        }

        private void AVRParity_None_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRParity_Even_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRParity_Mark_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRParity_Odd_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRParity_Space_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRStopbitNone_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRStopbit1_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRStopbit15_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRStopbit2_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRHandshake_None_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRHandshake_RTS_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRHandshake_RTSX_Click(object sender, RoutedEventArgs e)
        {

        }

        private void AVRHandshake_XonXoff_Click(object sender, RoutedEventArgs e)
        {

        }


        //private void mainGraphHost_Loaded(object sender, RoutedEventArgs e)
        //{

        //}
        
        #endregion

        #region Methods

        /// <summary>
        /// Generic function to display an Error on a Message Box.
        /// </summary>
        /// <param name="str">The STR.</param>
        /// <param name="ex">The ex.</param>
        private void ErrDlg(string str, Exception ex)
        {
            System.Windows.MessageBox.Show(str + ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Asterisk);
        }

        /// <summary>
        /// Launches the avrdude.
        /// This is need to push the AR bootloader to end astarting the application code
        /// </summary>
        /// <exception cref="System.Exception">AVRDUDE fails, impossible to connect to AVR</exception>
        protected void launchAVRDude()
        {
            //  Setting up the the data structure to launch the external executable
            ProcessStartInfo avrdude = new ProcessStartInfo();

            avrdude.CreateNoWindow = false;
            avrdude.UseShellExecute = false;
            string BootLoader_Protocol;

            //  If the AVR has an Arduino bootloader set the protocol as arduino else as avr109
            if (isArduinoBootloader) BootLoader_Protocol = "arduino";
            else BootLoader_Protocol = "avr109";

            //  Defines the external executable name and process mode
            avrdude.WindowStyle = ProcessWindowStyle.Hidden;
            avrdude.FileName = "avrdude.exe";
            //  Defines the arguments
            avrdude.Arguments = string.Format("-c {0} -p m32u4 -P {1} -U lfuse:r:0xfc:m -U hfuse:r:0xd0:m -U efuse:r:0xf3:m", BootLoader_Protocol, AVRBootLoader_COM);
            //  Launch the executable and wait until has not ended
            try
            {
                using (Process exeProc = Process.Start(avrdude))
                {
                    exeProc.WaitForExit();
                }
            }
            catch
            {
                //  In case of error report it to the user
                throw new Exception("AVRDUDE fails, impossible to connect to AVR");
            }
        }

        #endregion

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {

        }

    }
}
