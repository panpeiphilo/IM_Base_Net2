using System;
using System.Collections.Generic;
using System.Text;
using Philo.Messager.Entity;
using System.Net.Sockets;

namespace Philo.Messager.Service
{
    public class TcpMessager
    {
        public static TcpMessage ConvertToTcpMessage(byte[] byteMessage)
        {
            if (byteMessage.Length >= 26)
            {
                TcpMessage tmEntity = new TcpMessage();
                //判断消息类型
                switch (byteMessage[0])
                {
                    case 1:
                        tmEntity.MessageType = MType.Login;
                        break;
                    case 2:
                        tmEntity.MessageType = MType.Logout;
                        break;
                    case 3:
                        tmEntity.MessageType = MType.Notice;
                        break;
                    case 4:
                        tmEntity.MessageType = MType.Message;
                        break;
                    case 5:
                        tmEntity.MessageType = MType.Request;
                        break;
                    case 6:
                        tmEntity.MessageType = MType.UserList;
                        break;     
                    default:
                        break;
                }

                int senderIdLength = byteMessage[1];
                int receiverIdLength = byteMessage[2];
                int contentLength = byteMessage[3];
                int used = 10;                
                //发送者
                tmEntity.SenderId = Encoding.UTF8.GetString(byteMessage, used, senderIdLength);
                used += senderIdLength;
                //接收者
                tmEntity.ReceiverId = Encoding.UTF8.GetString(byteMessage, used, receiverIdLength);
                used += receiverIdLength;
                //内容
                tmEntity.Content = Encoding.UTF8.GetString(byteMessage, used, contentLength);
                return tmEntity;
            }
            else
            {
                return null;
            }
        }

        public static byte[] ConvertTcpMessageToBytes(TcpMessage message)
        {
            if (message != null)
            {
                //获取各部分长度
                int contentLength = Encoding.UTF8.GetByteCount(message.Content.Trim());
                int senderIdLength = Encoding.UTF8.GetByteCount(message.SenderId.Trim());
                int receiverIdLength = Encoding.UTF8.GetByteCount(message.ReceiverId.Trim());
                //初始化数组
                int totalLength = 10 + contentLength + senderIdLength + receiverIdLength;
                if (totalLength <= int.MaxValue)
                {
                    byte[] messagebytes = new byte[totalLength];
                    //设置各部分长度值
                    messagebytes[0] = Convert.ToByte(message.MessageType);
                    messagebytes[1] = (byte)senderIdLength;
                    messagebytes[2] = (byte)receiverIdLength;
                    messagebytes[3] = (byte)contentLength;
                    //设置各部分内容
                    int used = 10;
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(message.SenderId), 0, messagebytes, used, senderIdLength);
                    used += senderIdLength;
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(message.ReceiverId), 0, messagebytes, used, receiverIdLength);
                    used += receiverIdLength;
                    Buffer.BlockCopy(Encoding.UTF8.GetBytes(message.Content), 0, messagebytes, used, contentLength);
                    return messagebytes;
                }
            }
            return null;
        }

        public static int SendTcpMessage(Socket socket, TcpMessage message)
        {
            if (socket.Connected)
            {
                int i = socket.Send(ConvertTcpMessageToBytes(message));
                return i;
            }
            else
            {
                return 0;
            }
        }
    }
}
