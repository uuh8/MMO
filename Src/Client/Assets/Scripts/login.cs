using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class login : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        Network.NetClient.Instance.Init("127.0.0.1", 8000);                         //��ʼ��
        Network.NetClient.Instance.Connect();                                       //���� 

        SkillBridge.Message.NetMessage msg = new SkillBridge.Message.NetMessage();  //��������Ϣ
        msg.Request = new SkillBridge.Message.NetMessageRequest();
        msg.Request.firstRequest = new SkillBridge.Message.FirstTestRequest();      //���������Լ��������Ϣ
        msg.Request.firstRequest.HelloWorld = "hello world!";                       //���Լ��������Ϣ�������

        Network.NetClient.Instance.SendMessage(msg);
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
