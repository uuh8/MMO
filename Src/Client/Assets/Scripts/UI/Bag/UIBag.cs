using Managers;
using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class UIBag : UIWindow
{
    public Text money;
    public Transform[] pages;
    public GameObject bagItem;  // 一个格子里面放了什么图标 Icon\文本 等

    List<Image> slots;  // 整个背包共有多少个格子

    void Start()
    {
        BagManager.Instance.OnBagUpdate += RefreshBag;
        User.Instance.OnGoldChanged += RefreshGold;

        // 打开背包时立刻同步一次当前金币
        if (User.Instance.CurrentCharacter != null)
            this.money.text = User.Instance.CurrentCharacter.Gold.ToString();

        // 共有多少个格子
        if (slots == null)
        {
            slots = new List<Image>();
            // 页数
            for(int page = 0; page < this.pages.Length; page++)
            {
                // 一页有多少个格子，添加进去
                // 遍历 Content 下面第一层的所有子物体。也就是每个格子的根节点：Image、Image (1)、Image (2) ……
                foreach (Transform child in this.pages[page])
                {
                    // 在当前这个子物体上去查有没有 Image 组件，如果这个子物体身上确实有 Image，就把这个 Image 当作一个“格子”加入 slots 列表。
                    var img = child.GetComponent<Image>();
                    if (img != null)
                        slots.Add(img);
                }
            }
        }
        StartCoroutine(InitBags());
    }
    void OnDestroy()
    {
        BagManager.Instance.OnBagUpdate -= RefreshBag; // 防泄漏
        User.Instance.OnGoldChanged -= RefreshGold;
    }

    // 刷新金币，不重建格子
    private void RefreshGold()
    {
        Debug.Log("[UIBag] RefreshGold called");
        if (User.Instance.CurrentCharacter != null)
            this.money.text = User.Instance.CurrentCharacter.Gold.ToString();
    }

    private void RefreshBag()
    {
        this.money.text = User.Instance.CurrentCharacter.Gold.ToString(); // 更新金币
        this.Clear();
        StartCoroutine(InitBags());
    }

    /// <summary>
    /// 初始化背包
    /// </summary>
    /// <returns></returns>
    IEnumerator InitBags()
    {
        for(int i = 0; i < BagManager.Instance.Items.Length; i++)
        {
            var item = BagManager.Instance.Items[i];
            if(item.ItemId > 0)
            {
                GameObject uiBagItem = Instantiate(bagItem, slots[i].transform, false);
                var uiBagIconItem = uiBagItem.GetComponent<UIIconItem>();
                var def = ItemManager.Instance.Items[item.ItemId].Define;
                uiBagIconItem.SetMainIcon(def.Icon, item.Count.ToString());    // 拿两个字段：图标和数量
            }
        }
        // 把还没解锁的道具格子变灰色
        for(int i = BagManager.Instance.Items.Length; i < slots.Count; i++)
        {
            slots[i].color = Color.gray;
        }
        yield return null;
    }


    public void SetTitle()
    {
        this.money.text = User.Instance.CurrentCharacter.Gold.ToString();
    }

    /// <summary>
    /// 背包整理
    /// </summary>
    public void OnReset()
    {
        BagManager.Instance.Reset();
        this.Clear();
        StartCoroutine(InitBags());
    }
    /// <summary>
    /// 用于背包整理
    /// </summary>
    void Clear()
    {
        for (int i = 0; i < slots.Count; i++)
        {
            if (slots[i].transform.childCount > 0)
            {
                Destroy(slots[i].transform.GetChild(0).gameObject);
            }
        }
    }

}
