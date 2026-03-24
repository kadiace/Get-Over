using System.Collections.Generic;
using UnityEngine;

public abstract class BaseController<T> : MonoBehaviour
     where T : System.Enum
{
    public Define.WorldObject WorldObjectType { get; protected set; } = Define.WorldObject.Unknown;

    Animator Anim;

    protected T _animState;

    public virtual T AnimState
    {
        get { return _animState; }
        set
        {
            if (EqualityComparer<T>.Default.Equals(_animState, value))
                return;
            _animState = value;

            Anim.CrossFade(_animState.ToString(), 0.1f);
        }
    }

    void Start() => Init();

    void Update() => UpdateState();

    protected virtual void Init()
    {
        Anim = gameObject.GetComponent<Animator>();
    }
    protected abstract void UpdateState();
}
