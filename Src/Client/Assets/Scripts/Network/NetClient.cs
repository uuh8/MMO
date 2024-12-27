using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using UnityEngine;
using SkillBridge.Message;

namespace Network
{
    class NetClient : MonoSingleton<NetClient>
    {

        const int DEF_POLL_INTERVAL_MILLISECONDS = 100; //网络线程的轮询间隔时间（毫秒），即每次网络线程检查网络状态之间的等待时间。设置为100毫秒。
        const int DEF_TRY_CONNECT_TIMES = 3;            //连接失败后，尝试重新连接的次数。这里设置为3次。
        const int DEF_RECV_BUFFER_SIZE = 64 * 1024;     //接收数据时的缓冲区大小，单位是字节（bytes）。设置为64KB，意味着在接收数据时，客户端会预先分配64KB的内存来存储接收到的数据。
        const int DEF_PACKAGE_HEADER_LENGTH = 4;        //定义了数据包头的大小，单位是字节。这里设置为4字节，通常是为了存储数据包的长度、类型等信息。
        const int DEF_SEND_PING_INTERVAL = 30;          //定义了每隔多少秒发送一个 ping 包的时间间隔，用于检查服务器是否在线。设置为30秒。
        const int NetConnectTimeout = 10000;    //定义了连接超时的时间，单位是毫秒（ms）。这里是10秒（10000毫秒），即客户端在连接过程中，等待服务器响应的最大时间。
        const int DEF_LOAD_WHEEL_MILLISECONDS = 1000;   //定义了客户端在加载过程中，等待多少毫秒后显示加载动画。设置为1000毫秒（1秒）。
        const int NetReconnectPeriod = 10;              //定义了每次尝试重新连接的间隔时间，单位是秒。设置为10秒。

        public const int NET_ERROR_UNKNOW_PROTOCOL = 2;         //协议错误
        public const int NET_ERROR_SEND_EXCEPTION = 1000;       //发送异常
        public const int NET_ERROR_ILLEGAL_PACKAGE = 1001;      //接受到错误数据包
        public const int NET_ERROR_ZERO_BYTE = 1002;            //收发0字节
        public const int NET_ERROR_PACKAGE_TIMEOUT = 1003;      //收包超时
        public const int NET_ERROR_PROXY_TIMEOUT = 1004;        //proxy超时
        public const int NET_ERROR_FAIL_TO_CONNECT = 1005;      //3次连接不上
        public const int NET_ERROR_PROXY_ERROR = 1006;          //proxy重启
        public const int NET_ERROR_ON_DESTROY = 1007;           //结束的时候，关闭网络连接
        public const int NET_ERROR_ON_KICKOUT = 25;             //被踢了

        public delegate void ConnectEventHandler(int result, string reason);    //用于连接事件的委托，传递连接的结果和原因。
        public delegate void ExpectPackageEventHandler();                       //用于处理数据包超时的事件委托。

        public event ConnectEventHandler OnConnect;                     //连接成功时触发的事件。
        public event ConnectEventHandler OnDisconnect;                  //断开连接时触发的事件。
        public event ExpectPackageEventHandler OnExpectPackageTimeout;  //期望的数据包超时事件。
        public event ExpectPackageEventHandler OnExpectPackageResume;   //恢复期望数据包的事件。

        //socket instance
        private IPEndPoint address;     //存储服务器的IP地址和端口号，类型是 IPEndPoint（一个包含IP地址和端口的结构）。
        private Socket clientSocket;    //用于与服务器建立连接的 Socket 对象。
        private MemoryStream sendBuffer = new MemoryStream();                           //存储待发送数据的内存流，用于缓存发送数据。
        private MemoryStream receiveBuffer = new MemoryStream(DEF_RECV_BUFFER_SIZE);    //存储接收数据的内存流，大小是 DEF_RECV_BUFFER_SIZE，也就是64KB。
        private Queue<NetMessage> sendQueue = new Queue<NetMessage>();                  //存储待发送的网络消息队列，确保消息按顺序发送。

        private bool connecting = false;

        private int retryTimes = 0;                             //当前连接失败后的重试次数。
        private int retryTimesTotal = DEF_TRY_CONNECT_TIMES;    //最大重试次数，默认为 DEF_TRY_CONNECT_TIMES（3次）。
        private float lastSendTime = 0;                         //上次发送数据的时间，单位通常是秒，用于控制发送频率。
        private int sendOffset = 0;                             //发送数据的偏移量，通常用于分包发送数据时记录当前发送的字节位置。

        public bool running { get; set; }

        public PackageHandler packageHandler = new PackageHandler(null);

        void Awake()
        {
            running = true;
        }

        protected override void OnStart()
        {
            MessageDistributer.Instance.ThrowException = true;
        }

        protected virtual void RaiseConnected(int result, string reason)
        {
            ConnectEventHandler handler = OnConnect;
            if (handler != null)
            {
                handler(result, reason);
            }
        }

        public virtual void RaiseDisonnected(int result, string reason = "")
        {
            ConnectEventHandler handler = OnDisconnect;
            if (handler != null)
            {
                handler(result, reason);
            }
        }

        protected virtual void RaiseExpectPackageTimeout()
        {
            ExpectPackageEventHandler handler = OnExpectPackageTimeout;
            if (handler != null)
            {
                handler();
            }
        }
        protected virtual void RaiseExpectPackageResume()
        {
            ExpectPackageEventHandler handler = OnExpectPackageResume;
            if (handler != null)
            {
                handler();
            }
        }

        public bool Connected
        {
            get
            {
                return (clientSocket != default(Socket)) ? clientSocket.Connected : false;
            }
        }

        public NetClient()
        {
        }

        public void Reset()
        {
            MessageDistributer.Instance.Clear();
            this.sendQueue.Clear();

            this.sendOffset = 0;

            this.connecting = false;

            this.retryTimes = 0;
            this.lastSendTime = 0;

            this.OnConnect = null;
            this.OnDisconnect = null;
            this.OnExpectPackageTimeout = null;
            this.OnExpectPackageResume = null;
        }

        public void Init(string serverIP, int port)
        {
            this.address = new IPEndPoint(IPAddress.Parse(serverIP), port);
        }

        /// <summary>
        /// Connect
        /// asynchronous connect.
        /// Please use OnConnect handle connect event 
        /// </summary>
        /// <param name="retryTimes"></param>
        /// <returns></returns>
        public void Connect(int times = DEF_TRY_CONNECT_TIMES)
        {
            if (this.connecting)
            {
                return;
            }

            if (this.clientSocket != null)
            {
                this.clientSocket.Close();
            }
            if (this.address == default(IPEndPoint))
            {
                throw new Exception("Please Init first.");
            }
            Debug.Log("DoConnect");
            this.connecting = true;
            this.lastSendTime = 0;

            this.DoConnect();
        }

        public void OnDestroy()
        {
            Debug.Log("OnDestroy NetworkManager.");
            this.CloseConnection(NET_ERROR_ON_DESTROY);
        }

        public void CloseConnection(int errCode)
        {
            Debug.LogWarning("CloseConnection(), errorCode: " + errCode.ToString());
            this.connecting = false;
            if (this.clientSocket != null)
            {
                this.clientSocket.Close();
            }

            //清空缓冲区
            MessageDistributer.Instance.Clear();
            this.sendQueue.Clear();

            this.receiveBuffer.Position = 0;
            this.sendBuffer.Position = sendOffset = 0;

            switch (errCode)
            {
                case NET_ERROR_UNKNOW_PROTOCOL:
                    {
                        //致命错误，停止网络服务
                        this.running = false;
                    }
                    break;
                case NET_ERROR_FAIL_TO_CONNECT:
                case NET_ERROR_PROXY_TIMEOUT:
                case NET_ERROR_PROXY_ERROR:
                    //NetworkManager.Instance.dropCurMessage();
                    //NetworkManager.Instance.Connect();
                    break;
                //离线处理
                case NET_ERROR_ON_KICKOUT:
                case NET_ERROR_ZERO_BYTE:
                case NET_ERROR_ILLEGAL_PACKAGE:
                case NET_ERROR_SEND_EXCEPTION:
                case NET_ERROR_PACKAGE_TIMEOUT:
                default:
                    this.lastSendTime = 0;
                    this.RaiseDisonnected(errCode);
                    break;
            }

        }

        //send a Protobuf message
        public void SendMessage(NetMessage message)
        {
            if (!running)
            {
                return;
            }

            if (!this.Connected)
            {
                this.receiveBuffer.Position = 0;
                this.sendBuffer.Position = sendOffset = 0;

                this.Connect();
                Debug.Log("Connect Server before Send Message!");
                return;
            }

            sendQueue.Enqueue(message);
        
            if (this.lastSendTime == 0)
            {
                this.lastSendTime = Time.time;
            }
        }

        void DoConnect()
        {
            Debug.Log("NetClient.DoConnect on " + this.address.ToString());
            try
            {
                if (this.clientSocket != null)
                {
                    this.clientSocket.Close();
                }


                this.clientSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                this.clientSocket.Blocking = true;

                Debug.Log(string.Format("Connect[{0}] to server {1}", this.retryTimes, this.address) + "\n");
                IAsyncResult result = this.clientSocket.BeginConnect(this.address, null, null);
                bool success = result.AsyncWaitHandle.WaitOne(NetConnectTimeout);
                if (success)
                {
                    this.clientSocket.EndConnect(result);
                }
            }
            catch(SocketException ex)
            {
                if(ex.SocketErrorCode == SocketError.ConnectionRefused)
                {
                    this.CloseConnection(NET_ERROR_FAIL_TO_CONNECT);
                }
                Debug.LogErrorFormat("DoConnect SocketException:[{0},{1},{2}]{3} ", ex.ErrorCode,ex.SocketErrorCode,ex.NativeErrorCode, ex.ToString()); 
            }
            catch (Exception e)
            {
                Debug.Log("DoConnect Exception:" + e.ToString() + "\n");
            }

            if (this.clientSocket.Connected)
            {
                this.clientSocket.Blocking = false;
                this.RaiseConnected(0, "Success");
            }
            else
            {
                this.retryTimes++;
                if (this.retryTimes >= this.retryTimesTotal)
                {
                    this.RaiseConnected(1, "Cannot connect to server");
                }
            }
            this.connecting = false;
        }

        bool KeepConnect()
        {
            if (this.connecting)
            {
                return false;
            }
            if (this.address == null)
                return false;

            if (this.Connected)
            {
                return true;
            }

            if (this.retryTimes < this.retryTimesTotal)
            {
                this.Connect();
            }
            return false;
        }

        bool ProcessRecv()
        {
            bool ret = false;
            try
            {
                if (this.clientSocket.Blocking)
                {
                    Debug.Log("this.clientSocket.Blocking = true\n");
                }
                bool error = this.clientSocket.Poll(0, SelectMode.SelectError);
                if (error)
                {
                    Debug.Log("ProcessRecv Poll SelectError\n");
                    this.CloseConnection(NET_ERROR_SEND_EXCEPTION);
                    return false;
                }

                ret = this.clientSocket.Poll(0, SelectMode.SelectRead);
                if (ret)
                {
                    int n = this.clientSocket.Receive(this.receiveBuffer.GetBuffer(), 0, this.receiveBuffer.Capacity, SocketFlags.None);
                    if (n <= 0)
                    {
                        this.CloseConnection(NET_ERROR_ZERO_BYTE);
                        return false;
                    }

                    this.packageHandler.ReceiveData(this.receiveBuffer.GetBuffer(), 0, n);

                }
            }
            catch (Exception e)
            {
                Debug.Log("ProcessReceive exception:" + e.ToString() + "\n");
                this.CloseConnection(NET_ERROR_ILLEGAL_PACKAGE);
                return false;
            }
            return true;
        }

        bool ProcessSend()
        {
            bool ret = false;
            try
            {
                if (this.clientSocket.Blocking)
                {
                    Debug.Log("this.clientSocket.Blocking = true\n");
                }
                bool error = this.clientSocket.Poll(0, SelectMode.SelectError);
                if (error)
                {
                    Debug.Log("ProcessSend Poll SelectError\n");
                    this.CloseConnection(NET_ERROR_SEND_EXCEPTION);
                    return false;
                }
                ret = this.clientSocket.Poll(0, SelectMode.SelectWrite);
                if (ret)
                {
                    //sendStream exist data
                    if (this.sendBuffer.Position > this.sendOffset)
                    {
                        int bufsize = (int)(this.sendBuffer.Position - this.sendOffset);
                        int n = this.clientSocket.Send(this.sendBuffer.GetBuffer(), this.sendOffset, bufsize, SocketFlags.None);
                        if (n <= 0)
                        {
                            this.CloseConnection(NET_ERROR_ZERO_BYTE);
                            return false;
                        }
                        this.sendOffset += n;
                        if (this.sendOffset >= this.sendBuffer.Position)
                        {
                            this.sendOffset = 0;
                            this.sendBuffer.Position = 0;
                            this.sendQueue.Dequeue();//remove message when send complete
                        }
                    }
                    else
                    {
                        //fetch package from sendQueue
                        if (this.sendQueue.Count > 0)
                        {
                            NetMessage message = this.sendQueue.Peek();
                            byte[] package = PackageHandler.PackMessage(message);
                            this.sendBuffer.Write(package, 0, package.Length);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("ProcessSend exception:" + e.ToString() + "\n");
                this.CloseConnection(NET_ERROR_SEND_EXCEPTION);
                return false;
            }

            return true;
        }

        void ProceeMessage()
        {
            MessageDistributer.Instance.Distribute();
        }

        //Update need called once per frame
        public void Update()
        {
            if (!running)
            {
                return;
            }

            if (this.KeepConnect())
            {
                if (this.ProcessRecv())
                {
                    if (this.Connected)
                    {
                        this.ProcessSend();
                        this.ProceeMessage();
                    }
                }
            }
        }
    }
}
