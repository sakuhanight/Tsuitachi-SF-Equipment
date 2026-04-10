# TSFE 共通パターン

**Tsuitachi-SF-Equipment** のコードベース全体で共有される設計パターン・実装パターンを文書化。リファクタリングや新規実装時の指針として使用。

最終更新: 2026-04-11

---

## 目次

- [DFUNC共通パターン](#dfunc共通パターン)
- [SFEXT共通パターン](#sfext共通パターン)
- [バスシステムパターン](#バスシステムパターン)
- [同期パターン](#同期パターン)
- [状態管理パターン](#状態管理パターン)
- [SAVControl参照パターン](#savcontrol参照パターン)
- [VR入力処理パターン](#vr入力処理パターン)
- [サウンド管理パターン](#サウンド管理パターン)
- [アニメーション制御パターン](#アニメーション制御パターン)
- [故障モデリングパターン](#故障モデリングパターン)
- [リセット・初期化パターン](#リセット初期化パターン)

---

## DFUNC共通パターン

すべてのDFUNC（Dial Function）コンポーネントで共有される設計。

### 自動注入フィールド

SaccEntityから自動的に注入される3つのフィールド：

```csharp
[System.NonSerialized] public bool LeftDial = false;
[System.NonSerialized] public int DialPosition = -999;  // 廃止予定
[System.NonSerialized] public SaccEntity EntityControl;
```

### 必須フィールド

```csharp
public UdonSharpBehaviour SAVControl;
public GameObject Dial_Funcon;          // 単一ダイヤル表示（旧）
public GameObject[] Dial_Funcon_Array;  // 複数ダイヤル表示（推奨）
```

### 必須実装メソッド

```csharp
public void DFUNC_LeftDial()
{
    trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
}

public void DFUNC_RightDial()
{
    trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
}

public void DFUNC_Selected()
{
    selected = true;

    // LeftDialに応じてtrackingTargetを設定（保険）
    trackingTarget = LeftDial
        ? VRCPlayerApi.TrackingDataType.LeftHand
        : VRCPlayerApi.TrackingDataType.RightHand;

    // 非Ownerが選択した場合、Ownershipを取得
    if (!isOwner)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }
}

public void DFUNC_Deselected()
{
    selected = false;
}
```

### SaccEntityライフサイクル

```csharp
public void SFEXT_L_EntityStart()
{
    // SAVControlから必要な変数を取得・キャッシュ
    vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
    controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
    if (!controlsRoot) controlsRoot = EntityControl.transform;

    // 初期化処理
    ResetStatus();
}

public void SFEXT_O_PilotEnter() { isPilot = true; isOwner = true; selected = false; }
public void SFEXT_O_PilotExit() { isPilot = false; }
public void SFEXT_O_TakeOwnership() { isOwner = true; }
public void SFEXT_O_LoseOwnership() { isOwner = false; }

public void SFEXT_G_PilotEnter() { hasPilot = true; gameObject.SetActive(true); }
public void SFEXT_G_PilotExit() { hasPilot = false; }
public void SFEXT_G_Explode() { ResetStatus(); }
public void SFEXT_G_RespawnButton() { ResetStatus(); }
```

### 共通状態変数

```csharp
private bool isPilot, isOwner, selected, hasPilot;
private VRCPlayerApi.TrackingDataType trackingTarget;
private Transform controlsRoot;
private Animator vehicleAnimator;
```

### ダイヤル表示管理

```csharp
// 選択時
TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, true);

// 非選択時
TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, false);
```

---

## SFEXT共通パターン

SaccEntity拡張（SFEXT）で共有される設計。

### 必須フィールド

```csharp
public UdonSharpBehaviour SAVControl;  // SaccAirVehicle参照
[System.NonSerialized] public SaccEntity EntityControl;  // 自動注入
```

### SaccEntityライフサイクル

```csharp
public void SFEXT_L_EntityStart()
{
    // SAVControlから必要な変数を取得・キャッシュ
    vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
    vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");

    // 初期化処理
    initialized = true;
}

public void SFEXT_O_PilotEnter() { isPilot = true; }
public void SFEXT_O_PilotExit() { isPilot = false; }
public void SFEXT_O_PassengerEnter() { isPassenger = true; }
public void SFEXT_O_PassengerExit() { isPassenger = false; }

public void SFEXT_G_PilotEnter() { gameObject.SetActive(true); }
public void SFEXT_G_PilotExit() { gameObject.SetActive(false); }
public void SFEXT_G_Explode() { ResetStatus(); }
public void SFEXT_G_RespawnButton() { ResetStatus(); }
```

### 共通状態変数

```csharp
private bool initialized;
private bool isPilot, isPassenger, isOwner;
private Animator vehicleAnimator;
private Rigidbody vehicleRigidbody;
```

---

## バスシステムパターン

電源・油圧・空圧バスで共通する設計。

### Indicator GameObjectパターン

状態をGameObjectの`activeInHierarchy`で表現：

```csharp
[Header("電源入力")]
public GameObject apuRunningIndicator;
public GameObject[] engineRunningIndicators;

private void UpdatePowerState()
{
    bool powered = false;

    // APUチェック
    if (apuRunningIndicator != null && apuRunningIndicator.activeInHierarchy)
    {
        powered = true;
    }

    // エンジンチェック
    if (!powered && engineRunningIndicators != null)
    {
        foreach (var indicator in engineRunningIndicators)
        {
            if (indicator != null && indicator.activeInHierarchy)
            {
                powered = true;
                break;
            }
        }
    }

    BusPowered = powered;
}
```

**利点:**
- 他のスクリプトは単にGameObject.SetActive(state)を呼ぶだけ
- 疎結合（`GetComponent`不要）
- 既存のアニメーション・VFXと統合しやすい

### 定期更新パターン

```csharp
public float updateInterval = 0.1f;
private float lastUpdateTime = 0f;

void Update()
{
    if (Time.time - lastUpdateTime < updateInterval)
        return;

    UpdateState();
    lastUpdateTime = Time.time;
}
```

### 公開プロパティパターン

```csharp
/// <summary>
/// バス電力が供給されているか（読み取り専用）
/// </summary>
[System.NonSerialized] public bool BusPowered = false;

/// <summary>
/// 手動でバス電力状態を取得
/// </summary>
public bool IsBusPowered()
{
    UpdatePowerState();
    return BusPowered;
}
```

**使用例:**
```csharp
// 他のコンポーネントから
if (powerBus.BusPowered) { ... }  // 現在のキャッシュ値
if (powerBus.IsBusPowered()) { ... }  // 最新値を取得
```

---

## 同期パターン

UdonSyncedとFieldChangeCallbackの組み合わせ。

### 基本形

```csharp
[UdonSynced, FieldChangeCallback(nameof(State))]
private bool _state = false;

public bool State
{
    get => _state;
    private set
    {
        _state = value;

        // 副作用（アニメーション、サウンド、ダイヤル表示等）
        if (vehicleAnimator) vehicleAnimator.SetBool(parameterName, value);
        TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, value);

        // 他のコンポーネントへ通知
        if (dependentComponent != null)
        {
            dependentComponent.SetProgramVariable("VariableName", value);
        }
    }
}

private void Toggle()
{
    State = !State;
    RequestSerialization();  // 即座に同期
}
```

### Enum同期パターン

```csharp
public enum EngineState { Off, Windmilling, Starting, Running, Seized }

[UdonSynced] private int _stateInt = 0;

public EngineState State
{
    get => (EngineState)_stateInt;
    private set { _stateInt = (int)value; }
}
```

**注意:** UdonはEnumを直接同期できないため、int経由で同期。

### 初期化時の注意

```csharp
private bool initialized = false;

public bool State
{
    private set
    {
        _state = value;
        if (!initialized) return;  // 初期化前は副作用をスキップ

        // 副作用処理...
    }
    get => _state;
}

public void SFEXT_L_EntityStart()
{
    // 初期化処理
    initialized = true;
    State = initialState;  // これで副作用が実行される
}
```

---

## 状態管理パターン

### Ownership管理

```csharp
private bool isOwner = false;

void Start()
{
    isOwner = Networking.IsOwner(gameObject);
}

public override void OnOwnershipTransferred(VRCPlayerApi player)
{
    isOwner = Networking.IsOwner(gameObject);
}

private void ModifyState()
{
    if (!isOwner)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }

    // 状態変更...
    RequestSerialization();
}
```

### パイロット状態管理

```csharp
private bool isPilot = false;
private bool isPassenger = false;
private bool hasPilot = false;  // グローバル（誰かがパイロット中）

public void SFEXT_O_PilotEnter() { isPilot = true; }
public void SFEXT_O_PilotExit() { isPilot = false; }
public void SFEXT_O_PassengerEnter() { isPassenger = true; }
public void SFEXT_O_PassengerExit() { isPassenger = false; }
public void SFEXT_G_PilotEnter() { hasPilot = true; }
public void SFEXT_G_PilotExit() { hasPilot = false; }
```

**用途:**
- `isPilot`: ローカルプレイヤーがパイロット → 入力処理、UI表示
- `isPassenger`: ローカルプレイヤーが乗客 → 乗客専用UI
- `hasPilot`: 誰かがパイロット中 → システム有効化判定

---

## SAVControl参照パターン

### 初期化時の取得

```csharp
public void SFEXT_L_EntityStart()
{
    // 頻繁にアクセスする変数はキャッシュ
    vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
    vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
    controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");

    // フォールバック
    if (!controlsRoot) controlsRoot = EntityControl.transform;
}
```

### ランタイム参照

```csharp
void FixedUpdate()
{
    // 動的な値はその都度取得
    float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
    bool engineOn = (bool)SAVControl.GetProgramVariable("EngineOn");
    Vector3 airVel = (Vector3)SAVControl.GetProgramVariable("AirVel");

    // 処理...
}
```

**注意:** `GetProgramVariable`はコストが高いため、ループ内では最小限に。

### 累積値の書き込み

```csharp
// ExtraDrag, ExtraLiftなどは累積型
float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
SAVControl.SetProgramVariable("ExtraDrag", currentDrag + deltaDrag);
```

**重要:** 常に現在値を読み取ってから加算。上書きではなく累積。

---

## VR入力処理パターン

### トリガー入力（アナログ）

```csharp
private VRCPlayerApi.TrackingDataType trackingTarget;

// DFUNC_LeftDial/RightDialで設定
public void DFUNC_LeftDial()
{
    trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
}

void Update()
{
    if (selected && Networking.LocalPlayer.IsUserInVR())
    {
        float trigger = TSFEUtil.GetTriggerInput(LeftDial);
        // 0.0 - 1.0 のアナログ値
    }
}
```

### トリガー入力（ブーリアン、エッジ検出）

```csharp
private bool _triggerLastFrame = false;

void Update()
{
    if (selected && Networking.LocalPlayer.IsUserInVR())
    {
        bool trigger = TSFEUtil.IsTriggerPressed(LeftDial);  // >0.75

        // 立ち上がりエッジ検出
        if (trigger && !_triggerLastFrame)
        {
            OnTriggerPress();
        }

        _triggerLastFrame = trigger;
    }
}
```

### VR手位置追跡

```csharp
void Update()
{
    if (selected && Networking.LocalPlayer.IsUserInVR())
    {
        var player = Networking.LocalPlayer;
        var handData = player.GetTrackingData(trackingTarget);
        Vector3 handWorldPos = handData.position;

        // ControlsRootローカル座標に変換
        Vector3 handLocalPos = controlsRoot.InverseTransformPoint(handWorldPos);

        // vrInputAxis方向への移動量を計算
        float movement = Vector3.Dot(handLocalPos - prevHandPos, vrInputAxis);
        prevHandPos = handLocalPos;

        // 移動量に応じた処理
        if (Mathf.Abs(movement) > controllerSensitivity)
        {
            // デテント変更等...
        }
    }
}
```

### デスクトップ入力

```csharp
public KeyCode desktopKey = KeyCode.F;

void Update()
{
    if (isPilot || selected)
    {
        if (Input.GetKeyDown(desktopKey))
        {
            OnInput();
        }
    }
}
```

---

## サウンド管理パターン

### 初期化時の音量・ピッチキャッシュ

```csharp
public AudioSource[] audioSources;
private float[] audioVolumes, audioPitches;

public void SFEXT_L_EntityStart()
{
    audioVolumes = new float[audioSources.Length];
    audioPitches = new float[audioSources.Length];

    for (var i = 0; i < audioSources.Length; i++)
    {
        var src = audioSources[i];
        if (!src) continue;
        audioVolumes[i] = src.volume;
        audioPitches[i] = src.pitch;
    }
}
```

**理由:** Prefab/Unityエディタで設定した元の値を保持し、ランタイムで相対調整可能に。

### 音量倍率パターン

```csharp
[Header("サウンド音量調整 (1.0 = 通常)")]
[Range(0f, 2f)]
public float volumeMultiplier = 1f;

void UpdateSound()
{
    if (audioSource != null)
    {
        audioSource.volume = baseVolume * volumeMultiplier * dynamicFactor;
    }
}
```

### キャノピー連動音量制御

```csharp
[Header("キャノピー連動音量制御")]
public bool enableCanopyAttenuation = true;
public string canopyParameterName = "canopy";
public bool invertCanopyParameter = false;
[Range(0f, 1f)]
public float canopyClosedVolumeMultiplier = 0.3f;

private Animator vehicleAnimator;

void Update()
{
    float canopyFactor = 1f;

    if (enableCanopyAttenuation && vehicleAnimator != null)
    {
        bool canopyOpen = vehicleAnimator.GetBool(canopyParameterName);
        if (invertCanopyParameter) canopyOpen = !canopyOpen;

        canopyFactor = canopyOpen ? 1f : canopyClosedVolumeMultiplier;
    }

    audioSource.volume = baseVolume * volumeMultiplier * canopyFactor;
}
```

### クロスフェードパターン

```csharp
public AudioSource startSound;
public AudioSource loopSound;
public AudioSource stopSound;

private float startVol, loopVol, stopVol;

void UpdateCrossFade(float normalizedProgress)
{
    if (normalizedProgress < 0.5f)
    {
        // startSound → loopSound
        float t = normalizedProgress / 0.5f;
        startSound.volume = startVol * (1f - t);
        loopSound.volume = loopVol * t;
        stopSound.volume = 0f;
    }
    else
    {
        // loopSound → stopSound
        float t = (normalizedProgress - 0.5f) / 0.5f;
        startSound.volume = 0f;
        loopSound.volume = loopVol * (1f - t);
        stopSound.volume = stopVol * t;
    }
}
```

---

## アニメーション制御パターン

### パラメータ名のpublicフィールド化

```csharp
public string boolParameterName = "flaps";
public string angleParameterName = "flapsAngle";
public string targetAngleParameterName = "flapsTarget";

private Animator vehicleAnimator;

void UpdateAnimator()
{
    if (vehicleAnimator)
    {
        vehicleAnimator.SetBool(boolParameterName, deployed);
        vehicleAnimator.SetFloat(angleParameterName, currentAngle);
        vehicleAnimator.SetFloat(targetAngleParameterName, targetAngle);
    }
}
```

**利点:** エディタでパラメータ名を変更可能 → 複数の機体で再利用可能。

### AnimatorのNull安全パターン

```csharp
if (vehicleAnimator) vehicleAnimator.SetFloat("param", value);
```

常にnullチェックを行う（Animatorが設定されていない機体でもエラーにならない）。

---

## 故障モデリングパターン

### MTBF（平均故障間隔）パターン

```csharp
public float meanTimeBetweenFailures = 3600f;  // 秒

void FixedUpdate()
{
    if (TSFEUtil.CheckMTBF(Time.fixedDeltaTime, meanTimeBetweenFailures))
    {
        OnFailure();
    }
}
```

### ダメージ倍率付きMTBF

```csharp
public float overspeedDamageMultiplier = 2f;

void FixedUpdate()
{
    float damageMultiplier = (currentSpeed > speedLimit) ? overspeedDamageMultiplier : 1f;

    if (TSFEUtil.CheckMTBF(Time.fixedDeltaTime, meanTimeBetweenFailures, damageMultiplier))
    {
        OnFailure();
    }
}
```

**`CheckMTBF`の実装:**
```csharp
// TSFEUtil.cs
public static bool CheckMTBF(float deltaTime, float mtbf, float damageMultiplier = 1f)
{
    if (mtbf <= 0f) return false;
    float failureRate = damageMultiplier / mtbf;
    float failureProbability = failureRate * deltaTime;
    return Random.value < failureProbability;
}
```

---

## リセット・初期化パターン

### ResetStatusメソッド

```csharp
private void ResetStatus()
{
    // 状態変数をデフォルトに戻す
    currentAngle = 0f;
    targetAngle = 0f;
    detentIndex = 0;
    isBroken = false;

    // アニメーションをリセット
    if (vehicleAnimator)
    {
        vehicleAnimator.SetFloat(angleParameterName, 0f);
        vehicleAnimator.SetBool(brokenParameterName, false);
    }

    // サウンドを停止
    foreach (var src in audioSources)
    {
        if (src) src.Stop();
    }
}

public void SFEXT_G_Explode() { ResetStatus(); }
public void SFEXT_G_RespawnButton() { ResetStatus(); }
```

### 同期変数のリセット

```csharp
private void ResetStatus()
{
    // 同期変数のリセット
    State = false;

    // Ownerのみ同期を送信
    if (isOwner)
    {
        RequestSerialization();
    }
}
```

### バスシステムのリセット

```csharp
public bool initialState = false;

public void SFEXT_G_RespawnButton()
{
    ResetToInitialState();
}

public void SFEXT_G_Explode()
{
    ResetToInitialState();
}

private void ResetToInitialState()
{
    if (isOwner)
    {
        BatteryOn = initialState;
        RequestSerialization();
    }
}
```

---

## まとめ

### リファクタリング時の指針

以下の観点で共通パターンを適用：

1. **コード重複の削減**
   - 共通処理をTSFEUtilに移動
   - ベースクラス抽出（要検討：Udon制約あり）

2. **一貫性の向上**
   - 命名規則の統一（`isPilot`, `isOwner`, `vehicleAnimator`等）
   - イベントハンドラの実装順序統一

3. **保守性の向上**
   - publicフィールドに適切な`[Tooltip]`を追加
   - 状態変数にXMLドキュメントコメント

4. **テスト容易性**
   - SAVControl参照をインターフェース化（Mock対応）
   - 副作用をFieldChangeCallbackに集約

5. **パフォーマンス**
   - `GetProgramVariable`呼び出しの最小化
   - 頻繁なアクセスはキャッシュ
   - 定期更新はupdateIntervalで間引き

### 新規実装時のチェックリスト

- [ ] 適切な`UdonBehaviourSyncMode`を設定
- [ ] 必須ライフサイクルイベントを実装（`SFEXT_L_EntityStart`等）
- [ ] SAVControl参照を適切にキャッシュ
- [ ] 同期変数に`FieldChangeCallback`を使用
- [ ] Ownership変更時に`RequestSerialization()`を呼ぶ
- [ ] Animatorのnullチェックを実施
- [ ] `ResetStatus()`を実装し`SFEXT_G_Explode`等から呼ぶ
- [ ] デバッグログに`[ComponentName]`プレフィックスを追加
