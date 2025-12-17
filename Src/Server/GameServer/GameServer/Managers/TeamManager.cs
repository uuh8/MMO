using Common;
using GameServer.Entities;
using GameServer.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace GameServer.Managers
{
    class TeamManager : Singleton<TeamManager>
    {
        // 列表方便遍历使用
        public List<Team> Teams = new List<Team>();
        // key 是 characterId，value 是队伍
        public Dictionary<int, Team> CharacterTeams = new Dictionary<int, Team>();

        public void Init()
        {

        }

        /// <summary>
        /// 通过 characterId 获取队伍
        /// </summary>
        /// <param name="characterId"></param>
        /// <returns></returns>
        public Team GetTeamByCharacter(int characterId)
        {
            Team team = null;
            this.CharacterTeams.TryGetValue(characterId, out team);
            return team;
        }

        public void AddTeamMember(Character leader, Character member)
        {
            if(leader.Team == null)
            {
                leader.Team = CreateTeam(leader);
            }
            leader.Team.AddMember(member);
        }
        /// <summary>
        /// 创建队伍
        /// </summary>
        /// <param name="leader"></param>
        /// <returns></returns>
        private Team CreateTeam(Character leader)
        {
            // 为了防止队伍频繁创建和销毁导致的内存开销，做了一个设计，队伍列表Teams只会增加不会减少，没有member的队伍只是一个空队伍，而不是销毁
            Team team = null;
            for(int i = 0; i < this.Teams.Count; i++)
            {
                team = this.Teams[i];   // 存入数据结构
                if(team.Members.Count == 0)
                {
                    // 遍历队伍列表Teams，如果找到了一个空队伍，添加自己
                    team.AddMember(leader);
                    return team;
                }
            }
            // 当前Teams中没有空队伍了，才创建新Team
            team = new Team(leader);
            this.Teams.Add(team);
            team.Id = this.Teams.Count; // 由于Team数量只增不减，因此可以用 Count 来表示Team Id

            return team;
        }
    }
}
