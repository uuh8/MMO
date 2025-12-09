using Entities;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UINameBar : MonoBehaviour
{
    public Text avaverName;
    public Character character;

    // Use this for initialization
    void Start()
    {
        if (this.character != null)
        {

        }
    }

    // Update is called once per frame
    void Update()
    {
        this.UpdateInfo();

        // 获取主摄像机
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogWarning("未找到主摄像机！");
            return;
        }

        
    }

    void UpdateInfo()
    {
        if (this.character != null)
        {
            string name = this.character.Name + " Lv." + this.character.Info.Level;
            // 因为该方法是在Update中调用，如果每次都赋值会影响性能，因此这儿判断可以优化性能
            if (name != this.avaverName.text)
            {
                this.avaverName.text = name;
            }
        }
    }
}
