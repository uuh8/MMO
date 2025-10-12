using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UICharacterView : MonoBehaviour {
    public GameObject[] characters; // 需要在Unity中拖入
    private int currentCharacter = 1;

    public int CurrectCharacter
    {
        get{ return currentCharacter; }
        set
        {
            if (value >= 0 && value < characters.Length)
            {
                currentCharacter = value;
                Debug.Log($"[UICharacterView] currentCharacter的值是：{currentCharacter}");
                this.UpdateCharacter();
            }
            else
            {
                Debug.LogWarning($"[UICharacterView] 试图设置无效的character索引: {value}");
            }
        }
    }

    void UpdateCharacter()
    {
        for(int i = 0; i < characters.Length; i++)
        {
            characters[i].SetActive(i == this.currentCharacter);
        }
    }
}
