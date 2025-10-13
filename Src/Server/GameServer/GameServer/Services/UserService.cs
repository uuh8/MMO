using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Network;
using SkillBridge.Message;
using GameServer.Entities;
using System.Security.Cryptography;
using System.Data.SqlTypes;
using GameServer.Managers;

namespace GameServer.Services
{
    class UserService : Singleton<UserService>
    {
        //构造函数
        public UserService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserRegisterRequest>(this.OnRegister); 
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserLoginRequest>(this.OnLogin);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserCreateCharacterRequest>(this.OnCreateCharacter);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameEnterRequest>(this.OnGameEnter);
            //MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameLeaveRequest>(this.OnGameLeave);
        }

        public void Init()
        {

        }

        #region 客户端消息处理

        /// <summary>
        /// 处理客户端的登录请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnLogin(NetConnection<NetSession> sender, UserLoginRequest request)
        {
            Log.InfoFormat("[UserService] UserLoginRequest: User:{0} Pass:{1}", request.User, request.Password);

            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.userLogin = new UserLoginResponse();


            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user == null)
            {
                message.Response.userLogin.Result = Result.Failed;
                message.Response.userLogin.Errormsg = "用户不存在";
            }
            else if (user.Password != request.Password)
            {
                message.Response.userLogin.Result = Result.Failed;
                message.Response.userLogin.Errormsg = "密码错误";
            }
            else // 登录成功
            {
                // 注意这儿会话里存的是 EF 跟踪的实体，而不是独立的 DTO 或数据库重新查询的结果。
                sender.Session.User = user;

                // 装配DTO
                message.Response.userLogin.Result = Result.Success;
                message.Response.userLogin.Errormsg = "None";           
                message.Response.userLogin.Userinfo = new NUserInfo();  // 初始化用户信息
                message.Response.userLogin.Userinfo.Id = 1;
                message.Response.userLogin.Userinfo.Player = new NPlayerInfo(); // 初始化玩家信息
                message.Response.userLogin.Userinfo.Player.Id = user.Player.ID;

                /*加载玩家的角色信息*/
                //遍历当前玩家账号下所有已创建的角色
                foreach (var c in user.Player.Characters)
                {
                    //为当前遍历到的角色创建一个 NCharacterInfo 对象。NCharacterInfo 是一个数据传输对象（DTO），用于传递角色信息给客户端。
                    NCharacterInfo info = new NCharacterInfo();
                    info.Id = c.ID;
                    info.Name = c.Name;
                    info.Class = (CharacterClass)c.Class;

                    message.Response.userLogin.Userinfo.Player.Characters.Add(info);
                }
            }

            byte[] data = PackageHandler.PackMessage(message);  // 将 NetMessage 对象序列化为字节数组，方便通过网络发送。
            sender.SendData(data, 0, data.Length);              // 将打包好的字节数组通过当前会话 sender 发送给客户端。
        }

        /// <summary>
        /// 处理客户端的注册请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnRegister(NetConnection<NetSession> sender, UserRegisterRequest request)
        {
            Log.InfoFormat("UserRegisterRequest: User:{0}  Pass:{1}", request.User, request.Passward);

            //构建响应消息
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.userRegister = new UserRegisterResponse();
            /*查询数据库，检查用户名是否已存在。*/
            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user != null)       // 查看注册的用户存不存在
            {
                message.Response.userRegister.Result = Result.Failed;
                message.Response.userRegister.Errormsg = "用户已存在.";
            }
            else                    // 不存在则添加到数据库中
            {
                TPlayer player = DBService.Instance.Entities.Players.Add(new TPlayer());
                DBService.Instance.Entities.Users.Add(new TUser() {
                    Username = request.User, 
                    Password = request.Passward, 
                    Player = player 
                });
                DBService.Instance.Entities.SaveChanges();

                message.Response.userRegister.Result = Result.Success;
                message.Response.userRegister.Errormsg = "None";
            }

            //使用 PackageHandler 将响应消息打包为字节数组。
            // Protobuf 提供的序列化方法 ToByteArray()将 NetMessage 对象序列化成二进制数据
            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);
        }

        /// <summary>
        /// 处理客户端的创建角色请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="message"></param>
        private void OnCreateCharacter(NetConnection<NetSession> sender, UserCreateCharacterRequest message)
        {
            // 客户端发送来的网络消息
            Log.InfoFormat("UserCreateCharacterRequest: Name:{0}  Class:{1}", message.Name, message.Class);

            /*—————————————————————— 数据库操作 ————————————————————————*/
            // 1. 创建角色对象，对象的每个字段对应数据库表的一列
            // 创建角色对象TCharacter，TCharacter 类是一个 C# 对象，表示角色，它映射到数据库中的 Characters 表
            TCharacter character = new TCharacter()
            {
                Name = message.Name,
                Class = (int)message.Class,
                TID = (int)message.Class,
                MapID = 1,
                MapPosX = 5000,
                MapPosY = 4000,
                MapPosZ = 820
            };

            // 2. 把新角色对象加入到“待插入”的集合中
            // Entities.Characters其实是DbSet<TCharacter>，相当于一个操作数据库表的“集合”。
            // Add(character)并不会立刻写入数据库，只是告诉EF有一个新对象要插入到数据库表中。
            DBService.Instance.Entities.Characters.Add(character);

            // 3. 把角色加到玩家角色列表（服务器内存结构，不是数据库）里
            // 这只是把新角色对象加到当前登录玩家的内存角色列表，与数据库无关，属于服务端业务层数据结构。
            sender.Session.User.Player.Characters.Add(character);

            // 4. 把所有“待插入/待更新”的数据 一次性 同步到数据库
            // 这行才是真正把所有Add/Remove/Update的对象变成SQL语句，批量发送给数据库执行。
            DBService.Instance.Entities.SaveChanges();

            /*—————————————————————— 网络发送 ————————————————————————*/
            // 构建DTO发给客户端
            NetMessage response = new NetMessage();
            response.Response = new NetMessageResponse();
            response.Response.createChar = new UserCreateCharacterResponse();
            response.Response.createChar.Result = Result.Success;
            response.Response.createChar.Errormsg = "None";

            // 消息打包成字节流发送给客户端
            byte[] data = PackageHandler.PackMessage(response);
            sender.SendData(data, 0, data.Length);
        }

        /// <summary>
        /// 处理客户端进入游戏的请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnGameEnter(NetConnection<NetSession> sender, UserGameEnterRequest request)
        {
            TCharacter dbchar = sender.Session.User.Player.Characters.ElementAt(request.characterIdx);
            Log.InfoFormat("UserGameEnterRequest: characterID:{0}:{1} Map:{2}", dbchar.ID, dbchar.Name, dbchar.MapID);
            Character character = CharacterManager.Instance.AddCharacter(dbchar);

            // 构建DTO
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.gameEnter = new UserGameEnterResponse();
            message.Response.gameEnter.Result = Result.Success;
            message.Response.gameEnter.Errormsg = "None";

            // 发送消息给客户端
            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);

            // 将角色赋值给会话，此后随时可以通过Session的Character获取当前是在对哪一个角色操作
            sender.Session.Character = character;

            MapManager.Instance[dbchar.MapID].CharacterEnter(sender, character);
        }

        /*private void OnGameLeave(NetConnection<NetSession> sender, UserCreateCharacterRequest request)
        {

        }*/

        #endregion
    }
}
