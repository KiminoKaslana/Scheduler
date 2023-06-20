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
using System.IO.Ports;
using System.Diagnostics;
using System.Timers;

namespace Scheduler
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        List<SerialPort> arduionPortList = new();
        Timer timer = new(1000);

        public MainWindow()
        {
            InitializeComponent();
            arduionPortList = ScanPorts();
            if (arduionPortList == null)
            {
                ShowError();
            }
            timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            if (arduionPortList.Count != 0)
            {
                foreach (var item in arduionPortList)
                {
                    if (GetRawData(item, out string raws))
                    {
                        RawHandler(raws);
                    }
                    item.Write("GetData");
                }
            }
            else
            {
                arduionPortList = ScanPorts();
            }
        }

        /// <summary>
        /// 扫描并返回Arduino所在端口
        /// </summary>
        /// <returns></returns>
        List<SerialPort> ScanPorts()
        {
            string[] allPorts = SerialPort.GetPortNames();
            List<SerialPort> portList = new();

            // 遍历所有串口并尝试连接
            foreach (string port in allPorts)
            {
                try
                {
                    SerialPort serialPort = new SerialPort(port, 9600);
                    serialPort.Open();
                    serialPort.WriteLine("GetArduino"); // 向Arduino发送一条消息
                    string response = serialPort.ReadLine(); // 读取来自Arduino的响应
                    if (response.Contains("Arduino")) // 检查响应是否包含"Arduino"字样
                    {
                        Trace.WriteLine("Arduino已连接到" + port);
                        portList.Add(serialPort);
                    }
                    serialPort.Close();
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
            string raw = port.ReadExisting();
            if (raw.Contains(":"))
            {
                rawString = raw;
            }
            else
            {
                rawString = "";
            }
            return raw != "";
        }

        SensorDatas RawHandler(string rawData)
        {
            SensorDatas sensorDatas = new SensorDatas();
            string[] sDatas = rawData.Split("@");
            foreach (var item in sDatas)
            {
                string[] dataPair = item.Split(':');
                try
                {
                    switch (dataPair[0])
                    {
                        case "Humidity":
                            sensorDatas.Humidity = Convert.ToInt32(dataPair[1]);
                            break;
                        case "Temperature":
                            sensorDatas.Temperature = Convert.ToInt32(dataPair[1]);
                            break;
                        case "AirQuality":
                            sensorDatas.AirQuality = Convert.ToInt32(dataPair[1]);
                            break;
                        default:
                            break;
                    }
                }
                catch (Exception e)
                {
                    Trace.WriteLine(e);
                }

            }
            return sensorDatas;
        }

        void DataHandler(SensorDatas datas)
        {

        }

        void ShowError()
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {

        }
    }
}
