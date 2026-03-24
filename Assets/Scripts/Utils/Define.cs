using UnityEngine;

public class Define
{
    public enum WorldObject
    {
        Unknown,
        Player,
        Human,
        SmellSource,
        SmellMarker,

    }

    public enum InputMode
    {
        Player,
        UI,
        Cutscene,
    }

    public enum PlayerAnimState
    {
        IDLE,
        CROUCH,
        RUN,
        WALK,
        CLIMB,
        JUMP,
        GLIDE,
        DIVE,
    }

    public enum HumanAnimState
    {
        IDLE,
        RUN,
        WALK,
        JUMP,
    }

    public enum Scene
    {
        Unknown,
        Login,
        Lobby,
        Game,

    }
    public enum Layer
    {
        Monster = 8,
        Ground = 9,
        Block = 10,

    }

    public enum Sound
    {
        Bgm,
        Effect,
        MaxCount,
    }

    public enum UIEvent
    {
        Click,
        Drag,
    }

    public enum MouseEvent
    {
        Move,
        Press,
        PointerDown,
        PointerUp,
        Click,
    }
    public enum CameraMode
    {
        QuarterView,
    }

    public static float epsilon = 1e-8f;

    public struct HitInfo
    {
        public Vector3 point;
        public Vector3 normal;
        public Collider collider;
        public float distance;
        public Vector3 moveDirection;
    }
}
