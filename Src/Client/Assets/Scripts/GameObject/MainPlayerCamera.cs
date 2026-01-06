using Models;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

/// <summary>
/// 第三人称 Orbit Camera（轨道相机）
/// 核心思想：
/// 1) 鼠标只负责改变 yaw/pitch（相机的角度状态）
/// 2) LateUpdate 用 yaw/pitch 算出相机在目标点周围的“轨道位置”
/// 3) 相机永远 LookAt 目标点
/// 这样键盘移动/角色转向不会再“带着镜头转”。
/// </summary>
public class MainPlayerCamera : MonoSingleton<MainPlayerCamera>
{
    public Camera camera;

    public Transform viewPoint; // 玩家身上挂一个 CameraPivot（头顶/胸口），相机看向它
    public GameObject player;   // 当前主角对象

    [Header("Orbit Params")]
    public float distance = 2f;   // 相机距离目标点的“后退距离”（第三人称相机一般在目标后方）
    public float height = 2.2f;   // 相机相对目标点的基础高度
    public float side = 0f;       // 肩偏：>0 右肩视角，<0 左肩视角，0 中间

    [Header("Orbit Angles (Mouse Control)")]
    public float yaw = 0f;        // 水平旋转角：绕世界 Y 轴（左右摇镜头）
    public float pitch = 15f;     // 俯仰角：绕 X 轴（上下抬头低头）
    public float minPitch = -30f; // 俯仰下限（避免镜头翻到脚底造成反转）
    public float maxPitch = 70f;  // 俯仰上限（避免翻过头顶）

    // 用于避免“第一次锁定玩家时镜头跳变”
    private bool initedAngles = false;

    /// <summary>
    /// 给输入层调用：把鼠标的增量映射到 yaw/pitch 上。
    /// 注意：这里的 yaw/pitch 是“状态”，会在多帧持续累积。
    /// </summary>
    public void AddRotation(float yawDelta, float pitchDelta)
    {
        // 左右鼠标移动 -> yaw 改变
        yaw += yawDelta;

        // 上下鼠标移动 -> pitch 改变（并且要 clamp）
        pitch = Mathf.Clamp(pitch + pitchDelta, minPitch, maxPitch);
    }

    /// <summary>
    /// LateUpdate：等角色这一帧所有移动/动画/物理更新结束后，再摆相机。
    /// 这么做能减少相机抖动（尤其是角色用 Rigidbody 或 root motion 时）。
    /// </summary>
    private void LateUpdate()
    {
        // 1) 找玩家：主角对象挂在 User.Instance.CurrentCharacterObject
        if (player == null && User.Instance.CurrentCharacterObject != null)
        {
            player = User.Instance.CurrentCharacterObject.gameObject;
            viewPoint = null;

            // 换玩家后需要重新初始化一次 yaw/pitch（否则用旧角色的角度可能会跳）
            initedAngles = false;
        }
        if (player == null) return;

        // 2) 找 ViewPoint：角色 prefab 上放一个空物体叫 ViewPoint
        //    这样镜头会盯着头部附近，而不是脚底位置
        if (viewPoint == null)
        {
            viewPoint = FindDeepChild(player.transform, "ViewPoint");
        }
        // 3) 目标点 target：如果找到了 ViewPoint 就用它，否则退化为“角色位置 + 一个高度”
        Vector3 target = viewPoint.position;

        // 4) 第一次初始化角度：用“当前相机位置 - 目标点”反推出 yaw/pitch
        //    目的：避免一进入游戏/切换角色，相机突然跳到默认 yaw/pitch 对应的位置
        if (!initedAngles)
        {
            // offset = 相机相对目标点的向量
            Vector3 offset = transform.position - target;

            // horiz = offset 在水平面（XZ）上的长度
            float horiz = Mathf.Sqrt(offset.x * offset.x + offset.z * offset.z);
            if (horiz < 1e-4f) horiz = 1e-4f;

            // yaw：通过 atan2(x, z) 求水平角（Unity 里 forward 是 z 方向）
            // Mathf.Rad2Deg是一个常量，它的值是 180 / π，它是一个转换系数，用于将弧度 转换为角度。
            yaw = Mathf.Atan2(offset.x, offset.z) * Mathf.Rad2Deg;

            // pitch：通过 atan2(y, 水平距离) 求俯仰角
            pitch = Mathf.Atan2(offset.y, horiz) * Mathf.Rad2Deg;
            pitch = Mathf.Clamp(pitch, minPitch, maxPitch);

            initedAngles = true;
        }

        // 5) 核心：用 yaw/pitch 把一个“固定偏移 baseOffset”旋转到世界空间
        //    这就是“相机在隐形球壳/轨道上绕目标点转”的实现。
        //
        // baseOffset 的含义（在相机局部空间）：
        // - x: side    -> 肩偏（右肩/左肩）
        // - y: height  -> 抬高
        // - z: -distance -> 往后退（第三人称相机一般在目标后方）
        //
        // rot 是由 pitch/yaw 构成的旋转：相机绕目标点转，而不是依赖 player.forward
        Quaternion rot = Quaternion.Euler(pitch, yaw, 0f);
        Vector3 baseOffset = new Vector3(side, height, -distance);

        // 把 baseOffset 旋转到世界空间，得到相机相对 target 的最终偏移
        Vector3 worldOffset = rot * baseOffset;

        // 相机最终位置 = target + worldOffset
        transform.position = target + worldOffset;

        // 6) 相机永远看向目标点
        //    用 Vector3.up 确保“上方向”稳定（避免某些旋转导致 roll 翻滚）
        transform.LookAt(target, Vector3.up);
    }
    private Transform FindDeepChild(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name) return child;
            var r = FindDeepChild(child, name);
            if (r != null) return r;
        }
        return null;
    }

}
