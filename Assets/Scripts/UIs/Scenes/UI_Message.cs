using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI_Message : UI_PauseScene
{
    private const string DefaultMessage =
        "[Controls]\n" +
        "- Arrow Keys: Move\n" +
        "- Mouse: Look around\n" +
        "- Space: Jump / Glide\n" +
        "- ESC: Open menu\n\n" +
        "[Mission Brief]\n" +
        "You are a flying squirrel agent deployed on a covert mission.\n" +
        "To find the distributor of narcotics ingredients, you were delivered to the suspect's house in a briefcase,\n" +
        "where your boss explained the controls and your scent-tracking ability.\n" +
        "Follow the charcoal scent provided for tracking and investigate the clues.\n\n" +
        "[Hint]\n" +
        "You are connected to your boss by radio, so hints will be given at the right moments.";

    enum GameObjects
    {
        MessagePanel,
        Content,
        ConfirmButton,
    }

    enum Texts
    {
        ScriptText,
        ConfirmButtonText,
    }

    private Action _confirmAction;
    private bool _isInitialized;
    private string _currentMessage = DefaultMessage;

    public override void Init()
    {
        if (_isInitialized)
            return;

        base.Init();

        Bind<GameObject>(typeof(GameObjects));
        Bind<Text>(typeof(Texts));

        GetText((int)Texts.ScriptText).text = _currentMessage;
        GetText((int)Texts.ConfirmButtonText).text = "Retry";

        BindEvent(GetObject((int)GameObjects.ConfirmButton), OnClickConfirm);
        _isInitialized = true;
    }

    private void OnClickConfirm(PointerEventData eventData)
    {
        if (eventData.button != PointerEventData.InputButton.Left)
            return;

        ConfirmCurrentMessage();
    }

    private void Update()
    {
        if (_confirmAction == null)
            return;

        Keyboard keyboard = Keyboard.current;
        if (keyboard == null)
            return;

        if (!keyboard.enterKey.wasPressedThisFrame && !keyboard.numpadEnterKey.wasPressedThisFrame)
            return;

        ConfirmCurrentMessage();
    }

    private void ConfirmCurrentMessage()
    {
        if (_confirmAction != null)
        {
            _confirmAction.Invoke();
            return;
        }

        GameScene scene = UnityEngine.Object.FindAnyObjectByType<GameScene>();
        if (scene != null)
            scene.OnStartupMessageConfirmed();
    }

    public void SetMessage(string message)
    {
        if (!_isInitialized)
            Init();

        _currentMessage = string.IsNullOrWhiteSpace(message) ? DefaultMessage : message;

        Text scriptText = GetText((int)Texts.ScriptText);
        if (scriptText == null)
            return;

        scriptText.text = _currentMessage;
    }

    public void SetConfirmAction(Action onConfirm)
    {
        _confirmAction = onConfirm;
    }
}

public class UI_Score : UI_Scene
{
    enum Texts
    {
        ScoreText,
    }

    private bool _isInitialized;

    public override void Init()
    {
        if (_isInitialized)
            return;

        base.Init();
        Bind<Text>(typeof(Texts));
        _isInitialized = true;
        SetScore(0);
    }

    public void SetScore(int score)
    {
        if (!_isInitialized)
            Init();

        Text scoreText = GetText((int)Texts.ScoreText);
        if (scoreText == null)
            return;

        scoreText.text = $"SCORE {score}";
    }
}
