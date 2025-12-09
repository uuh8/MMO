using Common.Data;
using GameServer.Core;
using GameServer.Managers;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Entities
{
    class Character : CharacterBase
    {
        public TCharacter Data;
        public ItemManager ItemManager;
        public QuestManager QuestManager;
        public StatusManager StatusManager;

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

            this.Info = new NCharacterInfo();
            this.Info.Type = type;
            this.Info.Id = cha.ID;
            this.Info.Name = cha.Name;
            this.Info.Level = 10;
            this.Info.Tid = cha.TID;
            this.Info.Class = (CharacterClass)cha.Class;
            this.Info.mapId = cha.MapID;
            this.Info.Gold = cha.Gold;
            this.Info.Entity = this.EntityData; 

            this.Define = DataManager.Instance.Characters[this.Info.Tid];

            this.ItemManager = new ItemManager(this);
            this.ItemManager.GetItemInfos(this.Info.Items);

            this.Info.Bag = new NBagInfo();
            this.Info.Bag.Unlocked = this.Data.Bag.Unlocked; 
            this.Info.Bag.Items = this.Data.Bag.Items;

            this.Info.Equips = this.Data.Equips;

            this.QuestManager = new QuestManager(this);         // 构建实例
            this.QuestManager.GetQuestInfos(this.Info.Quests);  // 从DB里把网络的数据填充上

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
    }
}
