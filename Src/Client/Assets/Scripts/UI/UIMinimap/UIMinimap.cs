using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using Managers;

public class UIMinimap : MonoBehaviour
{
    public Collider minimapBoundingBox; // 通过包围盒拿到地图的长宽高
    public Image minimap;
    public Image arrow;
    public Text mapName;

    private Transform playerTransform;  // 角色的游戏对象
    // Start is called before the first frame update
    void Start()
    {
        InitMap();
    }

    void InitMap()
    {
        this.mapName.text = User.Instance.CurrentMapData.Name;
        if(this.minimap.overrideSprite == null)
        {
            this.minimap.overrideSprite = MinimapManager.Instance.LoadCurrentMinimap();
        };
        this.minimap.SetNativeSize();
        minimap.transform.localPosition = Vector3.zero;
    }

    // Update is called once per frame
    void Update()
    {
        if(playerTransform == null)
        {
            playerTransform = MinimapManager.Instance.PlayerTransform;
        }
        if (minimapBoundingBox == null || playerTransform == null) return;

        float realWidth = minimapBoundingBox.bounds.size.x;
        float realHeight = minimapBoundingBox.bounds.size.z;

        // 角色在地图中的相对坐标
        float relaX = playerTransform.position.x - minimapBoundingBox.bounds.min.x;
        float relaY = playerTransform.position.z - minimapBoundingBox.bounds.min.z;

        // 利用角色的pivot中心点来位移小地图的image
        float pivotX = relaX / realWidth;
        float pivotY = relaY / realHeight;

        // 小地图移动
        this.minimap.rectTransform.pivot = new Vector2(pivotX, pivotY);
        this.minimap.rectTransform.localPosition = Vector2.zero;

        // 箭头转圈
        this.arrow.transform.eulerAngles = new Vector3(0, 0, -playerTransform.eulerAngles.y);
    }
}
