using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;
using System.Diagnostics;
using System.Timers;
using System.IO.Ports;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Threading;

namespace Scheduler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<SerialPort> arduionPortList = new();
        System.Timers.Timer timer = new(1000);

        SynchronizationContext? synchronizationContext = SynchronizationContext.Current;

        Server server;

        int TempExpValue = 26;
        int HumidityExpValue = 50;

        bool ADPower = false;
        bool HDPower = false;
        bool ACPower = false;

        public MainWindow()
        {
            InitializeComponent();
            arduionPortList = ScanPorts();

            timer.Elapsed += Timer_Elapsed;
            timer.AutoReset = true;
            timer.Start();

            server = new(logArea: serverLog);
            server.Start();
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            synchronizationContext?.Post(o =>
            {
                if (arduionPortList.Count != 0)
                {
                    Trace.WriteLine("捕获到arduino");
                    sensorState.Content = "传感器正常";
                    sensorState.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                    foreach (var item in arduionPortList)
                    {
                        try
                        {
                            if (GetRawData(item, out string raws))
                            {
                                RawHandler(raws);
                            }                 
                            
                            item.WriteLine("GetData");//写在后面，是利用刷新间隔来等待数据进入缓冲区
                        }
                        catch (Exception e)
                        {
                            arduionPortList.Remove(item);
                            Trace.TraceError(e.Message);
                            return;
                        }
                    }
                }
                else
                {
                    sensorState.Content = "传感器离线";
                    sensorState.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));

                    if (SensorDatas.instance != null)
                    {
                        SensorDatas.instance.IsDataValid = false;
                    }

                    arduionPortList = ScanPorts();
                }

                if (NetworkInterface.GetIsNetworkAvailable())
                {
                    netState.Content = "网络正常";
                    netState.Foreground = new SolidColorBrush(Color.FromRgb(0, 0, 0));
                }
                else
                {
                    netState.Content = "连接异常";
                    netState.Foreground = new SolidColorBrush(Color.FromRgb(255, 0, 0));
                }
            }, null);
        }

        /// <summary>
        /// 扫描并返回Arduino所在端口
        /// </summary>
        /// <returns></returns>
        List<SerialPort> ScanPorts()
        {
            string[] allPorts = SerialPort.GetPortNames();
            Trace.WriteLine(allPorts.Length);
            List<SerialPort> portList = new();

            // 遍历所有串口并尝试连接
            foreach (string port in allPorts)
            {
                try
                {
                    SerialPort serialPort = new SerialPort(port, 9600);
                    Trace.WriteLine("portName:" + port);
                    serialPort.Open();
                    Trace.WriteLine("端口已打开");
                    serialPort.WriteLine("GetArduino"); // 向Arduino发送一条消息
                    string response = serialPort.ReadLine(); // 读取来自Arduino的响应
                    if (response.Contains("Arduino")) // 检查响应是否包含"Arduino"字样
                    {
                        Trace.WriteLine("Arduino已连接到" + port);
                        portList.Add(serialPort);
                    }
                    else
                    {
                        serialPort.Close();
                    }

                }
                catch (Exception e)
                {
                    // 连接失败，将尝试下一个串口
                    Trace.WriteLine(e);
                }
            }

            return portList;
        }

        bool GetRawData(SerialPort port, out string rawString)
        {
            try
            {
                string raw = port.ReadExisting();
                if (raw.Contains(':'))
                {
                    rawString = raw;
                    return true;
                }
                else
                {
                    rawString = "";
                    return false;
                }
                
            }
            catch (Exception e)
            {
                Trace.TraceError(e.Message);
                rawString = "";
                return false;
            }

        }

        SensorDatas RawHandler(string rawData)
        {
            SensorDatas sensorDatas;
            if (SensorDatas.instance == null)
            {
                sensorDatas = new SensorDatas();
            }
            else
            {
                sensorDatas = SensorDatas.instance;
            }

            string[] sDatas = rawData.Split("@");
            foreach (var item in sDatas)
            {
                string[] dataPair = item.Split(':');
                try
                {
                    switch (dataPair[0])
                    {
                        case "Humidity":
                            sensorDatas.Humidity = Convert.ToDouble(dataPair[1].Trim());
                            break;
                        case "Temperature":
                            sensorDatas.Temperature = Convert.ToDouble(dataPair[1].Trim());
                            break;
                        case "AirQuality":
                            sensorDatas.AirQuality = Convert.ToInt32(dataPair[1].Trim());
                            break;
                        default:
                            break;
                    }

                    SensorDatas.instance = sensorDatas;
                    DataHandler(sensorDatas);
                }
                catch (Exception e)
                {
                    Trace.WriteLine("error data:" + dataPair[1]);
                    Trace.WriteLine(e);
                }

            }
            return sensorDatas;
        }

        void DataHandler(SensorDatas datas)
        {
            tempratureEnv.Content = string.Format("{0}°C", datas.Temperature);
            humidityEnv.Content = string.Format("{0}%", datas.Humidity);
            airQuality.Content = datas.AirQuality;
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            Timer_Elapsed(null, null);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            server.Stop();
            Application.Current.Shutdown();
        }

        private void Temp_lower_Click(object sender, RoutedEventArgs e)
        {
            if (TempExpValue <= 16)
            {
                return;
            }
            TempExpValue--;
            temperatureExpect.Content = string.Format("{0}°C", TempExpValue);
        }

        private void Temp_higher_Click(object sender, RoutedEventArgs e)
        {
            if (TempExpValue >= 30)
            {
                return;
            }
            TempExpValue++;
            temperatureExpect.Content = string.Format("{0}°C", TempExpValue);
        }

        private void airConditionerSwitch_Click(object sender, RoutedEventArgs e)
        {
            ADPower = !ADPower;
            if (!ADPower)
            {
                airConditionerSwitch.Content = "已关机";
            }
            else
            {
                airConditionerSwitch.Content = "正在运行";
            }
        }

        private void humidity_lower_Click(object sender, RoutedEventArgs e)
        {
            if (HumidityExpValue <= 0)
            {
                return;
            }
            HumidityExpValue -= 10;
            humidityExpect.Content = string.Format("{0}%", HumidityExpValue);
        }

        private void humidityControlerSwitch_Click(object sender, RoutedEventArgs e)
        {
            HDPower = !HDPower;
            if (!HDPower)
            {
                humidityControlerSwitch.Content = "已关机";
            }
            else
            {
                humidityControlerSwitch.Content = "正在运行";
            }
        }

        private void humidity_higher_Click(object sender, RoutedEventArgs e)
        {
            if (HumidityExpValue >= 100)
            {
                return;
            }
            HumidityExpValue += 10;
            humidityExpect.Content = string.Format("{0}%", HumidityExpValue);
        }

        private void airCleanerSwitch_Click(object sender, RoutedEventArgs e)
        {
            ACPower = !ACPower;
            if (!ACPower)
            {
                airCleanerSwitch.Content = "已关机";
            }
            else
            {
                airCleanerSwitch.Content = "正在运行";
            }
        }
    }
}
