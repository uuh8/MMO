using JetBrains.Annotations;
using Models;
using Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UITeam : MonoBehaviour
{
    public Text teamTitle;
    public UITeamItem[] Members;
    public ListView list;

    // Start is called before the first frame update
    void Start()
    {
        // 组队界面打开的时候需要先校验是否已经有Team
        if(User.Instance.TeamInfo == null)
        {
            // 没有队伍的时候默认隐藏
            this.gameObject.SetActive(false);
            return;
        }
        foreach(var item in Members)
        {
            this.list.AddItem(item);
        }
    }
    /// <summary>
    /// OnEnable 是每当对象被启用时都调用（Start只是脚本实例启动后第一帧前调用）
    /// </summary>
    void OnEnable()
    {
        UpdateTeamUI();
    }

    public void ShowTeam(bool show)
    {
        this.gameObject.SetActive(show);
        if (show)
        {
            UpdateTeamUI();
        }
    }

    public void UpdateTeamUI()
    {
        if (User.Instance.TeamInfo == null)
            return;
        this.teamTitle.text = string.Format("我的队伍（{0}/5）", User.Instance.TeamInfo.Members.Count);
        // 队伍最多4人
        for(int i = 0; i < 4; i++)
        {
            if(i < User.Instance.TeamInfo.Members.Count)
            {
                this.Members[i].SetMemberInfo(
                    i, 
                    User.Instance.TeamInfo.Members[i], 
                    User.Instance.TeamInfo.Members[i].Id == User.Instance.TeamInfo.Leader
                    );
                this.Members[i].gameObject.SetActive(true);
            }
            else
            {
                this.Members[i].gameObject.SetActive(true);
            }
        }
    }
    /// <summary>
    /// 离开Team
    /// </summary>
    public void OnClickLeave()
    {
        MessageBox.Show("确认要离开队伍吗？", "退出队伍", MessageBoxType.Confirm, "确定离开", "取消").OnYes = () =>
        {
            TeamService.Instance.SendTeamLeaveRequest(User.Instance.TeamInfo.Id);
        };
    }
}
