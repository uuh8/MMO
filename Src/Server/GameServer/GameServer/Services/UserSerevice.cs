using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Common;
using Network;
using SkillBridge.Message;
using GameServer.Entities;

namespace GameServer.Services
{
    class UserService : Singleton<UserService>
    {
        //构造函数
        public UserService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserRegisterRequest>(this.OnRegister); //订阅消息
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserLoginRequest>(this.OnLogin);
        }

        void OnLogin(NetConnection<NetSession> sender, UserLoginRequest request)
        {
            Log.InfoFormat("UserLoginRequest: User:{0} Pass:{1}", request.User, request.Password);

            /*message UserLoginResponse {
                RESULT result = 1;
                string errormsg = 2;
                NUserInfo userinfo = 3;
            }*/
            NetMessage message = new NetMessage();
            message.Response = new NetMessageResponse();
            message.Response.userLogin = new UserLoginResponse();

            //.FirstOrDefault()表示返回查询结果中的第一个元素，如果没有结果，则返回 null。
            /*会被 ORM 转换为类似的 SQL 查询：
             SELECT TOP 1 *
             FROM Users
             WHERE Username = @Username*/
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
            else
            {
                //登录成功后，服务器将当前用户绑定到当前会话
                /*message UserLoginResponse {
                    RESULT result = 1;
                    string errormsg = 2;
                    NUserInfo userinfo = 3;
                }
                enum RESULT
                {
                	SUCCESS = 0;
                	FAILED = 1;
                }
                 */
                sender.Session.User = user;
                message.Response.userLogin.Result = Result.Success;  // 将 Result.Success 赋值给 UserLoginResponse 消息的 Result 字段，表示登录成功。
                message.Response.userLogin.Errormsg = "None";           
                message.Response.userLogin.Userinfo = new NUserInfo();  // 初始化用户信息
                message.Response.userLogin.Userinfo.Id = 1;
                message.Response.userLogin.Userinfo.Player = new NPlayerInfo(); // 初始化玩家信息
                message.Response.userLogin.Userinfo.Player.Id = user.Player.ID;

                /*加载玩家的角色信息*/
                //遍历当前玩家账号下所有已创建的角色
                foreach (var c in user.Player.Characters)
                {
                    //为当前遍历到的角色（c）创建一个 NCharacterInfo 对象。NCharacterInfo 是一个数据传输对象（DTO），用于传递角色信息给客户端。
                    NCharacterInfo info = new NCharacterInfo();
                    // 填充角色信息
                    info.Id = c.ID;
                    info.Name = c.Name;
                    info.Class = (CharacterClass)c.Class;
                    //message.Response.userLogin.Userinfo.Player.Characters 是一个集合，用于存储所有角色的信息。例如：
                    /*message.Response.userLogin.Userinfo.Player.Characters = [
                        { Id = 1, Name = "Warrior1", Class = CharacterClass.Warrior },
                        { Id = 2, Name = "Mage1", Class = CharacterClass.Mage }
                    ];*/
                    message.Response.userLogin.Userinfo.Player.Characters.Add(info);
                }
            }

            byte[] data = PackageHandler.PackMessage(message);  // 将 NetMessage 对象序列化为字节数组，方便通过网络发送。
            sender.SendData(data, 0, data.Length);  // 将打包好的字节数组通过当前会话 sender 发送给客户端。
        }


        public void Init()
        {
            Log.Info("UserService Init");
        }

        void OnRegister(NetConnection<NetSession> sender, UserRegisterRequest request)
        {
            Log.InfoFormat("UserRegisterRequest: User:{0}  Pass:{1}", request.User, request.Passward);


            /*NetMessage、NetMessageResponse、UserRegisterResponse 等类，通常由 Protobuf 的 .proto 文件定义。
            这些类由 Protobuf 编译器（protoc）生成。*/
            //NetMessage是消息的顶层容器，用于包装所有可能的请求或响应。
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
                DBService.Instance.Entities.Users.Add(new TUser() { Username = request.User, Password = request.Passward, Player = player });
                DBService.Instance.Entities.SaveChanges();
                message.Response.userRegister.Result = Result.Success;
                message.Response.userRegister.Errormsg = "None";
            }

            //使用 PackageHandler 将响应消息打包为字节数组。
            // Protobuf 提供的序列化方法 ToByteArray()将 NetMessage 对象序列化成二进制数据
            byte[] data = PackageHandler.PackMessage(message);
            sender.SendData(data, 0, data.Length);
        }
    }
}
