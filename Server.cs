using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Controls;

namespace Scheduler
{
    class Server
    {
        Socket tcpSocket;
        TextBox? LogArea;
        bool isRunning = false;
        SynchronizationContext? context = SynchronizationContext.Current;

        Thread netThread;

        public Server(string ips = "0.0.0.0", int port = 6000, TextBox? logArea = null)
        {
            LogArea = logArea;

            //创建一个socket对象
            tcpSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            //绑定监听消息IP和端口号
            IPAddress ip = IPAddress.Parse(ips);
            EndPoint endPoint = new IPEndPoint(ip, port);
            tcpSocket.Bind(endPoint);
        }


        public void Start()
        {
            isRunning = true;

            Thread thread = new Thread(AcceptionLoop);
            netThread = thread;
            thread.Start();

            LogLine("Server已启动");
        }

        public void Stop()
        {
            isRunning = false;
            tcpSocket.Close();
        }

        void LogLine(string formatLog, params object[] args)
        {
            context?.Send(o =>
            {
                LogArea?.AppendText("[" + DateTime.Now.ToString() + "] " + string.Format(formatLog, args) + '\n');
                LogArea?.ScrollToEnd();
            }, null);

        }

        void AcceptionLoop()
        {
            tcpSocket.Listen(100);
            while (isRunning)//开始监听客户端的连接请求
            {
                try
                {
                    Socket socket = tcpSocket.Accept();
                    DateTime date = DateTime.Now;
                    LogLine("接受客户端连接，时间戳{0}", date);

                    //接收客户端的消息
                    SocketAsyncEventArgs e = new SocketAsyncEventArgs();

                    e.UserToken = socket;
                    e.SetBuffer(new byte[1024], 0, 1024);
                    e.Completed += ReceiveData;

                    socket.ReceiveAsync(e);
                }
                catch (Exception ee)
                {
                    isRunning = false;
                    return;
                }
            }
        }

        private void ReceiveData(object? sender, SocketAsyncEventArgs e)
        {
            try
            {
                if (e.BytesTransferred == 0)
                {
                    (sender as Socket)?.Dispose();
                    LogLine("一个连接断开");
                    return;
                }

                if (e.SocketError == SocketError.Success)
                {
                    string message = Encoding.UTF8.GetString(e.Buffer, 0, e.BytesTransferred);

                    switch (message)
                    {
                        case "GetData":
                            SendData(sender as Socket, SensorDatas.instance);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    LogLine("一个连接异常");
                    (sender as Socket)?.Dispose();
                }
            }
            catch (Exception ee)
            {
                LogLine(ee.Message);
            }

        }

        void SendData(Socket? socket, SensorDatas? data)
        {
            if (data != null && socket != null)
            {
                string jsonS = JsonSerializer.Serialize(data);
                SendCallback(socket, jsonS);
            }
            else
            {
                LogLine("无法回应客户端请求，因为传感器数据为空");
            }
        }

        void SendCallback(Socket socket, string message)
        {
            SocketAsyncEventArgs e = new();
            try
            {
                e.SetBuffer(Encoding.UTF8.GetBytes(message));

                e.Completed += OnSendCompleted;

                if (!socket.SendAsync(e))
                {
                    OnSendCompleted(socket, e);
                }
            }
            catch (Exception ee)
            {
                LogLine("回调消息发送异常：{0}，断开连接", ee.Message);
                socket.Close();
            }
        }

        private void OnSendCompleted(object? sender, SocketAsyncEventArgs e)
        {
            if (e.SocketError == SocketError.Success)
            {
                LogLine("回调成功");
            }
            else
            {
                LogLine("回调发送失败");
            }
        }
    }
}
