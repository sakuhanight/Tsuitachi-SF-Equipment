# Fly-By-Wire (FBW) System 詳細設計書

## 1. 概要

### 1.1 目的

**SFEXT_FlyByWire** は、SaccFlightAndVehicles 1.8 に対する電子式飛行制御システムを提供する。パイロットの操縦入力とセンサーデータに基づき、フライトコントロールロー (Flight Control Law) を適用してアクチュエータへの指令を計算し、飛行エンベロープ保護 (Flight Envelope Protection) を実現する。

**民間機 (Airbus/Boeing方式) と戦闘機 (F-35/F-22方式) の両方に対応**し、オートフラップ・マニューバーフラップ機能も統合する。

### 1.2 実装コンポーネント

| コンポーネント | 役割 | カテゴリ |
|--------------|------|---------|
| `SFEXT_FlyByWire` | フライトコントロールロー実装 (Normal/Alternate/Direct/Combat Law) | SFEXT |
| `DFUNC_FBWControlPanel` | 制御モード切替UI (VR/デスクトップ) | DFUNC |
| `SFEXT_FBWIndicator` | 現在の制御モード・保護状態表示 | SFEXT |
| `SFEXT_AutoFlaps` | 速度・AOA・G負荷連動の自動フラップ制御 | SFEXT |

### 1.3 他システムとの違い

#### vs. DFUNC_ElevatorTrim

| 特徴 | DFUNC_ElevatorTrim | SFEXT_FlyByWire |
|-----|-------------------|-----------------|
| **動作原理** | トリム量に応じた固定トルクを直接加算 | 操縦入力を解釈してトルク/力を計算 |
| **入力** | トリムホイール位置 (-1〜1) | スティック入力 (RotationInputs) |
| **出力** | ピッチ軸トルクのみ | ピッチ・ロール・ヨー3軸 + オートトリム |
| **飛行保護** | なし | AOA制限・バンク角制限・Load Factor保護 |
| **自動化** | なし | オートレベル・オートトリム |
| **相互作用** | 併用可能 (トリム + FBW制御) | トリムを上書き可能 (オートトリムモード時) |

#### vs. SaccAirVehicle 標準物理

| 特徴 | SaccAirVehicle 標準 | SFEXT_FlyByWire |
|-----|-------------------|-----------------|
| **操縦応答** | 直接的 (入力 → 即座に舵面角) | 間接的 (入力 → 目標姿勢/レート → 舵面角) |
| **制御ロジック** | なし | PID制御・レート制限・エンベロープ保護 |
| **失速挙動** | 物理演算のみ | 保護ロジックで防止可能 |
| **統合方法** | `RotationInputs` を直接使用 | `RotationInputs` を読み取り → 補正 → `AddTorque` / `AddForce` |

### 1.4 設計方針

**SFV 1.8 との協調動作:**
- SaccAirVehicle の標準物理を無効化せず、**追加トルク/力による補正** で制御
- `RotationInputs` (パイロット入力) を読み取り専用として扱い、別途 `AddTorque` で制御入力を適用
- 必要に応じて `PitchStrength` / `YawStrength` / `RollStrength` を動的に減衰させて標準応答を抑制

**モジュール性:**
- FBW なしでも既存の TSFE コンポーネント (AdvancedFlaps/Gear/Engine 等) は動作
- FBW を追加しても既存コンポーネントとの競合なし

**実機相当の制御モード:**
- **Normal Law**: 完全な保護あり (Airbus 方式のハード保護) - 民間機向け
- **Alternate Law**: 一部保護のみ (AOA保護なし、ピッチ/ロールレート制限のみ) - 民間機向け
- **Combat Law**: 機動性重視、高AOA対応、Post-Stall制御 (F-35/Su-57 方式) - 戦闘機向け
- **Direct Law**: 保護なし (従来の直接操縦)

### 1.5 民間機 vs 戦闘機の制御ロー比較

| 特徴 | 民間機 (Normal Law) | 戦闘機 (Combat Law) |
|-----|-------------------|-------------------|
| **AOA制限** | 15°前後で厳格に制限 | 30-60°まで許容、Post-Stall可 |
| **バンク角** | 67°制限 (自動復帰) | 制限なし (360°ロール可) |
| **Load Factor** | +2.5/-1.0G制限 | +9.0/-3.0G対応 |
| **ピッチ制御** | Load Factor指令 (C*) | AOA指令 または ピッチレート指令 |
| **ロール制御** | バンク角指令 (自動水平復帰) | ロールレート指令 (連続ロール可) |
| **ヨー制御** | ヨーダンパー + 旋回協調 | ダンパーのみ (パイロット主導) |
| **オートトリム** | 常時有効 | 無効 (機動性優先) |
| **フラップ** | 離着陸用 (速度制限厳格) | マニューバーフラップ (自動展開) |
| **安定性** | 高安定 (快適性重視) | 意図的不安定 (機動性重視) |

---

## 2. SFEXT_FlyByWire

### 2.1 クラス定義

```csharp
namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    [DefaultExecutionOrder(100)] // SaccAirVehicle より後、他の SFEXT より前
    public class SFEXT_FlyByWire : UdonSharpBehaviour
```

**実行順序:** `[DefaultExecutionOrder(100)]`
- SaccAirVehicle (順序 0) が `RotationInputs` を更新した後
- DFUNC_ElevatorTrim (順序指定なし) や SFEXT_AdvancedEngine (1000) より前
- これにより `RotationInputs` を読み取り → FBW 補正 → 他システムが補正後の状態を参照

### 2.2 フィールド

#### Inspector 設定

| グループ | フィールド | 型 | 既定値 | 説明 |
|---------|-----------|---|--------|------|
| **Basic** | `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle 参照 |
| **Control Laws** | `enableNormalLaw` | `bool` | true | Normal Law 有効化 |
| | `enableAlternateLaw` | `bool` | true | Alternate Law 有効化 |
| | `enableCombatLaw` | `bool` | false | Combat Law 有効化 (戦闘機用) |
| | `enableDirectLaw` | `bool` | true | Direct Law 有効化 |
| | `defaultLaw` | `int` | 0 | 初期モード (0=Normal, 1=Alternate, 2=Combat, 3=Direct) |
| | `aircraftType` | `AircraftType` | `Airliner` | 機体タイプ (Airliner/Fighter) |
| **Normal Law - Pitch** | `pitchMode` | `PitchControlMode` | `LoadFactorCommand` | ピッチ制御モード |
| | `maxLoadFactor` | `float` | 2.5 | 最大Load Factor (+G) |
| | `minLoadFactor` | `float` | -1.0 | 最小Load Factor (-G) |
| | `maxPitchRate` | `float` | 15.0 | 最大ピッチレート (deg/s) |
| | `pitchRateAtFullDeflection` | `float` | 15.0 | フルスティック時ピッチレート (deg/s) |
| | `maxAOA` | `float` | 15.0 | 最大AOA (度) |
| | `minAOA` | `float` | -10.0 | 最小AOA (度) |
| **Normal Law - Roll** | `rollMode` | `RollControlMode` | `BankAngleCommand` | ロール制御モード |
| | `maxBankAngle` | `float` | 67.0 | 最大バンク角 (度) |
| | `maxRollRate` | `float` | 15.0 | 最大ロールレート (deg/s) |
| | `rollRateAtFullDeflection` | `float` | 15.0 | フルスティック時ロールレート (deg/s) |
| **Normal Law - Yaw** | `yawDamperGain` | `float` | 1.0 | ヨーダンパーゲイン |
| | `turnCoordinationGain` | `float` | 0.5 | 旋回協調ゲイン |
| **Alternate Law** | `altPitchRateLimit` | `float` | 20.0 | Alternate ピッチレート制限 (deg/s) |
| | `altRollRateLimit` | `float` | 20.0 | Alternate ロールレート制限 (deg/s) |
| | `altLoadFactorLimit` | `float` | 3.0 | Alternate Load Factor制限 (+G) |
| **PID Gains - Pitch** | `pitchP` | `float` | 2.0 | Pitch P gain |
| | `pitchI` | `float` | 0.1 | Pitch I gain |
| | `pitchD` | `float` | 0.5 | Pitch D gain |
| **PID Gains - Roll** | `rollP` | `float` | 1.5 | Roll P gain |
| | `rollI` | `float` | 0.05 | Roll I gain |
| | `rollD` | `float` | 0.3 | Roll D gain |
| **PID Gains - Yaw** | `yawP` | `float` | 1.0 | Yaw P gain |
| | `yawI` | `float` | 0.0 | Yaw I gain |
| | `yawD` | `float` | 0.2 | Yaw D gain |
| **Auto Trim** | `enableAutoTrim` | `bool` | true | オートトリム有効 (Normal Law時) |
| | `autoTrimSpeed` | `float` | 0.05 | オートトリム速度 (1/s) |
| **Authority Blending** | `standardInputSuppression` | `float` | 0.9 | 標準入力抑制率 (0-1) |
| | `fbwAuthorityMultiplier` | `float` | 1.0 | FBW出力倍率 |
| **Debug** | `showDebugInfo` | `bool` | false | デバッグ情報表示 |

#### Enum 定義

```csharp
public enum ControlLaw
{
    Normal = 0,     // 民間機: 完全保護
    Alternate = 1,  // 民間機: 一部保護
    Combat = 2,     // 戦闘機: 高機動モード
    Direct = 3      // 直接操縦 (FBWバイパス)
}

public enum PitchControlMode
{
    LoadFactorCommand = 0,  // C*法則: スティック入力 → 目標Load Factor (民間機)
    PitchRateCommand = 1,   // レート指令: スティック入力 → 目標ピッチレート (民間機Alt)
    AOACommand = 2          // AOA指令: スティック入力 → 目標AOA (戦闘機)
}

public enum RollControlMode
{
    BankAngleCommand = 0,   // バンク角指令: スティック中立復帰で水平復帰 (民間機)
    RollRateCommand = 1     // レート指令: スティック入力 → ロールレート (戦闘機)
}
```

#### 同期変数

```csharp
[UdonSynced][FieldChangeCallback(nameof(CurrentLaw))] private int _currentLaw;
public int CurrentLaw
{
    get => _currentLaw;
    set
    {
        if (value == _currentLaw) return;
        _currentLaw = value;
        OnLawChanged();
    }
}

[UdonSynced] private bool _fbwActive; // FBWシステム全体の有効/無効
```

#### 内部状態

```csharp
// センサー状態
private float currentAOA;           // 現在のAOA (度)
private float currentPitchRate;     // ピッチレート (deg/s)
private float currentRollRate;      // ロールレート (deg/s)
private float currentYawRate;       // ヨーレート (deg/s)
private float currentBankAngle;     // バンク角 (度)
private float currentLoadFactor;    // 現在のLoad Factor (G)
private Vector3 currentAngularVel;  // 角速度 (rad/s)

// PID積分項
private float pitchIntegral;
private float rollIntegral;
private float yawIntegral;

// オートトリム
private float autoTrimPitch;
private float autoTrimRoll;

// キャッシュ
private Rigidbody vehicleRigidbody;
private Transform vehicleTransform;
private float originalPitchStrength;
private float originalYawStrength;
private float originalRollStrength;

// 参照
private DFUNC_ElevatorTrim elevatorTrim; // オートトリム時にトリム値を上書き
```

### 2.3 制御ロー詳細

#### 2.3.1 Normal Law

**ピッチ軸 (Load Factor Command モード):**

```
入力 pilotPitchInput ∈ [-1, 1]
目標 Load Factor = Lerp(minLoadFactor, maxLoadFactor, (pilotPitchInput + 1) / 2)

エラー = 目標LoadFactor - 現在LoadFactor
PID出力 = pitchP * エラー + pitchI * ∫エラー + pitchD * d(エラー)/dt

AOA保護:
  if (currentAOA > maxAOA):
    保護トルク = -(currentAOA - maxAOA) * aoaProtectionGain
  else if (currentAOA < minAOA):
    保護トルク = -(currentAOA - minAOA) * aoaProtectionGain
  else:
    保護トルク = 0

最終ピッチトルク = Clamp(PID出力 + 保護トルク, -maxPitchTorque, maxPitchTorque)
```

**ピッチ軸 (Pitch Rate Command モード):**

```
目標ピッチレート = pilotPitchInput * pitchRateAtFullDeflection

エラー = 目標ピッチレート - currentPitchRate
PID出力 = pitchP * エラー + pitchI * ∫エラー + pitchD * d(エラー)/dt

最終ピッチトルク = Clamp(PID出力, -maxPitchTorque, maxPitchTorque)
```

**ロール軸 (Bank Angle Command モード):**

```
目標バンク角 = pilotRollInput * maxBankAngle

エラー = 目標バンク角 - currentBankAngle
PID出力 = rollP * エラー + rollI * ∫エラー + rollD * d(エラー)/dt

バンク角保護:
  if (abs(currentBankAngle) > maxBankAngle):
    保護トルク = -(currentBankAngle - sign(currentBankAngle) * maxBankAngle) * bankProtectionGain

最終ロールトルク = Clamp(PID出力 + 保護トルク, -maxRollTorque, maxRollTorque)
```

**ヨー軸 (ヨーダンパー + 旋回協調):**

```
ヨーダンパー = -currentYawRate * yawDamperGain

旋回協調 = currentRollRate * turnCoordinationGain  // ロール中の横滑り防止

最終ヨートルク = (ヨーダンパー + 旋回協調) * yawP
```

**オートトリム:**

```
Normal Law かつ enableAutoTrim == true:
  if (abs(pilotPitchInput) < 0.05):  // スティック中立
    autoTrimPitch += Sign(pitchIntegral) * autoTrimSpeed * deltaTime
    autoTrimPitch = Clamp(autoTrimPitch, -1, 1)
    pitchIntegral *= 0.95  // 積分項をゆっくり減衰

    // DFUNC_ElevatorTrim が存在する場合、trim フィールドを上書き
    if (elevatorTrim != null):
      elevatorTrim.trim = autoTrimPitch
```

#### 2.3.2 Alternate Law

Normal Law の簡易版。AOA保護とバンク角保護を削除し、レート制限のみ適用。

```
ピッチレート制限:
  目標ピッチレート = Clamp(pilotPitchInput * altPitchRateLimit, -altPitchRateLimit, altPitchRateLimit)

ロールレート制限:
  目標ロールレート = Clamp(pilotRollInput * altRollRateLimit, -altRollRateLimit, altRollRateLimit)

Load Factor制限:
  if (currentLoadFactor > altLoadFactorLimit):
    保護トルク = -(currentLoadFactor - altLoadFactorLimit) * loadFactorProtectionGain
```

オートトリム無効。

#### 2.3.3 Combat Law (戦闘機モード)

**設計思想:**
- 高AOA領域 (30-60°) での制御性維持
- Post-Stall Maneuver 対応 (Cobra, Kulbit等の前提)
- 連続ロール・高Gターン対応
- マニューバーフラップとの連動

**ピッチ軸 (AOA Command モード):**

```
入力 pilotPitchInput ∈ [-1, 1]
目標AOA = Lerp(combatMinAOA, combatMaxAOA, (pilotPitchInput + 1) / 2)
  // 例: -15° 〜 +60°

エラー = 目標AOA - currentAOA
PID出力 = combatPitchP * エラー + combatPitchI * ∫エラー + combatPitchD * d(エラー)/dt

Post-Stall補正 (currentAOA > 25°):
  // 高AOA時は舵面効力低下を補償
  effectivenessMultiplier = 1.0 + (currentAOA - 25) * 0.05
  PID出力 *= effectivenessMultiplier

AOA保護 (ソフトリミット):
  if (currentAOA > combatMaxAOA):
    softLimit = -(currentAOA - combatMaxAOA) * softLimitGain  // ゲイン小 (0.1程度)
    PID出力 += softLimit  // パイロットが強制的に超過可能

最終ピッチトルク = Clamp(PID出力, -maxCombatPitchTorque, maxCombatPitchTorque)
```

**ロール軸 (Roll Rate Command モード):**

```
目標ロールレート = pilotRollInput * combatMaxRollRate  // 例: ±180 deg/s

エラー = 目標ロールレート - currentRollRate
PID出力 = combatRollP * エラー + combatRollI * ∫エラー + combatRollD * d(エラー)/dt

// バンク角制限なし (360°ロール可)

最終ロールトルク = Clamp(PID出力, -maxCombatRollTorque, maxCombatRollTorque)
```

**ヨー軸 (ヨーダンパーのみ):**

```
ヨーダンパー = -currentYawRate * combatYawDamperGain

// 旋回協調なし (パイロットがラダーで制御)

最終ヨートルク = ヨーダンパー * combatYawP
```

**Load Factor保護:**

```
if (currentLoadFactor > combatMaxLoadFactor):  // 例: +9.0G
  保護トルク = -(currentLoadFactor - combatMaxLoadFactor) * combatLoadFactorGain
  // ハードリミット (構造破壊防止)
else if (currentLoadFactor < combatMinLoadFactor):  // 例: -3.0G
  保護トルク = -(currentLoadFactor - combatMinLoadFactor) * combatLoadFactorGain
```

**Combat Law 専用パラメータ:**

| フィールド | 型 | 既定値 | 説明 |
|-----------|---|--------|------|
| `combatMinAOA` | `float` | -15.0 | Combat最小AOA (度) |
| `combatMaxAOA` | `float` | 60.0 | Combat最大AOA (度) |
| `combatMaxRollRate` | `float` | 180.0 | Combat最大ロールレート (deg/s) |
| `combatMaxLoadFactor` | `float` | 9.0 | Combat最大+G |
| `combatMinLoadFactor` | `float` | -3.0 | Combat最小-G |
| `combatPitchP/I/D` | `float` | 3.0/0.2/0.8 | Combat Pitch PID |
| `combatRollP/I/D` | `float` | 2.5/0.1/0.5 | Combat Roll PID |
| `combatYawDamperGain` | `float` | 0.5 | Combat ヨーダンパー |
| `postStallCompensation` | `bool` | true | Post-Stall補正有効 |

#### 2.3.4 Direct Law

FBW を完全にバイパス。`standardInputSuppression = 0` に設定し、SaccAirVehicle の標準応答をそのまま使用。

### 2.4 センサー計算

```csharp
private void UpdateSensors()
{
    // AOA計算
    Vector3 airVel = (Vector3)SAVControl.GetProgramVariable("AirVel");
    Vector3 localAirVel = vehicleTransform.InverseTransformDirection(airVel);
    currentAOA = Mathf.Atan2(-localAirVel.y, localAirVel.z) * Mathf.Rad2Deg;

    // 角速度
    currentAngularVel = vehicleRigidbody.angularVelocity;
    Vector3 localAngVel = vehicleTransform.InverseTransformDirection(currentAngularVel);
    currentPitchRate = localAngVel.x * Mathf.Rad2Deg;
    currentRollRate = localAngVel.z * Mathf.Rad2Deg;
    currentYawRate = localAngVel.y * Mathf.Rad2Deg;

    // バンク角
    Vector3 up = vehicleTransform.up;
    Vector3 worldUp = Vector3.up;
    currentBankAngle = Vector3.SignedAngle(
        Vector3.ProjectOnPlane(up, vehicleTransform.forward),
        worldUp,
        vehicleTransform.forward
    );

    // Load Factor
    Vector3 accel = (vehicleRigidbody.velocity - prevVelocity) / Time.fixedDeltaTime;
    prevVelocity = vehicleRigidbody.velocity;
    Vector3 localAccel = vehicleTransform.InverseTransformDirection(accel);
    currentLoadFactor = (localAccel.y + Physics.gravity.y) / -Physics.gravity.y;
}
```

### 2.5 標準入力の抑制

SaccAirVehicle の `PitchStrength` 等を動的に減衰させる:

```csharp
public void SFEXT_L_EntityStart()
{
    originalPitchStrength = (float)SAVControl.GetProgramVariable("PitchStrength");
    originalYawStrength = (float)SAVControl.GetProgramVariable("YawStrength");
    originalRollStrength = (float)SAVControl.GetProgramVariable("RollStrength");
}

private void ApplyInputSuppression()
{
    if (_currentLaw == (int)ControlLaw.Direct)
    {
        // Direct Law: 抑制なし
        SAVControl.SetProgramVariable("PitchStrength", originalPitchStrength);
        SAVControl.SetProgramVariable("YawStrength", originalYawStrength);
        SAVControl.SetProgramVariable("RollStrength", originalRollStrength);
    }
    else
    {
        // Normal/Alternate: 標準応答を抑制
        float suppression = standardInputSuppression;
        SAVControl.SetProgramVariable("PitchStrength", originalPitchStrength * (1 - suppression));
        SAVControl.SetProgramVariable("YawStrength", originalYawStrength * (1 - suppression));
        SAVControl.SetProgramVariable("RollStrength", originalRollStrength * (1 - suppression));
    }
}
```

### 2.6 イベントハンドラ

```csharp
public void SFEXT_L_EntityStart()
{
    vehicleRigidbody = EntityControl.GetComponent<Rigidbody>();
    vehicleTransform = EntityControl.transform;

    originalPitchStrength = (float)SAVControl.GetProgramVariable("PitchStrength");
    originalYawStrength = (float)SAVControl.GetProgramVariable("YawStrength");
    originalRollStrength = (float)SAVControl.GetProgramVariable("RollStrength");

    // DFUNC_ElevatorTrim を探索 (オートトリム連携用)
    var allExtensions = EntityControl.GetComponentsInChildren<UdonSharpBehaviour>(true);
    foreach (var ext in allExtensions)
    {
        if (ext is DFUNC_ElevatorTrim)
        {
            elevatorTrim = (DFUNC_ElevatorTrim)ext;
            break;
        }
    }

    _currentLaw = defaultLaw;
    _fbwActive = true;
}

public void SFEXT_O_PilotEnter()
{
    isPilot = true;
    ResetPID();
}

public void SFEXT_O_PilotExit()
{
    isPilot = false;
}

public void SFEXT_G_Explode()
{
    ResetPID();
    autoTrimPitch = 0;
    autoTrimRoll = 0;
}

public void SFEXT_G_RespawnButton()
{
    ResetPID();
    autoTrimPitch = 0;
    autoTrimRoll = 0;
}

private void ResetPID()
{
    pitchIntegral = 0;
    rollIntegral = 0;
    yawIntegral = 0;
}
```

### 2.7 FixedUpdate フロー

```csharp
private void FixedUpdate()
{
    if (!_fbwActive || !isPilot) return;

    UpdateSensors();
    ApplyInputSuppression();

    Vector3 pilotInput = (Vector3)SAVControl.GetProgramVariable("RotationInputs");
    float pitchInput = pilotInput.x;
    float yawInput = pilotInput.y;
    float rollInput = pilotInput.z;

    Vector3 controlTorque = Vector3.zero;

    switch (_currentLaw)
    {
        case (int)ControlLaw.Normal:
            controlTorque = ComputeNormalLaw(pitchInput, rollInput, yawInput);
            if (enableAutoTrim) UpdateAutoTrim(pitchInput);
            break;

        case (int)ControlLaw.Alternate:
            controlTorque = ComputeAlternateLaw(pitchInput, rollInput, yawInput);
            break;

        case (int)ControlLaw.Direct:
            // Direct Law は ApplyInputSuppression() で抑制を解除するのみ
            return;
    }

    // FBW トルクを適用
    Vector3 worldTorque = vehicleTransform.TransformDirection(controlTorque) * fbwAuthorityMultiplier;
    vehicleRigidbody.AddTorque(worldTorque, ForceMode.Force);
}
```

### 2.8 公開プロパティ (他コンポーネントから参照)

```csharp
[System.NonSerialized] public float CurrentAOA => currentAOA;
[System.NonSerialized] public float CurrentLoadFactor => currentLoadFactor;
[System.NonSerialized] public float CurrentBankAngle => currentBankAngle;
[System.NonSerialized] public bool IsNormalLaw => _currentLaw == (int)ControlLaw.Normal;
[System.NonSerialized] public bool IsAlternateLaw => _currentLaw == (int)ControlLaw.Alternate;
[System.NonSerialized] public bool IsDirectLaw => _currentLaw == (int)ControlLaw.Direct;
```

---

## 3. DFUNC_FBWControlPanel

### 3.1 概要

FBW の制御モード切替と有効/無効トグルを提供する DFUNC。

### 3.2 クラス定義

```csharp
namespace TSFE.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class DFUNC_FBWControlPanel : UdonSharpBehaviour
```

### 3.3 フィールド

```csharp
public UdonSharpBehaviour SAVControl;
public GameObject Dial_Funcon;
public GameObject[] Dial_Funcon_Array;

[Header("References")]
public SFEXT_FlyByWire flyByWire;

[Header("Controls")]
public KeyCode toggleFBWKey = KeyCode.F9;
public KeyCode cycleLawKey = KeyCode.F10;

[System.NonSerialized] public bool LeftDial = false;
[System.NonSerialized] public int DialPosition = -999;
[System.NonSerialized] public SaccEntity EntityControl;

private bool isSelected;
private bool triggerLastFrame;
```

### 3.4 動作

```csharp
public void DFUNC_Selected() { isSelected = true; }
public void DFUNC_Deselected() { isSelected = false; }

private void Update()
{
    if (!isSelected || !Networking.LocalPlayer.IsOwner(flyByWire.gameObject)) return;

    // VR: トリガー押下でモード循環
    bool trigger = TSFEUtil.IsTriggerPressed(LeftDial);
    if (trigger && !triggerLastFrame)
    {
        CycleLaw();
    }
    triggerLastFrame = trigger;

    // Desktop: キーボード操作
    if (Input.GetKeyDown(toggleFBWKey))
    {
        flyByWire.SendCustomNetworkEvent(NetworkEventTarget.All, "ToggleFBW");
    }
    if (Input.GetKeyDown(cycleLawKey))
    {
        CycleLaw();
    }
}

private void CycleLaw()
{
    int next = (flyByWire.CurrentLaw + 1) % 3;
    flyByWire.CurrentLaw = next;
    flyByWire.RequestSerialization();
}
```

---

## 4. SFEXT_FBWIndicator

### 4.1 概要

現在の制御モード・保護状態をテキスト/ライト/アニメーターで表示。

### 4.2 クラス定義

```csharp
namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    public class SFEXT_FBWIndicator : UdonSharpBehaviour
```

### 4.3 フィールド

```csharp
[Header("References")]
public SFEXT_FlyByWire flyByWire;

[Header("Display")]
public TextMeshProUGUI lawText;
public GameObject normalLawLight;
public GameObject alternateLawLight;
public GameObject directLawLight;
public Animator indicatorAnimator;
public string lawParameterName = "fbw_law"; // int: 0=Normal, 1=Alternate, 2=Direct

[Header("Protection Indicators")]
public GameObject aoaProtectionLight;
public GameObject bankProtectionLight;
public GameObject loadFactorProtectionLight;
```

### 4.4 動作

```csharp
private void Update()
{
    if (!flyByWire) return;

    int law = flyByWire.CurrentLaw;

    // テキスト
    if (lawText)
    {
        lawText.text = law switch
        {
            0 => "NORMAL LAW",
            1 => "ALTERNATE LAW",
            2 => "DIRECT LAW",
            _ => "UNKNOWN"
        };
    }

    // ライト
    if (normalLawLight) normalLawLight.SetActive(law == 0);
    if (alternateLawLight) alternateLawLight.SetActive(law == 1);
    if (directLawLight) directLawLight.SetActive(law == 2);

    // アニメーター
    if (indicatorAnimator) indicatorAnimator.SetInteger(lawParameterName, law);

    // 保護状態
    if (aoaProtectionLight)
    {
        bool aoaActive = law == 0 && (flyByWire.CurrentAOA > flyByWire.maxAOA * 0.9f ||
                                       flyByWire.CurrentAOA < flyByWire.minAOA * 0.9f);
        aoaProtectionLight.SetActive(aoaActive);
    }

    if (bankProtectionLight)
    {
        bool bankActive = law == 0 && Mathf.Abs(flyByWire.CurrentBankAngle) > flyByWire.maxBankAngle * 0.9f;
        bankProtectionLight.SetActive(bankActive);
    }

    if (loadFactorProtectionLight)
    {
        bool lfActive = law != 2 && (flyByWire.CurrentLoadFactor > flyByWire.maxLoadFactor * 0.9f ||
                                      flyByWire.CurrentLoadFactor < flyByWire.minLoadFactor * 0.9f);
        loadFactorProtectionLight.SetActive(lfActive);
    }
}
```

---

## 5. 他コンポーネントとの連携

### 5.1 DFUNC_ElevatorTrim との連携

| シナリオ | 動作 |
|---------|------|
| **FBW Normal Law + AutoTrim有効** | FBW が `elevatorTrim.trim` を上書き。手動トリム操作は無視される |
| **FBW Alternate/Direct Law** | オートトリム無効。手動トリムが通常通り動作 |
| **FBW無効** | 手動トリムが通常通り動作 |

### 5.2 GPWS との連携

GPWS は SFEXT_FlyByWire の `CurrentAOA` を参照可能:

```csharp
// GPWS.cs に追加
public SFEXT_FlyByWire flyByWire;

private void Update()
{
    float aoa = flyByWire ? flyByWire.CurrentAOA : CalculateAOA(); // FBWがあればセンサー値を共有
    // ...
}
```

### 5.3 AuralWarnings との連携

Stick Shaker を FBW の AOA保護状態と連動:

```csharp
// AuralWarnings.cs に追加
public SFEXT_FlyByWire flyByWire;

private void Update()
{
    if (flyByWire && flyByWire.IsNormalLaw)
    {
        // Normal Law 中は AOA保護があるため、Stick Shaker を早めに作動
        float protectedMaxAOA = flyByWire.maxAOA * 0.8f;
        // ...
    }
}
```

### 5.4 SFEXT_AdvancedEngine との連携

エンジン出力に応じた制御ゲイン調整:

```csharp
// SFEXT_FlyByWire.cs に追加
public SFEXT_AdvancedEngine[] engines;

private float GetTotalThrust()
{
    float totalN1 = 0;
    foreach (var eng in engines)
    {
        if (eng) totalN1 += eng.n1 / eng.takeOffN1;
    }
    return totalN1 / engines.Length; // 正規化されたN1平均
}

private Vector3 ComputeNormalLaw(...)
{
    float thrustFactor = Mathf.Clamp01(GetTotalThrust());
    float effectivePitchP = pitchP * Mathf.Lerp(0.5f, 1.0f, thrustFactor); // 低推力時はゲイン低下
    // ...
}
```

### 5.5 SFEXT_InstrumentsAnimationDriver との連携

FBW の状態を計器に表示:

```csharp
// SFEXT_InstrumentsAnimationDriver.cs に追加
public SFEXT_FlyByWire flyByWire;

private void Update()
{
    if (flyByWire && vehicleAnimator)
    {
        vehicleAnimator.SetFloat("fbw_loadfactor", flyByWire.CurrentLoadFactor);
        vehicleAnimator.SetFloat("fbw_aoa", flyByWire.CurrentAOA);
        vehicleAnimator.SetBool("fbw_active", flyByWire.IsNormalLaw || flyByWire.IsAlternateLaw);
    }
}
```

---

---

## 6. SFEXT_AutoFlaps

### 6.1 概要

速度・AOA・G負荷に応じて自動的にフラップ/スラットを展開する。**DFUNC_AdvancedFlaps** と連動し、民間機の離着陸用オートフラップと戦闘機のマニューバーフラップの両方に対応。

### 6.2 クラス定義

```csharp
namespace TSFE.SFEXT
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.None)]
    [DefaultExecutionOrder(200)] // FlyByWire (100) より後
    public class SFEXT_AutoFlaps : UdonSharpBehaviour
```

### 6.3 フィールド

| グループ | フィールド | 型 | 既定値 | 説明 |
|---------|-----------|---|--------|------|
| **References** | `SAVControl` | `UdonSharpBehaviour` | - | SaccAirVehicle参照 |
| | `advancedFlaps` | `DFUNC_AdvancedFlaps` | - | フラップ制御対象 |
| | `flyByWire` | `SFEXT_FlyByWire` | null | FBW参照 (オプション) |
| **Mode** | `mode` | `AutoFlapMode` | `Airliner` | 動作モード |
| | `enableAutoFlaps` | `bool` | false | 自動制御有効 (手動/自動切替) |
| **Airliner Mode** | `takeoffSpeed` | `float` | 170 | 離陸速度 (KIAS) |
| | `approachSpeed` | `float` | 150 | 進入速度 (KIAS) |
| | `landingSpeed` | `float` | 130 | 着陸速度 (KIAS) |
| | `flapSchedule` | `float[]` | {0,1,5,15,30,40} | 速度連動デテント |
| | `speedSchedule` | `float[]` | {340,250,210,190,175,162} | 各デテントの最大速度 |
| **Fighter Mode** | `maneuverAOAStart` | `float` | 12.0 | マニューバーフラップ開始AOA (度) |
| | `maneuverAOAFull` | `float` | 20.0 | 完全展開AOA (度) |
| | `maneuverGStart` | `float` | 4.0 | G負荷開始 (+G) |
| | `maneuverGFull` | `float` | 7.0 | 完全展開 (+G) |
| | `maneuverSpeedMax` | `float` | 400 | マニューバーフラップ最大速度 (KIAS) |
| | `maneuverFlapAngle` | `float` | 15.0 | マニューバーフラップ最大角度 (度) |
| | `maneuverResponse` | `float` | 2.0 | 展開速度倍率 |
| **Landing Detection** | `landingGearRequired` | `bool` | true | 脚下げ時のみオート有効 |
| | `advancedGears` | `SFEXT_AdvancedGear[]` | - | ギア参照配列 |

### 6.4 Enum 定義

```csharp
public enum AutoFlapMode
{
    Airliner = 0,  // 離着陸用: 速度連動
    Fighter = 1    // マニューバーフラップ: AOA/G負荷連動
}
```

### 6.5 動作ロジック

#### 6.5.1 Airliner Mode

```csharp
private void UpdateAirlinerMode()
{
    if (!enableAutoFlaps) return;

    // 脚下げ要求チェック
    if (landingGearRequired && !IsGearDown()) return;

    float ias = GetIAS();  // KIAS
    int targetDetent = 0;

    // 速度スケジュールから適切なデテントを選択
    for (int i = speedSchedule.Length - 1; i >= 0; i--)
    {
        if (ias <= speedSchedule[i])
        {
            targetDetent = i;
            break;
        }
    }

    // フラップ角度を設定
    float targetAngle = flapSchedule[targetDetent];
    advancedFlaps.targetAngle = targetAngle;
}
```

**速度スケジュール例 (B737相当):**

| 速度 (KIAS) | フラップ角 | 用途 |
|------------|----------|------|
| > 250 | 0° | 巡航 |
| ≤ 250 | 1° | 降下開始 |
| ≤ 210 | 5° | 進入準備 |
| ≤ 190 | 15° | 進入 |
| ≤ 175 | 30° | ファイナル |
| ≤ 162 | 40° | 着陸 |

#### 6.5.2 Fighter Mode (マニューバーフラップ)

```csharp
private void UpdateFighterMode()
{
    if (!enableAutoFlaps) return;

    float ias = GetIAS();
    if (ias > maneuverSpeedMax) return;  // 高速時は無効

    // AOA による展開量計算
    float aoa = flyByWire ? flyByWire.CurrentAOA : CalculateAOA();
    float aoaRatio = TSFEUtil.ClampedRemap01(aoa, maneuverAOAStart, maneuverAOAFull);

    // G負荷 による展開量計算
    float loadFactor = flyByWire ? flyByWire.CurrentLoadFactor : CalculateLoadFactor();
    float gRatio = TSFEUtil.ClampedRemap01(loadFactor, maneuverGStart, maneuverGFull);

    // AOA と G のうち大きい方を採用
    float deployRatio = Mathf.Max(aoaRatio, gRatio);

    // 目標フラップ角度
    float targetAngle = deployRatio * maneuverFlapAngle;

    // 高速展開 (戦闘機は応答速度が速い)
    float currentAngle = advancedFlaps.targetAngle;
    float newAngle = Mathf.MoveTowards(currentAngle, targetAngle,
        maneuverResponse * Time.deltaTime * maneuverFlapAngle);

    advancedFlaps.targetAngle = newAngle;
}
```

**マニューバーフラップ展開ロジック:**

```
例: F-16 相当
AOA:  12° で展開開始 → 20° で最大 (15°フラップ)
G負荷: 4G で展開開始 → 7G で最大 (15°フラップ)

高Gターン中:
  loadFactor = 6.5G
  gRatio = (6.5 - 4.0) / (7.0 - 4.0) = 0.83
  targetAngle = 0.83 * 15° = 12.5°

失速寸前:
  aoa = 18°
  aoaRatio = (18 - 12) / (20 - 12) = 0.75
  targetAngle = 0.75 * 15° = 11.25°
```

### 6.6 他コンポーネントとの連携

#### 6.6.1 DFUNC_AdvancedFlaps との連携

```csharp
// SFEXT_AutoFlaps が advancedFlaps.targetAngle を直接操作
// DFUNC_AdvancedFlaps の手動操作 (VR/デスクトップ) は enableAutoFlaps=false 時のみ有効

public void DFUNC_AdvancedFlaps.Update()
{
    // AutoFlaps が有効な場合、手動入力を無視
    if (autoFlaps && autoFlaps.enableAutoFlaps)
    {
        // targetAngle は AutoFlaps が管理
        return;
    }

    // 通常の手動操作ロジック...
}
```

#### 6.6.2 SFEXT_FlyByWire との連携

```csharp
// FBW の Combat Law 中は自動的に Fighter Mode に切り替え
public void Update()
{
    if (flyByWire && flyByWire.IsInCombatLaw)
    {
        mode = AutoFlapMode.Fighter;
        enableAutoFlaps = true;
    }
}
```

#### 6.6.3 SFEXT_AdvancedGear との連携

```csharp
// 脚下げ検出
private bool IsGearDown()
{
    if (advancedGears == null || advancedGears.Length == 0) return true;

    foreach (var gear in advancedGears)
    {
        if (gear && gear.targetPosition < 0.9f) return false;  // 1つでも格納状態
    }
    return true;  // 全て展開
}
```

### 6.7 公開プロパティ

```csharp
[System.NonSerialized] public bool IsManeuverFlapsDeployed => mode == AutoFlapMode.Fighter && advancedFlaps.targetAngle > 1.0f;
[System.NonSerialized] public float CurrentDeployRatio => advancedFlaps.targetAngle / advancedFlaps.maxAngle;
```

---

## 7. 実装フェーズ

### Phase 5 (新規): Fly-By-Wire System

| 順序 | スクリプト | 説明 | 難易度 |
|-----|-----------|------|--------|
| 5.1 | `SFEXT_FlyByWire` | コア FBW ロジック (850行想定) | **極高** |
| 5.2 | `SFEXT_AutoFlaps` | 自動フラップ制御 (250行想定) | 中 |
| 5.3 | `DFUNC_FBWControlPanel` | モード切替 UI (150行想定) | 低 |
| 5.4 | `SFEXT_FBWIndicator` | 状態表示 (180行想定) | 低 |

**総行数:** 約 1430行
**必須依存:** DFUNC_AdvancedFlaps (Phase 1)
**オプション連携:** DFUNC_ElevatorTrim, GPWS, AuralWarnings, SFEXT_AdvancedEngine, SFEXT_AdvancedGear

---

## 8. テスト計画

### 8.1 単体テスト (民間機モード)

| テスト項目 | 手順 | 期待結果 |
|----------|------|---------|
| **Normal Law - ピッチ** | フルスティック引き → 保持 | Load Factor が maxLoadFactor に収束、AOA保護作動 |
| **Normal Law - ロール** | スティック右 → 中立に戻す | バンク角が maxBankAngle まで増加 → 中立で 0° に復帰 |
| **Normal Law - ヨー** | ロール中 | 自動的に旋回協調、横滑りなし |
| **AOA保護** | 低速で急引き起こし | maxAOA 到達時に自動的にプッシュ、失速回避 |
| **Alternate Law** | AOA保護なしでフルスティック | maxAOA を超過可能、レート制限のみ |
| **Direct Law** | モード切替 | SFV 標準操縦と同じ挙動 |
| **オートトリム** | 水平飛行でスティック中立 | 自動的に trim 値が調整され、スティック力不要 |

### 8.2 単体テスト (戦闘機モード)

| テスト項目 | 手順 | 期待結果 |
|----------|------|---------|
| **Combat Law - 高AOA** | 低速でフルスティック引き (60°) | AOA 60° まで到達、失速せず制御維持 |
| **Combat Law - 連続ロール** | スティック右フル保持 | 180 deg/s で連続ロール、バンク角制限なし |
| **Post-Stall補正** | AOA 30° 以上で機動 | 舵面効力補償が作動、制御性維持 |
| **マニューバーフラップ** | 高Gターン (7G) | 自動的にフラップ15°展開、旋回半径減少 |
| **マニューバーフラップ** | 高AOA (20°) | 自動的にフラップ15°展開、失速速度低下 |
| **Load Factor保護** | 急激なスティック操作 | 9G 到達時にハードリミット、構造破壊防止 |
| **高速時フラップ無効** | 400KIAS 超過時 | マニューバーフラップ格納、オーバースピード防止 |

### 8.3 統合テスト

| テスト項目 | 手順 | 期待結果 |
|----------|------|---------|
| **ElevatorTrim連携** | Normal Law中に手動トリム操作 | 手動トリムは無視され、オートトリムが優先 |
| **GPWS連携** | 低高度で急降下 | FBWのAOAセンサー値をGPWSが参照、警報作動 |
| **Engine連携** | 片肺停止 | 非対称推力下でもヨーダンパーが動作、機体安定 |
| **AutoFlaps連携** | Combat Law切替 | 自動的に Fighter Mode に移行、マニューバーフラップ有効 |
| **AdvancedGear連携** | 脚格納状態で着陸態勢 | Airliner Mode のオートフラップ無効 (脚下げ要求) |

---

## 8. パフォーマンス考慮事項

### 8.1 最適化

- `UpdateSensors()` は FixedUpdate でのみ実行 (60Hz)
- `GetProgramVariable` の呼び出しを SFEXT_L_EntityStart でキャッシュ
- PID 計算は固定時間ステップ (Time.fixedDeltaTime) を想定

### 8.2 負荷見積もり

| 処理 | コスト |
|-----|--------|
| センサー計算 (ベクトル演算×5) | 低 |
| PID計算 (3軸) | 低 |
| AddTorque (1回) | 低 |
| GetProgramVariable (RotationInputs, AirVel) | 中 |

**総合:** SFEXT_AdvancedEngine (極高負荷) より軽量。FixedUpdate あたり 0.5ms 以下想定。

---

## 10. 今後の拡張

### 10.1 民間機向け

- **Flare Mode**: 着陸時の自動フレア (高度に応じた自動ピッチアップ)
- **Alpha Floor Protection**: 低速時の自動TOGA推力 (Engine連携)
- **Wind Shear Detection**: 突風検出と自動補正
- **Landing Gear Extension Law**: 脚下げ時の制御ロー変更
- **Takeoff/Go-Around (TOGA)**: 離陸・復行時の最適制御ロー
- **Autothrottle Integration**: FBW と連動した自動スロットル

### 10.2 戦闘機向け

- **Thrust Vectoring**: 推力偏向ノズル制御 (Post-Stall時の姿勢制御)
- **Departure Resistance**: スピン防止ロジック (高AOA時のヨー安定化)
- **Automatic Spin Recovery**: スピン自動回復
- **Air-to-Air Refueling Mode**: 空中給油時の微細制御モード
- **Carrier Landing Mode**: 空母着艦用制御ロー (AOA保持優先)
- **Supermaneuverability**: Cobra/Kulbit等の Post-Stall Maneuver 専用シーケンス

---

## 11. まとめ

**SFEXT_FlyByWire** と **SFEXT_AutoFlaps** は TSFE パッケージに本格的な Fly-By-Wire 制御を追加し、以下を実現する:

### 民間機 (Airbus A320/Boeing 777 相当)
1. **4段階の制御ロー** (Normal/Alternate/Direct + Combat) による柔軟な運用
2. **エンベロープ保護** (AOA 15°制限, バンク角 67°制限, Load Factor ±2.5/-1.0G) による安全性向上
3. **オートトリム** による操縦負荷軽減
4. **速度連動オートフラップ** による離着陸支援

### 戦闘機 (F-35/F-22/Su-57 相当)
1. **Combat Law** による高AOA制御 (最大60°, Post-Stall対応)
2. **連続ロール** (180 deg/s) とバンク角制限なし
3. **高G対応** (最大9G, 構造破壊防止)
4. **マニューバーフラップ** (AOA/G負荷連動の自動展開)
5. **Post-Stall補正** による舵面効力低下の補償

### 共通
- **既存コンポーネントとの協調** (ElevatorTrim, GPWS, Engine, Gear等)
- **SFV 1.8 標準物理との共存** (追加トルク方式)
- **モジュラー設計** (FBW なしでも既存機能は動作)

これにより、VRChat 内で民間旅客機から最新鋭戦闘機まで、幅広い航空機の現代的な飛行制御体験を提供できる。
