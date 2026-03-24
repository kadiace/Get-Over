using System;
using System.Collections.Generic;
using UnityEngine;

public class SmellManager
{
    private static readonly string[] SmellNames =
    {
        "Charcoal",
        "Ingredient",
        "Temp",
        "Test",
    };

    private string[] _smellNames = Array.Empty<string>();
    private bool[] _isRegistered = Array.Empty<bool>();
    private bool[] _isVisible = Array.Empty<bool>();
    private readonly Dictionary<string, int> _smellIndexByName = new(StringComparer.OrdinalIgnoreCase);
    private int _activeSmellIndex = -1;

    public string ActiveSmellName => IsValidIndex(_activeSmellIndex) ? _smellNames[_activeSmellIndex] : string.Empty;
    public IReadOnlyDictionary<string, int> SmellIndexByName => _smellIndexByName;

    public void Init()
    {
        BuildSmellCatalog();
        Clear();
    }

    public void Clear()
    {
        for (int i = 0; i < _isRegistered.Length; i++)
        {
            _isRegistered[i] = false;
            _isVisible[i] = false;
        }

        _activeSmellIndex = -1;
    }

    public bool RegisterSmellObject(string smellName, bool visibleOnRegister = true)
    {
        if (!TryGetSmellIndex(smellName, out int index))
        {
            Debug.LogWarning($"[SmellManager] Unknown smell key '{smellName}'.");
            return false;
        }

        return RegisterSmellObject(index, visibleOnRegister);
    }

    public bool RegisterSmellObject(int index, bool visibleOnRegister = true)
    {
        if (!IsValidIndex(index))
            return false;

        bool changed = !_isRegistered[index];
        _isRegistered[index] = true;

        if (visibleOnRegister)
            _isVisible[index] = true;

        if (_activeSmellIndex < 0 && _isVisible[index])
            _activeSmellIndex = index;

        return changed;
    }

    public bool IsSmellRegistered(string smellName)
    {
        return TryGetSmellIndex(smellName, out int index) && _isRegistered[index];
    }

    public bool IsSmellVisible(string smellName)
    {
        return TryGetSmellIndex(smellName, out int index) && _isVisible[index];
    }

    public void SetSmellVisibility(string smellName, bool visible)
    {
        if (!TryGetSmellIndex(smellName, out int index) || !_isRegistered[index])
            return;

        SetSmellVisibility(index, visible);
    }

    public void SetSmellVisibility(int index, bool visible)
    {
        if (!IsValidIndex(index) || !_isRegistered[index])
            return;

        _isVisible[index] = visible;
    }

    public void SetActiveSmell(string smellName)
    {
        if (string.IsNullOrEmpty(smellName))
        {
            _activeSmellIndex = -1;
            return;
        }

        if (!TryGetSmellIndex(smellName, out int index) || !_isRegistered[index])
            return;

        SetActiveSmell(index);
    }

    public void SetActiveSmell(int index)
    {
        if (!IsValidIndex(index) || !_isRegistered[index])
            return;

        _activeSmellIndex = index;
    }

    public string[] GetRegisteredSmellNames()
    {
        int count = 0;
        for (int i = 0; i < _isRegistered.Length; i++)
        {
            if (_isRegistered[i])
                count++;
        }

        if (count == 0)
            return Array.Empty<string>();

        string[] names = new string[count];
        int write = 0;
        for (int i = 0; i < _isRegistered.Length; i++)
        {
            if (_isRegistered[i])
                names[write++] = _smellNames[i];
        }

        return names;
    }

    public string[] GetAllSmellNames()
    {
        if (_smellNames.Length == 0)
            return Array.Empty<string>();

        string[] names = new string[_smellNames.Length];
        Array.Copy(_smellNames, names, _smellNames.Length);
        return names;
    }

    private void BuildSmellCatalog()
    {
        _smellNames = (string[])SmellNames.Clone();

        _smellIndexByName.Clear();
        for (int i = 0; i < _smellNames.Length; i++)
            _smellIndexByName[_smellNames[i]] = i;

        _isRegistered = new bool[_smellNames.Length];
        _isVisible = new bool[_smellNames.Length];
    }

    public bool TryGetSmellIndex(string smellName, out int index)
    {
        if (string.IsNullOrWhiteSpace(smellName))
        {
            index = -1;
            return false;
        }

        return _smellIndexByName.TryGetValue(smellName, out index);
    }

    public bool TryGetSmellName(int index, out string smellName)
    {
        if (!IsValidIndex(index))
        {
            smellName = string.Empty;
            return false;
        }

        smellName = _smellNames[index];
        return true;
    }

    private bool IsValidIndex(int index)
    {
        return index >= 0 && index < _smellNames.Length;
    }
}
