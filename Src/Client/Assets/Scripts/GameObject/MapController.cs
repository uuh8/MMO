using Managers;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;

public class MapController : MonoBehaviour
{
    public Collider minimapBoundingBox;

    void Start()
    {
        MinimapManager.Instance.UpdateMinimap(minimapBoundingBox);
    }

}
