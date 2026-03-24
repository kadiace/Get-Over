using System;
using System.Collections.Generic;
using UnityEngine;

public interface ILoader<Key, Value>
{
    Dictionary<Key, Value> MakeDict();
}

namespace Data
{
    [Serializable]
    public class Stat
    {
        public int level;
        public int maxHp;
        public int attack;
        public int totalExp;
    }

    [Serializable]
    public class StatData : ILoader<int, Stat>
    {
        public List<Stat> stats = new();

        public Dictionary<int, Stat> MakeDict()
        {
            Dictionary<int, Stat> dict = new();
            foreach (Stat stat in stats)
                dict.Add(stat.level, stat);
            return dict;
        }
    }

    [Serializable]
    public class HumanRoute
    {
        public string name;
        public List<Vector3> positions = new();
    }

    [Serializable]
    public class HumanRouteData : ILoader<string, List<Vector3>>
    {
        public List<HumanRoute> routes = new();

        public Dictionary<string, List<Vector3>> MakeDict()
        {
            Dictionary<string, List<Vector3>> dict = new();
            foreach (HumanRoute route in routes)
            {
                if (string.IsNullOrWhiteSpace(route.name) || route.positions == null)
                    continue;

                dict[route.name] = route.positions;
            }
            return dict;
        }
    }

    [Serializable]
    public class SmellSmoke
    {
        public string name;
        public List<Vector3> linePoints = new();
        public float pointSpacing = 0.6f;
        public float drawInterval = 0.06f;
        public Vector3 scale = new Vector3(0.6f, 0.6f, 0.6f);
    }

    [Serializable]
    public class SmellSmokeData : ILoader<string, SmellSmoke>
    {
        public List<SmellSmoke> smells = new();

        public Dictionary<string, SmellSmoke> MakeDict()
        {
            Dictionary<string, SmellSmoke> dict = new(StringComparer.OrdinalIgnoreCase);
            foreach (SmellSmoke smell in smells)
            {
                if (string.IsNullOrWhiteSpace(smell.name))
                    continue;

                dict[smell.name] = smell;
            }

            return dict;
        }
    }

}
