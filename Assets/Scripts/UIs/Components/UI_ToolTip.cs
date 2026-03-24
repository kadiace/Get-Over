using UnityEngine;
using UnityEngine.UI;

public class UI_ToolTip : UI_Base
{
    private const string DefaultMessage = "보스: 주변 냄새를 추적해.";

    enum GameObjects
    {
        Panel,
    }

    enum Texts
    {
        MessageText,
    }

    public override void Init()
    {
        Managers.UI.ShowCanvas(gameObject);
        Bind<GameObject>(typeof(GameObjects));
        Bind<Text>(typeof(Texts));
        SetMessage(DefaultMessage);
    }

    public void Show(string message)
    {
        SetMessage(message);
        gameObject.SetActive(true);
    }

    public void Hide()
    {
        gameObject.SetActive(false);
    }

    public void SetMessage(string message)
    {
        Text text = GetText((int)Texts.MessageText);
        if (text == null)
            return;

        text.text = string.IsNullOrWhiteSpace(message) ? DefaultMessage : message;
    }
}
