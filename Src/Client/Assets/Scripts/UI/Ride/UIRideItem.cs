using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIRideItem : ListView.ListViewItem
{
    public Image icon;
    public Text Name;
    public Text level;

    public Image background;
    public Sprite normalBg;
    public Sprite selectedBg;

    public override void onSelected(bool selected)
    {
        this.background.overrideSprite = selected ? selectedBg : normalBg;
    }

    public Item item;

    public void SetRideItem(Item item, UIRide owner, bool rided)
    {
        this.item = item;

        if (this.Name != null) this.Name.text = this.item.Define.Name;
        if (this.level != null) this.level.text = item.Define.Level.ToString();
        if (this.icon != null) this.icon.overrideSprite = Resloader.Load<Sprite>(this.item.Define.Icon);
    }
}
