using System.Collections.Generic;
using UnityEngine;

public class DataManager
{
    public Dictionary<string, List<Vector3>> HumanRouteDict { get; protected set; } = new();
    public Dictionary<string, Data.SmellSmoke> SmellDict { get; protected set; } = new();

    public void Init()
    {
        Data.HumanRouteData humanRouteData = LoadJson<Data.HumanRouteData, string, List<Vector3>>("HumanRouteData");
        if (humanRouteData != null)
            HumanRouteDict = humanRouteData.MakeDict();

        Data.SmellSmokeData smellSmokeData = LoadJson<Data.SmellSmokeData, string, Data.SmellSmoke>("Smell");
        if (smellSmokeData != null)
            SmellDict = smellSmokeData.MakeDict();
    }

    private Loader LoadJson<Loader, Key, Value>(string path) where Loader : ILoader<Key, Value>
    {
        TextAsset textAsset = Managers.Resource.Load<TextAsset>($"Datas/{path}");
        if (textAsset == null)
            return default;
        return JsonUtility.FromJson<Loader>(textAsset.text);
    }

    public void Clear() { }
}
