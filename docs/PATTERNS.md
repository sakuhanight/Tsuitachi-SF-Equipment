# TSFE 共通パターン

**Tsuitachi-SF-Equipment** のコードベース全体で共有される設計パターン・実装パターンを文書化。リファクタリングや新規実装時の指針として使用。

最終更新: 2026-04-11

---

## 目次

- [DFUNC共通パターン](#dfunc共通パターン)
- [SFEXT共通パターン](#sfext共通パターン)
- [DFUNCとSFEXTの使い分け原則](#dfuncとsfextの使い分け原則)
- [バスシステムパターン](#バスシステムパターン)
- [同期パターン](#同期パターン)
- [状態管理パターン](#状態管理パターン)
- [SAVControl参照パターン](#savcontrol参照パターン)
- [VR入力処理パターン](#vr入力処理パターン)
- [サウンド管理パターン](#サウンド管理パターン)
- [アニメーション制御パターン](#アニメーション制御パターン)
- [故障モデリングパターン](#故障モデリングパターン)
- [リセット・初期化パターン](#リセット初期化パターン)
- [外部制御パターン](#外部制御パターン)

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

## DFUNCとSFEXTの使い分け原則

TSFEではコンポーネントを**DFUNC**と**SFEXT**の2種類に分類します。この使い分け基準を明確化します。

### DFUNC = パイロットの物理コントロール

**定義**: 現実のコックピットでパイロットが**手で直接操作する物理コントロール**の再現

```
現実のコックピットで...
  ↓
レバーを引く/押す
スイッチをON/OFFする
ダイヤルを回す
トリムホイールを回す
  ↓
DFUNC実装
```

#### DFUNC実装の必要条件

1. **VR/デスクトップ両対応の入力処理**
   - VRダイヤル選択（`DFUNC_Selected()` / `DFUNC_Deselected()`）
   - VR手追跡（`trackingTarget`）
   - デスクトップキーバインド

2. **パイロットの意識的な操作**
   - レバー位置を変更する
   - スイッチをトグルする
   - トリムホイールを回す

3. **即座の物理効果**
   - ExtraDrag/ExtraLift変更
   - SAVControlパラメータ変更
   - アニメーション再生

#### DFUNC実装例（TSFE）

| コンポーネント | 現実の操作 | VR入力 | キー入力 |
|-------------|----------|--------|---------|
| **DFUNC_AdvancedFlaps** | フラップレバー | ダイヤル回転 | F / Shift+F |
| **DFUNC_ElevatorTrim** | トリムホイール | 軸入力 | Up/Down |
| **DFUNC_AdvancedSpeedBrake** | スピードブレーキレバー | ダイヤル回転 | B（ホールド） |
| **DFUNC_ThrustReverser** | リバーサーレバー | ダイヤル回転 | V（トグル） |
| **DFUNC_AdvancedParkingBrake** | パーキングブレーキレバー | ダイヤル回転 | P（トグル） |
| **DFUNC_AdvancedWaterRudder** | ウォーターラダーレバー | ダイヤル回転 | キー |
| **DFUNC_MethodCaller** | 汎用スイッチ | ダイヤル回転 | キー |

---

### SFEXT = システム・自動制御・拡張機能

**定義**: 以下の**いずれか**に該当するコンポーネント

#### カテゴリ1: 自動制御・補助システム

**特性**: パイロット操作不要、条件ベースで自動動作

| コンポーネント | 動作条件 |
|-------------|---------|
| **SFEXT_AutoFlaps** | 速度/AoA/G/Machベース |
| **SFEXT_AutoStarter** | APU起動完了後、自動シーケンス |
| **SFEXT_DihedralEffect** | 横滑り検出時 |
| **SFEXT_WakeTurbulence** | 飛行中常時 |
| **SFEXT_EngineFanDriver** | エンジンRPMベース |
| **SFEXT_InstrumentsAnimationDriver** | SAV変数ベース |
| **SFEXT_Warning** | エンジン異常検出時 |

---

#### カテゴリ2: 複雑な内部状態を持つシステム

**特性**: State Enum、多数の内部変数、間接制御

| コンポーネント | 内部状態 | 制御方法 |
|-------------|---------|---------|
| **SFEXT_AdvancedEngine** | `EngineState`, N1, N2, EGT, ECT | SAVのThrottle/EngineOn経由 |
| **SFEXT_AuxiliaryPowerUnit** | `APUState`, RPM, EGT | SAVのStartボタン経由 |
| **SFEXT_AdvancedPropellerThrust** | PropRPM, BladeAngle | SAVのThrottle経由 |

**なぜSFEXT?**:
```csharp
// パイロットは直接N1/N2を操作しない
// Throttle → SFEXT_AdvancedEngine → N1/N2計算 → Thrust出力
SAVControl.SetProgramVariable("ThrottleStrength", thrust);
```

DFUNCとして実装するには**内部状態が複雑すぎる**ため、SFEXTとして分離。

---

#### カテゴリ3: 物理制約・依存システム

**特性**: バスシステム依存、物理演算、拡張機能

| コンポーネント | 依存システム | 理由 |
|-------------|------------|------|
| **SFEXT_AdvancedGear** | TSFE_HydraulicBus（計画） | 油圧バス統合、WheelCollider制御 |
| **SFEXT_Chock** | なし | ワールド配置Pickup、Rigidbody制約 |
| **SFEXT_EngineToggle** | なし | SAVのEngineOnを直接制御（UI用） |
| **SFEXT_BoardingCollider** | なし | 搭乗判定コライダー制御 |

**SFEXT_AdvancedGearの特殊性**:
- 標準SFVに**DFUNC_Gear**が既存（GearUp/GearDownイベント発行）
- SFEXT_AdvancedGearは**拡張機能**として動作
  - WheelCollider物理制御
  - 油圧バス統合（計画）
  - 故障モデリング（タイヤバースト）
  - ステアリング・ブレーキ制御

```csharp
// 標準DFUNC_Gearからイベント受信
public void SFEXT_G_GearUp() { targetPosition = 0; }
public void SFEXT_G_GearDown() { targetPosition = 1; }
```

---

### 判定フローチャート

新規コンポーネント実装時の判定：

```
┌────────────────────────────────────┐
│ パイロットが手で直接操作する       │
│ 物理コントロール（レバー/スイッチ）│
│ か？                               │
└─────────┬──────────────────────────┘
          │
    YES   │   NO
          ↓
    ┌─────────┐
    │  DFUNC  │
    └─────────┘
          │
          ↓
    以下を実装:
    • DFUNC_Selected/Deselected
    • DFUNC_LeftDial/RightDial
    • VR手追跡
    • デスクトップキー

          ↓
┌────────────────────────────────────┐
│ 以下のいずれかに該当するか？       │
├────────────────────────────────────┤
│ 1. 自動動作（パイロット操作不要）  │
│ 2. 複雑な内部状態（State Enum等）  │
│ 3. 他システム依存（バス等）        │
│ 4. 間接制御（SAV経由）             │
│ 5. 物理効果・視覚効果のみ          │
│ 6. 既存DFUNCの拡張機能             │
└─────────┬──────────────────────────┘
          │
    YES   │
          ↓
    ┌─────────┐
    │  SFEXT  │
    └─────────┘
```

---

### 外部制御パターンとの関係

DFUNCとSFEXTの使い分けは、外部制御パターンの設計にも影響します。

#### 制御層の設計

```
┌─────────────────────────────────┐
│  手動制御層（DFUNC）             │  ← パイロットの物理操作
├─────────────────────────────────┤
│ • VRダイヤル                     │
│ • デスクトップキー               │
│ • trackingTarget手追跡           │
└──────────┬──────────────────────┘
           │ TargetAngle (UdonSynced)
           ↓
┌─────────────────────────────────┐
│  コアロジック層（DFUNC）         │  ← 物理・同期・アニメーション
├─────────────────────────────────┤
│ • ExtraDrag/ExtraLift計算        │
│ • Udon同期                       │
│ • 故障モデリング                 │
└────┬─────────────────┬──────────┘
     │                 │
     ↓                 ↓
┌─────────┐      ┌─────────────┐
│ 自動制御 │      │  拡張機能   │
│ (SFEXT) │      │  (SFEXT)    │
├─────────┤      ├─────────────┤
│ AutoFlaps│     │ ContactFlaps│
└─────────┘      └─────────────┘
  ↑ SetTargetAngle()
```

#### DFUNCに外部制御APIを実装

```csharp
// DFUNC_AdvancedFlaps, DFUNC_AdvancedSpeedBrake など
public void SetTargetAngle(float angle)
{
    if (!isOwner) return;
    if (isPilot || selected) return; // 手動操作が最優先
    TargetAngle = angle;
}

public bool IsManualControlActive() => isPilot || selected;
```

#### SFEXTから呼び出す

```csharp
// SFEXT_AutoFlaps（自動制御）
if (!flapsControl.IsManualControlActive())
{
    flapsControl.SetTargetAngle(autoAngle);
}

// SFEXT_ContactFlaps（VR物理レバー、将来実装）
if (!flapsControl.IsManualControlActive())
{
    flapsControl.SetTargetDetent(contactDetent);
}
```

---

### 設計原則のまとめ

| 観点 | DFUNC | SFEXT |
|------|-------|-------|
| **目的** | パイロットの物理操作再現 | システム・自動制御・拡張機能 |
| **入力** | VRダイヤル + デスクトップキー | 条件ベース自動動作 or SAV経由 |
| **状態複雑度** | シンプル（角度・detent等） | 複雑（State Enum、多数の変数） |
| **依存性** | 独立動作 | バスシステム・他コンポーネント依存可 |
| **必須実装** | DFUNC_Selected等のイベント | SFEXT_L_EntityStart等のライフサイクル |
| **Ownership** | DFUNC_Selected時に取得 | isPilot時にOwner（通常） |
| **同期** | Continuous（通常） | 機能による（None/Manual/Continuous） |

**金言**: 「パイロットが手で触るものはDFUNC、触らないものはSFEXT」

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

## 外部制御パターン

DFUNCコンポーネント（FlapやSpeedBrakeなど）を複数の制御ソースから操作可能にする設計パターン。

### 設計原則

**制御層の分離**: DFUNCは物理・同期・アニメーションのコアロジックを担当し、外部制御APIを公開する。

```
┌─────────────────────────────────────┐
│  制御入力層（複数可）                 │
├─────────────────────────────────────┤
│ • DFUNC手動操作（ダイヤル+デスクトップ）│
│ • SFEXT_Auto* （自動制御）            │
│ • SFEXT_Contact* （VR物理レバー）     │
│ • SFEXT_Keyboard* （キーバインド）    │
└──────────┬──────────────────────────┘
           │ SetTargetAngle(), SetDetent() など
           ↓
┌─────────────────────────────────────┐
│  DFUNC コアロジック層                 │
├─────────────────────────────────────┤
│ • 物理計算（ExtraDrag/ExtraLift）     │
│ • アニメーション制御                   │
│ • Udon同期（UdonSynced変数）          │
│ • Ownership管理                       │
│ • 故障モデリング                       │
└─────────────────────────────────────┘
```

### 制御優先順位

| 優先度 | 制御ソース | 条件 | 実装方法 |
|-------|----------|------|---------|
| **1** | 手動（ダイヤル/キー） | `isPilot \|\| selected` | DFUNC内部処理 |
| **2** | Contact操作 | `!IsManualControlActive()` | `SFEXT_Contact*` → `SetTargetDetent()` |
| **3** | 自動制御 | `!IsManualControlActive()` | `SFEXT_Auto*` → `SetTargetAngle()` |

### DFUNC側の実装パターン

```csharp
public class DFUNC_AdvancedFlaps : UdonSharpBehaviour
{
    // ========== 外部制御API ==========
    /// <summary>
    /// 外部から目標角度を設定（自動制御用）
    /// 手動制御中（isPilot || selected）は無視される
    /// </summary>
    public void SetTargetAngle(float angle)
    {
        if (!isOwner) return;                // OwnerチェックでローカルOwnerのみ制御可能
        if (isPilot || selected) return;     // 手動制御が最優先
        TargetAngle = angle;                 // UdonSynced変数なので自動同期
    }

    /// <summary>
    /// 外部から目標Detentを設定（Contact操作用）
    /// </summary>
    public void SetTargetDetent(int index)
    {
        if (!isOwner) return;
        if (isPilot || selected) return;
        SetDetent(index);
    }

    /// <summary>
    /// 手動制御が有効か確認（外部制御コンポーネントがチェックする）
    /// </summary>
    public bool IsManualControlActive()
    {
        return isPilot || selected;
    }

    // ========== 内部実装 ==========
    private void Update()
    {
        if (!isOwner) return;

        // 手動入力処理（既存実装）
        if (isPilot || selected)
        {
            // VR/デスクトップ入力処理...
            TriggerState = selected && TSFEUtil.IsTriggerPressed(LeftDial);
            if (Input.GetKeyDown(flapsUpKey)) DecreaseDetent();
            // ...
        }

        // 物理計算、アニメーション更新...
    }
}
```

### 自動制御SFEXT側の実装パターン

```csharp
public class SFEXT_AutoFlaps : UdonSharpBehaviour
{
    public DFUNC_AdvancedFlaps flapsControl;

    private bool isPilot, isOwner;

    public void SFEXT_O_PilotEnter()
    {
        isPilot = true;
        isOwner = true;
    }

    public void SFEXT_O_PilotExit()
    {
        isPilot = false;
    }

    private void Update()
    {
        if (!isPilot) return;                             // パイロットのみ
        if (!flapsControl) return;
        if (flapsControl.IsManualControlActive()) return; // 手動制御中は何もしない

        // 自動制御ロジック
        float targetAngle = CalculateAutoFlaps();
        flapsControl.SetTargetAngle(targetAngle);
    }

    private float CalculateAutoFlaps()
    {
        // 速度、AoA、Gベースの計算...
        return calculatedAngle;
    }
}
```

### Contact操作SFEXT側の実装パターン

```csharp
public class SFEXT_ContactFlaps : UdonSharpBehaviour
{
    public DFUNC_AdvancedFlaps flapsControl;
    public Transform flapsLeverTransform;

    [Header("Lever Settings")]
    public float leverMinAngle = 0f;
    public float leverMaxAngle = 60f;

    private bool isPilot;

    public void SFEXT_O_PilotEnter() { isPilot = true; }
    public void SFEXT_O_PilotExit() { isPilot = false; }

    private void Update()
    {
        if (!isPilot) return;
        if (!flapsControl) return;
        if (flapsControl.IsManualControlActive()) return;

        // レバー角度からdetent計算
        float leverAngle = flapsLeverTransform.localEulerAngles.x;
        if (leverAngle > 180f) leverAngle -= 360f; // -180~180に正規化

        int detentCount = flapsControl.detentAngles.Length;
        int targetDetent = Mathf.RoundToInt(
            Mathf.InverseLerp(leverMinAngle, leverMaxAngle, leverAngle) * (detentCount - 1)
        );

        flapsControl.SetTargetDetent(targetDetent);
    }
}
```

### Ownership管理の原則

- **DFUNCがOwnershipを持つ**: 物理計算とUdon同期のため
- **外部制御コンポーネントはOwnership取得不要**: DFUNCのAPIを呼ぶだけ
- **DFUNC_Selected時にOwnership取得**: ダイヤル選択時に自動取得

```csharp
// DFUNC側
public void DFUNC_Selected()
{
    selected = true;
    trackingTarget = LeftDial ? VRCPlayerApi.TrackingDataType.LeftHand
                               : VRCPlayerApi.TrackingDataType.RightHand;

    // 非Ownerが選択した場合、Ownershipを取得
    if (!isOwner)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }
}

// 外部制御API（OwnerチェックでローカルOwnerのみ制御可能）
public void SetTargetAngle(float angle)
{
    if (!isOwner) return; // 重要: Ownershipを取得せず、既存Ownerのみ制御
    if (isPilot || selected) return;
    TargetAngle = angle;
}
```

### 実装例: DFUNC_AdvancedFlaps + SFEXT_AutoFlaps

現在のTSFE実装では、このパターンが既に部分的に採用されています：

**DFUNC_AdvancedFlaps.cs**:
- `TargetAngle`プロパティが`public`（外部から設定可能）
- `detentAngles`が`public`（外部から参照可能）

**SFEXT_AutoFlaps.cs**:
- `Update()`で`flapsControl.TargetAngle`を直接設定
- パイロット搭乗時のみ動作（`isPilot`チェック）

### 推奨される改善

既存実装を明示的な外部制御パターンに準拠させる：

1. **DFUNCに外部制御用メソッド追加**:
   ```csharp
   public void SetTargetAngle(float angle) { ... }
   public bool IsManualControlActive() { return isPilot || selected; }
   ```

2. **SFEXTでチェック追加**:
   ```csharp
   if (flapsControl.IsManualControlActive()) return;
   ```

3. **Contact操作用SFEXTの作成** （必要に応じて）

### 利点

- **関心の分離**: 制御ロジックとコアロジックが独立
- **拡張性**: 新しい制御方法を追加しやすい
- **優先順位の明確化**: 手動 > Contact > 自動
- **テスト容易性**: Mock実装でテスト可能

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
