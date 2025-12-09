using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;
using Models;
using Managers;
using Common.Data;

public class UIQuestSystem : UIWindow
{
    public Text title;

    public GameObject itemPrefab;

    public TabView Tabs;
    public ListView listMain;
    public ListView listBranch;

    public UIQuestInfo questInfo;

    private bool showAvailableList = false;

    void Start()
    {
        this.listMain.onItemSelected += this.OnQuestSelected;    
        this.listBranch.onItemSelected += this.OnQuestSelected;
        this.Tabs.OnTabSelect += OnSelectTab;
        RefreshUI();
    }

    void OnSelectTab(int idx)
    {
        showAvailableList = idx == 1;
        RefreshUI();
    }

    private void RefreshUI()
    {
        ClearAllQuestList();
        InitAllQuestItems();
    }

    private void InitAllQuestItems()
    {
        foreach(var kv in QuestManager.Instance.allQuests)
        {
            // 判断是否是一个可接任务（是否已接取）
            if (showAvailableList)
            {
                if (kv.Value.Info != null)  
                    continue;
            }
            else
            {
                if (kv.Value.Info == null)
                    continue;
            }

            // 创建列表
            GameObject go = Instantiate(itemPrefab, kv.Value.Define.Type == QuestType.Main ? this.listMain.transform : this.listBranch.transform);
            UIQuestItem ui = go.GetComponent<UIQuestItem>();
            ui.SetQuestInfo(kv.Value);
            if (kv.Value.Define.Type == QuestType.Main)
                this.listMain.AddItem(ui);
            else
                this.listBranch.AddItem(ui);
        }
    }
    private void ClearAllQuestList()
    {
        this.listMain.RemoveAll();
        this.listBranch.RemoveAll();
    }

    public void OnQuestSelected(ListView.ListViewItem item)
    {
        UIQuestItem questItem = item as UIQuestItem;
        this.questInfo.SetQuestInfo(questItem.quest);
    }
}