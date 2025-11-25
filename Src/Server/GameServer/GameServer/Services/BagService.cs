using Common;
using GameServer.Entities;
using Network;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Services
{
    class BagService : Singleton<BagService>
    {
        public BagService()
        {
            MessageDistributer<NetConnection<NetSession>>.Instance.Subscribe<BagSaveRequest>(this.OnBagSave);
        }
        public void Init()
        { 

        }
        /// <summary>
        /// 保存客户端发来的更新的背包信息到 数据库
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="request"></param>
        void OnBagSave(NetConnection<NetSession> sender, BagSaveRequest request)
        {
            Character character = sender.Session.Character;
            Log.InfoFormat("[BagService] BagSaveRequest: character: {0} : Unlocked {1}", character.Id, request.BagInfo.Unlocked);

            if(request.BagInfo != null)
            {
                character.Data.Bag.Items = request.BagInfo.Items;
                DBService.Instance.Save();
            }
        }
    }
}
