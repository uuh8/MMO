using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Configuration;

using System.Threading;

using Network;
using GameServer.Services;

namespace GameServer
{
    class GameServer
    {
        Thread thread;          //用于控制服务器更新逻辑的后台线程
        bool running = false;   //一个标志位，用于指示服务器是否正在运行。
        NetService network;     //网络服务的实例，用于处理网络连接和数据传输。

        public bool Init()
        {
            network = new NetService();      //实例化网络服务。
            network.Init(8000);              //初始化网络服务并指定端口号 8000。
            DBService.Instance.Init();       //DBService 会初始化数据库连接，用于访问和管理数据库中的数据。
            UserService.Instance.Init();

            thread = new Thread(new ThreadStart(this.Update));

            return true;
        }

        public void Start()
        {
            network.Start();                        //启动网络服务，使服务器开始监听客户端连接。
            running = true;                         //设置 running 标志位为 true，指示服务器正在运行。
            thread.Start();                         //启动服务器更新线程，使 Update 方法在后台持续运行。
        }


        public void Stop()
        {
            running = false;    //设置 running 为 false，指示服务器停止运行。
            thread.Join();      //等待 Update 线程完成，这样可以确保服务器停止前完成所有必要的更新操作。
            network.Stop();     //停止网络服务，不再接受新的客户端连接或处理数据。
        }

        public void Update()
        {
            while (running)             //当 running 为 true 时，服务器进入循环，持续更新。
            {
                Time.Tick();            //调用 Time 的 Tick() 方法，这可能用于更新服务器的计时器、帧数或其他时间相关的逻辑。
                Thread.Sleep(100);      //线程休眠 100 毫秒，以控制循环频率，减轻服务器的负载。
                //Console.WriteLine("{0} {1} {2} {3} {4}", Time.deltaTime, Time.frameCount, Time.ticks, Time.time, Time.realtimeSinceStartup);
            }
        }
    }
}
