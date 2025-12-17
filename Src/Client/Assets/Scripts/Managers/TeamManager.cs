using Models;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Managers
{
    public class TeamManager : Singleton<TeamManager>
    {
        public void Init()
        {

        }

        public void UpdateTeamInfo(NTeamInfo team)
        {
            // 收到网络消息后更新User中的队伍信息
            User.Instance.TeamInfo = team;
            // 更新UI
            ShowTeamUI(team != null);
        }
        public void ShowTeamUI(bool show)
        {
            // 校验主场景中 UIMain 是否存在（防止某些特殊情况例如切换场景的时候 UIMain 已经被销毁了，这种情况就不显示队伍信息）
            if (UIMain.Instance != null)
            {
                UIMain.Instance.ShowTeamUI(show);
            }
        }
    }

}
