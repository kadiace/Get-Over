using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class UI_Button : UI_Base
{
    int _score = 0;

    enum Buttons
    {
        PointButton,
    }

    enum Texts
    {
        PointText,
        ScoreText,
    }

    enum GameObjects
    {
        TestObject,
    }

    enum Images
    {
        ItemIcon,
    }

    public override void Init()
    {
        Managers.UI.ShowCanvas(gameObject, true);
        Bind<Button>(typeof(Buttons));
        Bind<Text>(typeof(Texts));
        Bind<Image>(typeof(Images));
        Bind<GameObject>(typeof(GameObjects));

        GetButton((int)Buttons.PointButton).gameObject.BindEvent(OnButtonClicked);

        GameObject go = GetImage((int)Images.ItemIcon).gameObject;
        BindEvent(go, eventData => { go.transform.position = eventData.position; }, Define.UIEvent.Drag);
    }

    public void OnButtonClicked(PointerEventData eventData)
    {
        Debug.Log("Clicked");
        _score++;
        GetText((int)Texts.ScoreText).text = $"Score {_score}";
    }
}
