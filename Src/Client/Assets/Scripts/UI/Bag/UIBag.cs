using Managers;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIBag : UIWindow
{
    public Text money;

    public Transform[] pages;

    public GameObject bagItem;  // 一个格子里面放了什么图标 Icon\文本 等

    List<Image> slots;  // 整个背包共有多少个格子

    // Start is called before the first frame update
    void Start()
    {
        // 共有多少个格子
        if(slots == null)
        {
            slots = new List<Image>();
            // 页数
            for(int page = 0; page < this.pages.Length; page++)
            {
                // 一页有多少个格子，添加进去
                slots.AddRange(this.pages[page].GetComponentsInChildren<Image>(true));
            }
        }

        StartCoroutine(InitBags());
    }

    IEnumerator InitBags()
    {
        for(int i = 0; i < BagManager.Instance.Items.Length; i++)
        {
            var item = BagManager.Instance.Items[i];
            if(item.ItemId > 0)
            {
                GameObject go = Instantiate(bagItem, slots[i].transform);
                var ui = go.GetComponent<UIIconItem>();
                var def = ItemManager.Instance.Items[item.ItemId].Define;
                ui.SetMainIcon(def.Icon, item.Count.ToString());    // 拿两个字段：图标和数量
            }
        }

        // 把还没解锁的道具格子变灰色
        for(int i = BagManager.Instance.Items.Length; i < slots.Count; i++)
        {
            slots[i].color = Color.gray;
        }
        yield return null;
    }

    public void SetTitle(string title)
    {
        this.money.text = User.Instance.CurrentCharacter.Id.ToString();
    }
    
    public void OnReset()
    {
        BagManager.Instance.Reset();
    }
}
