using Managers;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIQuestInfo : MonoBehaviour
{
    public Text title;
    public Text[] targets;
    public Text description;
    public UIIconItem rewardItems;
    public Text rewardMoney;
    public Text rewardExp;

    /// 任务导航（自动寻路）
    public Button navButton;
    private int npc = 0;

    public void SetQuestInfo(Quest quest)
    {
        this.title.text = string.Format("[{0}]{1}", quest.Define.Type, quest.Define.Name);
        if (quest.Info == null)
        {
            this.description.text = quest.Define.Dialog;
        }
        else
        {
            if (quest.Info.Status == SkillBridge.Message.QuestStatus.Completed)
            {
                this.description.text = quest.Define.DialogFinish;
            }
        }

        this.rewardMoney.text = quest.Define.RewardGold.ToString();
        this.rewardExp.text = quest.Define.RewardExp.ToString();

        if(quest.Info == null)
        {
            // 任务还没接，导航到“接任务”的npc
            this.npc = quest.Define.AcceptNPC;
        }
        else if( quest.Info.Status == SkillBridge.Message.QuestStatus.Completed)
        {
            // 任务已经完成了，导航到完成任务的npc
            this.npc = quest.Define.SubmitNPC;
        }
        // 如果既没有接，又没有完成，就不显示导航按钮
        this.navButton.gameObject.SetActive(this.npc > 0);

        // 内容设置完成后强制布局一次（刷新ui）
        foreach (var fitter in this.GetComponentsInChildren<ContentSizeFitter>())
        {
            fitter.SetLayoutVertical();
        }
    }
    /// <summary>
    /// 任务界面点击导航按钮自动导航npc
    /// </summary>
    public void OnClickNav()
    {
        Vector3 pos = NPCManager.Instance.GetNpcPosition(this.npc);
        User.Instance.CurrentCharacterObject.StartNav(pos);
        UIManager.Instance.Close<UIQuestSystem>();
    }

}
