using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Network;
using UnityEngine;

using SkillBridge.Message;
using Models;
using static UnityEditor.PlayerSettings;

namespace Services
{
    class UserService : Singleton<UserService>, IDisposable
    {
        public UnityEngine.Events.UnityAction<Result, string> OnRegister;   //通过回调 OnRegister 通知 UI 层
        public UnityEngine.Events.UnityAction<Result, string> OnLogin;      //通过回调 OnLogin 通知 UI 层
        public UnityEngine.Events.UnityAction<Result, string> OnCharCreate;  //通过回调 OnCharCreate 通知 UI 层(给UI层使用)

        NetMessage pendingMessage = null;      //网络消息的缓存变量，用来保存尚未发送的消息（例如当网络断开时）。
        bool connected = false;

        public UserService()//构造函数
        {
            NetClient.Instance.OnConnect += OnGameServerConnect;                     //当客户端连接到服务器时触发。
            NetClient.Instance.OnDisconnect += OnGameServerDisconnect;               //当客户端与服务器断开时触发。

            MessageDistributer.Instance.Subscribe<UserRegisterResponse>(this.OnUserRegister); //监听来自服务器的 UserRegisterResponse 消息。当服务器返回用户注册结果时，触发 OnUserRegister 方法。
            MessageDistributer.Instance.Subscribe<UserLoginResponse>(this.OnUserLogin);
            MessageDistributer.Instance.Subscribe<UserCreateCharacterResponse>(this.OnUserCreateCharacter);

        }


        public void Dispose()//资源释放，解除订阅的事件和消息，防止内存泄漏或对象被销毁后仍然调用事件逻辑。
        {
            MessageDistributer.Instance.Unsubscribe<UserRegisterResponse>(this.OnUserRegister);
            MessageDistributer.Instance.Unsubscribe<UserLoginResponse>(this.OnUserLogin);
            MessageDistributer.Instance.Unsubscribe<UserCreateCharacterResponse>(this.OnUserCreateCharacter);


            NetClient.Instance.OnConnect -= OnGameServerConnect;
            NetClient.Instance.OnDisconnect -= OnGameServerDisconnect;
        }
        /*————————————————————————————————————————————————————————————————————————————————————————————————————————————*/
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
        /*————————————————————————————————————————————————————————————————————————————————————————————————————————————*/
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
        void OnUserRegister(object sender, UserRegisterResponse response)   //处理注册响应
        {
            Debug.LogFormat("OnUserRegister:{0} [{1}]", response.Result, response.Errormsg);

            if (this.OnRegister != null)
            {
                this.OnRegister(response.Result, response.Errormsg);
            }
        }
        /*————————————————————————————————————————————————————————————————————————————————————————————————————————————*/

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
        /*————————————————————————————————————————————————————————————————————————————————————————————————————————————*/
        public void SendCreateCharacter(string name, CharacterClass charClass)
        {
            Debug.LogFormat("UserCreateCharacterRequest::name :{0} class:{1}", name, charClass);

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.createChar = new UserCreateCharacterRequest();
            
            message.Request.createChar.Name = name;
            message.Request.createChar.Class = charClass;
            
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
        /*message UserCreateCharacterResponse 
        {
            RESULT result = 1;
            string errormsg = 2;
            repeated NCharacterInfo characters = 3;
        }*/
        private void OnUserCreateCharacter(object sender, UserCreateCharacterResponse message)
        {
            Debug.LogFormat("OnUserCreateCharacter:{0} [{1}]", message.Result, message.Errormsg);

            if (message.Result == Result.Success)
            {
                Models.User.Instance.Info.Player.Characters.Clear();    // 清除当前角色的信息
                Models.User.Instance.Info.Player.Characters.AddRange(message.Characters); //通过 AddRange(response.Characters) 将从服务端返回的角色列表添加到当前玩家的角色列表中。message.Characters 是一个包含新角色的集合。
            }
            else
            {
                // 检查 OnCharCreate 是否被赋值（即是否有注册回调,OnCharCreate是给UI用的）。
                if (this.OnCharCreate != null)
                {
                    this.OnCharCreate(message.Result, message.Errormsg); 

                }
            }
        }

    }
}
