using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Net;
using System.Net.Sockets;
using GameServer;
using Common;

namespace Network
{
    class NetService
    {
        static TcpSocketListener ServerListener;
        public bool Init(int port)
        {
            ServerListener = new TcpSocketListener("127.0.0.1", GameServer.Properties.Settings.Default.ServerPort, 10);
            ServerListener.SocketConnected += OnSocketConnected;
            return true;
        }
         
        public void Start()
        {
            //启动监听
            Log.Warning("Starting Listener...");
            ServerListener.Start();

            MessageDistributer<NetConnection<NetSession>>.Instance.Start(8);
            Log.Warning("NetService Started");
        }

        public void Stop()
        {
            Log.Warning("Stop NetService...");

            ServerListener.Stop();

            Log.Warning("Stoping Message Handler...");
            MessageDistributer<NetConnection<NetSession>>.Instance.Stop();
        }

        /// <summary>
        /// 当有新客户端连上来
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnSocketConnected(object sender, Socket e)
        {
            IPEndPoint clientIP = (IPEndPoint)e.RemoteEndPoint;
            //可以在这里对IP做一级验证,比如黑名单

            SocketAsyncEventArgs args = new SocketAsyncEventArgs();
            NetSession session = new NetSession();

            NetConnection<NetSession> connection = new NetConnection<NetSession>(e, args,
                new NetConnection<NetSession>.DataReceivedCallback(DataReceived),
                new NetConnection<NetSession>.DisconnectedCallback(Disconnected), session);


            Log.WarningFormat("Client[{0}]] Connected", clientIP);
        }

        /// <summary>
        /// 断开连接时触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void Disconnected(NetConnection<NetSession> sender, SocketAsyncEventArgs e)
        {
            sender.Session.Disconnected();
            Log.WarningFormat("Client[{0}] Disconnected", e.RemoteEndPoint);
        }


        /// <summary>
        /// 收到数据时触发
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        static void DataReceived(NetConnection<NetSession> sender, DataEventArgs e)
        {
            // 由包处理器处理封包
            lock (sender.packageHandler)
            {
                sender.packageHandler.ReceiveData(e.Data, 0, e.Data.Length);
            }
        }
    }
}
