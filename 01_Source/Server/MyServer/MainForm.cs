using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using Philo.Messager.Service;
using Philo.Messager.Entity;

namespace MyServer
{
    public partial class MainForm : Form
    {
        Socket serverSocket;//服务器Socket
        Thread listenConnectThread;//监听连接线程
        bool iClosed = true;//关闭状态
        Dictionary<string, Socket> clientList = new Dictionary<string, Socket>();//客户端列表
        Dictionary<string, Thread> listenReceiveList = new Dictionary<string, Thread>();//监听接收消息列表
        DataTable dtUserList;
        public MainForm()
        {
            InitializeComponent();
        }

        //开启服务器
        private void StartServer()
        {
            serverSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            IPEndPoint bindIp = new IPEndPoint(IPAddress.Any, 39999);
            serverSocket.Bind(bindIp);
            serverSocket.Listen(2000);
            iClosed = true;
            listenConnectThread = new Thread(StartListenConnect);
            listenConnectThread.Start();
        }

        //开始监听连接
        private void StartListenConnect()
        {
            while (iClosed)
            {
                try
                {
                    Socket clientSocket = serverSocket.Accept();
                    byte[] receiveBytes = new byte[1024 * 1024 * 2];
                    int receiveLength = clientSocket.Receive(receiveBytes);
                    TcpMessage tmessage = TcpMessager.ConvertToTcpMessage(receiveBytes);
                    DeliveryMessage(tmessage, clientSocket);
                }
                catch (Exception ex)
                {
                    iClosed = true;
                    throw;
                }
            }
        }

        //开始接受消息
        private void StartListenReceive(object clientid)
        {
            while (iClosed)
            {
                Socket clientSocket = (Socket)clientList[clientid.ToString()];
                try
                {
                    byte[] receiveBytes = new byte[1024 * 1024 * 2];
                    int receiveLength = clientSocket.Receive(receiveBytes);
                    TcpMessage tmessage = TcpMessager.ConvertToTcpMessage(receiveBytes);
                    DeliveryMessage(tmessage, clientSocket);
                }
                catch (Exception ex)
                {
                    DeliveryMessage(new TcpMessage { SenderId = clientid.ToString(),MessageType = MType.Logout }, clientSocket);
                    MessageBox.Show(clientid.ToString() + "失去了服务器连接！");
                }
            }
        }

        //处理消息
        private void DeliveryMessage(TcpMessage message, Socket clientSocket)
        {
            switch (message.MessageType)
            {
                case MType.Login:
                    clientList.Add(message.SenderId, clientSocket);
                    Thread receiceThread = new Thread(StartListenReceive);
                    receiceThread.Start(message.SenderId);
                    listenReceiveList.Add(message.SenderId, receiceThread);                    
                    AddUserToUserTable(message.SenderId);
                    SendUserList(message,clientSocket);
                    SendAllUserLogInOrOutMessage(message);//通知所有人登陆消息
                    RefreshUserList();
                    MessageBox.Show(message.SenderId + "已登录！");
                    break;
                case MType.Logout:
                    CloseClient(message.SenderId);
                    SendAllUserLogInOrOutMessage(message);//通知所有人退出消息
                    RemoveUserFormUserTable(message.SenderId);
                    RefreshUserList();
                    MessageBox.Show(message.SenderId + "已退出！");
                    break;
                case MType.Notice:
                    break;
                case MType.Message:
                    TcpMessager.SendTcpMessage((Socket)clientList[message.ReceiverId], message);
                    break;
                case MType.Request:
                    break;
                default:
                    break;
            }
        }

        //给所有人发送有人登陆的消息
        private void SendAllUserLogInOrOutMessage(TcpMessage message)
        {
            foreach (var item in clientList)
            {
                TcpMessager.SendTcpMessage((Socket)item.Value, message);
            }
        }

        //给客户端发送用户列表
        private void SendUserList(TcpMessage message, Socket clientSocket)
        {
            string userstr = "";
            for (int i = 0; i < dtUserList.Rows.Count; i++)
            {
                userstr += dtUserList.Rows[i]["用户编码"].ToString() + ",";
            }
            TcpMessager.SendTcpMessage(clientSocket, new TcpMessage { SenderId = "0", ReceiverId = message.SenderId, MessageType = MType.UserList, Content = userstr });
        }

        //关闭某个客户端
        private void CloseClient(string clientid)
        {
            Socket socket = (Socket)clientList[clientid];
            if (socket != null)
            {
                socket.Close();
                clientList.Remove(clientid);
            }
            Thread thread = (Thread)listenReceiveList[clientid];
            if (thread != null)
            {
                thread.Abort();
                listenReceiveList.Remove(clientid);
            }
        }
        //关闭监听
        private void StopListenConnect()
        {
            if (serverSocket != null)
            {
                serverSocket.Close();
            }
            if (listenConnectThread != null)
            {
                listenConnectThread.Abort();
            }
            foreach (var item in listenReceiveList)
            {
                ((Thread)item.Value).Abort();
            }
            listenReceiveList.Clear();
            foreach (var item in clientList)
            {
                ((Socket)item.Value).Close();
            }
            clientList.Clear();
        }

        //初始化按钮状态
        private void MainForm_Load(object sender, EventArgs e)
        {
            tsmiStart.Enabled = iClosed;
            tsmiStop.Enabled = !iClosed;
        }

        //点击开始
        private void tsmiStart_Click(object sender, EventArgs e)
        {
            if (iClosed)
            {
                StartServer();
                tsmiStart.Enabled = !iClosed;
                tsmiStop.Enabled = iClosed;
                iClosed = false;
            }
        }

        //点击结束
        private void tsmiStop_Click(object sender, EventArgs e)
        {
            if (!iClosed)
            {
                StopListenConnect();
                tsmiStart.Enabled = iClosed;
                tsmiStop.Enabled = !iClosed;
                iClosed = true;
            }
        }

        //初始化用户列表
        private DataTable InitUserTable()
        {
            DataTable dt = new DataTable();
            DataColumn dc1 = new DataColumn("用户编码");
            dt.Columns.Add(dc1);
            return dt;
        }

        private void AddUserToUserTable(string uid)
        {
            DataRow dr = dtUserList.NewRow();
            dr["用户编码"] = uid;
            dtUserList.Rows.Add(uid);
        }

        private void RemoveUserFormUserTable(string uid)
        {
            for (int i = 0; i < dtUserList.Rows.Count; i++)
            {
                if (dtUserList.Rows[i]["用户编码"].ToString().Trim().Equals(uid))
                {
                    dtUserList.Rows.RemoveAt(i);
                    break;
                }
            }
        }

        private void RefreshUserList()
        {
            lbUserList.DataSource = dtUserList;
            lbUserList.Refresh();
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopListenConnect();
            Application.Exit();
        }

    }
}
