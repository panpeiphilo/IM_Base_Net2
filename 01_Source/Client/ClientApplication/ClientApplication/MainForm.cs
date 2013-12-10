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

namespace ClientApplication
{
    public partial class MainForm : Form
    {
        bool iClosed = true;//连接是否关闭
        DataTable dtUserList;
        
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

        private void RefreshUserList()
        {
            lbUserList.DataSource = dtUserList;
            lbUserList.Refresh();
        }
    }
}
