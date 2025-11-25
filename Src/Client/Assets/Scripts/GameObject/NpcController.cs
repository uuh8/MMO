using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Common.Data;
using Managers;
using UnityEditor;
using Models;

public class NpcController : MonoBehaviour
{
    public int npcID;

    SkinnedMeshRenderer renderer;
    private Animator anim;
    Color orignColor;

    private bool inInteractive = false;

    private NpcDefine npc;

    void Start()
    {
        renderer = this.gameObject.GetComponentInChildren<SkinnedMeshRenderer>();
        anim = this.gameObject.GetComponent<Animator>();
        orignColor = renderer.sharedMaterial.color;
        npc = NPCManager.Instance.GetNpcDefine(this.npcID);
        this.StartCoroutine(Actions());
    }
    IEnumerator Actions()
    {
        // npc 闲置动作协程
        while (true)
        {
            if (inInteractive)
                yield return new WaitForSeconds(2f);
            else
                yield return new WaitForSeconds(Random.Range(5f, 10f));
            // npc的Idle动作
            this.Relax();
        }
    }

    /// <summary>
    /// npc 闲置动作
    /// </summary>
    void Relax()
    {
        anim.SetTrigger("Relax");
    }

    #region 与npc接触鼠标高亮
    void OnMouseEnter()
    {
        Highlight(true);
    }
    void OnMouseOver()
    {
        Highlight(true);
    }
    void OnMouseExit()
    {
        Highlight(false);
    }
    /// <summary>
    /// 鼠标移动高光
    /// </summary>
    /// <param name="highlight"></param>
    private void Highlight(bool highlight)
    {
        if (highlight)
        {
            if (renderer.sharedMaterial.color != Color.white)
                renderer.sharedMaterial.color = Color.white;
        }
        else
        {
            if (renderer.sharedMaterial.color != orignColor)
                renderer.sharedMaterial.color = orignColor;
        }
    }

    #endregion

    #region 与npc交互
    /// <summary>
    /// 点击后调用，与npc进行交互
    /// </summary>
    IEnumerator DoInteractice()
    {
        yield return FaceToPlayer();
        // 把npc交互请求发送给 NPCManager
        if (NPCManager.Instance.Interactive(npc))
        {
            anim.SetTrigger("Talk");
        }
        // 结束之后3s之内无法重复点击该npc
        yield return new WaitForSeconds(3f);
        inInteractive = false;
    }
    IEnumerator FaceToPlayer()
    {
        Vector3 faceTo = (User.Instance.CurrentCharacterObject.transform.position - this.transform.position).normalized;
        while (Mathf.Abs(Vector3.Angle(this.gameObject.transform.forward, faceTo)) > 5)
        {
            // 这里用插值目的是让npc慢慢的转，而不是瞬间转过去
            this.gameObject.transform.forward = Vector3.Lerp(this.gameObject.transform.forward, faceTo, Time.deltaTime * 5f);
            yield return null;
        }
    }
    void OnMouseDown()
    {
        Interactive();
    }
    private void Interactive()
    {
        // 这个判断是为了防止用户连着点结果连着跳出窗口
        if (!inInteractive)
        {
            inInteractive = true;
            StartCoroutine(DoInteractice());
        }
    }
    #endregion

}
