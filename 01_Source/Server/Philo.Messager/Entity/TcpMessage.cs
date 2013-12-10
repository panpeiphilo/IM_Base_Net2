using System;
using System.Collections.Generic;
using System.Text;

namespace Philo.Messager.Entity
{
    public enum MType
    {
        Login = 1,
        Logout = 2,
        Notice = 3,
        Message = 4,
        Request = 5,
        UserList = 6
    }

    public partial class TcpMessage
    {
        public MType MessageType { get; set; }
        public string SenderId { get; set; }
        public string ReceiverId { get; set; }
        public string Content { get; set; }
    }
}
