using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class login : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Network.NetClient.Instance.Init("127.0.0.1", 8000);                         //初始化
        Network.NetClient.Instance.Connect();                                       //链接 

        SkillBridge.Message.NetMessage msg = new SkillBridge.Message.NetMessage();  //创建主消息
        msg.Request = new SkillBridge.Message.NetMessageRequest();
        msg.Request.firstRequest = new SkillBridge.Message.FirstTestRequest();      //创建我们自己定义的消息
        msg.Request.firstRequest.HelloWorld = "hello world!";                       //给自己定义的消息填充数据

        Network.NetClient.Instance.SendMessage(msg);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
