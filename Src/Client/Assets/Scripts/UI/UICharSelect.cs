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

    //��ǰչʾģ��
    private GameObject currentModel;

    // Start is called before the first frame update
    void Start()
    {
        // ȷ����ɫ�����Ѽ���
        DataManager.Instance.Load();
        // ���ó�ʼģ��
        currentModel = warriorModel;
        //��ʼ��ģ��
        warriorModel.SetActive(true);
        archerModel.SetActive(false);
        magicianModel.SetActive(false);

        // �󶨰�ť����¼�
        warrior_bt.onClick.AddListener(OnClickWarrior);
        archer_bt.onClick.AddListener(OnClickArcher);
        magician_bt.onClick.AddListener(OnClickMagician);
    }


    void Update()
    {

    }

    /*�����ɫ��ť*/
    private void ChangeChar(GameObject model, int characterClass)
    {
        // �� int ת��Ϊ CharacterClass ö������
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
