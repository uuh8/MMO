using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SkillBridge.Message;
using System;

public class UICharSelect : MonoBehaviour
{
    public GameObject warriorModel;
    public GameObject archerModel;
    public GameObject magicianModel;

    CharacterClass charClass;

    public Button warrior_bt;
    public Button archer_bt;
    public Button magician_bt;

    public Text char_intros;

    //当前展示模型
    private GameObject currentModel;

    // Start is called before the first frame update
    void Start()
    {
        // 确保角色数据已加载
        DataManager.Instance.Load();
        // 设置初始模型
        currentModel = warriorModel;
        //初始化模型
        warriorModel.SetActive(true);
        archerModel.SetActive(false);
        magicianModel.SetActive(false);

        // 绑定按钮点击事件
        warrior_bt.onClick.AddListener(OnClickWarrior);
        archer_bt.onClick.AddListener(OnClickArcher);
        magician_bt.onClick.AddListener(OnClickMagician);
    }


    void Update()
    {

    }

    /*点击角色按钮*/
    private void ChangeChar(GameObject model, int characterClass)
    {
        // 将 int 转换为 CharacterClass 枚举类型
        this.charClass = (CharacterClass)characterClass;

        if (currentModel != model)
        {
            currentModel.SetActive(false);
            currentModel = model;
            currentModel.SetActive(true);
        }


        char_intros.text = DataManager.Instance.Characters[(int)this.charClass].Description;

    }
    public void OnClickWarrior()
    {
        ChangeChar(warriorModel, CharacterClass.Warrior);
    }
    public void OnClickArcher() 
    {
        ChangeChar(archerModel, CharacterClass.Archer);
    } 
    public void OnClickMagician()
    {
        ChangeChar(magicianModel, CharacterClass.Wizard);
    } 
}
