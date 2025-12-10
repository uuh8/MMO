using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

public class ListView : MonoBehaviour
{
    // 当列表中“选中的项”发生变化时，对外发事件
    public UnityAction<ListViewItem> onItemSelected;

    // 内部的“列表项”基类
    public class ListViewItem : MonoBehaviour, IPointerClickHandler
    {
        private bool selected;

        // 是否被选中
        public bool Selected
        {
            get { return selected; }
            set
            {
                selected = value;
                // 每次选中状态变化，回调 onSelected，
                // 让子类决定自己怎么刷新外观（比如高亮背景）
                onSelected(selected);
            }
        }

        /// <summary>
        /// 供子类重写使用，用来切换 UI 外观
        /// </summary>
        /// <param name="selected"></param>
        public virtual void onSelected(bool selected)
        {
        }

        // 这项 ListViewItem 所属的 ListView
        public ListView owner;

        /// <summary>
        /// 实现 IPointerClickHandler 接口
        /// </summary>
        /// <param name="eventData"></param>
        public void OnPointerClick(PointerEventData eventData)
        {
            // 1）如果自己还没被标记为选中，则先标记选中
            if (!this.selected)
            {
                this.Selected = true;   // 改自己的 Selected 状态
            }
            // 2）通知 owner：把“当前选中项”改成我
            if (owner != null && owner.SelectedItem != this)
            {
                owner.SelectedItem = this;  // 告诉 ListView：我现在是选中项
            }
        }
    }

    // 持有的所有子项
    List<ListViewItem> items = new List<ListViewItem>();

    // 当前选中的那一项
    private ListViewItem selectedItem = null;

    public ListViewItem SelectedItem
    {
        get { return selectedItem; }
        private set
        {
            // 把之前选中的那一项取消选中（它的 Selected setter 会自动刷新外观）
            if (selectedItem != null && selectedItem != value)
            {
                selectedItem.Selected = false;  // 取消旧选中（旧 item 外观置灰）
            }
            // 记录新的选中项
            selectedItem = value;
            // 事件触发，通知外部：列表选中项变了
            if (onItemSelected != null)
                onItemSelected.Invoke((ListViewItem)value);
        }
    }
    /// <summary>
    /// 往列表中加一个 item（不会 Instantiate，只是建立关系）
    /// </summary>
    /// <param name="item"></param>
    public void AddItem(ListViewItem item)
    {
        item.owner = this;      // 反向指针：item 记住自己属于哪个 ListView
        this.items.Add(item);
    }
    /// <summary>
    /// 清空列表：把所有子物体 Destroy 掉，清空列表
    /// </summary>
    public void RemoveAll()
    {
        foreach (var it in items)
        {
            Destroy(it.gameObject);
        }
        items.Clear();
    }
}
