using Common;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    class HelloWorldService: Singleton<HelloWorldService>
    {
        public void init()
        {

        }

        public void start()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<FirstTestRequest>(onFirstTestRequest);
        }

        void onFirstTestRequest(NetConnection<NetSession> sender, FirstTestRequest request)
        {
            Log.InfoFormat("onFirstTestRequest:第一条信息：{0}", request.HelloWorld);
        }

        public void stop()
        {

        }
    }
}
