using System;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

using System.IO.Ports;
using System.Text.RegularExpressions;
using System.Timers;

namespace SerialAssistant
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        SerialPort serialPort = new SerialPort();
        int receiveCount = 0;
        StringBuilder message = new StringBuilder();
        string currentSendMsgASCII;
        string currentSendMsgHexStr;
        byte[] currentSendMsgHex;

        string portV, baudV, dataBitV, verifyBitV, StopBitV, flowControlV, receiveFormat = "Hex", sendFormat = "Hex";
        int reSendTime = 1000;
        bool autoWrap = true, showSend = true, showTime = true, autoReSend = false;

        Timer mainTimer;
        public MainWindow()
        {
            InitializeComponent();

            initTimer();
            initControls();
            InitData();
        }
        private void initTimer()
        {
            mainTimer = new Timer();
            mainTimer.Elapsed += new ElapsedEventHandler(AutoReSendMsg);
            mainTimer.AutoReset = true;
            mainTimer.Enabled = false;
            mainTimer.Interval = reSendTime;

        }
        private void initControls()
        {
            AutoWrap.IsChecked = true;
            ShowSend.IsChecked = true;
            ShowTime.IsChecked = true;
            AutoReSend.IsChecked = false;
        }

        private void InitData()
        {
            var portNames = SerialPort.GetPortNames();
            if(portNames.Length < 1)
            {
                MessageBox.Show("当前没有可用的端口连接");
                return;
            }
            ComboBoxPort.ItemsSource = portNames;
            ComboBoxPort.SelectedIndex = 0;
            portV = portNames[0];

            string[] baud = { "9600", "19200", "38400", "57600", "115200"};
            ComboBoxBaud.ItemsSource = baud;
            ComboBoxBaud.SelectedIndex = 4;
            baudV = baud[4];

            string[] dbit ={  "5", "6", "7", "8" };
            ComboBoxDataBit.ItemsSource = dbit;
            ComboBoxDataBit.SelectedIndex = 3;
            dataBitV = dbit[3];

            string[] vbit = { "None", "Even", "Odd", "Mark", "Space" };
            ComboBoxVeriBit.ItemsSource = vbit;
            ComboBoxVeriBit.SelectedIndex = 0;
            verifyBitV = vbit[0];

            string[] sbit = { "1", "1.5", "2"};
            ComboBoxStopBit.ItemsSource = sbit;
            ComboBoxStopBit.SelectedIndex = 0;
            StopBitV = sbit[0];

            string[] fc = { "None", "RTS/CTS", "XON/XOFF" };
            ComboBoxFlowControl.ItemsSource = fc;
            ComboBoxFlowControl.SelectedIndex = 0;
            flowControlV = fc[0];

        }
        private void ConnectSerial_Click(object sender, RoutedEventArgs e)
        {
            Button btn = sender as Button;
            try
            {
                if (serialPort.IsOpen)
                {
                    serialPort.Close();
                    btn.Content = "打开串口";
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Green"));
                    SerialStatus.Text = "Closed";
                    message.Clear();
                    MessageReceive.Text = "";
                }
                else
                {
                    serialPort.PortName = ComboBoxPort.Text;
                    serialPort.BaudRate = Convert.ToInt32(ComboBoxBaud.Text);
                    serialPort.DataBits = Convert.ToInt32(ComboBoxDataBit.Text);
                    Parity parity = Parity.None;
                    switch (ComboBoxVeriBit.Text)
                    {
                        case "None":
                            parity = Parity.None;
                            break;
                        case "Odd":
                            parity = Parity.Odd;
                            break;
                        case "Even":
                            parity = Parity.Even;
                            break;
                        case "Space":
                            parity = Parity.Space;
                            break;
                        case "Mark":
                            parity = Parity.Mark;
                            break;
                    }
                    serialPort.Parity = parity;
                    StopBits stopBits = StopBits.One;
                    switch (ComboBoxStopBit.Text)
                    {
                        case "1":
                            stopBits = StopBits.One;
                            break;
                        case "1.5":
                            stopBits = StopBits.OnePointFive;
                            break;
                        case "2":
                            stopBits = StopBits.Two;
                            break;
                    }
                    serialPort.StopBits = stopBits;

                    serialPort.DataReceived += new SerialDataReceivedEventHandler(SerialDataReceive);

                    serialPort.Open();
                    btn.Content = "关闭串口";
                    btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Red"));
                    SerialStatus.Text = $"Connected, {serialPort.PortName}, {serialPort.BaudRate}, {serialPort.DataBits}, {serialPort.Parity}, {serialPort.StopBits}";

                }
            }catch(Exception ex)
            {
                serialPort = new SerialPort();
                btn.Content = "打开串口";
                btn.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("Green"));
                SerialStatus.Text = "CLOSED";
                message.Clear();
                MessageReceive.Text = "";
            }
        }

        private void SerialDataReceive(object sender, SerialDataReceivedEventArgs e)
        {
            int num = serialPort.BytesToRead;
            byte[] buffer = new byte[num];
            receiveCount += num;
            serialPort.Read(buffer, 0, num);
            ReceiveLength.Text = num.ToString();

            string msg = "";
            if (showTime == true)
            {
                msg += $"[{DateTime.Now.ToString("HH:mm:ss.fff")}]  ";
            }

            if (receiveFormat.Equals("Hex"))
            {
                foreach (byte b in buffer)
                {
                    msg += $"{b.ToString("X2")} ";

                }
            }else if (receiveFormat.Equals("ASCII"))
            {
                msg += $"{Encoding.ASCII.GetString(buffer)} ";
            }

            message.Append(msg);
            if(autoWrap == true)
            {
                message.Append("\r\n");
            }
            
            try
            {
                this.Dispatcher.Invoke(new Action(() =>
                {
                    MessageReceive.Text = message.ToString();
                }));
            }catch(Exception ex)
            {

            }
        }

        private void SendMsg_Click(object sender, RoutedEventArgs e)
        {
            byte[] data;
            if (serialPort.IsOpen)
            {
                string msg = MessageSend.Text.ToString();
                if (!string.IsNullOrWhiteSpace(msg))
                {
                    try
                    {
                        if (sendFormat.Equals("ASCII"))
                        {
                            serialPort.Write(msg);
                            HandleSendFormat(msg, "ASCII");

                            currentSendMsgASCII = msg;
                            if (ComboBoxSendRecord.Items.Count == 0 || !ComboBoxSendRecord.Items.Contains(msg))
                            {
                                ComboBoxSendRecord.Items.Insert(0, msg);
                                ComboBoxSendRecord.SelectedIndex = 0;
                            }
                            SendLength.Text = Encoding.Default.GetBytes(msg).Length.ToString();
                        }
                        else if (sendFormat.Equals("Hex"))
                        {
                            string pattern = @"\s";
                            string relacement = "";
                            Regex rgx = new Regex(pattern);
                            string msg1 = rgx.Replace(msg, relacement);
                            int len = Convert.ToInt32(Math.Ceiling((double)msg1.Length / 2));
                            data = new byte[len];

                            for (int i = 0; i < data.Length; i++)
                            {
                                int s = 0;
                                try
                                {
                                    s = Convert.ToInt32(msg1.Substring(i * 2, 2), 16);
                                }catch(Exception ex)
                                {
                                    s = Convert.ToInt32(msg1.Substring(i * 2, 1) + "0", 16);
                                }
                                data[i] = Convert.ToByte(s);
                            }
                            serialPort.Write(data, 0, data.Length);
                            HandleSendFormat(msg1, "Hex");

                            currentSendMsgHexStr = msg1;
                            currentSendMsgHex = data;
                            if (ComboBoxSendRecord.Items.Count == 0 || !ComboBoxSendRecord.Items.Contains(msg1))
                            {
                                ComboBoxSendRecord.Items.Insert(0, msg1);
                                ComboBoxSendRecord.SelectedIndex = 0;
                            }
                            SendLength.Text = data.Length.ToString();
                        }

                        if(autoReSend == true)
                        {
                            if (reSendTime > 0)
                            {
                                mainTimer.Interval = reSendTime;
                                mainTimer.Start();
                            }
                            else
                            {
                                MessageBox.Show("自动重发时间需大于0ms");
                            }
                        }
                        

                    }
                    catch(Exception ex)
                    {
                        MessageBox.Show(ex.Message);
                    }
                }
            }
            else
            {
                MessageBox.Show("请先打开串口");
            }
        }
        private void HandleSendFormat(string msg, string format)
        {
            if (showSend == true)
            {
                if (showTime == true)
                {
                    message.Append($"[{DateTime.Now.ToString("HH:mm:ss.fff")}]  ");
                }
                if (format.Equals("Hex")){
                    for (int i = 2; i < msg.Length; i += 2 + 1)
                    {
                        msg = msg.Insert(i, " ");
                    }
                }
                message.Append(msg);
                if (autoWrap == true)
                {
                    message.Append("\r\n");
                }
                try
                {
                    this.Dispatcher.Invoke(new Action(() =>
                    {
                        MessageReceive.Text = message.ToString();
                    }));
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void AutoReSendMsg(object source, System.Timers.ElapsedEventArgs e)
        {
            if (serialPort.IsOpen)
            {
                try
                {
                    if (sendFormat.Equals("ASCII"))
                    {
                        serialPort.Write(currentSendMsgASCII);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            SendLength.Text = Encoding.Default.GetBytes(currentSendMsgASCII).Length.ToString();
                        }));

                        HandleSendFormat(currentSendMsgASCII, "ASCII");

                    }
                    else if (sendFormat.Equals("Hex"))
                    {
                        serialPort.Write(currentSendMsgHex, 0, currentSendMsgHex.Length);
                        Dispatcher.Invoke(new Action(() =>
                        {
                            SendLength.Text = currentSendMsgHex.Length.ToString();
                        }));

                        string msg = currentSendMsgHexStr;
                        HandleSendFormat(msg, "Hex");
                    }
                }
                catch (Exception ex)
                {

                }
            }
            else
            {
                MessageBox.Show("请先打开串口");
            }
        }


        private void RadioButton_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            string s = rb.Content.ToString();
            if (s.Equals("ASCII"))
            {
                receiveFormat = s;
            }else if (s.Equals("Hex"))
            {
                receiveFormat = s;
            }
        }

        private void RadioButton1_Checked(object sender, RoutedEventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            string s = rb.Content.ToString();
            if (s.Equals("ASCII"))
            {
                sendFormat = s;
            }
            else if (s.Equals("Hex"))
            {
                sendFormat = s;
            }
        }

        private void AutoWrap_Checked(object sender, RoutedEventArgs e)
        {
            autoWrap = true;
        }

        private void ShowSend_Checked(object sender, RoutedEventArgs e)
        {
            showSend = true;
        }

        private void ShowTime_Checked(object sender, RoutedEventArgs e)
        {
            showTime = true;
        }

        private void AutoReSend_Checked(object sender, RoutedEventArgs e)
        {
            autoReSend = true;
        }

        private void AutoWrap_Unchecked(object sender, RoutedEventArgs e)
        {
            autoWrap = false;
        }

        private void ShowSend_Unchecked(object sender, RoutedEventArgs e)
        {
            showSend = false;
        }

        private void ShowTime_Unchecked(object sender, RoutedEventArgs e)
        {
            showTime = false;
        }

        private void AutoReSend_Unchecked(object sender, RoutedEventArgs e)
        {
            autoReSend = false;
            mainTimer.Stop();
        }

        private void ComboBoxSendRecord_SelectionChanged(object sender, RoutedEventArgs e)
        {

            MessageSend.Text = ComboBoxSendRecord.SelectedItem.ToString();
        }

        private void ReSendTime_TextChanged(object sender, TextChangedEventArgs e)
        {
            TextBox tb = sender as TextBox;
            string temp = tb.Text.ToString();
            reSendTime = Convert.ToInt32(temp);
        }

        private void MessageSend_GotFocus(object sender, RoutedEventArgs e)
        {
            if(mainTimer.Enabled == true)
            {
                mainTimer.Stop();
            }
        }

    }
}
