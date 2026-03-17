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
        QuestManager.Instance.onQuestStatusChanged += OnQuestStatusChanged;
        RefreshUI();
    }
    void OnDestroy()
    {
        QuestManager.Instance.onQuestStatusChanged -= OnQuestStatusChanged; // ★ 防内存泄漏
    }
    private void OnQuestStatusChanged(Quest quest)
    {
        RefreshUI();
    }

    public void OnQuestSelected(ListView.ListViewItem item)
    {
        // 把抽象的 ListViewItem 转成具体的 UIQuestItem，读出它绑定的 quest，然后丢给右侧的 UIQuestInfo
        UIQuestItem questItem = item as UIQuestItem;
        this.questInfo.SetQuestInfo(questItem.quest);
    }

    private void OnSelectTab(int idx)
    {
        showAvailableList = (idx == 1);   // 0=进行中, 1=可接取
        RefreshUI();
    }

    private void RefreshUI()
    {
        ClearAllQuestList();    // 清空主线/支线两个列表
        InitAllQuestItems();    // 重建两个列表
    }

    private void InitAllQuestItems()
    {
        foreach(var kv in QuestManager.Instance.allQuests)
        {
            // 判断是否是一个可接任务（是否已接取）
            if (showAvailableList)
            {
                // "可接取”界面
                // 已接取的不显示，直接跳过
                if (kv.Value.Info != null)  
                    continue;
            }
            else
            {
                // “进行中”界面
                // 未接取的不显示，直接跳过
                if (kv.Value.Info == null)
                    continue;
            }

            // 再按主线/支线，决定往哪个 ListView 里塞
            Transform parent = kv.Value.Define.Type == QuestType.Main
                ? this.listMain.transform
                : this.listBranch.transform;

            GameObject go = Instantiate(itemPrefab, parent, false);
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
}