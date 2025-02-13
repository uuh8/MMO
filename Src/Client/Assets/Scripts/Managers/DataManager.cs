using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;
using System.Text;
using System;
using System.IO;

using Common.Data;

using Newtonsoft.Json;

public class DataManager : Singleton<DataManager>
{
    public string DataPath;
    public Dictionary<int, MapDefine> Maps = null;
    public Dictionary<int, CharacterDefine> Characters = null;
    public Dictionary<int, TeleporterDefine> Teleporters = null;                    
    public Dictionary<int, Dictionary<int, SpawnPointDefine>> SpawnPoints = null;


    public DataManager()
    {
        this.DataPath = "Data/";//
        Debug.LogFormat("DataManager > DataManager()");
    }

    /*�����ʹ�á�Load() �������ȡ��Ϸ�����ļ�����CharacterDefine.txt�����������ɶ����� CharacterDefine�����洢���ֵ��С�*/
    public void Load()
    {

        string json = File.ReadAllText(this.DataPath + "MapDefine.txt"); //��ȡ MapDefine.txt �ļ�������
        this.Maps = JsonConvert.DeserializeObject<Dictionary<int, MapDefine>>(json);

        json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");//��ȡ CharacterDefine.txt �ļ�������
        this.Characters = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);

        json = File.ReadAllText(this.DataPath + "TeleporterDefine.txt");//��ȡ TeleporterDefine.txt �ļ�������
        this.Teleporters = JsonConvert.DeserializeObject<Dictionary<int, TeleporterDefine>>(json);

        //json = File.ReadAllText(this.DataPath + "SpawnPointDefine.txt");//��ȡ SpawnPointDefine.txt �ļ�������
        //this.SpawnPoints = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, SpawnPointDefine>>> (json);
    }

    /*�ͻ���ʹ��*/
    public IEnumerator LoadData()
    {
        string json = File.ReadAllText(this.DataPath + "MapDefine.txt");
        this.Maps = JsonConvert.DeserializeObject<Dictionary<int, MapDefine>>(json);

        yield return null;

        json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");
        this.Characters = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);

        yield return null;

        json = File.ReadAllText(this.DataPath + "TeleporterDefine.txt");
        this.Teleporters = JsonConvert.DeserializeObject<Dictionary<int, TeleporterDefine>>(json);

        yield return null;

        json = File.ReadAllText(this.DataPath + "SpawnPointDefine.txt");
        this.SpawnPoints = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, SpawnPointDefine>>>(json);

        yield return null;
    }

#if UNITY_EDITOR
    public void SaveTeleporters()
    {
        string json = JsonConvert.SerializeObject(this.Teleporters, Formatting.Indented);
        File.WriteAllText(this.DataPath + "TeleporterDefine.txt", json);
    }

    public void SaveSpawnPoints()
    {
        string json = JsonConvert.SerializeObject(this.SpawnPoints, Formatting.Indented);
        File.WriteAllText(this.DataPath + "SpawnPointDefine.txt", json);
    }

#endif
}
