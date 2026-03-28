using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class UI_Message : UI_PauseScene
{
    private const string DefaultMessage =
        "[기본 조작법]\n" +
        "- Arrow Keys: 이동\n" +
        "- Mouse: 시점 이동\n" +
        "- Space: 점프, 활강\n" +
        "- ESC: 메뉴 열기\n\n" +
        "[상황 브리핑]\n" +
        "당신은 임무에 투입된 날다람쥐 에이전트입니다.\n" +
        "마약 원재료를 유통하는 인물을 찾기 위해 브리프케이스에 담겨 제조범의 집으로 배송되었으며,\n" +
        "브리프케이스 안에서 보스에게 조작법과 냄새 탐지 능력을 안내받았습니다.\n" +
        "추적을 위해 제공된 숯 냄새를 따라 단서를 추적해보세요.\n\n" +
        "[힌트]\n" +
        "보스와 무선 통신으로 연결되어 있어 적절할 때 힌트를 들을 수 있습니다.";

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
        GetText((int)Texts.ConfirmButtonText).text = "확인";

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
