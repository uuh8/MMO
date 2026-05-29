using UnityEditor;
using UnityEngine;

public class AssetTool
{
    [MenuItem("Assets/Only For Prefab")]
    private static void PrintSelected()
    {
        foreach (Object obj in Selection.objects)
        {
            string path = AssetDatabase.GetAssetPath(obj);
            Debug.Log($"Name: {obj.name}, Path: {path}");
        }
    }

    [MenuItem("Assets/Only For Prefab", true)]
    static bool ValidatePrintSelected()
    {
        return Selection.activeObject is GameObject;
    }

}