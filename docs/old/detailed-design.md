# SF-AdvancedEquipment 詳細設計書

## 1. 共通設計

### 1.1 ユーティリティクラス (SFAEUtil)

全スクリプトで重複するコードパターンを静的メソッドに集約し、冗長性排除と可読性向上を図る。

UdonSharp 1.x では `UdonSharpBehaviour` 内の `static` メソッドがサポートされるため、1クラスに静的メソッドをまとめる。

**ファイル:** `Runtime/Utility/SFAEUtil.cs`

```csharp
namespace SFAdvEquipment.Utility
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFAEUtil : UdonSharpBehaviour
    {
        // ============================================================
        // 単位変換定数
        // ============================================================
        public const float MS_TO_KNOTS = 1.94384f;
        public const float KNOTS_TO_MS = 0.514444f;
        public const float METERS_TO_FEET = 3.28084f;
        public const float FEET_TO_METERS = 0.3048f;
        public const float FPM_TO_MS = 0.00508f;      // ft/min → m/s

        // ============================================================
        // 数学ユーティリティ
        // ============================================================

        /// <summary>値を [oldMin, oldMax] → [0, 1] にリマップ (クランプなし)</summary>
        public static float Remap01(float value, float oldMin, float oldMax)
        {
            return (value - oldMin) / (oldMax - oldMin);
        }

        /// <summary>値を [oldMin, oldMax] → [0, 1] にリマップ (0-1 クランプ)</summary>
        public static float ClampedRemap01(float value, float oldMin, float oldMax)
        {
            return Mathf.Clamp01(Remap01(value, oldMin, oldMax));
        }

        /// <summary>値を [oldMin, oldMax] → [newMin, newMax] にリマップ (クランプ)</summary>
        public static float ClampedRemap(
            float value, float oldMin, float oldMax, float newMin, float newMax)
        {
            return ClampedRemap01(value, oldMin, oldMax) * (newMax - newMin) + newMin;
        }

        /// <summary>3点補間: t が [tMin,tMid] で a→b、[tMid,tMax] で b→c</summary>
        public static float Lerp3(
            float a, float b, float c, float t,
            float tMin, float tMid, float tMax)
        {
            return Mathf.Lerp(a,
                Mathf.Lerp(b, c, Remap01(t, tMid, tMax)),
                Remap01(t, tMin, tMid));
        }

        // ============================================================
        // 故障判定 (MTBF)
        // ============================================================

        /// <summary>
        /// MTBF (平均故障間隔) に基づく確率的故障判定。
        /// 毎フレーム呼び出して使用する。
        /// </summary>
        /// <returns>このフレームで故障が発生した場合 true</returns>
        public static bool CheckMTBF(float deltaTime, float mtbf)
        {
            return Random.value < deltaTime / mtbf;
        }

        /// <summary>ダメージ倍率付き MTBF 判定</summary>
        public static bool CheckMTBF(float deltaTime, float mtbf, float damageMultiplier)
        {
            return Random.value < damageMultiplier * deltaTime / mtbf;
        }

        // ============================================================
        // DFUNC ヘルパー
        // ============================================================

        /// <summary>VR トリガー入力値を取得</summary>
        public static float GetTriggerInput(bool leftDial)
        {
            return Input.GetAxisRaw(leftDial
                ? "Oculus_CrossPlatform_PrimaryIndexTrigger"
                : "Oculus_CrossPlatform_SecondaryIndexTrigger");
        }

        /// <summary>VR トリガーが押されているか (閾値 0.75)</summary>
        public static bool IsTriggerPressed(bool leftDial)
        {
            return GetTriggerInput(leftDial) > 0.75f;
        }

        /// <summary>Dial_Funcon と Dial_Funcon_Array の表示切替</summary>
        public static void SetDialFuncon(
            GameObject dialFuncon, GameObject[] dialFunconArray, bool active)
        {
            if (dialFuncon) dialFuncon.SetActive(active);
            if (dialFunconArray != null)
            {
                for (int i = 0; i < dialFunconArray.Length; i++)
                {
                    if (dialFunconArray[i]) dialFunconArray[i].SetActive(active);
                }
            }
        }

        /// <summary>ハプティクスフィードバック再生</summary>
        public static void PlayHaptics(
            bool leftDial, float duration, float amplitude, float frequency)
        {
            Networking.LocalPlayer.PlayHapticEventInHand(
                leftDial ? VRC_Pickup.PickupHand.Left : VRC_Pickup.PickupHand.Right,
                duration, amplitude, frequency);
        }

        // ============================================================
        // 航空単位変換ヘルパー
        // ============================================================

        /// <summary>m/s → KIAS (ノット)</summary>
        public static float ToKnots(float ms) => ms * MS_TO_KNOTS;

        /// <summary>KIAS → m/s</summary>
        public static float FromKnots(float knots) => knots * KNOTS_TO_MS;

        /// <summary>メートル → フィート</summary>
        public static float ToFeet(float meters) => meters * METERS_TO_FEET;

        /// <summary>フィート → メートル</summary>
        public static float FromFeet(float feet) => feet * FEET_TO_METERS;
    }
}
```

#### 使用例

```csharp
using SFAdvEquipment.Utility;

// 数学ユーティリティ
float normalized = SFAEUtil.ClampedRemap01(altitude, 0, maxAltitude);
float value = SFAEUtil.Lerp3(idle, continuous, takeoff, ect, idleECT, contECT, toECT);

// MTBF 判定
if (SFAEUtil.CheckMTBF(Time.deltaTime, mtbfActuatorBroken, overspeedDamage))
    actuatorBroken = true;

// 単位変換
float kias = SFAEUtil.ToKnots(airVehicle.AirSpeed);
float altFt = SFAEUtil.ToFeet(position.y - seaLevel);

// DFUNC ヘルパー
SFAEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, active);
if (SFAEUtil.IsTriggerPressed(LeftDial)) { ... }
SFAEUtil.PlayHaptics(LeftDial, hapticDuration, hapticAmplitude, hapticFrequency);
```

#### パターン適用マトリクス

| メソッド | 使用スクリプト数 | 主な使用箇所 |
|---------|----------------|-------------|
| `Remap01` | 6 | InstrumentsAnimationDriver, AdvancedEngine, GPWS |
| `ClampedRemap01` | 3 | AuralWarnings, AdvancedGear, AdvancedEngine |
| `Lerp3` | 2 | AdvancedEngine (8箇所), WakeTurbulence |
| `CheckMTBF` | 4 | AdvancedFlaps, AdvancedGear, AdvancedEngine, AdvancedPropellerThrust |
| `GetTriggerInput` | 10+ | 全 DFUNC |
| `SetDialFuncon` | 9+ | 全 DFUNC |
| `PlayHaptics` | 3 | AdvancedFlaps, ElevatorTrim, IHaveControl |
| `ToKnots` | 7 | AdvancedFlaps, GPWS, AuralWarnings, AdvancedGear 等 |
| `ToFeet` | 5 | GPWS, InstrumentsAnimationDriver |

---

### 1.2 SFV 1.8 標準 DFUNC パターン

SFV 1.8 の DFUNC は `UdonSharpBehaviour` を直接継承し、以下のフィールドが `SaccEntity.Start()` により自動注入される。

```csharp
// SaccEntity が SetProgramVariable で注入するフィールド
[System.NonSerialized] public SaccEntity EntityControl;
[System.NonSerialized] public bool LeftDial = false;
[System.NonSerialized] public int DialPosition = -999;

// Inspector で設定 (SaccAirVehicle への参照)
public UdonSharpBehaviour SAVControl;
```

**VR トリガー入力パターン (DFUNC_Base 廃止後):**

```csharp
// SFV 1.8 標準: Update() 内で直接読み取り
float Trigger;
if (LeftDial)
    Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger");
else
    Trigger = Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");
```

**SAVControl データアクセスパターン:**

```csharp
// Get
float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
bool engineOn = (bool)SAVControl.GetProgramVariable("EngineOn");

// Set (加算型)
SAVControl.SetProgramVariable("ExtraDrag",
    (float)SAVControl.GetProgramVariable("ExtraDrag") + delta);
```

### 1.3 EsnyaSFAddons からの共通変更点

| 変更項目 | EsnyaSFAddons | SF-AdvEquipment |
|----------|--------------|-----------------|
| 基底クラス (DFUNC) | `DFUNC_Base` | `UdonSharpBehaviour` 直接継承 |
| VR トリガー処理 | `DFUNC_Base.Update()` が自動処理 | 各 DFUNC で自前実装 |
| 名前空間 | `EsnyaSFAddons.DFUNC` / `.SFEXT` | `SFAdvEquipment.DFUNC` / `.SFEXT` |
| UdonToolkit 属性 | `[SectionHeader]`, `[HideIf]`, `[Popup]`, `[UTEditor]` | `[Header]`, `[Tooltip]` に置換、または削除 |
| InariUdon 依存 | `UdonLogger` 等 | 排除 |
| JetBrains.Annotations | `[NotNull]`, `[CanBeNull]` | 削除 |
| SaccAirVehicle 参照 | 直接型参照 `SaccAirVehicle airVehicle` | `UdonSharpBehaviour SAVControl` + `GetProgramVariable` |
| Dial_Funcon | `GameObject Dial_Funcon` のみ | `GameObject Dial_Funcon` + `GameObject[] Dial_Funcon_Array` 両対応 |

### 1.4 DFUNC_Base が提供していた機能の移植

`DFUNC_Base` (62行) は以下を提供していた。SF-AdvEquipment では各スクリプトに展開する。

```csharp
// DFUNC_Base の機能 → 各 DFUNC に展開
// 1. DFUNC_LeftDial / DFUNC_RightDial → triggerAxis 設定
// 2. DFUNC_Selected / DFUNC_Deselected → isSelected + gameObject.SetActive
// 3. Update() → トリガー読み取り → DFUNC_TriggerPressed / Released
// 4. SFEXT_L_EntityStart / SFEXT_O_PilotEnter / Exit → Deselected 呼び出し
```

**展開テンプレート (ActivateOnSelected=true の場合):**

```csharp
using SFAdvEquipment.Utility;

private bool _triggerLastFrame;
private bool _selected;

public void DFUNC_LeftDial() { }   // LeftDial は SaccEntity が注入
public void DFUNC_RightDial() { }  // LeftDial は SaccEntity が注入

public void DFUNC_Selected() { _selected = true; gameObject.SetActive(true); }
public void DFUNC_Deselected() { _selected = false; gameObject.SetActive(false); }

private void Update()
{
    if (_selected && Networking.LocalPlayer.IsUserInVR())
    {
        var trigger = SFAEUtil.IsTriggerPressed(LeftDial);
        if (trigger != _triggerLastFrame)
        {
            if (trigger) OnTriggerPressed();
            else OnTriggerReleased();
        }
        _triggerLastFrame = trigger;
    }
}
```

**展開テンプレート (ActivateOnSelected=false の場合):**

DFUNC_AdvancedParkingBrake, DFUNC_AdvancedWaterRudder 等はダイアル選択不要で動作するため、`DFUNC_Selected`/`DFUNC_Deselected` で `gameObject.SetActive` を呼ばない。

### 1.5 SaccAirVehicle フィールドマップ

DFUNC/SFEXT から参照する SaccAirVehicle の主要フィールド:

| フィールド名 | 型 | 用途 |
|-------------|---|------|
| `ExtraDrag` | `float` | 追加抗力係数 (加算型) |
| `ExtraLift` | `float` | 追加揚力係数 (加算型) |
| `ExtraVelLift` | `float` | 速度揚力係数 |
| `MaxLift` | `float` | 最大揚力 |
| `ThrottleStrength` | `float` | 推力強度 |
| `EngineOutput` | `float` | 現在エンジン出力 |
| `_EngineOn` / `EngineOn` | `bool` | エンジン状態 |
| `Fuel` | `float` | 現在燃料量 |
| `FullFuel` | `float` | 満タン燃料量 |
| `LowFuel` | `float` | 低燃料閾値 |
| `AirSpeed` | `float` | 対気速度 |
| `AirVel` | `Vector3` | 対気速度ベクトル |
| `Speed` | `float` | 対地速度 |
| `CurrentVel` | `Vector3` | 速度ベクトル |
| `Wind` | `Vector3` | 風ベクトル |
| `WindGustiness` | `float` | 突風強度 |
| `WindTurbulanceScale` | `float` | 乱気流スケール |
| `Atmosphere` | `float` | 大気密度係数 (0-1) |
| `SeaLevel` | `float` | 海面高度 |
| `PitchStrength` | `float` | ピッチ強度 |
| `RotMultiMaxSpeed` | `float` | 最大操舵速度 |
| `Taxiing` | `bool` | 地上走行中 |
| `Floating` | `bool` | 着水中 |
| `PitchDown` | `bool` | 負迎角 |
| `ControlsRoot` | `Transform` | 操縦入力基準Transform |
| `VehicleAnimator` | `Animator` | 車両アニメータ |
| `VehicleRigidbody` | `Rigidbody` | 車両Rigidbody |
| `VehicleTransform` | `Transform` | 車両Transform |
| `ThrottleOverridden` | `int` | スロットルオーバーライドカウンタ |
| `ThrottleOverride` | `float` | スロットルオーバーライド値 |
| `ThrottleInput` | `float` | スロットル入力値 |
| `RotationInputs` | `Vector3` | 操縦入力 (x=pitch, y=yaw, z=roll) |
| `AirFriction` | `float` | 空気抵抗係数 |
| `DisableGroundDetection` | `int` | 地上検出無効カウンタ |
| `DisableTaxiRotation_` | `int` | 地上旋回無効カウンタ |

### 1.6 SFEXTP イベント体系

`SAV_PassengerFunctionsController` が発火するイベント:

| イベント名 | タイミング |
|-----------|-----------|
| `SFEXTP_L_EntityStart` | エンティティ初期化 |
| `SFEXTP_O_UserEnter` | ローカルユーザーが搭乗 |
| `SFEXTP_O_UserExit` | ローカルユーザーが降車 |
| `SFEXTP_G_PilotEnter` | パイロット搭乗 (全クライアント) |
| `SFEXTP_G_PilotExit` | パイロット降車 (全クライアント) |
| `SFEXTP_O_PlayerJoined` | プレイヤー参加 |
| `SFEXTP_G_Explode` | 爆発 |
| `SFEXTP_G_RespawnButton` | リスポーン |

---

## 2. Phase 1: コア装備 (8スクリプト)

---

### 2.1 DFUNC_AdvancedFlaps

**元ファイル:** `DFUNC/DFUNC_AdvancedFlaps.cs` (372行)
**カテゴリ:** DFUNC | **同期:** `Continuous` | **難易度:** 高

#### 概要

多段デテントフラップ。速度制限・アクチュエータ破損・翼破損・ハプティクスフィードバックを備える。SFV 1.8 標準 `DFUNC_Flaps` は単純 ON/OFF のみで、これらの機能を持たない。

#### クラス定義

```csharp
namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedFlaps : UdonSharpBehaviour
```

#### フィールド

**Inspector 設定:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` | `GameObject` | - | ダイアル表示 |
| `Dial_Funcon_Array` | `GameObject[]` | - | ダイアル表示配列 (SFV 1.8対応追加) |
| `detents` | `float[]` | {0,1,2,5,10,15,25,30,40} | デテント角度 (度) |
| `speedLimits` | `float[]` | {340,250,...,162} | 各デテントの速度制限 (KIAS) |
| `dragMultiplier` | `float` | 1.4 | 抗力係数 |
| `liftMultiplier` | `float` | 1.35 | 揚力係数 |
| `response` | `float` | 1.0 | アクチュエータ応答速度 |
| `powerSource` | `GameObject` | - | 電源依存 (activeで動作) |
| `controllerSensitivity` | `float` | 0.1 | VR 感度 |
| `vrInputAxis` | `Vector3` | forward | VR 入力軸 |
| `desktopKey` | `KeyCode` | F | デスクトップキー |
| `seamless` | `bool` | true | シームレス/デテント入力 |
| `boolParameterName` | `string` | "flaps" | Animator bool |
| `angleParameterName` | `string` | "flapsangle" | Animator float (実角度) |
| `targetAngleParameterName` | `string` | "flapstarget" | Animator float (目標角度) |
| `brokenParameterName` | `string` | "flapsbroken" | Animator bool |
| `audioSources` | `AudioSource[]` | - | アクチュエータ音 |
| `soundResponse` | `float` | 1.0 | 音量応答速度 |
| `breakingSounds` | `AudioSource[]` | - | 破損音 |
| `meanTimeBetweenActuatorBrokenOnOverspeed` | `float` | 120.0 | MTBF(秒) |
| `meanTimeBetweenWingBrokenOnOverspeed` | `float` | 240.0 | MTBF(秒) |
| `overspeedDamageMultiplier` | `float` | 10.0 | 超過速度時ダメージ倍率 |
| `brokenDragMultiplier` | `float` | 2.9 | 破損時抗力 |
| `brokenLiftMultiplier` | `float` | 0.3 | 破損時揚力 |
| `hapticDuration` | `float` | 0.2 | ハプティクス |
| `hapticAmplitude` | `float` | 0.5 | ハプティクス |
| `hapticFrequency` | `float` | 0.1 | ハプティクス |

**同期変数:**

| フィールド | 型 | 同期 | 説明 |
|-----------|---|------|------|
| `targetAngle` | `float` | `UdonSyncMode.Smooth` | 目標フラップ角度 |
| `actuatorBroken` | `bool` | `UdonSynced` | アクチュエータ破損 |
| `_wingBroken` | `bool` | `FieldChangeCallback(WingBroken)` | 翼破損 |

**自動注入 (SaccEntity):**

| フィールド | 型 |
|-----------|---|
| `EntityControl` | `SaccEntity` |
| `LeftDial` | `bool` |
| `DialPosition` | `int` |

#### イベントハンドラ

| イベント | 処理 |
|---------|------|
| `SFEXT_L_EntityStart` | SAVControl から airVehicle 取得、初期化 |
| `SFEXT_O_PilotEnter` | パイロットフラグ設定、gameObject有効化 |
| `SFEXT_O_PilotExit` | パイロットフラグ解除 |
| `SFEXT_O_TakeOwnership` | オーナーフラグ設定 |
| `SFEXT_O_LoseOwnership` | オーナーフラグ解除 |
| `SFEXT_G_PilotEnter` | 全クライアントで表示更新 |
| `SFEXT_G_PilotExit` | 全クライアントで表示更新 |
| `SFEXT_G_Explode` | 状態リセット |
| `SFEXT_G_RespawnButton` | 状態リセット |
| `DFUNC_Selected` | 選択状態、gameObject有効化 |
| `DFUNC_Deselected` | 非選択状態 |

#### 主要アルゴリズム

**1. オーバースピード破損モデル:**
```
超過速度 = IAS - speedLimits[currentDetent]
if (超過速度 > 0):
    破損確率 = deltaTime / MTBF * (超過速度 * overspeedDamageMultiplier)
    Random.value < 破損確率 → actuatorBroken or wingBroken
```

**2. デテント管理:**
- VR: ハンド位置デルタ → targetAngle 連続変更 → 最近接デテントにスナップ
- Desktop: キー押下 → デテントインデックスを循環

**3. 空力パラメータ適用:**
```
extraDrag += (angle / maxAngle) * dragMultiplier
extraLift += (angle / maxAngle) * liftMultiplier
破損時: extraDrag += brokenDragMultiplier, extraLift *= brokenLiftMultiplier
```

**4. 音響:**
- アクチュエータ移動速度に比例した音量・ピッチ
- ランダムオフセットで音の多様性

#### 移植時の変更点

- `DFUNC_Base` 廃止 → VR トリガー入力を自前実装
- `SAVControl` + `GetProgramVariable` で `ExtraDrag`/`ExtraLift`/`AirSpeed` アクセス
- `Dial_Funcon_Array` 対応追加
- 外部依存なし

#### 公開プロパティ (他スクリプトから参照)

| プロパティ | 型 | 参照元 |
|-----------|---|--------|
| `targetAngle` | `float` | GPWS (フラップ展開状態) |
| `speedLimit` | `float` | AuralWarnings (VMO制限) |
| `detentIndex` | `int` | GPWS (フラップ位置) |

---

### 2.2 DFUNC_ElevatorTrim

**元ファイル:** `DFUNC/DFUNC_ElevatorTrim.cs` (217行)
**カテゴリ:** DFUNC | **同期:** `Continuous` | **難易度:** 低

#### 概要

エレベータートリム制御。VR ハンドトラッキングまたはデスクトップキーで連続調整し、ピッチ方向の回転力をRigidbodyに直接加える。

#### クラス定義

```csharp
namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_ElevatorTrim : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` | `GameObject` | - | ダイアル表示 |
| `Dial_Funcon_Array` | `GameObject[]` | - | ダイアル表示配列 |
| `controllerSensitivity` | `float` | 0.5 | VR感度 |
| `desktopUp` | `KeyCode` | T | トリムアップ |
| `desktopDown` | `KeyCode` | Y | トリムダウン |
| `desktopStep` | `float` | 0.02 | デスクトップ調整ステップ |
| `trimStrengthMultiplier` | `float` | 1.0 | トリム力倍率 |
| `trimStrengthCurve` | `float` | 1.0 | トリム力カーブ指数 |
| `animatorParameterName` | `string` | "elevtrim" | Animator float |
| `vrInputAxis` | `Vector3` | forward | VR入力軸 |
| `trimBias` | `float` | 0 | バイアス力 |
| `hapticDuration/Amplitude/Frequency` | `float` | 0.2/0.5/0.1 | ハプティクス |

**同期変数:** `[UdonSynced] float trim` (範囲 -1〜1)

#### 主要アルゴリズム

**トリム力適用 (FixedUpdate):**
```
force = Sign(trim) * |trim|^curve * strength * (airspeed / maxSpeed) * atmosphere + bias
vehicleRigidbody.AddTorqueAtPosition(force, centerOfMass)
```

#### 移植時の変更点

- `DFUNC_Base` 廃止 → VR ハンドポジション追跡を自前実装
- `SAVControl.GetProgramVariable` で `PitchStrength`, `RotMultiMaxSpeed`, `Atmosphere` アクセス
- `VehicleRigidbody` は `SAVControl.GetProgramVariable("VehicleRigidbody")` で取得

---

### 2.3 DFUNC_AdvancedParkingBrake

**元ファイル:** `DFUNC/DFUNC_AdvancedParkingBrake.cs` (102行)
**カテゴリ:** DFUNC | **同期:** `Manual` | **難易度:** 低

#### 概要

パーキングブレーキのトグル制御。ダイアル選択不要 (`ActivateOnSelected=false` 相当) で、キー押下またはトリガーで状態切替。`SFEXT_AdvancedGear` の `parkingBrake` フィールドを設定する。

#### クラス定義

```csharp
namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedParkingBrake : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` | `GameObject` | - | 表示 |
| `Dial_Funcon_Array` | `GameObject[]` | - | 表示配列 |
| `desktopControl` | `KeyCode` | N | デスクトップキー |
| `parameterName` | `string` | "parkingbrake" | Animator bool |

**同期変数:** `[UdonSynced][FieldChangeCallback(nameof(State))] bool _state`

#### 主要アルゴリズム

- State プロパティのコールバックで Animator と `SFEXT_AdvancedGear[]` に状態配信
- `RequestSerialization()` でネットワーク同期
- `ActivateOnSelected` 相当が `false` → `DFUNC_Selected/Deselected` で gameObject 制御しない

#### 依存関係

- `SFEXT_AdvancedGear` (Phase 2) の `parkingBrake` フィールドを設定
- Phase 2 完了まではギア連携なしで単独動作可能 (Animator のみ)

---

### 2.4 DFUNC_AdvancedSpeedBrake

**元ファイル:** `DFUNC/DFUNC_AdvancedSpeedBrake.cs` (181行)
**カテゴリ:** DFUNC | **同期:** `Continuous` | **難易度:** 中

#### 概要

スピードブレーキ/スポイラー。0〜1 の連続値で展開量を制御し、抗力・揚力に影響。VR ハンド位置追跡またはデスクトップキーで操作。

#### クラス定義

```csharp
namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedSpeedBrake : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` | `GameObject` | - | 表示 |
| `Dial_Funcon_Array` | `GameObject[]` | - | 表示配列 |
| `liftMultiplier` | `float` | 0.6 | 揚力変化倍率 |
| `dragMultiplier` | `float` | 1.4 | 抗力倍率 |
| `response` | `float` | 1.0 | 応答速度 |
| `vrInputDistance` | `float` | 0.1 | VR距離感度 |
| `incrementStep` | `float` | 0.5 | デスクトップ増分 |
| `desktopKey` | `KeyCode` | B | デスクトップキー |
| `floatParameterName` | `string` | "speedbrake" | Animator float (実位置) |
| `floatInputParameterName` | `string` | "speedbrakeinput" | Animator float (目標) |

**同期変数:** `[UdonSynced(UdonSyncMode.Smooth)][FieldChangeCallback(nameof(TargetAngle))] float _targetAngle`

#### 主要アルゴリズム

**差分ドラッグ/リフト適用:**
```
角度変化時: ExtraDrag += (newAngle - oldAngle) * dragMultiplier
            ExtraLift += (newAngle - oldAngle) * liftMultiplier
```

---

### 2.5 SFEXT_AuxiliaryPowerUnit

**元ファイル:** `SFEXT/SFEXT_AuxiliaryPowerUnit.cs` (190行)
**カテゴリ:** SFEXT | **同期:** `Manual` | **難易度:** 中

#### 概要

APU (補助動力装置) の始動・停止シーケンス。オーディオクロスフェードとパーティクルエフェクトで表現。SFV 依存度が低く、変更は最小限。

#### クラス定義

```csharp
namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class SFEXT_AuxiliaryPowerUnit : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `apuAudioSource` | `AudioSource` | - | APU音声 |
| `apuStart` | `AudioClip` | - | 始動音 |
| `apuLoop` | `AudioClip` | - | 運転音 |
| `apuStop` | `AudioClip` | - | 停止音 |
| `crossFadeDuration` | `float` | 3.0 | クロスフェード秒数 |
| `defaultApuStartDuration` | `float` | 30.0 | 始動所要時間 |
| `defaultApuStopDuration` | `float` | 10.0 | 停止所要時間 |
| `exhaustEffect` | `ParticleSystem` | - | 排気エフェクト |

**同期変数:** `[UdonSynced] bool run`

**公開フィールド:**
- `[NonSerialized] public bool started` — 始動完了
- `[NonSerialized] public bool terminated` — 停止完了

#### ステートマシン

```
Shutdown → Starting → Started → Shuttingdown → Shutdown
```

#### 移植時の変更点

- 名前空間変更のみ
- `SFEXT_O_TakeOwnership` の戻り値を `bool` → `void` に修正

#### 公開プロパティ (他スクリプトから参照)

| プロパティ | 型 | 参照元 |
|-----------|---|--------|
| `started` | `bool` | DFUNC_AutoStarter, SFEXT_Warning |
| `terminated` | `bool` | SFEXT_Warning |
| `run` | `bool` | DFUNC_AutoStarter |

---

### 2.6 SFEXT_EngineFanDriver

**元ファイル:** `SFEXT/SFEXT_EngineFanDriver.cs` (69行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 低

#### 概要

エンジン N1 回転数に応じてファンの Transform を回転させるビジュアルドライバー。

#### クラス定義

```csharp
namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_EngineFanDriver : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `fanTransforms` | `Transform[]` | - | ファンオブジェクト |
| `fanAxises` | `Vector3[]` | {Vector3.up} | 回転軸 |

#### 主要アルゴリズム

```
angle += n1 * deltaTime * 360
angle %= 360
fanTransform.localRotation = Quaternion.AngleAxis(angle, axis)
```

#### 依存関係

- `SFEXT_AdvancedEngine` (Phase 2) の `n1` フィールドを参照
- Phase 2 完了まで動作不可 → Phase 2 と同時にテスト

---

### 2.7 GPWS

**元ファイル:** `Avionics/GPWS.cs` (359行)
**カテゴリ:** Avionics | **同期:** `NoVariableSync` | **難易度:** 高
**実行順序:** `[DefaultExecutionOrder(1100)]`

#### 概要

対地接近警報装置。電波高度計・対地接近率・降下率・構成 (ギア/フラップ) に基づく6モードの警報。

#### クラス定義

```csharp
namespace SFAdvEquipment.Avionics
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    [DefaultExecutionOrder(1100)]
    [RequireComponent(typeof(AudioSource))]
    public class GPWS : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `groundLayers` | `LayerMask` | -1 | レイキャストレイヤー |
| `groundDetector` | `Transform` | - | 電波高度計基準点 |
| `altitudeCallouts` | `AudioClip[]` | - | 高度コールアウト音 |
| `altitudeThresholds` | `float[]` | {5,10,...,2500} | コールアウト閾値 (ft) |
| `bankAngleSound` | `AudioClip` | - | バンクアングル警報 |
| `sinkRateSound` | `AudioClip` | - | シンクレート警報 |
| `pullUpSound` | `AudioClip` | - | プルアップ警報 |
| `terrainSound` | `AudioClip` | - | テレイン警報 |
| `dontSinkSound` | `AudioClip` | - | ドントシンク警報 |
| `tooLowGearSound` | `AudioClip` | - | ギア警報 |
| `tooLowFlapsSound` | `AudioClip` | - | フラップ警報 |
| `tooLowTerrainSound` | `AudioClip` | - | 低高度テレイン警報 |
| `initialClimbThreshold` | `float` | 1333 | 初期上昇閾値 (ft) |
| `smoothing` | `float` | 1.0 | 平滑化時定数 |
| `startDelay` | `float` | 30 | 警報開始遅延 (秒) |

#### 警報モード

| モード | 条件 | 警報 |
|--------|------|------|
| Mode 1 | 30-2450ft + 過大降下率 | SINK RATE → PULL UP |
| Mode 2A | 30-1650/2450ft + テレイン接近率 | TERRAIN → PULL UP |
| Mode 2B | 着陸構成 + テレイン接近率 | TERRAIN → PULL UP |
| Mode 3 | 30-1333ft + 初期上昇中高度ロス | DON'T SINK |
| Mode 4A | <500ft + ギアアップ + <190KIAS | TOO LOW GEAR |
| Mode 4B | <245ft + フラップ非着陸位置 + <159KIAS | TOO LOW FLAPS |
| Mode 6 | 高度コールアウト + バンクアングル | 高度読み上げ / BANK ANGLE |

#### ギア・フラップ参照の設計

GPWS は SFV 標準 `DFUNC_Gear`/`DFUNC_Flaps` と SF-AdvEquipment の `DFUNC_AdvancedFlaps` の両方を参照できる設計とする:

```csharp
[Header("Gear/Flaps References")]
[Tooltip("SFV標準 DFUNC_Gear またはnull")]
public UdonSharpBehaviour gear;
[Tooltip("SFV標準 DFUNC_Flaps またはnull")]
public UdonSharpBehaviour flaps;
[Tooltip("SF-AdvEquipment DFUNC_AdvancedFlaps またはnull")]
public DFUNC_AdvancedFlaps advancedFlaps;
```

ギア状態取得: `(bool)gear.GetProgramVariable("GearUp")`
フラップ状態取得: `(bool)flaps.GetProgramVariable("Flaps")` または `advancedFlaps.targetAngle > 0`

#### 移植時の変更点

- `SaccAirVehicle` 直接参照 → `SAVControl` + `GetProgramVariable`
- `DFUNC_Gear`/`DFUNC_Flaps` → `UdonSharpBehaviour` + `GetProgramVariable`
- `DFUNC_AdvancedFlaps` は同パッケージ内なので直接型参照可
- `EsnyaSFAddons.DFUNC` 名前空間参照を削除

---

### 2.8 SFEXT_Warning

**元ファイル:** `SFEXT/SFEXT_Warning.cs` (140行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 低

#### 概要

マスターコーション・エンジン火災・オーバーヒート・燃料低下・油圧低下・APU状態の統合警告システム。各種 GameObject 配列の ON/OFF で表示。

#### クラス定義

```csharp
namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_Warning : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 説明 |
|-----------|---|------|
| `SAVControl` | `UdonSharpBehaviour` | SaccAirVehicle参照 |
| `masterCautionLights` | `GameObject[]` | マスターコーション |
| `engineCautionLights` | `GameObject[]` | エンジン注意灯 |
| `hydroCautionLights` | `GameObject[]` | 油圧注意灯 |
| `fuelCautionLights` | `GameObject[]` | 燃料注意灯 |
| `engineOverheatLights` | `GameObject[]` | オーバーヒート灯 |
| `apuCautionLight` | `GameObject[]` | APU灯 |
| `engine1OverheatLights` / `engine2OverheatLights` | `GameObject[]` | 個別エンジンオーバーヒート |
| `engine1FireLights` / `engine2FireLights` | `GameObject[]` | 個別エンジン火災 |
| `engineFireLights` | `GameObject[]` | エンジン火災共通 |
| `engineFireAlarm` | `AudioSource` | 火災アラーム |

#### 警告閾値

| 警告 | 条件 |
|------|------|
| 火災 | `ect > fireECT` |
| ストール | `n1 < idleN1 * 0.9` |
| 油圧低下 | `n1 < idleN1 * 0.8` |
| 燃料低下 | `Fuel < FullFuel * 0.3` |
| APU | APU running (not terminated) |

#### 依存関係

- `SFEXT_AdvancedEngine` (Phase 2) — エンジンパラメータ参照
- `SFEXT_AuxiliaryPowerUnit` (Phase 1) — APU 状態参照

---

## 3. Phase 2: 高度な車両システム (6スクリプト)

---

### 3.1 SFEXT_AdvancedEngine

**元ファイル:** `SFEXT/SFEXT_AdvancedEngine.cs` (668行)
**カテゴリ:** SFEXT | **同期:** `Continuous` | **難易度:** 極高
**実行順序:** `[DefaultExecutionOrder(1000)]`

#### 概要

ターボファンエンジンシミュレーション。N1/N2 二重スプール、EGT/ECT 温度管理、始動シーケンス、逆噴射、火災・故障モデル、プレイヤーストライク、ジェットブラストを含む。

#### クラス定義

```csharp
namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(1000)]
    public class SFEXT_AdvancedEngine : UdonSharpBehaviour
```

#### フィールドグループ

**動力系:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `maxThrust` | `float` | 130408.51 | 最大推力 (N) |
| `thrustCurve` | `float` | 2.0 | 推力カーブ指数 |
| `idleN1` / `referenceN1` / `takeOffN1` | `float` | 879.6 / 4397 / 4586 | N1 RPM |
| `idleN2` / `referenceN2` / `takeOffN2` | `float` | 8583.5 / 17167 / 20171 | N2 RPM |
| `n1Response` / `n1DecreaseResponse` / `n1StartupResponse` | `float` | 0.1/0.08/0.01 | N1応答速度 |
| `n2Response` / `n2DecreaseResponse` / `n2StartupResponse` | `float` | 0.05/0.04/0.005 | N2応答速度 |

**温度系:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `idleEGT` / `continuousEGT` / `takeOffEGT` / `fireEGT` | `float` | 725/1013/1038/1812 | EGT (排気温度) |
| `idleECT` / `continuousECT` / `overheatECT` / `fireECT` | `float` | 196/274/343/850 | ECT (エンジンケース温度) |
| `egtResponse` / `ectResponse` / `ectOverheatResponse` | `float` | 0.02/0.1/0.001 | 応答速度 |

**逆噴射:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `reverserRatio` | `float` | 0.5 | 逆噴射推力比 |
| `reverserExtractResponse` / `reverserRetractResponse` | `float` | 0.5/0.5 | 展開/格納速度 |

**故障系:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `mtbFireAtContinuous` | `float` | 30日(秒) | 連続推力時MTBF |
| `mtbFireAtOverheat` | `float` | 90 | オーバーヒート時MTBF |
| `mtbFireAtFire` | `float` | 10 | 火災時MTBF |
| `mtbMeltdownOnFire` | `float` | 90 | メルトダウンMTBF |

**同期変数 (8個):**

| 変数 | 型 | 説明 |
|------|---|------|
| `reversing` | `bool` | 逆噴射中 |
| `starter` | `bool` | スターター動作中 |
| `fuel` | `bool` | 燃料供給中 |
| `n1` | `float` | N1回転数 |
| `n2` | `float` | N2回転数 |
| `egt` | `float` | 排気温度 |
| `ect` | `float` | ケース温度 |
| `fire` | `bool` | 火災状態 |

#### 主要サブシステム

**1. パワーシステム:** N1→推力、N2→N1駆動。スロットル→目標N1→MoveTowards→実N1
**2. リバーサー:** `ReverserPosition` 0-1補間、推力ベクトル反転
**3. 故障:** ECT連動の確率的火災発生。メルトダウンでエンジン停止
**4. サウンド:** idle/inside/thrust/takeoff の4チャンネル、N1/N2比例音量
**5. プレイヤーストライク:** 吸気口/排気口の円錐検知、推力比例の加速力
**6. ジェットブラスト:** パーティクルの放出速度をN1比例

#### 公開プロパティ (他スクリプトから参照)

| プロパティ | 型 | 参照元 |
|-----------|---|--------|
| `n1` | `float` | SFEXT_EngineFanDriver, SFEXT_Warning |
| `n2` | `float` | SFEXT_Warning, DFUNC_AutoStarter |
| `ect` | `float` | SFEXT_Warning |
| `fire` | `bool` | SFEXT_Warning |
| `reversing` | `bool` | DFUNC_AdvancedThrustReverser |
| `starter` | `bool` | DFUNC_AutoStarter |
| `fuel` | `bool` | DFUNC_AutoStarter |
| `idleN1` | `float` | SFEXT_Warning, DFUNC_AutoStarter |
| `fireECT` | `float` | SFEXT_Warning |

#### 移植時の変更点

- `SaccAirVehicle airVehicle` → `SAVControl` + `GetProgramVariable`
- `DFUNC_Brake` → `UdonSharpBehaviour` + `GetProgramVariable`
- `SAV_SoundController` → `UdonSharpBehaviour` + `GetProgramVariable`
- `DFUNC_AdvancedParkingBrake` / `SFEXT_AuxiliaryPowerUnit` は同パッケージ内なので直接型参照
- UdonToolkit 属性削除

---

### 3.2 SFEXT_AdvancedGear

**元ファイル:** `SFEXT/SFEXT_AdvancedGear.cs` (300行)
**カテゴリ:** SFEXT | **同期:** `Continuous` | **難易度:** 高

#### 概要

WheelCollider ベースの高度な脚システム。速度制限による展開/格納破損、タイヤバースト、サスペンションアニメーション、パーキングブレーキ連携。

#### クラス定義

```csharp
namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SFEXT_AdvancedGear : UdonSharpBehaviour
```

#### フィールドグループ

**物理:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `wheelCollider` | `WheelCollider` | - | ホイールコライダー |
| `suspensionTransform` | `Transform` | - | サスペンション |
| `steerTransform` | `Transform` | - | ステアリング |
| `wheelTransform` | `Transform` | - | ホイール |
| `maxSteerAngle` | `float` | 0 | 最大操舵角 |
| `brakeTorque` | `float` | 10000 | ブレーキトルク |

**故障:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `maxExtensionSpeed` | `float` | 270 | 展開最大速度 (KIAS) |
| `maxRetractionSpeed` | `float` | 235 | 格納最大速度 (KIAS) |
| `maxExtendedSpeed` | `float` | 320 | 展開状態最大速度 (KIAS) |
| `verticalSpeedLimit` | `float` | 600 | 着陸垂直速度制限 (ft/min) |
| `burstVerticalSpeed` | `float` | 900 | バースト垂直速度 (ft/min) |

**同期変数 (8個):**
`targetPosition`, `position`, `moving`, `inTransition`, `failed`, `broken`, `parkingBrake`, `_bursted`

#### 移植時の変更点

- `DFUNC_Brake` → `UdonSharpBehaviour` + `GetProgramVariable("BrakeInput")`
- `SaccAirVehicle` → `SAVControl` + `GetProgramVariable`
- `Taxiing`, `RotationInputs.y`, `AirSpeed`, `DisableTaxiRotation_` 等のアクセス方法変更

---

### 3.3 DFUNC_AdvancedThrustReverser

**元ファイル:** `DFUNC/DFUNC_AdvancedThrustReverser.cs` (69行)
**カテゴリ:** DFUNC | **同期:** `None` | **難易度:** 中

#### 概要

`SFEXT_AdvancedEngine` 連携の逆噴射制御。各エンジンの `reversing` フラグを操作する。同期は `SFEXT_AdvancedEngine` 側で行うため、この DFUNC 自体は同期不要。

#### クラス定義

```csharp
namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class DFUNC_AdvancedThrustReverser : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` | `GameObject` | - | 表示 |
| `Dial_Funcon_Array` | `GameObject[]` | - | 表示配列 |
| `keyboardControl` | `KeyCode` | R | デスクトップキー |

#### 動作

- トリガー/キー押下でエンジン `reversing = true`
- 解放で `reversing = false`
- スロットルがアイドルでない場合は無視

---

### 3.4 DFUNC_AutoStarter

**元ファイル:** `DFUNC/DFUNC_AutoStarter.cs` (283行)
**カテゴリ:** DFUNC | **同期:** `Manual` | **難易度:** 中
**実行順序:** `[DefaultExecutionOrder(1000)]`

#### 概要

APU→エンジン始動→APU停止の自動シーケンス。6状態のステートマシン。

#### ステートマシン

```
STATE_OFF (0)
  ↓ start=true
STATE_APU_START (1) → APU.run=true, APU.started待ち
  ↓
STATE_ENGINE_START (3) → 各エンジンを順次始動 (interval秒間隔)
  ↓ 全エンジン始動完了
STATE_APU_STOP (2) → APU.run=false
  ↓
STATE_ON (255)
  ↓ start=false
STATE_ENGINE_STOP (4) → 各エンジンを順次停止
  ↓
STATE_OFF (0)
```

#### 依存関係

- `SFEXT_AuxiliaryPowerUnit` (Phase 1)
- `SFEXT_AdvancedEngine` (Phase 2)

---

### 3.5 SFEXT_AdvancedPropellerThrust

**元ファイル:** `SFEXT/SFEXT_AdvancedPropellerThrust.cs` (427行)
**カテゴリ:** SFEXT | **同期:** `Continuous` | **難易度:** 中

#### 概要

プロペラ推力シミュレーション。プロペラスリップ理論に基づく推力計算、ミクスチャー制御、高度補正、エンジンストール、プレイヤーハザード。

#### クラス定義

```csharp
namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class SFEXT_AdvancedPropellerThrust : UdonSharpBehaviour
```

#### 主要フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `power` | `float` | 160 | 出力 (hp) |
| `diameter` | `float` | 1.9304 | プロペラ直径 (m) |
| `maxRPMCurve` | `AnimationCurve` | 0→2700,20000→2500 | 高度別最大RPM |
| `minRPM` | `float` | 600 | 最低RPM |
| `throttleCurve` | `AnimationCurve` | 0→0,1→1 | スロットルカーブ |
| `halfPowerAltitude` | `float` | 21000 | 半出力高度 (ft) |
| `rpmResponse` | `float` | 1.0 | RPM応答速度 |
| `mixture` | `float` | 1.0 | ミクスチャー (NonSerialized) |

**同期変数:** `[UdonSynced(UdonSyncMode.Smooth)] float _rpm` (FieldChangeCallback)

#### 推力計算

```
seaLevelThrustScale = (2 * airDensity * π * (d/2)² * power²)^(1/3)
slip = 1 - 31.5 * velocity / max(RPM, minRPM)
thrust = slip * RPM² / 120 * seaLevelThrustScale * altitudeEffect
```

---

### 3.6 SFEXT_InstrumentsAnimationDriver

**元ファイル:** `SFEXT/SFEXT_InstrumentsAnimationDriver.cs` (438行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 中

#### 概要

10種の飛行計器を Animator パラメータで駆動。ADI (姿勢)、HI (方位)、ASI (速度)、高度計、TC (旋回)、SI (滑り)、VSI (昇降)、磁気コンパス、時計。

#### クラス定義

```csharp
namespace SFAdvEquipment.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_InstrumentsAnimationDriver : UdonSharpBehaviour
```

#### 計器グループ

各計器は `has*` bool で有効/無効を切り替え:

| 計器 | パラメータ | 範囲 | 電源 |
|------|-----------|------|------|
| ADI | pitch (0-1), roll (0-1) | ±maxPitch, ±180° | 真空/電気 |
| HI | heading (0-1) | 0-360° | 真空/電気 |
| ASI | airspeed (0-1) | 0-maxAirspeed kt | 気圧式 |
| Altimeter | altitude (0-1) | 0-maxAltitude ft | 気圧式 |
| TC | turnrate (0-1) | ±maxTurn °/s | 真空/電気 |
| SI | slipangle (0-1) | ±maxSlip ° | 機械式 |
| VSI | vs (0-1) | ±maxVerticalSpeed ft/min | 気圧式 |
| Compass | compass (0-1) | 0-360° | 機械式 |
| Clock | clocktime (0-1) | 0-86400 s | 電気式 |

#### 移植時の変更点

- UdonToolkit `[HideIf]` → 削除 (Inspector での条件表示は Editor 拡張で対応)
- `[CanBeNull]` / `[NotNull]` → 削除
- `NavaidDatabase` 参照 → オプショナル (`GameObject.Find` 維持)
- `SaccAirVehicle` → `SAVControl` + `GetProgramVariable`

---

## 4. Phase 3: アビオニクス・ユーティリティ (8スクリプト)

---

### 4.1 AuralWarnings

**元ファイル:** `Avionics/AuralWarnings.cs` (100行)
**カテゴリ:** Avionics | **同期:** `None` | **難易度:** 中

#### 概要

VMO (最大運用速度) 超過警報とスティックシェイカー (失速警報)。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `defaultVmo` | `float` | 340 | 既定VMO (KIAS) |
| `stickShakerStartAoA` | `float` | 10 | シェイカー開始AoA (度) |
| `stickShakerMaxAoA` | `float` | 24 | シェイカー最大AoA (度) |
| `overspeed` | `AudioSource` | - | オーバースピード音 |
| `stickShaker` | `AudioSource` | - | スティックシェイカー音 |
| `updateInterval` | `int` | 30 | 更新間隔 (フレーム) |
| `velocitySmooth` | `float` | 1.0 | 速度平滑化 |

#### 主要アルゴリズム

```
IAS = dot(forward, airVelocity) * 1.94384
VMO = min(defaultVmo, advancedFlaps.speedLimit)  // AdvancedFlaps がある場合
overspeed.enabled = (IAS > 1.0 && IAS > VMO)
stickshakerVolume = pow(remap(-AoA, startAoA, maxAoA), 0.1)
```

#### 依存関係

- `DFUNC_AdvancedFlaps` (Phase 1) — オプショナル: speedLimit 参照

---

### 4.2 DFUNC_ThrustReverser

**元ファイル:** `DFUNC/DFUNC_ThrustReverser.cs` (136行)
**カテゴリ:** DFUNC | **同期:** `Manual` | **難易度:** 中

#### 概要

標準逆噴射制御。`SFEXT_AdvancedEngine` に依存せず、`SaccAirVehicle.ThrottleStrength` を直接反転。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` / `Dial_Funcon_Array` | `GameObject` / `GameObject[]` | - | 表示 |
| `ReversingThrottleMultiplier` | `float` | -0.5 | 逆噴射スロットル倍率 |
| `KeyboardControl` | `KeyCode` | R | デスクトップキー |
| `ThrustReverserAnimator` | `Animator` | - | アニメータ |
| `ParameterName` | `string` | "reverse" | Animator bool |

**同期変数:** `[UdonSynced][FieldChangeCallback(nameof(Reversing))] bool _reversing`

#### 動作

- Reversing プロパティコールバックで `ThrottleStrength` を元値 × `ReversingThrottleMultiplier` に変更
- `ThrottleOverridden` カウンタを操作してスロットルオーバーライド

---

### 4.3 DFUNC_SeatAdjuster

**元ファイル:** `DFUNC/DFUNC_SeatAdjuster.cs` (129行)
**カテゴリ:** DFUNC | **同期:** `None` | **難易度:** 低

#### 概要

VR ハンドトラッキングまたはデスクトップキーによるシート位置調整。ローカル操作のため同期不要。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` / `Dial_Funcon_Array` | `GameObject` / `GameObject[]` | - | 表示 |
| `desktopUp` / `desktopDown` | `KeyCode` | Home/End | 上下キー |
| `desktopForward` / `desktopBack` | `KeyCode` | Insert/Delete | 前後キー |
| `desktopStep` | `float` | 0.05 | デスクトップ調整幅 |

#### イベント

SFEXT_* と SFEXTP_* の両方を受信:
- `SFEXT_L_EntityStart` / `SFEXTP_L_EntityStart`
- `SFEXT_O_PilotEnter` / `SFEXTP_O_UserEnter`
- `SFEXT_O_PilotExit` / `SFEXTP_O_UserExit`

これにより、パイロット・パッセンジャー両方のシート調整に対応。

---

### 4.4 DFUNCP_IHaveControl

**元ファイル:** `DFUNC/DFUNCP_IHaveControl.cs` (290行)
**カテゴリ:** DFUNC | **同期:** `NoVariableSync` | **難易度:** 中

#### 概要

パッセンジャーからパイロットへの操縦権移譲。3秒長押しでシート交換。SFV 1.8 の `DFUNC_TakeControl` (パイロット→パッセンジャー方向) とは逆方向の操作。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `Dial_Funcon` | `GameObject` | - | 表示 |
| `desktopControl` | `KeyCode` | F8 | デスクトップキー |
| `pressTime` | `float` | 3.0 | 長押し時間 (秒) |
| `inverseSwitchHand` | `bool` | true | 交換時ハンド反転 |
| `hapticDuration/Amplitude/Frequency` | `float` | 0.2/0.5/0.1 | ハプティクス |

#### イベント (SFEXTP 系)

| イベント | 処理 |
|---------|------|
| `SFEXTP_L_EntityStart` | 初期化 |
| `SFEXTP_G_PilotEnter` | パイロット搭乗時にシート情報保存 |
| `SFEXTP_G_PilotExit` | パイロット降車時にリセット |
| `SFEXTP_O_UserEnter` | パッセンジャー搭乗、gameObject有効化 |
| `SFEXTP_O_UserExit` | パッセンジャー降車 |

#### シート交換アルゴリズム

1. 3秒長押し検出 (ハプティクス進行表示)
2. パイロットシートとパッセンジャーシートの子Transform位置を交換
3. 両プレイヤーのステーション退出・再搭乗
4. `SwitchHandsJoyThrottle` の反転 (オプション)
5. `EngineOffOnExit` を一時無効化して交換中のエンジン停止を防止

---

### 4.5 SFEXT_OutsideOnly

**元ファイル:** `SFEXT/SFEXT_OutsideOnly.cs` (29行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 低

#### 概要

車両搭乗時に外部オブジェクトを非表示にする。

#### フィールド

| フィールド | 型 | 説明 |
|-----------|---|------|
| `outsideOnly` | `GameObject[]` | 制御対象 |

#### イベント → 動作

- `SFEXT_O_PilotEnter` / `SFEXT_P_PassengerEnter` → 全オブジェクト非表示
- `SFEXT_O_PilotExit` / `SFEXT_P_PassengerExit` → 全オブジェクト表示
- `SFEXT_L_EntityStart` → 表示状態で初期化

---

### 4.6 SFEXT_PassengerOnly

**元ファイル:** `SFEXT/SFEXT_PassengerOnly.cs` (41行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 低

#### 概要

パッセンジャー搭乗時のみ gameObject を有効化。パイロットシートと除外シートを除く。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `moveToSeat` | `bool` | true | シート位置に移動 |
| `excludes` | `SaccVehicleSeat[]` | {} | 除外シート |

#### ロジック

```
PassengerEnter:
  seat = entity.VehicleStations[entity.MySeat].GetComponent<SaccVehicleSeat>()
  if (seat.IsPilotSeat || excludes.Contains(seat)) return
  if (moveToSeat) transform.SetPositionAndRotation(seat.position, seat.rotation)
  gameObject.SetActive(true)
```

---

### 4.7 SFEXT_SeatsOnly

**元ファイル:** `SFEXT/SFEXT_SeatsOnly.cs` (57行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 低

#### 概要

特定シートに搭乗中のみオブジェクトを表示。includeモード/excludeモード切替。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `seats` | `SaccVehicleSeat[]` | - | 対象シート |
| `excludeMode` | `bool` | false | 除外モード |
| `objects` | `GameObject[]` | - | 制御対象 |

#### ロジック

```
搭乗時:
  mySeat = seats.Find(s => s.ThisStationID == EntityControl.MySeat)
  includeMode: found → 表示
  excludeMode: not found → 表示
```

---

### 4.8 SFEXT_BoardingCollider

**元ファイル:** `SFEXT/SFEXT_BoardingCollider.cs` (209行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 中

#### 概要

停止中の航空機上を歩行可能にするコライダー。プレイヤーの相対位置を航空機の移動に追従させる。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `enableOnWater` | `bool` | true | 着水時も有効 |

#### 主要アルゴリズム

1. `PostLateUpdate` でコライダーのワールド位置を Entity に追従
2. プレイヤーがコライダー内にいる場合、Entity の移動差分をプレイヤーに適用
3. 回転差分も Quaternion 計算で補償
4. 地上 (+ 水上オプション) でのみ有効
5. カスタムイベント `SFEXT_L_BoardingEnter` / `SFEXT_L_BoardingExit` を発火

#### 移植時の注意

- `SFEXT_L_BoardingEnter` / `SFEXT_L_BoardingExit` は `SFEXT_AdvancedEngine` が受信するカスタムイベント

---

## 5. Phase 4: その他 (4スクリプト)

---

### 5.1 DFUNC_AdvancedWaterRudder

**元ファイル:** `DFUNC/DFUNC_AdvancedWaterRudder.cs` (188行)
**カテゴリ:** DFUNC | **同期:** `Manual` | **難易度:** 低

#### 概要

水上機用ラダー。流体力学的揚力/抗力を AnimationCurve で定義し、Rigidbody に力を加える。

#### クラス定義

```csharp
namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_AdvancedWaterRudder : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| `Dial_Funcon` / `Dial_Funcon_Array` | `GameObject` / `GameObject[]` | - | 表示 |
| `defaultExtracted` | `bool` | false | 初期展開状態 |
| `liftCoefficientCurve` | `AnimationCurve` | - | 揚力係数カーブ |
| `dragCoefficientCurve` | `AnimationCurve` | - | 抗力係数カーブ |
| `referenceArea` | `float` | 1.0 | 基準面積 (m²) |
| `waterDensity` | `float` | 999.1 | 水密度 |
| `maxRudderAngle` | `float` | 30 | 最大舵角 |
| `response` | `float` | 0.5 | 応答速度 |

**同期変数:** `[UdonSynced][FieldChangeCallback(nameof(Extracted))] bool _extracted`

#### 移植時の変更点

- `DFUNC_Base` 継承廃止 (`ActivateOnSelected=false` パターン)
- `KeyboardInput()` は SFV 1.8 標準の `KeyboardInput` イベントとして維持

---

### 5.2 SFEXT_WakeTurbulence

**元ファイル:** `SFEXT/SFEXT_WakeTurbulence.cs` (79行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 中

#### 概要

対気速度に応じたパーティクル放出量制御。ベルカーブ状の放出量変化。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `minSpeed` | `float` | 60 | 最低速度 (KIAS) |
| `peakSpeed` | `float` | 120 | ピーク速度 (KIAS) |
| `maxSpeed` | `float` | 300 | 最高速度 (KIAS) |
| `curve` | `float` | 2.0 | カーブ指数 |

#### 放出量計算

```
strength = 0 (IAS < minSpeed)
         = pow(lerp(minSpeed→peakSpeed), curve)  (minSpeed ≤ IAS ≤ peakSpeed)
         = pow(lerp(maxSpeed→peakSpeed), curve)   (peakSpeed < IAS ≤ maxSpeed)
         = 0 (IAS > maxSpeed)
emission = originalRate * strength
```

---

### 5.3 SFEXT_DihedralEffect

**元ファイル:** `SFEXT/SFEXT_DihedralEffect.cs` (75行)
**カテゴリ:** SFEXT | **同期:** `None` | **難易度:** 中

#### 概要

上反角効果。横滑り角に応じたロールトルクと追加抗力を Rigidbody に適用。

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `coefficient` | `float` | 0.01 | 効果係数 |
| `dynamicPressure` | `float` | 10 | 動圧 (Pa) |
| `referenceArea` | `float` | 1.0 | 基準面積 (m²) |
| `calacteristicLength` | `float` | 1.0 | 代表長さ (m) |
| `maxSlipAngle` | `float` | 20 | 最大横滑り角 (度) |
| `aoaCurve` | `float` | 1.0 | AoA カーブ指数 |
| `extraDrag` | `float` | 0 | 追加抗力係数 |

#### トルク計算

```
slipAngle = SignedAngle(forward, ProjectOnPlane(velocity, up), up)
normalizedSlip = clamp(slipAngle / maxSlipAngle, -1, 1)
curvedSlip = sign(normalizedSlip) * pow(|normalizedSlip|, aoaCurve)
torque = 0.5 * coeff * dynPressure * refArea * charLength * speed² * curvedSlip
AddRelativeTorque(0, 0, torque * rotLift)
```

---

### 5.4 PickupChock

**元ファイル:** `Accesories/PickupChock.cs` (133行)
**カテゴリ:** Accessories | **同期:** `Continuous` | **難易度:** 低

#### 概要

VRCPickup で持ち上げ可能な車輪止め。ドロップ時に地面にスナップ。SFV への依存なし。

#### クラス定義

```csharp
namespace SFAdvEquipment.Accessories
{
    [RequireComponent(typeof(VRCPickup))]
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class PickupChock : UdonSharpBehaviour
```

#### フィールド

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `groundLayerMask` | `LayerMask` | 0x0801 | 地面レイヤー |
| `raycastDistance` | `float` | 3.0 | レイキャスト距離 |
| `raycastOffset` | `float` | 1.0 | レイキャストオフセット |
| `sleepTimeout` | `float` | 3.0 | スリープタイムアウト (秒) |

**同期変数:**
- `[UdonSynced(UdonSyncMode.Smooth)] Vector3 _position`
- `[UdonSynced(UdonSyncMode.Smooth)] float _angle`

#### 動作

1. `OnPickup` → `RemoteOnPickup` をネットワーク送信 → コライダーをトリガーに
2. `OnDrop` → レイキャスト → 地面にスナップ → Position/Angle 同期
3. `sleepTimeout` 後 → コライダーを元に戻す (物理衝突有効)

---

## 6. スクリプト間依存関係

```
DFUNC_AutoStarter ──→ SFEXT_AuxiliaryPowerUnit
         │
         └──→ SFEXT_AdvancedEngine ←── DFUNC_AdvancedThrustReverser
                    ↑
         SFEXT_EngineFanDriver
                    ↑
              SFEXT_Warning ──→ SFEXT_AuxiliaryPowerUnit

GPWS ──→ DFUNC_AdvancedFlaps (オプション)
  │  └→ DFUNC_Gear (SFV標準, UdonSharpBehaviour参照)
  └──→ DFUNC_Flaps (SFV標準, UdonSharpBehaviour参照)

AuralWarnings ──→ DFUNC_AdvancedFlaps (オプション)

DFUNC_AdvancedParkingBrake ──→ SFEXT_AdvancedGear

SFEXT_BoardingCollider ──→ SFEXT_AdvancedEngine (BoardingEnter/Exit イベント)
```

**Phase 1 で独立動作可能:**
- DFUNC_AdvancedFlaps
- DFUNC_ElevatorTrim
- DFUNC_AdvancedSpeedBrake
- SFEXT_AuxiliaryPowerUnit
- GPWS (SFV標準 Gear/Flaps で動作)

**Phase 2 必須の Phase 1 スクリプト:**
- DFUNC_AdvancedParkingBrake → SFEXT_AdvancedGear
- SFEXT_EngineFanDriver → SFEXT_AdvancedEngine
- SFEXT_Warning → SFEXT_AdvancedEngine

---

## 7. パッケージ基盤

### 7.1 package.json

```json
{
  "name": "com.sakuha.sf-advequipment",
  "displayName": "SF-AdvancedEquipment",
  "version": "0.1.0",
  "description": "Advanced equipment for SaccFlightAndVehicles 1.8",
  "license": "MIT",
  "author": {
    "name": "sakuha"
  },
  "unity": "2022.3",
  "dependencies": {
    "com.unity.textmeshpro": "3.0.6"
  },
  "vpmDependencies": {
    "com.vrchat.worlds": "3.7.0",
    "com.vrchat.udonsharp": "1.3.1",
    "io.github.sacchan-vrc.sacc-flight-and-vehicles": "1.8.0"
  }
}
```

### 7.2 Assembly Definition

**Runtime (SFAdvEquipment.Runtime.asmdef):**
```json
{
  "name": "SFAdvEquipment.Runtime",
  "rootNamespace": "SFAdvEquipment",
  "references": [
    "UdonSharp.Runtime",
    "VRC.Udon",
    "VRC.SDKBase",
    "SaccFlightAndVehicles"
  ],
  "autoReferenced": true
}
```

**Editor (SFAdvEquipment.Editor.asmdef):**
```json
{
  "name": "SFAdvEquipment.Editor",
  "rootNamespace": "SFAdvEquipment.Editor",
  "references": [
    "SFAdvEquipment.Runtime",
    "UdonSharp.Editor"
  ],
  "includePlatforms": ["Editor"]
}
```
