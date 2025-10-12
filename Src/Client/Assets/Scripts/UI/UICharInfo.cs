using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UICharInfo : MonoBehaviour {
    public SkillBridge.Message.NCharacterInfo CharacterInfo;

    public Text charClass;
    public Text charName;
    public Image highlight;

    public bool Selected
    {
        get { return highlight.IsActive(); }
        set
        {
            highlight.gameObject.SetActive(value);
        }
    }

    // Use this for initialization
    void Start () {
		if(CharacterInfo != null)
        {
            this.charClass.text = this.CharacterInfo.Class.ToString();
            this.charName.text = this.CharacterInfo.Name;
        }
	}
	
	// Update is called once per frame
	void Update () {
		
	}
}
