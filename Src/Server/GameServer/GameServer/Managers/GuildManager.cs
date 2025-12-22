using Common;
using GameServer.Entities;
using GameServer.Models;
using GameServer.Services;
using SkillBridge.Message;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class GuildManager : Singleton<GuildManager>
    {
        public Dictionary<int, Guild> Guilds = new Dictionary<int, Guild>();
        private HashSet<string> GuildNames = new HashSet<string>();     // 存储所有公会的公会名称

        public void Init()
        {
            this.Guilds.Clear();
            // 遍历数据库中的公会填充到管理器字典 Guilds 中(缓存，避免频繁查询数据库)
            foreach (var guild in DBService.Instance.Entities.TGuilds)
            {
                this.AddGuild(new Guild(guild));
            }
        }
        /// <summary>
        /// 添加公会
        /// </summary>
        /// <param name="guild"></param>
        public void AddGuild(Guild guild)
        {
            this.Guilds.Add(guild.Id, guild);
            this.GuildNames.Add(guild.Name);
            guild.timestamp = TimeUtil.timestamp;
        }
        /// <summary>
        /// 校验公会名是否已存在
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public bool CheckNameExisted(string name)
        {
            return GuildNames.Contains(name);
        }

        /// <summary>
        /// 创建公会
        /// </summary>
        /// <param name="leader"></param>
        /// <returns></returns>
        public bool CreateGuild(string name, string notice, Character leader)
        {
            // 创建一个db对象并保存到数据库中
            DateTime now = DateTime.Now;
            TGuild dbGuild = DBService.Instance.Entities.TGuilds.Create();
            dbGuild.Name = name;
            dbGuild.Notice = notice;
            dbGuild.LeaderID = leader.Id;
            dbGuild.LeaderName = leader.Name;
            dbGuild.CreateTime = now;
            DBService.Instance.Entities.TGuilds.Add(dbGuild);

            // 
            Guild guild = new Guild(dbGuild);
            guild.AddMember(leader.Id, leader.Name, leader.Data.Class, leader.Data.Level, GuildTitle.President); // 把队长的身份填进去
            leader.Guild = guild;
            DBService.Instance.Save();
            leader.Data.GuildId = dbGuild.Id;
            DBService.Instance.Save();
            this.AddGuild(guild);   // 加入到公会的管理器数据结构中

            return true;
        }

        /// <summary>
        /// 获取公会
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public Guild GetGuild(int? guildId)
        {
            if (!guildId.HasValue) // 没加入公会时 GuildId = null
                return null;

            Guild guild = null;
            this.Guilds.TryGetValue(guildId.Value, out guild);
            return guild;
        }

        /// <summary>
        /// 获取公会清单（所有公会）
        /// </summary>
        /// <returns></returns>
        public List<NGuildInfo> GetGuildsInfo()
        {
            List<NGuildInfo> result = new List<NGuildInfo>();
            foreach (var kv in this.Guilds)
            {
                result.Add(kv.Value.GuildInfo(null));
            }
            return result;
        }
    }
}
