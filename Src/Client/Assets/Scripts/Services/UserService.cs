using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Network;
using UnityEngine;

using SkillBridge.Message;

namespace Services
{
    class UserService : Singleton<UserService>, IDisposable
    {
        public UnityEngine.Events.UnityAction<Result, string> OnRegister;   //通过回调 OnRegister 通知 UI 层
        public UnityEngine.Events.UnityAction<Result, string> OnLogin;      //通过回调 OnLogin 通知 UI 层

        NetMessage pendingMessage = null;      //网络消息的缓存变量，用来保存尚未发送的消息（例如当网络断开时）。
        bool connected = false;

        public UserService()//构造函数
        {
            NetClient.Instance.OnConnect += OnGameServerConnect;                     //当客户端连接到服务器时触发。
            NetClient.Instance.OnDisconnect += OnGameServerDisconnect;               //当客户端与服务器断开时触发。
            MessageDistributer.Instance.Subscribe<UserRegisterResponse>(this.OnUserRegister); //监听来自服务器的 UserRegisterResponse 消息。当服务器返回用户注册结果时，触发 OnUserRegister 方法。
            MessageDistributer.Instance.Subscribe<UserLoginResponse>(this.OnUserLogin);                                                                                   
        }



        public void Dispose()//资源释放，解除订阅的事件和消息，防止内存泄漏或对象被销毁后仍然调用事件逻辑。
        {
            MessageDistributer.Instance.Unsubscribe<UserRegisterResponse>(this.OnUserRegister);
            MessageDistributer.Instance.Unsubscribe<UserLoginResponse>(this.OnUserLogin);

            NetClient.Instance.OnConnect -= OnGameServerConnect;
            NetClient.Instance.OnDisconnect -= OnGameServerDisconnect;
        }

        public void Init()
        {

        }

        public void ConnectToServer()   //连接服务器
        {
            Debug.Log("ConnectToServer() Start ");
            //NetClient.Instance.CryptKey = this.SessionId;
            NetClient.Instance.Init("127.0.0.1", 8000);
            NetClient.Instance.Connect();
        }


        void OnGameServerConnect(int result, string reason) //连接回调
        {
            Log.InfoFormat("LoadingMesager::OnGameServerConnect :{0} reason:{1}", result, reason);

            if (NetClient.Instance.Connected)
            {
                this.connected = true;
                if(this.pendingMessage!=null)                               //如果有未发送的消息（如注册请求），在连接成功后立即发送。
                {
                    NetClient.Instance.SendMessage(this.pendingMessage);
                    this.pendingMessage = null;
                }
            }
            else
            {
                if (!this.DisconnectNotify(result, reason))
                {
                    MessageBox.Show(string.Format("网络错误，无法连接到服务器！\n RESULT:{0} ERROR:{1}", result, reason), "错误", MessageBoxType.Error);
                }
            }
        }

        public void OnGameServerDisconnect(int result, string reason)   //断开连接
        {
            this.DisconnectNotify(result, reason);
            return;
        }
        bool DisconnectNotify(int result,string reason) //断开连接处理
        {
            if (this.pendingMessage != null)
            {
                if (this.pendingMessage.Request.userRegister!=null)
                {
                    if (this.OnRegister != null)
                    {
                        this.OnRegister(Result.Failed, string.Format("服务器断开！\n RESULT:{0} ERROR:{1}", result, reason));
                    }
                }
                return true;
            }
            return false;
        }

        /*
        主要功能：
            构建用户注册请求消息。
            如果网络已连接，直接发送消息。
            如果网络未连接，缓存消息并重新连接服务器。
        消息结构：
            NetMessage 是一个通用消息对象，包含请求和响应字段。
            UserRegisterRequest 包含注册所需的用户名和密码。*/
        public void  SendRegister(string user, string psw)   //发送注册请求
        {
            Debug.LogFormat("UserRegisterRequest::user :{0} psw:{1}", user, psw);

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.userRegister = new UserRegisterRequest();

            message.Request.userRegister.User = user;
            message.Request.userRegister.Passward = psw;

            if (this.connected && NetClient.Instance.Connected)
            {
                this.pendingMessage = null;
                NetClient.Instance.SendMessage(message);
            }
            else
            {
                this.pendingMessage = message;
                this.ConnectToServer();
            }
        }
/*      功能：
          处理服务器返回的 UserRegisterResponse
          调用 OnRegister 回调，通知 UI 层注册是否成功。
        响应内容：
          response.Result：注册结果（成功或失败）。
          response.Errormsg：错误消息。*/
        void OnUserRegister(object sender, UserRegisterResponse response)   //处理注册响应
        {
            Debug.LogFormat("OnUserRegister:{0} [{1}]", response.Result, response.Errormsg);

            if (this.OnRegister != null)
            {
                this.OnRegister(response.Result, response.Errormsg);
            }
        }

        public void SendLogin(string user, string psw) //发送登录请求
        {
            Debug.LogFormat("UserLoginRequest::user :{0} psw:{1}", user, psw);

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.userLogin = new UserLoginRequest();

            message.Request.userLogin.User = user;
            message.Request.userLogin.Password = psw;

            if (this.connected && NetClient.Instance.Connected)
            {
                this.pendingMessage = null;
                NetClient.Instance.SendMessage(message);
            }
            else
            {
                this.pendingMessage = message;
                this.ConnectToServer();
            }
        }


        /* 功能：
          处理服务器返回的 UserLoginResponse
          调用 OnLogin 回调，通知 UI 层注册是否成功。
        响应内容：
          response.Result：注册结果（成功或失败）。
          response.Errormsg：错误消息。*/
        void OnUserLogin(object sender, UserLoginResponse response)
        {
            // 验证消息是否为 null
            if (response == null)
            {
                Debug.LogError("OnUserLogin: Received null message object!");
                return; // 直接退出，避免后续代码执行导致崩溃
            }

            Debug.LogFormat("OnLogin:{0} [{1}]", response.Result, response.Errormsg);

            if (response.Result == Result.Success)
            {//登陆成功逻辑
                Models.User.Instance.SetupUserInfo(response.Userinfo); 
            };
            //调用回调，通知UI层
            if (this.OnLogin != null)  
            {
                this.OnLogin(response.Result, response.Errormsg);

            }
        }
    }
}
