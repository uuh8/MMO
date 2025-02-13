using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SkillBridge.Message;
using System;
using Common;
using UnityEngine.EventSystems;
using Services;

public class UICharSelect : MonoBehaviour
{
    public GameObject warriorModel;
    public GameObject archerModel;
    public GameObject magicianModel;

    // CharacterClass charClass;

    public Button warrior_bt;
    public Button archer_bt;
    public Button magician_bt;

    // 创建角色
    public Button create_char_bt;

    public Text char_intros;

    // 当前显示模型
    private GameObject currentModel;

    private Dictionary<CharacterClass, GameObject> characterModels;

    // 选择框
    public GameObject select_frame;
    private GameObject warriorSelectFrame;
    private GameObject archerSelectFrame;
    private GameObject magicianSelectFrame;

    private int currentSelectedIndex = -1; // 跟踪当前选中的职业索引
    /*——————————————————————————————————————————————————————————————————————————————————————————————————————————*/
    // Start is called before the first frame update
    void Start()
    {
        UserService.Instance.OnCharCreate = OnCharCreate;

        // 查找子物体
        warriorSelectFrame = select_frame.transform.Find("warrior_select_frame").gameObject;
        archerSelectFrame = select_frame.transform.Find("archer_select_frame").gameObject;
        magicianSelectFrame = select_frame.transform.Find("magician_select_frame").gameObject;

        // 确保角色数据已加载
        DataManager.Instance.Load();
        // 设置初始模型
        currentModel = warriorModel;
        // 初始化模型显示状态
        warriorModel.SetActive(true);
        archerModel.SetActive(false);
        magicianModel.SetActive(false);

        // 初始化角色模型字典
        characterModels = new Dictionary<CharacterClass, GameObject>
        {
            { CharacterClass.Warrior, warriorModel },
            { CharacterClass.Archer, archerModel },
            { CharacterClass.Wizard, magicianModel }
        };

        // 添加事件监听
        AddButtonEvents(warrior_bt, 0, CharacterClass.Warrior);
        AddButtonEvents(archer_bt, 1, CharacterClass.Archer);
        AddButtonEvents(magician_bt, 2, CharacterClass.Wizard);

        // 初始时隐藏所有选择框
        warriorSelectFrame.SetActive(false);
        archerSelectFrame.SetActive(false);
        magicianSelectFrame.SetActive(false);


    }
    /*——————————————————————————————————————————————————————————————————————————————————————————————————————————*/
    // 初始化角色选择
    private void InitCharacterSelect(bool init)
    {

    }

    /*——————————————————————————————————————————————————————————————————————————————————————————————————————————*/

    private void AddButtonEvents(Button button, int positionIndex, CharacterClass charClass)
    {
        // 获取或添加EventTrigger组件
        EventTrigger trigger = button.gameObject.GetComponent<EventTrigger>();
        if (trigger == null)
        {
            trigger = button.gameObject.AddComponent<EventTrigger>();
        }

        // 清除可能存在的旧事件
        trigger.triggers.Clear();

        // 添加鼠标进入事件
        /*EventTrigger.Entry 是 Unity 中 EventTrigger 组件用来描述一个特定事件的一个条目。每个条目都包含了事件的类型 (eventID)，以及事件发生时的回调 (callback)。*/
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        // EventTriggerType 是 Unity 中一个枚举类型，用来定义各种触发的事件类型。PointerEnter 表示当鼠标指针进入某个 UI 元素（如按钮、图片等）时触发该事件。
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback = new EventTrigger.TriggerEvent();
        enterEntry.callback.AddListener((data) => { OnPointerEnter(charClass); });
        trigger.triggers.Add(enterEntry);

        // 添加鼠标离开事件
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback = new EventTrigger.TriggerEvent();
        exitEntry.callback.AddListener((data) => { OnPointerExit(); });
        trigger.triggers.Add(exitEntry);

        // 添加点击事件
        EventTrigger.Entry clickEntry = new EventTrigger.Entry();
        clickEntry.eventID = EventTriggerType.PointerClick;
        clickEntry.callback = new EventTrigger.TriggerEvent();
        clickEntry.callback.AddListener((data) => { OnCharacterSelected(charClass); });
        trigger.triggers.Add(clickEntry);

        // 保留原有的按钮点击事件
        button.onClick.AddListener(() => ChangeChar(charClass));
    }
    /*——————————————————————————————————————————————————————————————————————————————————————————————————————————*/
    //鼠标进入
    private void OnPointerEnter(CharacterClass charClass)
    {
        // 在没有选中职业时，鼠标进入则显示边框；若已选中某个职业，则鼠标进入其他职业则不显示边框
        if (currentSelectedIndex == -1)
        {
            // 根据职业类型显示对应的选择框
            switch (charClass)
            {
                case CharacterClass.Warrior:
                    warriorSelectFrame.SetActive(true);
                    archerSelectFrame.SetActive(false);
                    magicianSelectFrame.SetActive(false);
                    break;
                case CharacterClass.Archer:
                    warriorSelectFrame.SetActive(false);
                    archerSelectFrame.SetActive(true);
                    magicianSelectFrame.SetActive(false);
                    break;
                case CharacterClass.Wizard:
                    warriorSelectFrame.SetActive(false);
                    archerSelectFrame.SetActive(false);
                    magicianSelectFrame.SetActive(true);
                    break;
            }
        }
        // 如果已经选中了职业，则不显示鼠标悬停的边框效果
    }
    //鼠标移出
    private void OnPointerExit()
    {
        // 只有在没有选中职业时，或鼠标离开非选中职业时，才隐藏选择框
        if (currentSelectedIndex == -1)
        {
            warriorSelectFrame.SetActive(false);
            archerSelectFrame.SetActive(false);
            magicianSelectFrame.SetActive(false);
        }
    }

    private void OnCharacterSelected(CharacterClass charClass)
    {
        // 更新当前选中的职业索引
        switch (charClass)
        {
            case CharacterClass.Warrior:
                currentSelectedIndex = 0;
                warriorSelectFrame.SetActive(true);
                archerSelectFrame.SetActive(false);
                magicianSelectFrame.SetActive(false);
                break;
            case CharacterClass.Archer:
                currentSelectedIndex = 1;
                warriorSelectFrame.SetActive(false);
                archerSelectFrame.SetActive(true);
                magicianSelectFrame.SetActive(false);
                break;
            case CharacterClass.Wizard:
                currentSelectedIndex = 2;
                warriorSelectFrame.SetActive(false);
                archerSelectFrame.SetActive(false);
                magicianSelectFrame.SetActive(true);
                break;
        }

        // 处理角色选择逻辑
        ChangeChar(charClass);
    }
    /* 切换角色按钮 */
    private void ChangeChar(CharacterClass characterClass)
    {
        if (characterModels.ContainsKey(characterClass))
        {
            currentModel.SetActive(false);
            currentModel = characterModels[characterClass];
            currentModel.SetActive(true);
        }
        char_intros.text = DataManager.Instance.Characters[(int)characterClass].Description;
    }

    // 这些方法可以保留用于Unity编辑器中的按钮引用
    public void OnClickWarrior()
    {
        ChangeChar(CharacterClass.Warrior);
    }

    public void OnClickArcher()
    {
        ChangeChar(CharacterClass.Archer);
    }

    public void OnClickMagician()
    {
        ChangeChar(CharacterClass.Wizard);
    }

    private void OnCharCreate(Result result, string message)
    {
        if(result == Result.Success)
        {
            InitCharacterSelect(true);
        }
        else
        {
            MessageBox.Show(message,"错误",MessageBoxType.Error);
        }
    }


}
