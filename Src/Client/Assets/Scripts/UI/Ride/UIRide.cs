using Managers;
using Models;
using SkillBridge.Message;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRide : UIWindow
{
    public Text descript;
    public GameObject itemPrefab;
    public ListView listMain;
    public UIRideItem selectedItem;

    // Start is called before the first frame update
    void Start()
    {
        RefreshUI();
        this.listMain.onItemSelected += this.OnItemSelected;
    }

    public void OnItemSelected(ListView.ListViewItem item)
    {
        this.selectedItem = item as UIRideItem;
        this.descript.text = this.selectedItem.item.Define.Description;
    }

    private void RefreshUI()
    {
        ClearItems(); 
        InitRideItems(); 
    }

    /// <summary>
    /// 初始化所有装备列表
    /// </summary>
    /// <exception cref="NotImplementedException"></exception>
    private void InitRideItems()
    {
        foreach (var kv in ItemManager.Instance.Items)
        {
            if (kv.Value.Define.Type == ItemType.Ride && kv.Value.Define.LimitClass == CharacterClass.None || kv.Value.Define.LimitClass == User.Instance.CurrentCharacter.Class)
            {
                GameObject go = Instantiate(itemPrefab, this.listMain.transform);
                UIRideItem ui = go.GetComponent<UIRideItem>();
                ui.SetRideItem(kv.Value, this, false);
                this.listMain.AddItem(ui);
            }
        }
    }

    private void ClearItems()
    {
        this.listMain.RemoveAll();
    }

    /// <summary>
    /// 绑定“召唤”按钮
    /// </summary>
    public void DoRide()
    {
        if(this.selectedItem == null)
        {
            MessageBox.Show("请选择要召唤的坐骑", "提示");
            return;
        }
        User.Instance.Ride(this.selectedItem.item.Id);
    }
}
