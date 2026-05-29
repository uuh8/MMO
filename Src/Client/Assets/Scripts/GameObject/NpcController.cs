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

        // 用 Inspector 里设置的 npcID 查配置表，拿到这个NPC的完整定义
        npc = NPCManager.Instance.GetNpcDefine(this.npcID);

        // 把自己的世界坐标注册进 NPCManager 的位置字典
        // 任务系统的导航按钮需要用这个坐标来寻路
        NPCManager.Instance.UpdateNpcPosition(this.npcID, this.transform.position);

        // 启动随机动画协程
        this.StartCoroutine(Actions());

        // 查询当前任务状态，刷新头顶图标
        RefreshNpcStatus();

        // 订阅任务状态变化事件
        QuestManager.Instance.onQuestStatusChanged += OnQuestStatusChanged;
    }
    void Update()
    {
        if (User.Instance.CurrentCharacterObject == null) return;

        // 每帧检测玩家和 NPC 的距离
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
            inInteractive = true;   // npc进入交互中状态（防止重复点击导致多次交互）
            StartCoroutine(DoInteractice());
        }
    }

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

            // 播放 Relax 动画
            this.Relax();   
        }
    }
    void Relax() { anim.SetTrigger("Relax"); }

    IEnumerator DoInteractice()
    {
        // 第一步：NPC 转身朝向玩家
        yield return FaceToPlayer();

        // 第二步：执行交互分发
        if (NPCManager.Instance.Interactive(npc))
            anim.SetTrigger("Talk");

        // 第三步：等待3秒后重置交互状态
        yield return new WaitForSeconds(3f);
        inInteractive = false;
    }

    IEnumerator FaceToPlayer()
    {
        Vector3 faceTo = (User.Instance.CurrentCharacterObject.transform.position
                         - this.transform.position).normalized;

        // 每帧用 Lerp 插值旋转，直到夹角小于5度
        while (Mathf.Abs(Vector3.Angle(this.gameObject.transform.forward, faceTo)) > 5)
        {
            this.gameObject.transform.forward = Vector3.Lerp(
                this.gameObject.transform.forward, faceTo, Time.deltaTime * 5f);
            yield return null;  // 等一帧继续转
        }
    }
}
