using System.Collections;
using System.Collections.Generic;
using UnityEngine;


/*用于更新*/
public class UIWorldElement : MonoBehaviour
{
    public Transform owner;         // 用于跟踪

    public float height = 2.0f;     // 该元素距离“地面”有多高

    // Use this for initialization
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        if (owner != null)
        {
            this.transform.position = owner.position + Vector3.up * height;
        }

        if (Camera.main != null)
            this.transform.forward = Camera.main.transform.forward;
    }
}
