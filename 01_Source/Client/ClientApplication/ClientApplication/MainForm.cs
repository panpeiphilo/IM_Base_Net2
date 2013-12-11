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
using Philo.Messager.Entity;
using Philo.Messager.Service;

namespace ClientApplication
{
    public partial class MainForm : Form
    {
        bool iClosed = true;//连接是否关闭
        DataTable dtUserList;
        Socket clientSocket;
        Thread receiveThread;
        public MainForm()
        {
            InitializeComponent();
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
        private delegate void RefreshLB();
        private void RefreshUserList()
        {
            
            dgvUserList.DataSource = dtUserList;
            dgvUserList.Refresh();
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            btnStart.Enabled = iClosed;
            btnStop.Enabled = !iClosed;
            btnSend.Enabled = !iClosed;
            dtUserList = InitUserTable();
        }

        private void ConnectServer()
        {
            string uid = txtUserId.Text;
            if (!string.IsNullOrEmpty(uid))
            {
                clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                IPEndPoint ipend = new IPEndPoint(IPAddress.Parse("192.168.0.136"), 39999);
                clientSocket.Connect(ipend);
                Philo.Messager.Entity.TcpMessage msgEntity = new Philo.Messager.Entity.TcpMessage { 
                    SenderId = uid,
                    ReceiverId = "",
                    MessageType = MType.Login,
                    Content = ""
                };
                int i = TcpMessager.SendTcpMessage(clientSocket, msgEntity);
                receiveThread = new Thread(StartListenReceive);
                receiveThread.Start(clientSocket);
                txtUserId.ReadOnly = true;

                iClosed = false;
                btnStart.Enabled = iClosed;
                btnStop.Enabled = !iClosed;
                btnSend.Enabled = !iClosed;
            }
            else
            {
                MessageBox.Show("用户名不能为空！");
            }
        }

        private void CloseClient()
        {
              string uid = txtUserId.Text;
              if (!string.IsNullOrEmpty(uid))
              {
                  Philo.Messager.Entity.TcpMessage msgEntity = new Philo.Messager.Entity.TcpMessage
                  {
                      SenderId = uid,
                      ReceiverId = "",
                      MessageType = MType.Logout,
                      Content = ""
                  };
                  int i = TcpMessager.SendTcpMessage(clientSocket, msgEntity);
                 
                  if (clientSocket != null)
                  {
                      clientSocket.Close();
                  }
                  if (receiveThread != null)
                  {
                      receiveThread.Abort();
                  }
                  iClosed = true;
                  btnStart.Enabled = iClosed;
                  btnStop.Enabled = !iClosed;
                  btnSend.Enabled = !iClosed;
              }
              else
              { 
              
              }
            
        }

        //开始接受消息
        private void StartListenReceive(object client)
        {
            while (!iClosed)
            {
                Socket clientSocket = (Socket)client;
                try
                {
                    byte[] receiveBytes = new byte[1024 * 1024 * 2];
                    int receiveLength = clientSocket.Receive(receiveBytes);
                    TcpMessage tmessage = TcpMessager.ConvertToTcpMessage(receiveBytes);
                    DeliveryMessage(tmessage, clientSocket); 
                }
                catch (Exception ex)
                {
                    MessageBox.Show("失去了服务器连接！");
                }
            }
        }

        //处理消息
        private void DeliveryMessage(TcpMessage message, Socket clientSocket)
        {
            switch (message.MessageType)
            {
                case MType.Login:
                    AddUserToUserTable(message.SenderId);
                    break;
                case MType.Logout:
                    RemoveUserFormUserTable(message.SenderId);
                    break;
                case MType.Notice:
                    MessageBox.Show(message.Content);
                    break;
                case MType.Message:
                    txtMessage.Text = message.Content + "\n\r" + txtMessage.Text;
                    break;
                case MType.Request:
                    break;
                case MType.UserList:
                    string[] uids = message.Content.Split(',');
                    for (int i = 0; i < uids.Length; i++)
                    {
                        if (!string.IsNullOrEmpty(uids[i]))
                        {
                            AddUserToUserTable(uids[i]);
                        }
                    }                   
                    break;
                default:
                    break;
            }
            dgvUserList.Invoke(new RefreshLB(RefreshUserList));
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            ConnectServer();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            CloseClient();
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            DataGridViewRow drv = dgvUserList.CurrentRow;
            if (drv!= null)
            {
                DataRow dr = (dgvUserList.SelectedRows[dgvUserList.CurrentRow.Index].DataBoundItem as DataRowView).Row;
                string rid = dr[0].ToString();
                if (!string.IsNullOrEmpty(rid))
                {
                    TcpMessage tcmessage = new TcpMessage
                    {
                        SenderId = txtUserId.Text.Trim(),
                        ReceiverId = rid,
                        MessageType = MType.Message,
                        Content = txtSendMessage.Text.Trim()
                    };
                    TcpMessager.SendTcpMessage(clientSocket, tcmessage);
                }
            }
            else
            {
                MessageBox.Show("未选择发送对象！");
            }
        }

    }
}
