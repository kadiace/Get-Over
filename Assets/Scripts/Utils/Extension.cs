using System;
using UnityEngine;
using UnityEngine.EventSystems;

public static class Extension
{
    public static T GetorAddComponent<T>(this GameObject go) where T : Component
    {
        return Util.GetorAddComponent<T>(go);
    }

    public static void BindEvent(this GameObject go, Action<PointerEventData> action, Define.UIEvent type = Define.UIEvent.Click)
    {
        UI_Base.BindEvent(go, action, type);
    }

    public static bool IsValid(this GameObject go)
    {
        return Util.IsValid(go);
    }

    public static Define.HitInfo Convert(this ControllerColliderHit hit)
    {
        return Util.Convert(hit);
    }

    public static Define.HitInfo Convert(this RaycastHit hit)
    {
        return Util.Convert(hit);
    }
}