using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System;

public class UI_InteractionFail : UI_PauseScene
{
    private const string DefaultMessage = "힘이 부족해 열수 없다";

    enum GameObjects
    {
        Root,
        Panel,
    }

    enum Texts
    {
        MessageText,
        HintText,
    }

    private Action _onClosed;

    public override void Init()
    {
        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<Text>(typeof(Texts));

        SetMessage(DefaultMessage);
        GetText((int)Texts.HintText).text = "확인";

        BindEvent(GetObject((int)GameObjects.Root), OnClickClose);
        BindEvent(GetObject((int)GameObjects.Panel), OnClickClose);
    }

    public void SetMessage(string message)
    {
        Text messageText = GetText((int)Texts.MessageText);
        if (messageText == null)
            return;

        messageText.text = string.IsNullOrWhiteSpace(message) ? DefaultMessage : message;
    }

    public void SetCloseCallback(Action onClosed)
    {
        _onClosed = onClosed;
    }

    private void OnClickClose(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        _onClosed?.Invoke();

        Managers.Resource.Destory(gameObject);
    }
}
