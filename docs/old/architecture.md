# アーキテクチャ

## 概要

TSFE は、SaccFlightAndVehicles (SFV) の拡張ポイントを通じて統合されるコンポーネントベースのアーキテクチャに従っています。システムは 4 つの主要カテゴリに分類されます：

1. **DFUNC** - ダイアル機能（VR/デスクトップのインタラクティブコントロール）
2. **SFEXT** - SaccEntity 拡張（車両システム）
3. **Avionics** - アビオニクス（GPWS、警報）
4. **Utilities** - 共有ヘルパーシステム

## コンポーネントカテゴリ

### DFUNC (ダイアル機能)

**用途**: DFUNCダイアルに接続される VR/デスクトップユーザー向けインタラクティブコントロール。

**基本パターン**:
- `UdonSharpBehaviour` から直接派生（基底クラスなし）
- SFV により自動注入されるフィールド:
  - `EntityControl` (SaccEntity)
  - `LeftDial` (bool)
  - `DialPosition` (int)
- 必須メソッド:
  - `DFUNC_Selected()` - ダイアル選択時に呼び出される
  - `DFUNC_Deselected()` - ダイアル選択解除時に呼び出される
  - `DFUNC_LeftDial()` - 左ダイアル回転
  - `DFUNC_RightDial()` - 右ダイアル回転

**VR 入力パターン**:
```csharp
// 手動トリガー処理（DFUNC_Base なし）
float trigger = LeftDial
    ? Input.GetAxisRaw("Oculus_CrossPlatform_PrimaryIndexTrigger")
    : Input.GetAxisRaw("Oculus_CrossPlatform_SecondaryIndexTrigger");

bool pressed = trigger > 0.75f;
```

または TSFEUtil ヘルパーを使用:
```csharp
bool pressed = TSFEUtil.IsTriggerPressed(LeftDial);
```

**ダイアル表示パターン**:
```csharp
// 状態変化時にダイアル表示を切り替え
TSFEUtil.SetDialFuncon(Dial_Funcon, Dial_Funcon_Array, isActive);
```

**実装済み DFUNC コンポーネント**:
- `DFUNC_AdvancedFlaps` - 多段デテントフラップ
- `DFUNC_AdvancedParkingBrake` - パーキングブレーキ
- `DFUNC_AdvancedSpeedBrake` - スピードブレーキ
- `DFUNC_AdvancedThrustReverser` - 逆推力装置（AdvancedEngine 用）
- `DFUNC_AdvancedWaterRudder` - 水上ラダー
- `DFUNC_ElevatorTrim` - エレベータトリム
- `DFUNC_MethodCaller` - 汎用メソッド呼び出し
- `DFUNC_ThrustReverser` - 標準逆推力装置

### SFEXT (SaccEntity 拡張)

**用途**: SaccEntity に接続され、ライフサイクルイベントを受け取る車両システム。

**ライフサイクルイベント**:
- `SFEXT_L_EntityStart()` - エンティティ初期化
- `SFEXT_O_PilotEnter()` - ローカルパイロット搭乗
- `SFEXT_O_PilotExit()` - ローカルパイロット降機
- `SFEXT_P_PassengerEnter()` - ローカル乗客搭乗
- `SFEXT_P_PassengerExit()` - ローカル乗客降機
- `SFEXT_G_Explode()` - 車両爆発
- `SFEXT_G_RespawnButton()` - リスポーンボタン押下
- `SFEXT_O_TakeOwnership()` - ローカルプレイヤーがオーナー取得
- `SFEXT_O_LoseOwnership()` - ローカルプレイヤーがオーナー喪失

**SaccAirVehicle データアクセスパターン**:
```csharp
// 読み取り
float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
bool engineOn = (bool)SAVControl.GetProgramVariable("EngineOn");

// 書き込み（物理演算用の加算型）
float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
SAVControl.SetProgramVariable("ExtraDrag", currentDrag + deltaDrag);
```

**主要な SAVControl フィールド**:
- **物理**: `ExtraDrag`, `ExtraLift`, `AirSpeed`, `AirVel`, `Atmosphere`, `VehicleRigidbody`
- **エンジン**: `EngineOn`, `ThrottleStrength`, `EngineOutput`, `Fuel`, `FullFuel`
- **状態**: `Taxiing`, `Floating`, `PitchDown`
- **アニメーション**: `VehicleAnimator`

**実装済み SFEXT コンポーネント**:
- `SFEXT_AdvancedEngine` - ターボファンシミュレーション
- `SFEXT_AdvancedGear` - ランディングギア
- `SFEXT_AdvancedPropellerThrust` - プロペラ推力
- `SFEXT_AuxiliaryPowerUnit` - APU システム
- `SFEXT_AutoStarter` - 自動エンジン始動
- `SFEXT_BoardingCollider` - 搭乗エリア
- `SFEXT_DihedralEffect` - 上反角効果
- `SFEXT_EngineFanDriver` - ファン回転
- `SFEXT_EngineToggle` - エンジン ON/OFF トグル
- `SFEXT_InstrumentsAnimationDriver` - 計器駆動
- `SFEXT_OutsideOnly` - 外部専用オブジェクト
- `SFEXT_PassengerOnly` - 乗客専用オブジェクト
- `SFEXT_SeatsOnly` - 座席専用オブジェクト
- `SFEXT_WakeTurbulence` - 後方乱気流
- `SFEXT_Warning` - 汎用警報

### Avionics（アビオニクス）

**用途**: 航空電子システム（通常は同期なし、ローカルのみ）。

**パターン**:
- 通常 `BehaviourSyncMode.None`（ローカルのみ）
- public 参照を介して SFEXT コンポーネントにアクセス
- DFUNC コンポーネントとのオプション統合

**実装済みコンポーネント**:
- `GPWS` - 対地接近警報装置
- `AuralWarnings` - オーラル警報音

### Utilities（ユーティリティ）

**用途**: 共有ヘルパーシステムと数学ユーティリティ。

#### TSFEUtil (静的ヘルパークラス)

**単位変換**:
```csharp
float knots = TSFEUtil.ToKnots(metersPerSecond);
float ms = TSFEUtil.FromKnots(knots);
float feet = TSFEUtil.ToFeet(meters);
float meters = TSFEUtil.FromFeet(feet);
```

**数学ヘルパー**:
```csharp
// 0-1 への線形リマップ
float normalized = TSFEUtil.Remap01(value, min, max);

// クランプ付きリマップ
float clamped = TSFEUtil.ClampedRemap01(value, min, max);

// 3 点補間
float result = TSFEUtil.Lerp3(a, b, c, t, tMin, tMid, tMax);
```

**故障モデリング**:
```csharp
// MTBF ベースの故障判定
if (TSFEUtil.CheckMTBF(deltaTime, mtbfHours)) {
    // コンポーネント故障
}

// ダメージ倍率付き
if (TSFEUtil.CheckMTBF(deltaTime, mtbfHours, damageMultiplier)) {
    // 加速故障
}
```

**DFUNC ヘルパー**:
```csharp
float trigger = TSFEUtil.GetTriggerInput(leftDial);
bool pressed = TSFEUtil.IsTriggerPressed(leftDial);
TSFEUtil.SetDialFuncon(dialFuncon, dialFunconArray, active);
```

#### システムバス

**TSFE_PowerBus** - 電力配電:
- バッテリー、APU 発電機、エンジン発電機
- 電源優先度システム
- 電圧出力管理

**TSFE_BleedAirBus** - ブリード空気配給:
- APU ブリード、エンジンブリード
- 圧力管理

**TSFE_HydraulicBus** - 油圧システム:
- 複数の油圧回路
- 圧力管理
- `TSFE_HydraulicPump` によるポンプ統合

#### パラメータマッピング

**TSFE_ParameterTransform** - パラメータを Transform プロパティ（位置、回転、スケール）にマッピング

**TSFE_ParameterText** - パラメータを TextMeshPro テキスト表示にマッピング

## 同期モード

### 継続同期 (Continuous Sync)
リアルタイム状態同期を行うコンポーネント:
- `DFUNC_AdvancedFlaps`
- `DFUNC_ElevatorTrim`
- `DFUNC_AdvancedSpeedBrake`
- `SFEXT_AdvancedEngine`
- `SFEXT_AdvancedGear`
- `SFEXT_AdvancedPropellerThrust`

### 手動同期 (Manual Sync)
イベントベースの同期を行うコンポーネント:
- `DFUNC_AdvancedParkingBrake`
- `DFUNC_AdvancedWaterRudder`
- `SFEXT_AuxiliaryPowerUnit`
- `SFEXT_AutoStarter`
- `DFUNC_ThrustReverser`

### 同期なし (No Sync)
ローカル専用コンポーネント:
- 全アビオニクス（GPWS、AuralWarnings）
- 全ユーティリティ（表示専用）
- SFEXT_Warning
- DFUNC_MethodCaller

## 実行順序

タイミングクリティカルなコンポーネントのカスタム実行順序:
- `SFEXT_AdvancedEngine`: **1000**（依存システムより先に実行する必要がある）
- `SFEXT_AutoStarter`: **1000**（エンジンと連携）
- `GPWS`: **1100**（更新後にエンジン/フラップ状態を読み取る）

## コンポーネント依存関係

### Phase 1（コア、独立）
- `DFUNC_AdvancedFlaps`
- `DFUNC_ElevatorTrim`
- `DFUNC_AdvancedSpeedBrake`
- `SFEXT_AuxiliaryPowerUnit`
- `GPWS`（標準 SFV ギア/フラップで動作）

### Phase 2（エンジン & ギア）
- `SFEXT_AdvancedEngine` ← `DFUNC_AdvancedThrustReverser`, `SFEXT_EngineFanDriver`, `SFEXT_Warning`
- `SFEXT_AdvancedGear` ← `DFUNC_AdvancedParkingBrake`
- `SFEXT_AutoStarter` → `SFEXT_AuxiliaryPowerUnit`, `SFEXT_AdvancedEngine`
- `SFEXT_EngineToggle` → `SFEXT_AutoStarter`
- `SFEXT_AdvancedPropellerThrust`
- `SFEXT_InstrumentsAnimationDriver`

### Phase 3（アビオニクス & ユーティリティ）
- `AuralWarnings`（オプションで `DFUNC_AdvancedFlaps` を使用）
- `DFUNC_ThrustReverser`（標準、非 AdvancedEngine）
- `DFUNC_MethodCaller`
- `SFEXT_OutsideOnly`, `SFEXT_PassengerOnly`, `SFEXT_SeatsOnly`
- `SFEXT_BoardingCollider`

### Phase 4（特殊）
- `DFUNC_AdvancedWaterRudder`
- `SFEXT_WakeTurbulence`
- `SFEXT_DihedralEffect`
- `PickupChock`

## 設計パターン

### 状態管理
ほとんどのコンポーネントは明示的な状態 enum を使用:
```csharp
public enum EngineState { Off, Starting, Windmilling, Running }
[UdonSynced] public EngineState State;
```

### FieldChangeCallback パターン
```csharp
[UdonSynced, FieldChangeCallback(nameof(Fuel))]
private bool _fuel;
public bool Fuel
{
    get => _fuel;
    set
    {
        _fuel = value;
        if (value) OnFuelEnabled();
        else OnFuelDisabled();
    }
}
```

### INOP (作動不能) パターン
リアリズムのためにコンポーネントは INOP 状態をサポート:
```csharp
public bool IsInoperable; // ファイアハンドル、メンテナンス等により設定

// 操作を許可する前にチェック
if (IsInoperable) return;
```

## テストフレームワーク

**TestScenario** - 自動テストシーケンスを定義
**TestScenarioRunner** - テストシナリオを実行
**MockSAVControl** - ユニットテスト用の SaccAirVehicle モック

## EsnyaSFAddons からの移行

主なアーキテクチャ変更:
1. **DFUNC_Base なし**: 手動での VR トリガー処理が必要
2. **UdonToolkit なし**: 標準 Unity `[Header]`, `[Tooltip]` を使用
3. **InariUdon なし**: 依存関係を削除
4. **SAVControl パターン**: `UdonSharpBehaviour` 参照 + `GetProgramVariable()`
5. **名前空間**: `EsnyaSFAddons` → `TSFE`

## アセンブリ定義

**TSFE.Runtime** (`Runtime/TSFE.Runtime.asmdef`):
- 参照: UdonSharp.Runtime, VRC.Udon, VRC.SDKBase, VRC.Udon.Serialization.OdinSerializer, SaccFlightAndVehicles.Runtime
- ルート名前空間: `TSFE`
- 自動参照: true

**TSFE.Editor** (`Editor/TSFE.Editor.asmdef`):
- 参照: TSFE.Runtime, UdonSharp.Editor, VRC SDKs
- プラットフォーム: Editor のみ
- ルート名前空間: `TSFE.Editor`
