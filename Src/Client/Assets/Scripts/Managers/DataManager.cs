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
    public Dictionary<int, TeleporterDefine> Teleporters = null; // 传送点
    internal Dictionary<int, NpcDefine> Npcs = null;
    internal Dictionary<int, ItemDefine> Items = null;
    internal Dictionary<int, ShopDefine> Shops = null;
    internal Dictionary<int, Dictionary<int, ShopItemDefice>> ShopItems = null;
    internal Dictionary<int, EquipDefine> Equips = null;
    internal Dictionary<int, QuestDefine> Quests = null;
    internal Dictionary<int, RideDefine> Rides = null;
    public Dictionary<int, Dictionary<int, SpawnPointDefine>> SpawnPoints = null;
    public Dictionary<int, Dictionary<int, SpawnRuleDefine>> SpawnRules = null;
    public Dictionary<int, MonsterDefine> Monsters = null;


    public DataManager()
    {
        this.DataPath = "Data/"; 
    }

    /*Load() 是同步版本，调用后主线程会一直阻塞，把所有配置文件全部读完才继续执行。它用于 Editor 工具，比如 MapTools.ExportSpawnPoints() 点击菜单后需要立刻读到数据，没有进度条，没有等待，直接读完就用。*/
    public void Load()
    {
        string json = File.ReadAllText(this.DataPath + "MapDefine.txt"); 
        this.Maps = JsonConvert.DeserializeObject<Dictionary<int, MapDefine>>(json);

        json = File.ReadAllText(this.DataPath + "CharacterDefine.txt");
        this.Characters = JsonConvert.DeserializeObject<Dictionary<int, CharacterDefine>>(json);

        json = File.ReadAllText(this.DataPath + "TeleporterDefine.txt");
        this.Teleporters = JsonConvert.DeserializeObject<Dictionary<int, TeleporterDefine>>(json);

        json = File.ReadAllText(this.DataPath + "NpcDefine.txt");
        this.Npcs = JsonConvert.DeserializeObject<Dictionary<int, NpcDefine>>(json);

        json = File.ReadAllText(this.DataPath + "ItemDefine.txt");
        this.Items = JsonConvert.DeserializeObject<Dictionary<int, ItemDefine>>(json);

        json = File.ReadAllText(this.DataPath + "ShopDefine.txt");
        this.Shops = JsonConvert.DeserializeObject<Dictionary<int, ShopDefine>>(json);

        json = File.ReadAllText(this.DataPath + "ShopItemDefine.txt");
        this.ShopItems = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, ShopItemDefice>>>(json);

        json = File.ReadAllText(this.DataPath + "EquipDefine.txt");
        this.Equips = JsonConvert.DeserializeObject<Dictionary<int, EquipDefine>>(json);

        json = File.ReadAllText(this.DataPath + "QuestDefine.txt");
        this.Quests = JsonConvert.DeserializeObject<Dictionary<int, QuestDefine>>(json);

        json = File.ReadAllText(this.DataPath + "RideDefine.txt");
        this.Rides = JsonConvert.DeserializeObject<Dictionary<int, RideDefine>>(json);
    }

    /*LoadData() 是协程版本，放进 LoadingManager使用，每读一个文件就 yield return null 让出一帧，分散到多帧里执行。它用于游戏运行时的 Loading 界面，玩家在看进度条的时候，后台在逐帧加载数据，UI 不会卡死，动画也能正常播放*/
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

        json = File.ReadAllText(this.DataPath + "EquipDefine.txt");
        this.Equips = JsonConvert.DeserializeObject<Dictionary<int, EquipDefine>>(json);

        json = File.ReadAllText(this.DataPath + "QuestDefine.txt");
        this.Quests = JsonConvert.DeserializeObject<Dictionary<int, QuestDefine>>(json);

        yield return null;

        json = File.ReadAllText(this.DataPath + "SpawnPointDefine.txt");
        this.SpawnPoints = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, SpawnPointDefine>>>(json);

        json = File.ReadAllText(this.DataPath + "SpawnRuleDefine.txt");
        this.SpawnRules = JsonConvert.DeserializeObject<Dictionary<int, Dictionary<int, SpawnRuleDefine>>>(json);

        json = File.ReadAllText(this.DataPath + "RideDefine.txt");
        this.Rides = JsonConvert.DeserializeObject<Dictionary<int, RideDefine>>(json);

        json = File.ReadAllText(this.DataPath + "MonsterDefine.txt");
        this.Monsters = JsonConvert.DeserializeObject<Dictionary<int, MonsterDefine>>(json);

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
