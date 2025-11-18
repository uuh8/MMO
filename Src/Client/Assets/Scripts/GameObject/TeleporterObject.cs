using Common.Data;
using Services;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TeleporterObject : MonoBehaviour
{
    public int ID;
    Mesh mesh = null;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
    }

#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        if(this.mesh != null)
        {
            Gizmos.DrawWireMesh(this.mesh, this.transform.position + Vector3.up * this.transform.localScale.y * .5f, this.transform.rotation, this.transform.localScale);
        }
        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.ArrowHandleCap(0, this.transform.position, this.transform.rotation, 1f, EventType.Repaint);
    }
#endif

    void OnTriggerEnter(Collider other)
    {
        PlayerInputController playerController = other.GetComponent<PlayerInputController>();
        if(playerController != null && playerController.isActiveAndEnabled)
        {
            TeleporterDefine td = DataManager.Instance.Teleporters[this.ID];
            if(td == null)
            {
                Debug.LogErrorFormat("[TeleporterObject] TeleporterObject: Character [{0}] 进入传送门 [{1}], 但传送门不存在", playerController.character.Info.Name, td.ID);
                return;
            }

            Debug.LogFormat("[TeleporterObject] TeleporterObject: Character [{0}] 进入传送门 [{1}:{2}]", playerController.character.Info.Name, td.ID, td.Name);
            if(td.LinkTo > 0)
            {
                if (DataManager.Instance.Teleporters.ContainsKey(td.LinkTo))
                    MapService.Instance.SendMapTeleport(this.ID);
                else
                    Debug.LogErrorFormat("[TeleporterObject] Teleporter ID:{0} LinkID {1} 出错！", td.ID, td.LinkTo);
            }
        }
    }
}
