using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;

namespace XGview
{
    /// <summary>
    /// Interaktionslogik für SerialWindow.xaml
    /// </summary>
    public partial class SerialWindow : Window
    {
        bool? verbose = true;
        static SerialPort _serialPort;

        public SerialWindow()
        {
            InitializeComponent();
        }

        private void SerialWindow1_Loaded(object sender, RoutedEventArgs e)
        {
            foreach (string s in SerialPort.GetPortNames())
            {
                SerialPortList.Items.Add(s);
            }
            // Set port to user defined comport
            SerialPortList.Items.Insert(0, Properties.Settings.Default.comport);
            SerialPortList.SelectedIndex = 0;

            // Set baudrate to user defined baudrate
            BaudrateList.Items.Insert(0, Properties.Settings.Default.baudrate);
            BaudrateList.SelectedIndex = 0;

            DispatcherTimer dispatcherTimer = new DispatcherTimer();
            dispatcherTimer.Tick += new EventHandler(timer1_Tick);
            dispatcherTimer.Interval = TimeSpan.FromSeconds(0.2);
            dispatcherTimer.Start();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            verbose = VisibleDebug.IsChecked;
            // tick
            if (_serialPort != null && _serialPort.IsOpen)
            {
                _serialPort.Write("?"); //ask for status
            }
        }


        private void BaudrateList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (BaudrateList.Text != "")
            {
                Properties.Settings.Default.baudrate = Convert.ToInt32(BaudrateList.Text);
                Properties.Settings.Default.Save();
            }
        }

        private void SerialPortList_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (SerialPortList.Text != "")
            {
                Properties.Settings.Default.comport = SerialPortList.Text;
                Properties.Settings.Default.Save();
            }
        }

        private void ButtonConnect_Click(object sender, RoutedEventArgs e)
        {
            if (_serialPort != null && _serialPort.IsOpen)
            {
                ButtonConnect.Content = "Connect";
                _serialPort.Close(); //Serialport schliessen
                System.Threading.Thread.Sleep(1000);
                return;
            }
            else
            {
                ButtonConnect.Content = "Disconnect";

                // create serial port
                _serialPort = new SerialPort();
                _serialPort.PortName = SerialPortList.Text; //Com Port Name                
                _serialPort.BaudRate = Convert.ToInt32(BaudrateList.Text); //COM Port Sp
                _serialPort.Handshake = System.IO.Ports.Handshake.None;
                _serialPort.Parity = Parity.None;
                _serialPort.DataBits = 8;
                _serialPort.StopBits = StopBits.One;
            }


            // call connect
            if (!_serialPort.IsOpen)
            {
                try
                {
                    _serialPort.Open(); //Serialport öffnen
                }
                catch
                {
                    MessageBox.Show("Can't connect to comport! Please check connection between PC and GRBL!");
                    return;
                }
                this.IsEnabled = true;
            }
            _serialPort.DataReceived += new SerialDataReceivedEventHandler(serialPort_DataReceived); //DataRecieved Event abonnieren
        }

        string RecievedLine = " ";
        void serialPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            RecievedLine = " ";
            while (RecievedLine != "")
            {
                try
                {
                    RecievedLine = _serialPort.ReadLine();
                }
                catch
                {
                    return;
                }
                // Parse text and display info in statusbar
                if (RecievedLine.StartsWith("<"))
                {
                    _displayPositions(RecievedLine);
                }
                if (verbose == false && RecievedLine.StartsWith("<"))
                {
                    return;
                }
                if (RecievedLine.StartsWith("ok") || RecievedLine.StartsWith("error"))
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        var mainWin = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
                        mainWin.sendNextGcodeLine();
                    }));
                }
                // write text in console
                OnUIThread(() => textboxConsole.AppendText(RecievedLine));
                OnUIThread(() => textboxConsole.ScrollToEnd());
            }
        }

        private void OnUIThread(Action action)
        {
            if (Dispatcher.CheckAccess())
            {
                action();
            }
            else
            {
                // if you don't want to block the current thread while action is
                // executed, you can also call Dispatcher.BeginInvoke(action);
                Dispatcher.Invoke(action);
            }
        }

        void _displayPositions(string line)
        {

            // <Idle,MPos:0.000,0.000,0.000,WPos:0.000,0.000,0.000>
            string[] positions = line.Split(new Char[] { '<', '>', ',', ':', '\r', '\n' });
            if (positions.Length < 6)
            {
                return;
            }
            string status = positions[1];
            float xpos = Convert.ToSingle(positions[3], System.Globalization.CultureInfo.InvariantCulture);
            float ypos = Convert.ToSingle(positions[4], System.Globalization.CultureInfo.InvariantCulture);
            float zpos = Convert.ToSingle(positions[5], System.Globalization.CultureInfo.InvariantCulture);
            float wxpos = Convert.ToSingle(positions[7], System.Globalization.CultureInfo.InvariantCulture);
            float wypos = Convert.ToSingle(positions[8], System.Globalization.CultureInfo.InvariantCulture);
            float wzpos = Convert.ToSingle(positions[9], System.Globalization.CultureInfo.InvariantCulture);

            // Display Info: Idle X:0.000 Y:0.000 Z:0.000 in Statusbar
            Dispatcher.Invoke(new Action(() =>
            {
                var mainWin = Application.Current.Windows.Cast<Window>().FirstOrDefault(window => window is MainWindow) as MainWindow;
                mainWin.labelXpos.Text = xpos.ToString("0.000");
                mainWin.labelYpos.Text = ypos.ToString("0.000");
                mainWin.labelZpos.Text = zpos.ToString("0.000");

                mainWin.MoveTool(new System.Windows.Media.Media3D.Point3D(xpos, ypos, zpos), 1);
            }));


        }

        public void _sendCommand(string command)
        {
            textboxConsole.AppendText(">> " + command + "\r");
            try
            {
                _serialPort.WriteLine(command);
            }
            catch
            {
                MessageBox.Show("Can't connect to comport! Please check connection between PC and GRBL!");
                return;
            }
        }

        private void CommandsTextbox_KeyUp(object sender, KeyEventArgs e)
        {
            if (e.Key != Key.Enter)
                return;
            _sendCommand(CommandsTextbox.Text);
            CommandsTextbox.Text = "";
        }

        private void SerialWindow1_Closed(object sender, EventArgs e)
        {
            try
            {
                _serialPort.Close();
            }
            catch
            {
                return;
            }
        }

    }
}
