using System;
using System.Collections.Generic;
using System.Diagnostics;
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameScene : BaseScene
{
    private const int FloorLayer = 7;

    [Header("Floor Spawn")]
    [SerializeField] private string floorPrefabName = "Floor";
    [SerializeField] private float spawnPerSecond = 4f;
    [SerializeField] private float spawnRadiusFromYAxis = 5f;
    [SerializeField] private float spawnY = 10f;
    [SerializeField] private float floorMoveSpeed = 5f;
    [SerializeField] private float floorDespawnY = -30f;
    [SerializeField] private float overlapShrink = 0.01f;
    [SerializeField] private LayerMask floorOverlapMask = 1 << 7;

    [Header("Out Of Bounds")]
    [SerializeField] private float retryPromptDistanceFromYAxis = 6f;
    [SerializeField] private float retryPromptMinY = -20f;
    [SerializeField] private string retryPromptMessage = "재시도 하시겠습니까?";

    [Header("TwoSidedDissolve Preview")]
    [SerializeField] private string twoSidedDissolvePreviewObjectName = "Waterfall";

    private GameObject _floorOriginal;
    private float _spawnInterval;
    private float _spawnAccumulator;
    private readonly Collider[] _overlapBuffer = new Collider[32];
    private PlayerController _player;
    private CameraController _cameraController;
    private bool _isRetryPromptOpened;

    protected override void Init()
    {
        base.Init();
        SceneType = Define.Scene.Game;
        PrepareFloorSpawner();
        SetupTwoSidedDissolvePreview();
        _player = FindFirstObjectByType<PlayerController>();
    }

    private void SetupTwoSidedDissolvePreview()
    {
        GameObject previewObject = GameObject.Find(twoSidedDissolvePreviewObjectName);
        if (previewObject == null)
            return;

        TwoSidedDissolveParticleController controller = previewObject.GetorAddComponent<TwoSidedDissolveParticleController>();
        controller.Configure();
    }

    private void PrepareFloorSpawner()
    {
        _floorOriginal = Managers.Resource.Load<GameObject>($"Prefabs/{floorPrefabName}");
        _spawnInterval = spawnPerSecond > 0f ? 1f / spawnPerSecond : 0f;
        _spawnAccumulator = 0f;
    }

    private void Update()
    {
        if (_spawnInterval > 0f)
        {
            if (_floorOriginal == null)
                PrepareFloorSpawner();

            if (_floorOriginal != null)
            {
                _spawnAccumulator += Time.deltaTime;
                while (_spawnAccumulator >= _spawnInterval)
                {
                    _spawnAccumulator -= _spawnInterval;
                    SpawnFloorFromPool();
                }
            }
        }

        CheckPlayerOutOfBounds();
    }

    private void SpawnFloorFromPool()
    {
        Poolable pooledFloor = Managers.Pool.Pop(_floorOriginal, null);
        if (pooledFloor == null)
            return;

        float angle = UnityEngine.Random.value * Mathf.PI * 2f;
        Vector3 spawnPosition = new(
            Mathf.Cos(angle) * spawnRadiusFromYAxis,
            spawnY,
            Mathf.Sin(angle) * spawnRadiusFromYAxis);

        Vector3 surfaceNormal = new(spawnPosition.x, 0f, spawnPosition.z);
        if (surfaceNormal.sqrMagnitude <= Define.epsilon)
            surfaceNormal = Vector3.up;
        else
            surfaceNormal.Normalize();

        Transform floorTransform = pooledFloor.transform;
        Quaternion spawnRotation = Quaternion.LookRotation(Vector3.up, surfaceNormal);
        floorTransform.position = spawnPosition;
        floorTransform.rotation = spawnRotation;

        BoxCollider floorCollider = floorTransform.GetComponentInChildren<BoxCollider>();
        if (floorCollider != null && IsOverlappingWithExistingFloors(floorTransform, floorCollider))
        {
            Managers.Resource.Destory(pooledFloor.gameObject);
            return;
        }

        FloorController controller = pooledFloor.gameObject.GetorAddComponent<FloorController>();
        controller.Configure(floorMoveSpeed, floorDespawnY);
    }

    private bool IsOverlappingWithExistingFloors(Transform spawnedRoot, BoxCollider box)
    {
        // BoxCollider 정보로 OverlapBox 파라미터 구성
        Vector3 worldCenter = box.transform.TransformPoint(box.center);

        // lossyScale 반영한 halfExtents
        Vector3 halfExtents = Vector3.Scale(box.size, box.transform.lossyScale) * 0.5f;

        // 살짝 줄여서(또는 0으로) "닿기" 판정 튜닝
        if (overlapShrink > 0f)
        {
            halfExtents -= Vector3.one * overlapShrink;
            halfExtents.x = Mathf.Max(halfExtents.x, 0.001f);
            halfExtents.y = Mathf.Max(halfExtents.y, 0.001f);
            halfExtents.z = Mathf.Max(halfExtents.z, 0.001f);
        }

        Quaternion orientation = box.transform.rotation;

        int count = Physics.OverlapBoxNonAlloc(
            worldCenter,
            halfExtents,
            _overlapBuffer,
            orientation,
            floorOverlapMask,
            QueryTriggerInteraction.Ignore);

        for (int i = 0; i < count; i++)
        {
            Collider hit = _overlapBuffer[i];
            if (hit == null)
                continue;

            if (hit.transform.IsChildOf(spawnedRoot))
                continue;

            return true;
        }

        return false;
    }

    private void CheckPlayerOutOfBounds()
    {
        if (_isRetryPromptOpened)
            return;

        Vector3 playerPosition = _player.gameObject.transform.position;
        float distanceFromYAxis = new Vector2(playerPosition.x, playerPosition.z).magnitude;
        bool isFarFromYAxis = distanceFromYAxis >= retryPromptDistanceFromYAxis;
        bool isPastMinY = playerPosition.y <= retryPromptMinY;
        if (!isFarFromYAxis && !isPastMinY)
            return;

        ShowRetryMessage();
    }

    private void ShowRetryMessage()
    {
        _isRetryPromptOpened = true;
        Managers.Input.SetMode(Define.InputMode.UI);

        UI_Message opened = FindAnyObjectByType<UI_Message>();
        if (opened != null)
        {
            opened.SetMessage(retryPromptMessage);
            opened.SetConfirmAction(ReloadGameScene);
            return;
        }

        UI_Message message = Managers.UI.ShowSceneUI<UI_Message>();
        if (message == null)
            return;

        message.SetMessage(retryPromptMessage);
        message.SetConfirmAction(ReloadGameScene);
    }

    private static void ReloadGameScene()
    {
        Time.timeScale = 1f;
        Managers.Clear();
        SceneManager.LoadScene("GameScene");
    }

    public override void Clear()
    {
    }

    public static IReadOnlyList<string> GetHandledObjectNames()
    {
        return Array.Empty<string>();
    }

    public static string GetActiveSmellName()
    {
        return string.Empty;
    }

    public bool IsSmellVisible(string smellName)
    {
        return false;
    }

    public void OnMenuSmellAreaClicked(string smellName)
    {
    }

    public void RegisterHandledObject(string objectName)
    {
    }

    public void ResumeGameFromMenu()
    {
    }

    public void ExitGameFromMenu()
    {
    }

    public void OnStartupMessageConfirmed()
    {
    }
}
