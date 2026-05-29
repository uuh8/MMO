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
        // ────────────────────────────────────────────────────────
        // 常量配置区
        // ────────────────────────────────────────────────────────

        const int DEF_POLL_INTERVAL_MILLISECONDS = 100;
        // 轮询间隔，当前架构里没有后台线程，这个常量实际未用于控制帧率
        // 实际收发频率 = Unity 的帧率（每帧 Update 一次）

        const int DEF_TRY_CONNECT_TIMES = 3;
        // 连接失败后最多重试 3 次，超过就上报 NET_ERROR_FAIL_TO_CONNECT 并停止重连

        const int DEF_RECV_BUFFER_SIZE = 64 * 1024;
        // 64KB 接收缓冲区：每帧最多从网卡读取 64KB 字节
        // 注意：这只是"每次 Receive() 的读取上限"，不是"消息的最大尺寸"
        // PackageHandler 内部还有自己的积累缓冲区用于跨帧拼包

        const int DEF_PACKAGE_HEADER_LENGTH = 4;
        // 每个消息包头 = 4字节，存储消息体的字节长度（大端序整数）
        // 格式：[0x00, 0x00, 0x00, 0xC8] = 后面有 200 字节的 Protobuf 内容
        // PackageHandler 靠这 4 字节判断一个完整包是否已经收齐

        const int NetConnectTimeout = 10000;
        // 建立连接的超时时间 = 10 秒
        // 超时后不再等待，进入重连逻辑

        // ────────────────────────────────────────────────────────
        // 错误码定义
        // ────────────────────────────────────────────────────────
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



        private IPEndPoint address;     //存储服务器的IP地址和端口号，类型是 IPEndPoint（一个包含IP地址和端口的结构）。

        // ────────────────────────────────────────────────────────
        // 核心字段：三块缓冲区 + 一个 Socket
        // ────────────────────────────────────────────────────────
        private Socket clientSocket;    //用于与服务器建立连接的 Socket 对象。

        //-----------------发送相关
        private Queue<NetMessage> sendQueue = new Queue<NetMessage>();
        // 【第一层缓冲】逻辑发送队列，存储原始消息对象
        //
        // 职责分工：
        //   - 写入者：业务层调 SendMessage() → 压入队列
        //   - 读取者：ProcessSend() 每帧 Peek() 取出 → 序列化 → 放入 sendBuffer
        //   - 移除者：ProcessSend() 确认字节全部发完后 Dequeue()
        //
        // 关键：必须等 sendBuffer 里的字节全部发完，才从队列 Dequeue()
        // ⚠️ 已知问题：Queue<T> 非线程安全
        //    当前单线程架构下无问题，但若引入后台网络线程需换 ConcurrentQueue<T>
        private MemoryStream sendBuffer = new MemoryStream();
        // 【第二层缓冲】字节发送缓冲区，存储已序列化的字节流
        //
        // 内容格式：[4字节长度头][Protobuf消息体] 拼成的完整字节数组
        // Socket.Send() 直接从这里读字节推给网卡
        //
        // ⚠️ 已知 GC 问题：MemoryStream 超出容量时会在堆上重新分配数组
        //    生产级方案应使用固定大小 byte[] + ArrayPool<byte> 池化管理
        private int sendOffset = 0;
        // 追踪 sendBuffer 中已经成功发出的字节数（位置指针）
        //
        // 存在原因：Socket.Send() 的返回值 n 不保证 == 你想发的字节数！
        // 如果此刻网卡的内核发送缓冲区空间不足，Send() 只写入一部分
        // sendOffset 记录"已经成功写入 Socket 的字节位置"，下帧从这里继续
        //
        // 完整时序举例（发一条 300 字节的消息）：
        //   第1帧：Send(buffer, 0, 300) 返回 200，sendOffset = 200
        //   第2帧：Send(buffer, 200, 100) 返回 100，sendOffset = 300
        //   300 >= sendBuffer.Position → 全部发完 → Dequeue()

        //-----------------接收相关
        private MemoryStream receiveBuffer = new MemoryStream(DEF_RECV_BUFFER_SIZE);
        // 接收方向的64KB临时字节缓冲区（避免GC Alloc)
        // 每帧 Receive() 把网卡数据搬运到这里，立刻交给 PackageHandler
        // 这个缓冲区本身不做积累，积累拼包的工作在 PackageHandler 内部完成

        private bool connecting = false;

        private int retryTimes = 0;                             //当前连接失败后的重试次数。
        private int retryTimesTotal = DEF_TRY_CONNECT_TIMES;    //最大重试次数，默认为 DEF_TRY_CONNECT_TIMES（3次）。

        private float lastSendTime = 0;                         //上次发送数据的时间，单位通常是秒，用于控制发送频率。

        public PackageHandler packageHandler = new PackageHandler(null);
        // 共享库提供的拆包处理器，职责：
        // 1. 内部维护跨帧积累缓冲区，解决 TCP 粘包/拆包问题
        // 2. 每次收到字节后检查：前4字节长度 vs 已积累字节数
        //      凑齐 → 切出完整包 → Protobuf 反序列化 → 压入 MessageDistributer 队列
        //      未凑齐 → 等下帧继续积累
        // 3. 一帧内可能切出多个完整包（多条消息的字节同时到达时）


        public bool running { get; set; }


        protected override void OnStart()
        {
            running = true;
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

        // ────────────────────────────────────────────────────────
        // 关闭连接：CloseConnection()
        // 统一的断线处理入口，主动/被动断线都走这里
        // ────────────────────────────────────────────────────────
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

        // ────────────────────────────────────────────────────────
        // 业务层入口：SendMessage()
        // 只做一件事：把消息放入队列，立刻返回
        // 实际序列化和发送由 ProcessSend() 在下帧完成
        // ────────────────────────────────────────────────────────
        public void SendMessage(NetMessage message)
        {
            if (!running) return;

            if (!this.Connected)
            {
                // 断连时触发重连，本次消息丢弃（不加入队列）
                // ⚠️ 已知问题：重连成功后，这条消息已经丢失，不会重发
                //    生产级方案：重连成功后重发队列，或有重试机制
                this.Connect();
                return;
            }

            // 把消息放入发送队列，立刻返回
            // 发送开销（序列化、系统调用）全部推迟到下帧 ProcessSend()
            // 业务层调用成本极低，不受当前网络状态（如缓冲区满）的影响
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


        // 检测连接状态，断了就自动重连（最多3次）
        bool KeepConnect()
        {
            if (this.connecting) return false;  // 正在连接中，等待结果，不重复发起
            if (this.address == null) return false;
            if (this.Connected) return true;     // 已连接，正常路径

            // 未连接状态：还有重试次数就重连
            if (this.retryTimes < this.retryTimesTotal)
            {
                this.Connect();
            }
            // 超过重试次数 → 停止重连，等待上层（如弹出"连接失败"提示框）处理
            return false;
        }

        // ────────────────────────────────────────────────────────
        // 接收流程：ProcessRecv()
        // ────────────────────────────────────────────────────────
        bool ProcessRecv()
        {
            try
            {
                // 第一道防线：检测 Socket 错误状态（网络异常、连接被强制重置等）
                bool error = this.clientSocket.Poll(0, SelectMode.SelectError);
                if (error)
                {
                    this.CloseConnection(NET_ERROR_SEND_EXCEPTION);
                    return false;
                }

                // 第二道：零超时探测是否有数据可读
                //
                // Poll(超时时间, 模式) 的语义：
                //   超时时间 = 0  → 立刻返回当前状态，不等待
                //   SelectRead    → "网卡缓冲区有数据可读吗？"
                //
                // 返回 true  → 有数据，此时调 Receive() 一定不会阻塞
                // 返回 false → 没数据，直接跳过，本帧不读，主线程不卡
                bool hasData = this.clientSocket.Poll(0, SelectMode.SelectRead);
                if (hasData)
                {
                    // 从网卡缓冲区最多读 64KB 字节
                    // 为什么不直接读"一条消息"的大小？
                    // 因为 TCP 是流式协议，不知道"一条消息"从哪到哪
                    // 只能先读出来，交给 PackageHandler 去判断完整性
                    int n = this.clientSocket.Receive(
                        this.receiveBuffer.GetBuffer(),
                        0,
                        this.receiveBuffer.Capacity,
                        SocketFlags.None
                    );

                    if (n <= 0)
                    {
                        // n == 0 是 TCP 协议级别的"对端关闭"信号
                        // 当对方调用 socket.Close() 或程序退出时
                        // TCP 协议栈会发送一个 FIN 包，接收方的 Receive() 返回 0
                        // 这不是错误，是正常的连接关闭流程，但我们必须响应
                        this.CloseConnection(NET_ERROR_ZERO_BYTE);
                        return false;
                    }

                    // 把 n 个字节交给 PackageHandler
                    // PackageHandler 在内部做三件事：
                    //   1. 把字节追加到积累缓冲区（解决跨帧拼包问题）
                    //   2. 检查前4字节长度头，判断是否凑够了完整包
                    //   3. 凑够了就 Protobuf 反序列化，压入 MessageDistributer 队列
                    //      然后继续检查下一条（一帧可能同时凑够多条消息）
                    this.packageHandler.ReceiveData(this.receiveBuffer.GetBuffer(), 0, n);
                }
            }
            catch (Exception e)
            {
                Debug.Log("ProcessReceive exception:" + e.ToString());
                this.CloseConnection(NET_ERROR_ILLEGAL_PACKAGE);
                return false;
            }
            return true;
        }

        // ────────────────────────────────────────────────────────
        // 发送流程：ProcessSend()
        //
        // 内部是一个简单的两状态机：
        //
        //   状态 B（填充）：sendBuffer 空 → 从 sendQueue 取消息 → 序列化 → 填入 sendBuffer
        //         ↓（填好了）
        //   状态 A（发送）：sendBuffer 有数据 → Socket.Send() → 推给网卡
        //         ↓（发完了）
        //   回到状态 B
        //
        // 为什么分两个状态？
        // 因为 Socket.Send() 可能一帧内发不完（网卡缓冲区不足）
        // 必须跨帧持续推送，直到字节全部写入 Socket
        // ────────────────────────────────────────────────────────
        bool ProcessSend()
        {
            try
            {
                bool error = this.clientSocket.Poll(0, SelectMode.SelectError);
                if (error)
                {
                    this.CloseConnection(NET_ERROR_SEND_EXCEPTION);
                    return false;
                }

                // 零超时探测“网卡发送缓冲区”是否有空间
                //   SelectWrite：  "现在可以写数据进去吗？"
                //   返回 true  →  有空间，Send() 不会阻塞
                //   返回 false →  缓冲区满（可能对方读取太慢或网络拥塞），跳过
                bool canWrite = this.clientSocket.Poll(0, SelectMode.SelectWrite);
                if (canWrite)
                {
                    // ── 状态 A：sendBuffer 有未发完的字节，继续发 ──
                    if (this.sendBuffer.Position > this.sendOffset)
                    {
                        int remaining = (int)(this.sendBuffer.Position - this.sendOffset);

                        // ⚠️ Send() 的返回值 n 是"实际写入网卡缓冲区的字节数"
                        //    不保证 n == remaining！
                        //    网卡内核缓冲区空间不足时，n < remaining，只写了一部分
                        int n = this.clientSocket.Send(
                            this.sendBuffer.GetBuffer(),
                            this.sendOffset,    // 从上次停下的位置继续发
                            remaining,          // 还剩多少没发
                            SocketFlags.None
                        );

                        if (n <= 0)
                        {
                            this.CloseConnection(NET_ERROR_ZERO_BYTE);
                            return false;
                        }

                        this.sendOffset += n;   // 推进已发送位置

                        // 判断这条消息的所有字节是否全部推入了网卡缓冲区
                        if (this.sendOffset >= this.sendBuffer.Position)
                        {
                            // 全部发完，重置缓冲区，准备接收下一条消息的字节
                            this.sendOffset = 0;
                            this.sendBuffer.Position = 0;

                            // ★ 关键：只有此时才 Dequeue 移除消息
                            //
                            // 为什么不在取出消息时就 Dequeue？
                            // 取出消息 → 序列化 → 写入 sendBuffer 这一步完成后
                            // 消息只是"准备好了等发送"，字节还没真正离开本机
                            // 如果提前 Dequeue，中途发送失败，这条消息就永久消失了
                            // 用 Peek() + 延迟 Dequeue 保证消息不丢
                            this.sendQueue.Dequeue();

                            // 发完之后，下帧会进入状态 B，取下一条消息继续
                        }
                        // else：只发了一部分，sendOffset 记录了进度
                        //       下帧 canWrite=true 时继续从 sendOffset 位置发剩余部分
                    }
                    // ── 状态 B：sendBuffer 空了，去队列取下一条消息 ──
                    else
                    {
                        if (this.sendQueue.Count > 0)
                        {
                            // Peek()：取出队首消息，但不从队列移除
                            // 原因见上面"关键"注释
                            NetMessage message = this.sendQueue.Peek();

                            // PackageHandler.PackMessage() 做两件事：
                            //   1. Protobuf 序列化：NetMessage → byte[]
                            //   2. 加4字节长度头：[length(4B)][protobuf body(NB)]
                            //
                            // ⚠️ 已知 GC 问题：每次 new byte[] 分配新数组
                            //    生产级方案：
                            //    byte[] buf = ArrayPool<byte>.Shared.Rent(estimatedSize);
                            //    序列化写入 buf，Send 后 ArrayPool<byte>.Shared.Return(buf)
                            byte[] package = PackageHandler.PackMessage(message);

                            // 写入 sendBuffer，下帧状态 A 来发
                            this.sendBuffer.Write(package, 0, package.Length);
                        }
                        // sendQueue 为空 → 没有待发消息，什么都不做，等业务层 SendMessage()
                    }
                }
            }
            catch (Exception e)
            {
                Debug.Log("ProcessSend exception:" + e.ToString());
                this.CloseConnection(NET_ERROR_SEND_EXCEPTION);
                return false;
            }
            return true;
        }

        void ProceeMessage()
        {
            // Distribute() 做的事：
            //   从 MessageDistributer 内部队列取出本帧所有消息
            //   → 通过运行时反射拿到消息类型名作为 Key
            //   → 查 messageHandlers 字典找到订阅者委托
            //   → 依次触发回调（如 UserService.OnLoginResponse、MapService.OnEntitySync）
            //
            // 为什么不在 PackageHandler 解析完就立刻触发回调？
            //   → 解耦：PackageHandler 只管字节，不应该知道任何业务逻辑
            //   → 时序确定：所有业务回调集中在每帧同一时机执行，逻辑可预测
            //   → 线程安全：全程主线程，可以直接调任何 Unity API（Transform、UI等）
            MessageDistributer.Instance.Distribute();
        }

        public void Update()
        {
            if (!running) return;

            if (this.KeepConnect())         // ① 确保连接存活（断了就自动重连）
            {
                // 这里的逻辑含义不是"先收到数据才能发送"，而是"如果接收过程中发现网络已经出问题了，就不要再做后续操作了"，所以这个嵌套结构的语义是：接收正常 → 说明连接还活着 → 才有必要继续发送和处理消息。它是一种防御性的短路逻辑，而不是"发送依赖接收"的数据依赖关系。
                if (this.ProcessRecv())     // ② 接收：网卡字节 → PackageHandler → 消息队列
                {
                    if (this.Connected)
                    {
                        this.ProcessSend();     // ③ 发送：消息队列 → 序列化 → 网卡
                        this.ProceeMessage();   // ④ 消费：消息队列 → 路由 → 业务 Service
                    }
                }
            }
        }
    }
}
