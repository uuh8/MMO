using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common.Data;
using Managers;
using Models;
using SkillBridge.Message;

public class NpcController : MonoBehaviour
{
    public int npcID;
    public float interactDistance = 2.5f; // 触发交互提示的距离阈值

    private Animator anim;
    private bool inInteractive = false;
    private NpcDefine npc;
    NpcQuestStatus questStatus;

    // 当前是否在交互范围内
    private bool isInRange = false;


    void Start()
    {
        anim = this.gameObject.GetComponent<Animator>();
        npc = NPCManager.Instance.GetNpcDefine(this.npcID);
        NPCManager.Instance.UpdateNpcPosition(this.npcID, this.transform.position);
        this.StartCoroutine(Actions());
        RefreshNpcStatus();
        QuestManager.Instance.onQuestStatusChanged += OnQuestStatusChanged;
    }
    void Update()
    {
        // 每帧检测玩家和 NPC 的距离
        if (User.Instance.CurrentCharacterObject == null) return;

        float dist = Vector3.Distance(
            this.transform.position,
            User.Instance.CurrentCharacterObject.transform.position
        );

        if (dist <= interactDistance)
        {
            // 进入范围：显示提示，注册自己为当前可交互 NPC
            if (!isInRange)
            {
                isInRange = true;
                NPCManager.Instance.OnNpcEnterRange(this);
            }
        }
        else
        {
            // 离开范围：隐藏提示，取消注册
            if (isInRange)
            {
                isInRange = false;
                NPCManager.Instance.OnNpcLeaveRange(this);
            }
        }
    }

    private void OnDestroy()
    {
        // 销毁时确保清理状态
        if (isInRange)
            NPCManager.Instance.OnNpcLeaveRange(this);

        QuestManager.Instance.onQuestStatusChanged -= OnQuestStatusChanged;
        if (UIWorldElementManager.Instance != null)
            UIWorldElementManager.Instance.RemoveNpcQuestStatus(this.transform);
    }

    // 供 NPCInteractManager 调用的交互入口
    public void TryInteract()
    {
        if (!inInteractive)
        {
            inInteractive = true;
            StartCoroutine(DoInteractice());
        }
    }

    // 以下保持不变
    private void OnQuestStatusChanged(Quest quest) { RefreshNpcStatus(); }

    private void RefreshNpcStatus()
    {
        questStatus = QuestManager.Instance.GetQuestStatusByNpc(this.npcID);
        UIWorldElementManager.Instance.AddNpcQuestStatus(this.transform, questStatus);
    }

    IEnumerator Actions()
    {
        while (true)
        {
            if (inInteractive)
                yield return new WaitForSeconds(2f);
            else
                yield return new WaitForSeconds(Random.Range(5f, 10f));
            this.Relax();
        }
    }

    void Relax() { anim.SetTrigger("Relax"); }

    IEnumerator DoInteractice()
    {
        yield return FaceToPlayer();
        if (NPCManager.Instance.Interactive(npc))
            anim.SetTrigger("Talk");
        yield return new WaitForSeconds(3f);
        inInteractive = false;
    }

    IEnumerator FaceToPlayer()
    {
        Vector3 faceTo = (User.Instance.CurrentCharacterObject.transform.position
                         - this.transform.position).normalized;
        while (Mathf.Abs(Vector3.Angle(this.gameObject.transform.forward, faceTo)) > 5)
        {
            this.gameObject.transform.forward = Vector3.Lerp(
                this.gameObject.transform.forward, faceTo, Time.deltaTime * 5f);
            yield return null;
        }
    }
}
