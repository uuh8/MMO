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
using System.Windows.Forms;

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
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<UserGameLeaveRequest>(this.OnGameLeave); 
        }

        public void Init()
        {

        }

        #region 客户端消息处理

        /// <summary>
        /// 处理客户端的注册请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnRegister(NetConnection<NetSession> sender, UserRegisterRequest request)
        {
            Log.InfoFormat("UserRegisterRequest: User:{0}  Pass:{1}", request.User, request.Passward);

            //构建响应消息
            sender.Session.Response.userRegister = new UserRegisterResponse();

            /*查询数据库，检查用户名是否已存在。*/
            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user != null)       // 查看注册的用户存不存在
            {
                sender.Session.Response.userRegister.Result = Result.Failed;
                sender.Session.Response.userRegister.Errormsg = "用户已存在.";
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

                sender.Session.Response.userRegister.Result = Result.Success;
                sender.Session.Response.userRegister.Errormsg = "None";
            }
            sender.SendResponse();
        }

        /// <summary>
        /// 处理客户端的登录请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnLogin(NetConnection<NetSession> sender, UserLoginRequest request)
        {
            Log.InfoFormat("[UserService] UserLoginRequest: User:{0} Pass:{1}", request.User, request.Password);

            sender.Session.Response.userLogin = new UserLoginResponse(); 

            TUser user = DBService.Instance.Entities.Users.Where(u => u.Username == request.User).FirstOrDefault();
            if (user == null)
            {
                sender.Session.Response.userLogin.Result = Result.Failed;
                sender.Session.Response.userLogin.Errormsg = "用户不存在";
            }
            else if (user.Password != request.Password)
            {
                sender.Session.Response.userLogin.Result = Result.Failed;
                sender.Session.Response.userLogin.Errormsg = "密码错误";
            }
            else // 登录成功
            {
                // 注意这儿会话里存的是 EF 跟踪的实体，而不是独立的 DTO 或数据库重新查询的结果。
                sender.Session.User = user;

                // 装配DTO
                sender.Session.Response.userLogin.Result = Result.Success;
                sender.Session.Response.userLogin.Errormsg = "None";
                sender.Session.Response.userLogin.Userinfo = new NUserInfo();  // 初始化用户信息
                sender.Session.Response.userLogin.Userinfo.Id = (int)user.ID;
                sender.Session.Response.userLogin.Userinfo.Player = new NPlayerInfo(); // 初始化玩家信息
                sender.Session.Response.userLogin.Userinfo.Player.Id = user.Player.ID;

                /*加载玩家的角色信息*/
                // 遍历当前玩家账号下所有已创建的角色
                foreach (var c in user.Player.Characters)
                {
                    // 为当前遍历到的角色创建一个 NCharacterInfo 对象。
                    // NCharacterInfo 是一个数据传输对象（DTO），用于传递角色信息给客户端。
                    NCharacterInfo info = new NCharacterInfo();
                    info.Id = c.ID;  
                    info.ConfigId = c.ID;    // 这里用数据库中的 CharacterId
                    info.Type = CharacterType.Player;
                    info.Class = (CharacterClass)c.Class;
                    info.Name = c.Name;

                    sender.Session.Response.userLogin.Userinfo.Player.Characters.Add(info);
                }
            }

            sender.SendResponse();
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
                Level = 1,
                MapID = 1,
                MapPosX = 5000,
                MapPosY = 4000,
                MapPosZ = 820,
                Gold = 100000,   // 初始10w金币
                Equips = new byte[28]   // 每个装备槽4字节，共7个装备槽
            };
            // 背包初始化
            var bag = new TCharacterBag();
            bag.Owner = character;
            bag.Items = new byte[0];
            bag.Unlocked = 15;  // 默认最开始解锁20个格子
            TCharacterItem it = new TCharacterItem();
            character.Bag = DBService.Instance.Entities.CharacterBag.Add(bag);

            // 2. 把新角色对象加入到“待插入”的集合中
            // Entities.Characters其实是DbSet<TCharacter>，相当于一个操作数据库表的“集合”。
            // Add(character)并不会立刻写入数据库，只是告诉EF有一个新对象要插入到数据库表中。
            character = DBService.Instance.Entities.Characters.Add(character);

            // 新人送20红瓶和蓝瓶
            character.Items.Add(new TCharacterItem()
            {
                Owner = character,
                ItemID = 1,
                ItemCount = 20
            });
            character.Items.Add(new TCharacterItem()
            {
                Owner = character,
                ItemID = 2,
                ItemCount = 20
            });

            // 3. 把角色加到玩家角色列表（服务器内存结构，不是数据库）里
            // 这只是把新角色对象加到当前登录玩家的内存角色列表，与数据库无关，属于服务端业务层数据结构。
            sender.Session.User.Player.Characters.Add(character);

            // 4. 把所有“待插入/待更新”的数据 一次性 同步到数据库
            // 这行才是真正把所有Add/Remove/Update的对象变成SQL语句，批量发送给数据库执行。
            DBService.Instance.Entities.SaveChanges();

            /*—————————————————————— 网络发送 ————————————————————————*/
            // 构建DTO发给客户端
            sender.Session.Response.createChar = new UserCreateCharacterResponse();
            sender.Session.Response.createChar.Result = Result.Success;
            sender.Session.Response.createChar.Errormsg = "None";

            // 把当前已有的角色添加进列表（列表刷新）
            foreach(var c in sender.Session.User.Player.Characters)
            {
                // 为当前遍历到的角色创建一个 NCharacterInfo 对象。
                // NCharacterInfo 是一个数据传输对象（DTO），用于传递角色信息给客户端。
                NCharacterInfo info = new NCharacterInfo();
                info.Id = c.ID;    // 这里应该用 entityId 而不是数据库中的 CharacterId，但由于这个时候还没进入游戏，因此 entity 还没创建，因此这里初始化为 0 方便调试
                info.ConfigId = c.ID;    // 这里用数据库中的 CharacterId
                info.Type = CharacterType.Player;
                info.Class = (CharacterClass)c.Class;
                info.Name = c.Name;

                sender.Session.Response.createChar.Characters.Add(info);
            }
            sender.SendResponse();
        }

        /// <summary>
        /// 处理客户端进入游戏的请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnGameEnter(NetConnection<NetSession> sender, UserGameEnterRequest request)
        {
            // 1) 从“当前登录用户的角色列表”里取出一个 "EF实体"
            // 这行代码不是在“新建”TCharacter实体，而是从内存中读取一个TCharacter对象，而这个对象本质上最初是从数据库加载出来的，现在常驻在内存
            int characterId = request.characterId;
            TCharacter dbchar = sender.Session.User.Player.Characters.FirstOrDefault(c => c.ID == characterId);

            // 2) 把存档对象(TCharacter) 转换成运行时对象(Character)。从 DB 实体构造运行时角色，注册到内存（切断 EF 依赖）
            Character character = CharacterManager.Instance.AddCharacter(dbchar);

            SessionManager.Instance.AddSession(character.Id, sender);

            Log.InfoFormat("[UserService] UserGameEnterRequest 角色进入: characterID:{0}:{1} Map:{2}", character.Id, character.Info.Name, character.Info.mapId);

            // 3) 将角色赋值给会话，此后随时可以通过Session的Character获取当前是在对哪一个角色操作
            sender.Session.Character = character;
            sender.Session.PostResponser = character;   // 初始化后处理器，后处理器是由角色来执行的

            // 4) 构建响应DTO UserGameEnterResponse
            sender.Session.Response.gameEnter = new UserGameEnterResponse();
            sender.Session.Response.gameEnter.Result = Result.Success;
            sender.Session.Response.gameEnter.Errormsg = "None";

            // 5) 进入成功，发送初始角色信息给客户端
            sender.Session.Response.gameEnter.Character = character.Info;
            sender.SendResponse();


            // 6) 把运行时角色丢进对应地图，开始广播出生、同步周边实体等。
            MapManager.Instance[dbchar.MapID].CharacterEnter(sender, character);
        }

        /// <summary>
        /// 处理客户端离开游戏的请求
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        private void OnGameLeave(NetConnection<NetSession> sender, UserGameLeaveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[UserService] UserGameLeaveRequest: characterID:{0}:{1} Map:{2}", character.Id, character.Info.Name, character.Info.mapId);

            SessionManager.Instance.RemoveSession(character.Id);
            CharacterLeave(character);

            sender.Session.Response.gameLeave = new UserGameLeaveResponse();
            sender.Session.Response.gameLeave.Result = Result.Success;
            sender.Session.Response.gameLeave.Errormsg = "None";

            sender.SendResponse();
        }
        public void CharacterLeave(Character character)
        {
            CharacterManager.Instance.RemoveCharacter(character.Id);
            MapManager.Instance[character.Info.mapId].CharacterLeave(character);
            character.Clear();
        }

        #endregion
    }
}
