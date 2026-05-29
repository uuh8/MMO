using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.SceneManagement;
using UnityEngine;
using Common.Data;
using UnityEngine.AI;

/// <summary>
/// 作用：遍历场景中的传送点，将传送点的世界坐标转换成逻辑坐标，存到配置表中
/// </summary>
public class MapTools
{
    /// <summary>
    /// 导出传送点
    /// </summary>
    [MenuItem("Tools/Map Tools/Export Teleporters")]
    public static void ExportTeleporters() 
    {
        // 1. 加载所有配置表（地图/传送点定义）到内存
        DataManager.Instance.Load();    

        Scene current = EditorSceneManager.GetActiveScene();
        string currentScene = current.name;
        if (current.isDirty)
        {
            EditorUtility.DisplayDialog("提示", "请保存当前场景", "确定");
            return;
        }

        List<TeleporterObject> allTeleporters = new List<TeleporterObject>();

        foreach(var map in DataManager.Instance.Maps)
        {
            // 校验Scene场景文件是否存在
            string sceneFile = "Assets/Levels/" + map.Value.Resource + ".unity";
            if (!System.IO.File.Exists(sceneFile))
            {
                Debug.LogWarningFormat("Scene {0} 不存在！", sceneFile);
                continue;
            }
            // 强制打开该地图对应的 .unity 场景文件
            EditorSceneManager.OpenScene(sceneFile, OpenSceneMode.Single);

            // 找到当前场景下的所有传送点
            TeleporterObject[] teleporters = GameObject.FindObjectsOfType<TeleporterObject>();
            foreach(var teleporter in teleporters)
            {
                // 校验传送点是否存在
                if (!DataManager.Instance.Teleporters.ContainsKey(teleporter.ID))
                {
                    EditorUtility.DisplayDialog("错误", string.Format("地图：{0} 中配置的 Teleporter：[{1}] 中不存在", map.Value.Resource, teleporter.ID), "确定");
                    return;
                }

                // 校验地图的MapId
                TeleporterDefine def = DataManager.Instance.Teleporters[teleporter.ID];
                if(def.MapID != map.Value.ID)
                {
                    EditorUtility.DisplayDialog("错误", string.Format("地图：{0} 中配置的 Teleporter：[{1}] MapID:{2} 错误", map.Value.Resource, teleporter.ID, def.MapID), "确定");
                    return;
                }

                // 世界坐标转换成逻辑坐标
                def.Position = GameObjectTool.WorldToLogicN(teleporter.transform.position);
                def.Direction = GameObjectTool.WorldToLogicN(teleporter.transform.forward);
            }
            // 写回 JSON 配置文件
            DataManager.Instance.SaveTeleporters();
            EditorSceneManager.OpenScene("Assets/Levels/" + currentScene + ".unity");
            EditorUtility.DisplayDialog("提示", "传送点导出完成", "确定");
        }
    }

    /// <summary>
    /// 导出怪物出生点
    /// </summary>
    [MenuItem("Tools/Map Tools/Export SpawnPoints")]
    public static void ExportSpawnPoints()
    {
        // 把所有配置表（地图定义、刷怪点定义等）从磁盘读进内存
        // 必须先 Load，后面才能用 DataManager.Instance.SpawnPoints 等字典
        DataManager.Instance.Load();

        // GetActiveScene() 获取当前在 Unity Editor 里打开的场景
        Scene current = EditorSceneManager.GetActiveScene();
        string currentScene = current.name;

        // isDirty：场景有未保存的修改时为 true
        // 如果场景没保存就导出，刷怪点的位置可能不是最新的，所以强制要求先保存
        if (current.isDirty)
        {
            // DisplayDialog：弹出一个模态对话框
            // 参数：标题、内容、确认按钮文字
            EditorUtility.DisplayDialog("提示", "请保存当前场景", "确定");
            return;
        }

        // 如果 SpawnPoints 字典还是 null（第一次导出），先初始化
        if (DataManager.Instance.SpawnPoints == null)
            DataManager.Instance.SpawnPoints = new Dictionary<int, Dictionary<int, SpawnPointDefine>>();

        // 遍历所有地图定义，对每张地图的场景做一次导出
        foreach (var map in DataManager.Instance.Maps)
        {
            // 拼接该地图对应的 Unity 场景文件路径
            string sceneFile = "Assets/Levels/" + map.Value.Resource + ".unity";

            // System.IO.File.Exists 检查磁盘上这个文件是否真实存在
            // 有些地图可能只在配置表里定义，但还没有对应的场景文件
            if (!System.IO.File.Exists(sceneFile))
            {
                Debug.LogWarningFormat("[MapTools] Scene {0} 不存在！", sceneFile);
                continue; // 跳过这张地图，继续处理下一张
            }

            // 强制打开这张地图对应的场景文件
            // OpenSceneMode.Single：关闭当前所有场景，只打开这一个
            EditorSceneManager.OpenScene(sceneFile, OpenSceneMode.Single);

            // FindObjectsOfType<SpawnPoint>() 在当前打开的场景里
            // 找到所有挂了 SpawnPoint 组件的 GameObject
            // 注意：这只能找到当前已加载场景里的对象，所以前面必须先 OpenScene
            SpawnPoint[] spawnPoints = GameObject.FindObjectsOfType<SpawnPoint>();

            // 确保这张地图在 SpawnPoints 字典里有自己的二级字典
            // 外层 key = 地图 ID，内层 key = 刷怪点 ID
            if (!DataManager.Instance.SpawnPoints.ContainsKey(map.Value.ID))
            {
                DataManager.Instance.SpawnPoints[map.Value.ID] = new Dictionary<int, SpawnPointDefine>();
            }

            foreach (var spawnPoint in spawnPoints)
            {
                // 如果这个刷怪点的 ID 在字典里还没有，先创建一个空的 Define
                if (!DataManager.Instance.SpawnPoints[map.Value.ID].ContainsKey(spawnPoint.ID))
                {
                    DataManager.Instance.SpawnPoints[map.Value.ID][spawnPoint.ID] = new SpawnPointDefine();
                }

                // 拿到这个刷怪点对应的 Define 对象，准备填数据
                SpawnPointDefine def = DataManager.Instance.SpawnPoints[map.Value.ID][spawnPoint.ID];

                def.ID = spawnPoint.ID;
                def.MapID = map.Value.ID;

                // WorldToLogicN：把 Unity 世界坐标（float，米）转成逻辑坐标（int，厘米×100）
                // 例如世界坐标 (5.0, 0.1, 3.0) → 逻辑坐标 (500, 10, 300)
                // 服务端用整数坐标，避免浮点精度问题
                def.Position = GameObjectTool.WorldToLogicN(spawnPoint.transform.position);

                // transform.forward 是这个 GameObject 的正前方向向量
                // 表示怪物出生时的朝向
                def.Direction = GameObjectTool.WorldToLogicN(spawnPoint.transform.forward);

                // ── 新增：导出视野配置 ──────────────────────────────────
                // viewRadius 是世界单位（米），直接存，服务端计算时会换算成逻辑单位
                def.ViewRadius = spawnPoint.viewRadius;
                def.ViewAngle = spawnPoint.viewAngle;
            }

            // 把内存里更新过的 SpawnPoints 字典序列化成 JSON 写入磁盘
            // 这样服务端启动时 Load() 就能读到最新的数据
            DataManager.Instance.SaveSpawnPoints();

            // 导出完成后，重新打开我们最开始工作的那个场景
            // 否则工具执行完后，编辑器停留在最后处理的那张地图场景里
            EditorSceneManager.OpenScene("Assets/Levels/" + currentScene + ".unity");

            EditorUtility.DisplayDialog("提示", "刷怪点导出完成", "确定");
        }
    }

    /// <summary>
    /// 生成导航数据
    /// </summary>
    [MenuItem("Tools/Map Tools/Generate NavData")]
    public static void GenerateNavData()
    {
        Material red = new Material(Shader.Find("Particles/Alpha Blended"));
        red.color = Color.red;
        red.SetColor("_TintColor", Color.red);
        red.enableInstancing = true;
        GameObject go = GameObject.Find("MiniMapBoundingBox");
        if(go != null)
        {
            GameObject root = new GameObject("Root");
            BoxCollider bound = go.GetComponent<BoxCollider>();
            float step = 1f;
            for(float x = bound.bounds.min.x; x < bound.bounds.max.x; x += step)
            {
                for (float z = bound.bounds.min.z; z < bound.bounds.max.z; z += step)
                {
                    for (float y = bound.bounds.max.y; y < bound.bounds.max.y + 5f; y += step)
                    {
                        var pos = new Vector3(x, y, z);
                        NavMeshHit hit;
                        if(NavMesh.SamplePosition(pos, out hit, 0.5f, NavMesh.AllAreas))
                        {
                            if (hit.hit)
                            {
                                var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
                                box.name = "Hit" + hit.mask;
                                box.GetComponent<MeshRenderer>().sharedMaterial = red;
                                box.transform.SetParent(root.transform, true);
                                box.transform.position = pos;
                                box.transform.localScale = Vector3.one * 0.9f;
                            }
                        }
                    }
                }
            }
        }
    }
}
