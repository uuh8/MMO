using Common.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SkillBridge.Message;
using UnityEngine.Advertisements.Utilities;

namespace Models
{
    public class Quest
    {
        public QuestDefine Define;  // 本地配置信息
        public NQuestInfo Info;     // 网络信息

        // 构造函数
        public Quest() { }
        public Quest(NQuestInfo info)
        {
            this.Info = info;
            this.Define = DataManager.Instance.Quests[info.QuestId];
        }
        public Quest(QuestDefine define)
        {
            this.Define = define;
            this.Info = null;
        }

        /*public string GetTypeName()
        {
            return EnumUtilities.GetEnumDescriptioin(this.Define.Type);
        }*/
    }
}
