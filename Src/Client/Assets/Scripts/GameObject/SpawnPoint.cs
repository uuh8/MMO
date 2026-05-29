using Common.Data;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class SpawnPoint : MonoBehaviour
{
    [Header("刷怪配置")]
    public int ID;

    [Header("巡逻配置")]
    public List<Vector3> patrolPoints = new List<Vector3>();
    public float speed = 3.5f;
    public float stoppingDistance = 0.5f;

    [Header("视野配置")]
    public float viewRadius = 5f;
    [Range(0, 360)]
    public float viewAngle = 90f;

    Mesh mesh = null;

    void Start()
    {
        mesh = GetComponent<MeshFilter>().sharedMesh;
    }

    // ── Gizmos 可视化 ─────────────────────────────────────────────
#if UNITY_EDITOR
    void OnDrawGizmos()
    {
        // 计算一个位置：
        // 1. this.transform.position：获取当前物体的世界坐标位置。
        // 2. Vector3.up * this.transform.localScale.y * .5f：计算物体高度的一半（Y轴方向）。
        //    最终位置是在物体底部的正上方，高度为物体自身高度的一半处（通常是在物体的中心）。
        Vector3 pos = this.transform.position + Vector3.up * this.transform.localScale.y * .5f;
        Gizmos.color = Color.red;
        // 检查当前物体是否引用了网格模型
        if (this.mesh != null)
        {
            // 在场景中绘制一个线框网格。
            // 参数1 (this.mesh): 要绘制的网格模型。
            // 参数2 (pos): 绘制的中心位置（即上面计算出的 pos）。
            // 参数3 (this.transform.rotation): 使用当前物体的旋转角度。
            // 参数4 (this.transform.localScale): 使用当前物体的缩放比例。
            Gizmos.DrawWireMesh(this.mesh, pos, this.transform.rotation, this.transform.localScale);
        }


        UnityEditor.Handles.color = Color.red;
        UnityEditor.Handles.ArrowHandleCap(0, pos, this.transform.rotation, 1f, EventType.Repaint);
        UnityEditor.Handles.Label(pos, "SpawnPoint:" + this.ID);

        // 路径点不足两个，不画任何东西
        if (patrolPoints == null || patrolPoints.Count < 2) return;

        // 画路径连线
        // 遍历相邻的两个路径点，在它们之间画一条线
        Gizmos.color = Color.white;
        for (int i = 0; i < patrolPoints.Count - 1; i++)
        {
            // 在两个路径点之间采样 10 个中间点，每个都投影到地面
            // 采样点越多，连线越贴合地形，但性能消耗也越高
            // 20 个在编辑器里够用
            int sampleCount = 20;
            Vector3 prev = GetGroundPoint(patrolPoints[i]);

            for(int j = 1; j <= sampleCount; j++)
            {
                // Vector3.Lerp(a, b, t)含义：在 a 和 b 两点之间，按照比例 t 取中间点。t 的范围是 0 到 1
                float t = j / (float)sampleCount;
                Vector3 samplePoint = Vector3.Lerp(patrolPoints[i], patrolPoints[i + 1], t);

                // 把采样点投影到地面
                Vector3 groundPoint = GetGroundPoint(samplePoint);

                // 从上一个地面点连线到当前地面点
                Gizmos.DrawLine(prev, groundPoint);
                prev = groundPoint;
            }
        }

        // 在每个路径点位置画一个小球
        // 让策划能看到每个路径点具体在哪里
        Gizmos.color = Color.yellow;
        foreach (var point in patrolPoints)
        {
            // DrawSphere 参数：圆心位置，半径
            // 半径 0.3f 在场景里是一个比较小的球，不会遮挡视线
            Gizmos.DrawSphere(point, 0.2f);
        }

        // 出生点到第一个路径点的连线，让策划看到怪物出生后要走多远
        Gizmos.color = Color.gray;
        Gizmos.DrawLine(transform.position, patrolPoints[0]);
    }

    void OnDrawGizmosSelected()
    {
        if (patrolPoints != null && patrolPoints.Count >= 2)
        {
            Gizmos.color = Color.blue;
            Gizmos.DrawSphere(patrolPoints[0], 0.4f);

            Gizmos.color = Color.red;
            Gizmos.DrawSphere(patrolPoints[patrolPoints.Count - 1], 0.4f);
        }

        // ── 视野扇形可视化 ──────────────────────────────────────────

        // 用 Handles 画扇形，Handles 属于 UnityEditor
        // 必须在 #if UNITY_EDITOR 块里才能用

        // 画半透明填充扇形，表示视野覆盖区域
        // DrawSolidArc 参数：
        //   center    → 扇形圆心（怪物位置）
        //   normal    → 扇形所在平面的法线（Vector3.up 表示扇形在水平面上）
        //   from      → 扇形起始方向（从怪物朝向偏转半角度作为起点）
        //   angle     → 扇形张开的角度
        //   radius    → 扇形半径
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.1f); // 半透明橙色
        // DrawSolidArc  → 填充扇形，有面积，用半透明颜色表示覆盖范围
        UnityEditor.Handles.DrawSolidArc(
            transform.position,
            Vector3.up,
            Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward,
            viewAngle,
            viewRadius
        );

        // 再画一个扇形边框线，让边界更清晰
        UnityEditor.Handles.color = new Color(1f, 0.5f, 0f, 0.8f);
        // DrawWireArc   → 只画扇形的弧线边框，不填充
        UnityEditor.Handles.DrawWireArc(
            transform.position,
            Vector3.up,
            Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward,
            viewAngle,
            viewRadius
        );

        // 画两条从怪物出发的视野边界线
        // 左边界
        Vector3 leftBoundary = Quaternion.Euler(0, -viewAngle * 0.5f, 0) * transform.forward;
        // 右边界
        Vector3 rightBoundary = Quaternion.Euler(0, viewAngle * 0.5f, 0) * transform.forward;

        UnityEditor.Handles.DrawLine(
            transform.position, 
            transform.position + leftBoundary * viewRadius
        );
        UnityEditor.Handles.DrawLine(
            transform.position, 
            transform.position + rightBoundary * viewRadius
        );
    }

    /// <summary>
    /// 把空间中任意一点投影到地形表面
    /// </summary>
    /// <param name="point"></param>
    /// <returns></returns>
    Vector3 GetGroundPoint(Vector3 point)
    {
        // 从点的上方向下发射射线
        if (Physics.Raycast(point + Vector3.up * 10f, Vector3.down, out RaycastHit hit, 20f))
        {
            // 射线碰到地形，返回碰撞点（地形表面的坐标）
            return hit.point;
        }
        // 射线没有碰到任何东西（比如路径点在悬空位置）
        // 直接返回原始坐标，不做修正
        return point;
    }
#endif
}