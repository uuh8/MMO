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

        Network.NetClient.Instance.SendMessage(msg);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
