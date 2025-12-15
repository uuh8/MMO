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
using GameServer.Managers;

namespace GameServer
{
    class GameServer
    {
        Thread thread;          //用于控制服务器更新逻辑的后台线程
        bool running = false;   //一个标志位，用于指示服务器是否正在运行。
        NetService network;     //网络服务的实例，用于处理网络连接和数据传输。

        public bool Init()
        {

            DBService.Instance.Init();    //DBService 会初始化数据库连接，用于访问和管理数据库中的数据。
            UserService.Instance.Init();
            DataManager.Instance.Load();  // 读配置文件并反序列化为字典缓存 
            MapService.Instance.Init();
            ItemService.Instance.Init();
            QuestService.Instance.Init(); 
            FriendService.Instance.Init(); 

            network = new NetService();   //实例化网络服务。
            network.Init(8000);           //初始化网络服务并指定端口号 8000。

            thread = new Thread(new ThreadStart(this.Update));

            return true;
        }

        public void Start()
        {
            network.Start();  //启动网络服务，使服务器开始监听客户端连接。
            running = true;   //设置 running 标志位为 true，指示服务器正在运行。
            thread.Start();   //启动服务器更新线程，使 Update 方法在后台持续运行。
        }


        public void Stop()
        {
            running = false;    //设置 running 为 false，指示服务器停止运行。
            thread.Join();      //等待 Update 线程完成，这样可以确保服务器停止前完成所有必要的更新操作。
            network.Stop();     //停止网络服务，不再接受新的客户端连接或处理数据。
        }

        public void Update()
        {
            var mapManager = MapManager.Instance;
            while (running)
            {
                Time.Tick(); 
                Thread.Sleep(100); //线程休眠 100 毫秒，以控制循环频率，减轻服务器的负载。模拟100ms/帧
                mapManager.Update();
            }
        }
    }
}
