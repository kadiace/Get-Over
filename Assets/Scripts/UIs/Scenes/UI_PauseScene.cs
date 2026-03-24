using UnityEngine;
public abstract class UI_PauseScene : UI_Scene
{
    private bool _pauseApplied;

    public override void Init()
    {
        base.Init();
        Time.timeScale = 0f;
        Managers.Input.SetMode(Define.InputMode.UI);
        _pauseApplied = true;
    }

    private void OnDestroy()
    {
        if (!_pauseApplied)
            return;

        Time.timeScale = 1f;
        Managers.Input.SetMode(Define.InputMode.Player);
        _pauseApplied = false;
    }
}
