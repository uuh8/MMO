using Common;
using Common.Data;
using Common.Utils;
using GameServer.Core;
using GameServer.Managers;
using GameServer.Models;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entities
{
    /// <summary>
    /// Character
    /// 玩家角色类
    /// </summary>
    class Character : CharacterBase,IPostResponser
    {
        public TCharacter Data;
        public ItemManager ItemManager;
        public QuestManager QuestManager; 
        public StatusManager StatusManager;
        public FriendManager FriendManager;

        public Team Team;   // 由于Team没有DB，因此用Team类来管理
        public double TeamUpdateTS; // 时间戳（用于校验队伍信息是否变化）

        public Guild Guild;   
        public double GuildUpdateTS;

        public Chat Chat;   // 聊天不需要保存db，因此和聊天相关的内容都是保存在内存中

        /// <summary>
        /// 初始化
        /// Character 是在进入游戏的时候OnEnterGame中调用创建的
        /// </summary>
        /// <param name="type"></param>
        /// <param name="cha"></param>
        public Character(CharacterType type,TCharacter cha):
            base(new Core.Vector3Int(cha.MapPosX, cha.MapPosY, cha.MapPosZ),new Core.Vector3Int(100,0,0))
        {
            this.Data = cha;    // 把 “EF的持久化实体” 塞进长期存活的运行时对象
            this.Id = cha.ID;
            this.Info = new NCharacterInfo();
            this.Info.Type = type;
            this.Info.Id = cha.ID;
            this.Info.EntityId = this.entityId;
            this.Info.Name = cha.Name;
            this.Info.Level = 10;
            this.Info.ConfigId = cha.TID;
            this.Info.Class = (CharacterClass)cha.Class;
            this.Info.mapId = cha.MapID;
            this.Info.Gold = cha.Gold;
            this.Info.Entity = this.EntityData; 

            this.Define = DataManager.Instance.Characters[this.Info.ConfigId];

            this.ItemManager = new ItemManager(this);
            this.ItemManager.GetItemInfos(this.Info.Items);

            this.Info.Bag = new NBagInfo();
            this.Info.Bag.Unlocked = this.Data.Bag.Unlocked; 
            this.Info.Bag.Items = this.Data.Bag.Items;

            this.Info.Equips = this.Data.Equips;

            this.QuestManager = new QuestManager(this);         // 构建实例
            this.QuestManager.GetQuestInfos(this.Info.Quests);  // 从DB里把网络的数据填充上

            this.FriendManager = new FriendManager(this);
            this.FriendManager.GetFriendInfos(this.Info.Friends);

            this.Guild = GuildManager.Instance.GetGuild(this.Data.GuildId);

            this.Chat = new Chat(this);

            this.StatusManager = new StatusManager(this);
        }

        public long Gold
        {
            // 如果直接“取”金币的数值，就从数据库中调
            get { return this.Data.Gold; }
            // 如果对金币进行“赋值”，就把金币的状态变化值传给StatusManager
            set
            {
                if (this.Data.Gold == value)
                    return;
                this.StatusManager.AddGoldChange((int)(value - this.Data.Gold));
                this.Data.Gold = value;
            }
        }

        /// <summary>
        /// 后处理
        /// </summary>
        /// <param name="message"></param>
        public void PostProcess(NetMessageResponse message)
        {
            // Log.InfoFormat("[Character] PostProcess > Character: characterID:{0}:{1}", this.Id, this.Info.Name);

            // 好友后处理
            this.FriendManager.PostProcess(message);

            // 组队后处理
            if (this.Team != null)
            {
                Log.InfoFormat("[Character] PostProcess > Team: characterID:{0}:{1}  {2}<{3}", this.Id, this.Info.Name, TeamUpdateTS, this.Team.timestamp);
                if (TeamUpdateTS < this.Team.timestamp)
                {
                    // 只要角色自己的队伍时间戳 < 队伍的时间戳，说明队伍信息有变化，就执行Team中的后处理发送新的 TeamInfoResponse 给客户端更新队伍UI
                    TeamUpdateTS = Team.timestamp; 
                    this.Team.PostProcess(message);
                }
            }

            // 公会后处理
            if (this.Guild != null)
            {
                Log.InfoFormat("PostProcess > Guild: characterID:{0}:{1}  {2}<{3}", this.Id, this.Info.Name, GuildUpdateTS, this.Guild.timestamp);
                if (this.Info.Guild == null)
                {
                    this.Info.Guild = this.Guild.GuildInfo(this);
                    if (message.mapCharacterEnter != null)
                        GuildUpdateTS = Guild.timestamp;
                }
                if (GuildUpdateTS < this.Guild.timestamp && message.mapCharacterEnter == null)
                {
                    GuildUpdateTS = Guild.timestamp;
                    this.Guild.PostProcess(this, message);
                }
            }

			// 状态管理器后处理
            if (this.StatusManager.HasStatus)
            {
                this.StatusManager.PostProcess(message);
            }

            // 聊天管理器后处理
            this.Chat.PostProcess(message);
        }

        /// <summary>
        /// 角色离开游戏时调用
        /// </summary>
        public void Clear()
        {
            // 通知好友自己下线
            this.FriendManager.OfflineNotify();
        }

        public NCharacterInfo GetBasicInfo()
        {
            return new NCharacterInfo()
            {
                Id = this.Id,
                Name = this.Info.Name,
                Class = this.Info.Class,
                Level = this.Info.Level
            };
        }
    }
}
