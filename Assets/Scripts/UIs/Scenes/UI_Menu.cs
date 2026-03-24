using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Menu : UI_PauseScene
{
    private const int Columns = 5;
    private static readonly Vector2 CellSize = new(130f, 52f);
    private static readonly Vector2 CellSpacing = new(10f, 10f);
    private readonly Dictionary<string, Image> _itemImageByName = new();
    private readonly Dictionary<string, Text> _itemTextByName = new();
    private GameObject _gridItemTemplate;

    enum GameObjects
    {
        GridPanel,
        GridItemTemplate,
        ContinueButton,
        ExitButton,
    }

    enum Texts
    {
        TitleText,
    }

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<Text>(typeof(Texts));

        GetText((int)Texts.TitleText).text = "Handled Objects";

        GameObject continueButton = GetObject((int)GameObjects.ContinueButton);
        GameObject exitButton = GetObject((int)GameObjects.ExitButton);

        BindEvent(continueButton, OnClickContinue);
        BindEvent(exitButton, OnClickExit);

        _gridItemTemplate = GetObject((int)GameObjects.GridItemTemplate);
        if (_gridItemTemplate != null)
            _gridItemTemplate.SetActive(false);

        RebuildHandledObjectGrid();
    }

    private void OnClickContinue(PointerEventData eventData)
    {
        GameScene scene = Object.FindAnyObjectByType<GameScene>();
        if (scene != null)
            scene.ResumeGameFromMenu();
    }

    private void OnClickExit(PointerEventData eventData)
    {
        GameScene scene = Object.FindAnyObjectByType<GameScene>();
        if (scene != null)
            scene.ExitGameFromMenu();
        else
            Application.Quit();
    }

    private void RebuildHandledObjectGrid()
    {
        GameObject gridPanel = GetObject((int)GameObjects.GridPanel);
        RectTransform gridRect = gridPanel.GetComponent<RectTransform>();
        _itemImageByName.Clear();
        _itemTextByName.Clear();

        for (int i = gridRect.childCount - 1; i >= 0; i--)
        {
            Transform child = gridRect.GetChild(i);
            if (_gridItemTemplate != null && child.gameObject == _gridItemTemplate)
                continue;

            Managers.Resource.Destory(child.gameObject);
        }

        IReadOnlyList<string> smellNames = Managers.Smell.GetAllSmellNames();
        int count = smellNames.Count;

        if (count == 0)
            return;

        for (int i = 0; i < count; i++)
        {
            string label = smellNames[i];
            CreateGridItem(gridRect, i, label, true);
        }

        int rows = Mathf.Max(1, Mathf.CeilToInt(count / (float)Columns));
        float height = rows * CellSize.y + (rows - 1) * CellSpacing.y;
        gridRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
        RefreshSmellHighlights();
    }

    private void CreateGridItem(RectTransform parent, int index, string label, bool clickable)
    {
        if (_gridItemTemplate == null)
            return;

        GameObject item = Object.Instantiate(_gridItemTemplate, parent);
        item.name = $"Item_{index}";
        item.SetActive(true);

        RectTransform itemRect = item.GetComponent<RectTransform>();
        int row = index / Columns;
        int col = index % Columns;

        float x = col * (CellSize.x + CellSpacing.x);
        float y = -row * (CellSize.y + CellSpacing.y);

        itemRect.anchorMin = new Vector2(0f, 1f);
        itemRect.anchorMax = new Vector2(0f, 1f);
        itemRect.pivot = new Vector2(0f, 1f);
        itemRect.anchoredPosition = new Vector2(x, y);
        itemRect.sizeDelta = CellSize;

        Image itemImage = item.GetComponent<Image>();
        itemImage.color = new Color(0.18f, 0.18f, 0.2f, 0.9f);

        Text text = item.GetComponentInChildren<Text>(true);
        if (text == null)
            return;

        text.text = label;

        if (!clickable)
            return;

        _itemImageByName[label] = itemImage;
        _itemTextByName[label] = text;

        string capturedName = label;
        BindEvent(item, (eventData) => OnClickSmellArea(capturedName));
    }

    private void OnClickSmellArea(string smellName)
    {
        GameScene scene = Object.FindAnyObjectByType<GameScene>();
        if (scene == null)
            return;

        scene.OnMenuSmellAreaClicked(smellName);
        RefreshSmellHighlights();
    }

    private void RefreshSmellHighlights()
    {
        GameScene scene = Object.FindAnyObjectByType<GameScene>();
        if (scene == null)
            return;

        string activeName = Managers.Smell.ActiveSmellName;
        foreach (KeyValuePair<string, Image> pair in _itemImageByName)
        {
            string name = pair.Key;
            Image image = pair.Value;
            bool registered = Managers.Smell.IsSmellRegistered(name);
            bool visible = registered && Managers.Smell.IsSmellVisible(name);
            bool active = !string.IsNullOrEmpty(activeName) && activeName == name && visible;

            if (!registered)
                image.color = new Color(0.16f, 0.16f, 0.18f, 0.55f);
            else if (active)
                image.color = new Color(0.9f, 0.72f, 0.2f, 0.95f);
            else if (visible)
                image.color = new Color(0.26f, 0.5f, 0.3f, 0.9f);
            else
                image.color = new Color(0.22f, 0.22f, 0.24f, 0.75f);

            if (_itemTextByName.TryGetValue(name, out Text text))
                text.text = !registered ? "(unregistered)" : (visible ? $"{name} (on)" : $"{name} (off)");
        }
    }
}
