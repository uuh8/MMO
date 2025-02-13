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

namespace GameServer.Services
{
    class UserService : Singleton<UserService>
    {
        //构造函数
        public UserService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserRegisterRequest>(this.OnRegister); //订阅消息
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserLoginRequest>(this.OnLogin);
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserCreateCharacterRequest>(this.OnCreateCharacter);
        }

        public void Init()
        {

        }
        /*——————————————————————————————————————————————————————————————————————————————————————————————————————————————————*/
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

            // 在这段代码中，DBService.Instance.Entities.Users 实际上是 Entity Framework 中的一个 DbSet，它代表了应用程序中的一个集合，用于与 Users 表进行交互。
            // DBService.Instance.Entities.Users 是一个 ORM 映射对象，它代表了数据库中的 Users 表，并允许你用 C# 代码来操作数据库。
            // 实际上，数据库和代码中的 User 类之间的关系是通过 ORM 映射来建立的，EF 会在后台将你的 C# 类与数据库表进行映射，从而将对象操作转换为 SQL 操作。
            //.FirstOrDefault()表示返回查询结果中的第一个元素，如果没有结果，则返回 null。
            /*会被 ORM 转换为类似的 SQL 查询：
            SELECT TOP 1 *
            FROM Users
            WHERE Username = @Username*/
            // DBService.Instance.Entities 应用程序中所有数据库实体的集合，Users 则是其中的一个属性，代表数据库中的 Users 表。
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


        void OnRegister(NetConnection<NetSession> sender, UserRegisterRequest request)
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

        private void OnCreateCharacter(NetConnection<NetSession> sender, UserCreateCharacterRequest message)
        {
            // 客户端发送来的网络消息
            Log.InfoFormat("UserCreateCharacterRequest: Name:{0}  Class:{1}", message.Name, message.Class);

            /*—————————————————————— 数据库操作 ————————————————————————*/
            // 创建角色对象TCharacter，TCharacter 类是一个 C# 对象，表示角色。它映射到数据库中的 Characters 表，
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
            // 通过 Entity Framework 将 TCharacter 对象添加到 Characters 表中。
            // Entities.Characters 是 Entity Framework 中与数据库表 Characters 相关联的 DbSet<TCharacter>，它允许我们操作数据库中的角色数据。
            // Add(character) 会把新创建的角色对象 character 加入到 DbSet<TCharacter> 中，表示角色将被插入到数据库。
            DBService.Instance.Entities.Characters.Add(character);
            // 将角色对象添加到当前登录玩家的角色列表中。
            // sender.Session.User 代表当前会话的用户，User.Player 是该用户的玩家对象，Player.Characters 是该玩家已经拥有的角色集合。
            // 这样，在数据库中添加角色的同时，也在服务器内存中的玩家数据结构中更新角色列表。
            sender.Session.User.Player.Characters.Add(character);
            // 这行代码会将 DbSet<TCharacter> 中的更改（即新添加的角色）保存到数据库。
            // SaveChanges() 是 Entity Framework 提供的一个方法，它会生成 SQL 插入语句（INSERT），并将所有对 Entities 的更改持久化到数据库中。在这里，它会将新创建的角色插入到 Characters 表。
            DBService.Instance.Entities.SaveChanges();

            /*—————————————————————— 构建UserCreateCharacterResponse发送给客户端 ————————————————————————*/
            NetMessage response = new NetMessage();
            response.Response = new NetMessageResponse();
            response.Response.createChar = new UserCreateCharacterResponse();
            response.Response.createChar.Result = Result.Success;
            response.Response.createChar.Errormsg = "None";

            byte[] data = PackageHandler.PackMessage(response);
            sender.SendData(data, 0, data.Length);
        }
    }
}
