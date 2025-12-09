using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIQuestDialog : UIWindow
{
    public UIQuestInfo questInfo;
    public Quest quest;

    // 两个按钮组
    public GameObject openButtons;
    public GameObject SubmitButtons;

    public void SetQuest(Quest quest)
    {
        this.quest = quest;
        this.UpdateQuest();
        // 判断是否是可接任务(新任务）
        if(this.quest.Info == null)
        {
            openButtons.SetActive(true);
            SubmitButtons.SetActive(false);
        }
        else
        {
            // 根据任务是否已完成来决定开哪个按钮
            if(this.quest.Info.Status == SkillBridge.Message.QuestStatus.Completed)
            {
                openButtons.SetActive(false);
                SubmitButtons.SetActive(true);
            }
            else
            {
                openButtons.SetActive(false);
                SubmitButtons.SetActive(false);
            }
        }
    }

    void UpdateQuest()
    {
        if(this.questInfo != null)
        {
            this.questInfo.SetQuestInfo(quest);
        }
    }
}
