using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Common;
using Network;
using UnityEngine;
using SkillBridge.Message;
using Models;
using Managers;
using System.Security.Cryptography;

namespace Services
{
    class UserService : Singleton<UserService>, IDisposable
    {
        public UnityEngine.Events.UnityAction<Result, string> OnRegister; 
        public UnityEngine.Events.UnityAction<Result, string> OnLogin;   
        public UnityEngine.Events.UnityAction<Result, string> OnCharCreate;  

        NetMessage pendingMessage = null;      //网络消息的缓存变量，用来保存尚未发送的消息（例如当网络断开时）。
        bool connected = false;

        //构造函数
        public UserService()
        {
            NetClient.Instance.OnConnect += OnGameServerConnect;
            NetClient.Instance.OnDisconnect += OnGameServerDisconnect;

            // 监听来自服务器的消息。当服务器返回用户注册结果时，触发对应的方法。
            MessageDistributer.Instance.Subscribe<UserRegisterResponse>(this.OnUserRegister); 
            MessageDistributer.Instance.Subscribe<UserLoginResponse>(this.OnUserLogin);
            MessageDistributer.Instance.Subscribe<UserCreateCharacterResponse>(this.OnUserCreateCharacter);
            MessageDistributer.Instance.Subscribe<UserGameEnterResponse>(this.OnGameEnter);
            MessageDistributer.Instance.Subscribe<UserGameLeaveResponse>(this.OnGameLeave);
            MessageDistributer.Instance.Subscribe<MapCharacterEnterResponse>(this.OnCharacterEnterMap);
            // MessageDistributer.Instance.Subscribe<MapCharacterLeaveResponse>(this.OnCharacterLeaveMap);
        }

        //资源释放，解除订阅的事件和消息，防止内存泄漏或对象被销毁后仍然调用事件逻辑。
        public void Dispose()
        {
            MessageDistributer.Instance.Unsubscribe<UserRegisterResponse>(this.OnUserRegister);
            MessageDistributer.Instance.Unsubscribe<UserLoginResponse>(this.OnUserLogin);
            MessageDistributer.Instance.Unsubscribe<UserCreateCharacterResponse>(this.OnUserCreateCharacter);
            MessageDistributer.Instance.Unsubscribe<UserGameEnterResponse>(this.OnGameEnter);
            MessageDistributer.Instance.Unsubscribe<UserGameLeaveResponse>(this.OnGameLeave);
            MessageDistributer.Instance.Unsubscribe<MapCharacterEnterResponse>(this.OnCharacterEnterMap);
            // MessageDistributer.Instance.Unsubscribe<MapCharacterLeaveResponse>(this.OnCharacterLeaveMap);

            NetClient.Instance.OnConnect -= OnGameServerConnect;
            NetClient.Instance.OnDisconnect -= OnGameServerDisconnect;
        }

        public void Init()
        {

        }

        #region 服务器连接相关

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

        #endregion



        #region 向服务器发送请求

        /// <summary>
        /// 发送登录请求
        /// </summary>
        /// <param name="user"></param>
        /// <param name="psw"></param>
        public void SendLogin(string user, string psw) 
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

        /// <summary>
        /// 发送注册请求
        /// </summary>
        /// <param name="user"></param>
        /// <param name="psw"></param>
        public void SendRegister(string user, string psw) 
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

        /// <summary>
        /// 发送创建角色请求
        /// </summary>
        /// <param name="name"></param>
        /// <param name="charClass"></param>
        public void SendCreateCharacter(string name, CharacterClass charClass)
        {
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

        /// <summary>
        /// 发送角色进入游戏的请求
        /// </summary>
        /// <param name="characterIdx"></param>
        public void SendGameEnter(int characterId)
        {
            Debug.LogFormat("[UserService] UserGameEnterRequest::characterId:{0}", characterId);

            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.gameEnter = new UserGameEnterRequest();
            message.Request.gameEnter.characterId = characterId;
            NetClient.Instance.SendMessage(message);
        }
        
        /// <summary>
        /// 发送角色推出游戏的请求
        /// </summary>
        public void SendGameLeave()
        {
            Debug.Log("[UserService] UserGameLeaveRequest");
            NetMessage message = new NetMessage();
            message.Request = new NetMessageRequest();
            message.Request.gameLeave = new UserGameLeaveRequest();
            NetClient.Instance.SendMessage(message);
        }
        #endregion



        #region 响应服务器发来的消息
         
        /// <summary>
        /// 登录事件响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        void OnUserLogin(object sender, UserLoginResponse response)
        {
            if (response == null)
            {
                Debug.LogError("[UserService] OnUserLogin: Received null message object!");
                return;
            }

            Debug.LogFormat("[UserService] OnUserLogin:{0} [{1}]", response.Result, response.Errormsg);

            if (response.Result == Result.Success)
            {
                Models.User.Instance.SetupUserInfo(response.Userinfo);
            };
            //调用回调，通知UI层
            if (this.OnLogin != null)
            {
                this.OnLogin(response.Result, response.Errormsg);

            }
        }

        /// <summary>
        /// 注册事件响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        void OnUserRegister(object sender, UserRegisterResponse response) 
        {
            Debug.LogFormat("OnUserRegister:{0} [{1}]", response.Result, response.Errormsg);

            if (this.OnRegister != null)
            {
                this.OnRegister(response.Result, response.Errormsg);
            }
        }

        /// <summary>
        /// 用户创建角色响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        void OnUserCreateCharacter(object sender, UserCreateCharacterResponse message)
        {
            Debug.LogFormat("[UserService] OnUserCreateCharacter:{0} [{1}]", message.Result, message.Errormsg);

            if (message.Result == Result.Success)
            {
                User.Instance.Info.Player.Characters.Clear(); 
                User.Instance.Info.Player.Characters.AddRange(message.Characters); 
            }
            // 调用回调，通知UI层
            if (this.OnCharCreate != null)
            {
                Debug.LogFormat("<color=yellow>[UserService]</color> OnCharCreate触发了");
                this.OnCharCreate(message.Result, message.Errormsg);
            }
            else
            {
                Debug.LogFormat("<color=yellow>[UserService]</color> OnCharCreate没有被触发");
            }

        }

        /// <summary>
        /// 用户进入游戏的响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        void OnGameEnter(object sender, UserGameEnterResponse response)
        {
            /* UserGameEnterResponse 结构
             * message UserGameEnterResponse {
	                RESULT result = 1;   // 进入结果
	                string errormsg = 2; // 错误信息
	                NCharacterInfo character = 3; // 角色信息
                }
             */
            Debug.LogFormat("[UserService] OnGameEnter:{0} [{1}]", response.Result, response.Errormsg);

            if (response.Result == Result.Success)
            {
                if(response.Result == Result.Success)
                {
                    User.Instance.CurrentCharacter = response.Character;

                    // 初始化角色身上的各种 Manager
                    ItemManager.Instance.Init(response.Character.Items);
                    BagManager.Instance.Init(response.Character.Bag);
                    EquipManager.Instance.Init(response.Character.Equips);
                    QuestManager.Instance.Init(response.Character.Quests);
                    FriendManager.Instance.Init(response.Character.Friends);
                }
            }
        }

        /// <summary>
        /// 用户离开游戏的响应
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        void OnGameLeave(object sender, UserGameLeaveResponse response)
        {
            MapService.Instance.CurrentMapId = 0;
            User.Instance.CurrentCharacter = null;
            Debug.LogFormat("[UserService] OnGameLeave:{0} [{1}]", response.Result, response.Errormsg);
        }

        /// <summary>
        /// 处理角色进入地图的响应（场景加载）
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        void OnCharacterEnterMap(object sender, MapCharacterEnterResponse response)
        {
            /*
             * message MapCharacterEnterResponse{
	                int32 mapId = 1;                    // 当前地图ID
	                repeated NCharacterInfo characters = 2; // 地图内可见角色列表
                }
             */
            Debug.LogFormat("[UserService] OnCharacterEnterMap: mapId：{0}", response.mapId);

            var me = User.Instance.CurrentCharacter;
            if (me == null)
            {
                Debug.LogError("CurrentCharacter is null, OnGameEnter should have run first.");
                return;
            }

            // 这条消息可能是：
            // 1) 我自己进图的“全量列表”（含我 + 其他人 + 怪物）
            // 2) 别人进图时服务器推给我的“增量列表”（通常只有对方 1 个）
            // 所以必须用 dbId 精确找到“我自己那条”
            NCharacterInfo info = null;
            for (int i = 0; i < response.Characters.Count; i++)
            {
                var c = response.Characters[i];
                if (c.Type == CharacterType.Player && c.Id == me.Id) // db id 匹配
                {
                    info = c;
                    break;
                }
            }
            // 如果没找到自己，说明这是“别人进入地图”的增量包：只刷新/生成对方实体，不动 CurrentCharacter
            if (info == null)
            {
                return;
            }

            // 全量包：只更新“地图相关字段”，不要整包替换 CurrentCharacter（避免覆盖你在 OnGameEnter 初始化好的引用）
            me.EntityId = info.EntityId;
            me.Entity = info.Entity;
            me.mapId = response.mapId;

            // 只有当进入新地图时才加载场景（防止别人进图触发你反复 LoadScene）
            if (MapService.Instance.CurrentMapId != response.mapId)
            {
                if (DataManager.Instance.Maps.TryGetValue(response.mapId, out var map))
                {
                    User.Instance.CurrentMapData = map;
                    SceneManager.Instance.LoadScene(map.Resource);
                }
                else
                {
                    Debug.LogErrorFormat("[UserService] Map {0} not existed", response.mapId);
                }
            }
        }

        /// <summary>
        /// 处理角色离开地图的响应(本人或其他人)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="response"></param>
        /*void OnCharacterLeaveMap(object sender, MapCharacterLeaveResponse response)
        {
            Debug.LogFormat("[UserService] OnCharacterLeaveMap:{0}", response.characterId);
            // 判断离开地图的角色是不是“本人”
            if (response.characterId != User.Instance.CurrentCharacter.Id)
                CharacterManager.Instance.RemoveCharacter(response.characterId); // 离开的是其他人，只移除其他人
            else
                CharacterManager.Instance.Clear();  // "本人"不在地图中了，直接清空
        }*/

        #endregion
    }
}
