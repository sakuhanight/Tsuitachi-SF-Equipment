# SF-AdvancedEquipment 実装計画書

## 1. プロジェクト概要

**目的:** EsnyaSFAddons に実装されていた装備品を SaccFlightAndVehicles 1.8 対応として新規開発する。

**パッケージ名:** `com.sakuha.sf-advequipment`
**対象SFVバージョン:** 1.8.0
**名前空間:** `SFAdvEquipment`

---

## 2. 実装対象の全体像

### 2.1 スクリプト分類サマリ

| 分類 | 数 | 内容 |
|------|---|------|
| 実装対象 | 26 | SF-AdvEquipment として新規実装 |
| 移植不要 | 8 | SFV 1.8 標準で代用可能 |
| 対象外 | 14 | 需要限定・特殊用途・外部依存等 |
| 廃止 (基盤変更) | 2 | DFUNC_Base、Annotations |
| Editor拡張 | 5 | 別途検討 |

### 2.2 移植不要 (SFV 1.8 標準で代用)

| EsnyaSFAddons | SFV 1.8 代用機能 |
|--------------|-----------------|
| `DFUNC_LandingLight` | `DFUNC_ToggleBool` |
| `DFUNC_Empty` | (不要) |
| `DFUNC_Slider` | `DFUNC_SlideAnimation` |
| `DFUNC_SendCustomEvent` | `DFUNC_ToggleBool` (部分的) |
| `SFEXT_KillPenalty` | `SAV_KillTracker` |
| 基本フラップ ON/OFF | `DFUNC_Flaps` |
| 基本カタパルト | `DFUNC_Catapult` |
| 基本フック | `DFUNC_Hook` |

### 2.3 対象外

| スクリプト | 理由 |
|-----------|------|
| `SFEXT_AbstractVehicle` | SaccAirVehicle 完全代替 (1400+行)。SFV 1.8 では不要 |
| `ESFASceneSetup` | EsnyaSFAddons 固有のシーン設定 |
| `SFEXT_UdonChips` / `GetMoneyOnExplode` | 外部依存 (UdonChips-fork)。必要なら別パッケージ |
| `DFUNC_Base` | EsnyaSFAddons 独自基底クラス。SFV 1.8 パターンに変更 |
| `Annotations` (2個) | EsnyaSFAddons 固有ユーティリティ |
| `WindIndicator` | 需要限定 |
| `DFUNC_WingFold` | 特殊用途 (空母艦載機) |
| `DFUNC_AdvancedCatapult` | 特殊用途 (空母運用) |
| `CatapultController` | 特殊用途 (空母運用) |
| `DFUNC_BuddyPod` | 特殊用途 (空中給油) |
| `DrogueHead` | 特殊用途 (空中給油) |
| `SFEXT_FloodedEngineKiller` | 需要限定 (水陸両用機) |
| `Windsock` | 需要限定 |
| `GroundWindIndicator` | 需要限定 |
| `SFEXT_Logger` | InariUdon の `UdonLogger` に依存。ロギング基盤から再設計が必要 |
| `RemoteThrottle` | 需要限定 |
| `WheelDriver` | 需要限定 |
| `WheelLock` | 需要限定 |
| `FogController` | SFV非依存の汎用スクリプト。必要時にそのまま使用可 |
| `RandomWindChanger` | SFV 1.8 標準の `SAV_WindChanger` と機能重複 |
| `RVR_Controller` | 需要限定 |
| Editor拡張 (5個) | パッケージ完成後にまとめて対応 |

---

## 3. 実装対象一覧 (全26スクリプト)

### Phase 1: コア装備 (優先度A) - 8スクリプト

SFV 1.8 標準にない、または標準では機能不足のコア装備。

| # | スクリプト | カテゴリ | 概要 | 難易度 |
|---|----------|---------|------|--------|
| 1 | `DFUNC_AdvancedFlaps` | DFUNC | 多段デテントフラップ (速度制限・破損・ハプティクス) | 高 |
| 2 | `DFUNC_ElevatorTrim` | DFUNC | エレベータトリム制御 | 低 |
| 3 | `DFUNC_AdvancedParkingBrake` | DFUNC | パーキングブレーキ | 低 |
| 4 | `DFUNC_AdvancedSpeedBrake` | DFUNC | スピードブレーキ/スポイラー | 中 |
| 5 | `SFEXT_AuxiliaryPowerUnit` | SFEXT | APU始動/停止シーケンス | 中 |
| 6 | `SFEXT_EngineFanDriver` | SFEXT | エンジンファンアニメーション駆動 | 低 |
| 7 | `GPWS` | Avionics | 対地接近警報装置 (モード1-6) | 高 |
| 8 | `SFEXT_Warning` | SFEXT | 汎用警告システム | 低 |

### Phase 2: 高度な車両システム (優先度B) - 6スクリプト

エンジン・脚・計器などの複雑なシステム。Phase 1 完了後に着手。

| # | スクリプト | カテゴリ | 概要 | 難易度 |
|---|----------|---------|------|--------|
| 9 | `SFEXT_AdvancedEngine` | SFEXT | タービファンエンジン (N1/N2・EGT・始動・火災・FOD) | 極高 |
| 10 | `SFEXT_AdvancedGear` | SFEXT | 高度な脚 (速度制限・バースト・MTBF故障・サスペンション) | 高 |
| 11 | `DFUNC_AdvancedThrustReverser` | DFUNC | 逆噴射制御 (インターロック付き、エンジン連携) | 中 |
| 12 | `DFUNC_AutoStarter` | DFUNC | 自動エンジン始動シーケンス | 中 |
| 13 | `SFEXT_AdvancedPropellerThrust` | SFEXT | プロペラ推力シミュレーション | 中 |
| 14 | `SFEXT_InstrumentsAnimationDriver` | SFEXT | コクピット計器アニメーション駆動 | 中 |

### Phase 3: アビオニクス・ユーティリティ (優先度B-) - 8スクリプト

独立性が高く、個別に移植可能な装備。

| # | スクリプト | カテゴリ | 概要 | 難易度 |
|---|----------|---------|------|--------|
| 15 | `AuralWarnings` | Avionics | 音響警報システム | 中 |
| 16 | `DFUNC_ThrustReverser` | DFUNC | 標準逆噴射制御 (AdvancedEngine非依存) | 中 |
| 17 | `DFUNC_SeatAdjuster` | DFUNC | シート位置調整 (VR/デスクトップ) | 低 |
| 18 | `DFUNCP_IHaveControl` | DFUNC | パッセンジャーからの操縦権移譲 (SFV 1.8 代替なし) | 中 |
| 19 | `SFEXT_OutsideOnly` | SFEXT | 車両外のみオブジェクト表示 | 低 |
| 20 | `SFEXT_PassengerOnly` | SFEXT | パッセンジャーのみオブジェクト表示 | 低 |
| 21 | `SFEXT_SeatsOnly` | SFEXT | 特定シートのみオブジェクト表示 | 低 |
| 22 | `SFEXT_BoardingCollider` | SFEXT | 地上歩行用搭乗コライダー | 中 |

### Phase 4: その他 (優先度C) - 4スクリプト

特殊用途・環境向け。

| # | スクリプト | カテゴリ | 概要 | 難易度 |
|---|----------|---------|------|--------|
| 23 | `DFUNC_AdvancedWaterRudder` | DFUNC | 水上ラダー (水陸両用機) | 低 |
| 24 | `SFEXT_WakeTurbulence` | SFEXT | 後方乱気流シミュレーション | 中 |
| 25 | `SFEXT_DihedralEffect` | SFEXT | 上反角効果 | 中 |
| 26 | `PickupChock` | Accessories | 車輪止め | 低 |

---

## 4. パッケージ構造

```
SF-AdvEquipment/
├── Packages/
│   └── com.sakuha.sf-advequipment/
│       ├── Runtime/
│       │   ├── Utility/
│       │   │   └── SFAEUtil.cs
│       │   ├── DFUNC/
│       │   │   ├── DFUNC_AdvancedFlaps.cs
│       │   │   ├── DFUNC_ElevatorTrim.cs
│       │   │   ├── DFUNC_AdvancedParkingBrake.cs
│       │   │   ├── DFUNC_AdvancedSpeedBrake.cs
│       │   │   ├── DFUNC_AdvancedThrustReverser.cs
│       │   │   ├── DFUNC_ThrustReverser.cs
│       │   │   ├── DFUNC_AutoStarter.cs
│       │   │   ├── DFUNC_SeatAdjuster.cs
│       │   │   ├── DFUNC_AdvancedWaterRudder.cs
│       │   │   └── DFUNCP_IHaveControl.cs
│       │   ├── SFEXT/
│       │   │   ├── SFEXT_AuxiliaryPowerUnit.cs
│       │   │   ├── SFEXT_EngineFanDriver.cs
│       │   │   ├── SFEXT_Warning.cs
│       │   │   ├── SFEXT_AdvancedEngine.cs
│       │   │   ├── SFEXT_AdvancedGear.cs
│       │   │   ├── SFEXT_AdvancedPropellerThrust.cs
│       │   │   ├── SFEXT_InstrumentsAnimationDriver.cs
│       │   │   ├── SFEXT_OutsideOnly.cs
│       │   │   ├── SFEXT_PassengerOnly.cs
│       │   │   ├── SFEXT_SeatsOnly.cs
│       │   │   ├── SFEXT_BoardingCollider.cs
│       │   │   ├── SFEXT_DihedralEffect.cs
│       │   │   └── SFEXT_WakeTurbulence.cs
│       │   ├── Avionics/
│       │   │   ├── GPWS.cs
│       │   │   └── AuralWarnings.cs
│       │   ├── Accessories/
│       │   │   └── PickupChock.cs
│       │   └── SFAdvEquipment.Runtime.asmdef
│       ├── Editor/
│       │   └── SFAdvEquipment.Editor.asmdef
│       ├── Prefabs/
│       └── package.json
├── docs/
│   ├── investigation-report.md
│   └── implementation-plan.md
└── README.md
```

---

## 5. 設計方針

### 5.1 SFV 1.8 標準パターンへの準拠

EsnyaSFAddons 独自の `DFUNC_Base` 基底クラスを廃止し、SFV 1.8 標準パターンに統一する。

**変更前 (EsnyaSFAddons):**
```csharp
// 独自基底クラスを継承
public class DFUNC_AdvancedFlaps : DFUNC_Base { }

// DFUNC_Base が VR トリガー入力を処理
```

**変更後 (SF-AdvEquipment):**
```csharp
// UdonSharpBehaviour を直接継承 (SFV 1.8 標準)
namespace SFAdvEquipment.DFUNC
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Continuous)]
    public class DFUNC_AdvancedFlaps : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;
        public GameObject Dial_Funcon;
        public GameObject[] Dial_Funcon_Array;

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        // SFV 1.8 標準イベント
        public void SFEXT_L_EntityStart() { }
        public void SFEXT_O_PilotEnter() { }
        public void SFEXT_O_PilotExit() { }
        public void DFUNC_Selected() { }
        public void DFUNC_Deselected() { }
        public void KeyboardInput() { }
    }
}
```

### 5.2 共通ユーティリティクラス (SFAEUtil)

EsnyaSFAddons では各スクリプトに重複して定義されていたヘルパーメソッドを `SFAEUtil` 静的クラスに集約する。

| カテゴリ | メソッド | 使用箇所 |
|---------|---------|---------|
| 数学 | `Remap01`, `ClampedRemap01`, `ClampedRemap`, `Lerp3` | 6+ スクリプト |
| 故障判定 | `CheckMTBF` | AdvancedFlaps, AdvancedGear, AdvancedEngine, AdvancedPropellerThrust |
| 単位変換 | `ToKnots`, `ToFeet`, `FromKnots`, `FromFeet` + 定数 | 7+ スクリプト |
| DFUNC | `GetTriggerInput`, `IsTriggerPressed`, `SetDialFuncon`, `PlayHaptics` | 全 DFUNC |

詳細は `docs/detailed-design.md` セクション 1.1 を参照。

### 5.3 外部依存の排除

| EsnyaSFAddons の依存 | SF-AdvEquipment の方針 |
|---------------------|---------------------|
| `com.nekometer.esnya.inari-udon` | 排除。`UdonLogger` を使う `SFEXT_Logger` は実装保留 |
| `sh.orels.udontoolkit` | 排除。`[SectionHeader]`、`[HideIf]` 等の属性を標準の `[Header]`、`[Tooltip]` に置換 |
| `com.unity.textmeshpro` | 必要な場合のみ依存 |

### 5.4 SFEXTP イベント体系

`DFUNCP_IHaveControl` と `DFUNC_SeatAdjuster` は `SFEXTP_*` イベント (パッセンジャー用) を使用している。SFV 1.8 の `SAV_PassengerFunctionsController` が発火するイベント名を精査し、互換性を確認する。

### 5.5 命名規則

| 項目 | 規則 |
|------|------|
| 名前空間 | `SFAdvEquipment`, `SFAdvEquipment.DFUNC`, `SFAdvEquipment.SFEXT`, `SFAdvEquipment.Avionics` |
| クラス名 | EsnyaSFAddons と同一名を維持 (利用者の混乱防止) |
| ファイル名 | クラス名と同一 |

---

## 6. Phase 1 実装詳細

### 6.1 DFUNC_AdvancedFlaps

**元ファイル:** `EsnyaSFAddons/.../DFUNC/DFUNC_AdvancedFlaps.cs`
**主な変更点:**
- `DFUNC_Base` 継承を廃止 → `UdonSharpBehaviour` 直接継承
- VR トリガー入力処理を SFV 1.8 パターンで自前実装
- `UdonToolkit` 属性 (`[HideIf]`, `[Popup]`) を除去
- `EntityControl`, `LeftDial`, `DialPosition`, `SAVControl` フィールドを追加
- `Dial_Funcon_Array` 対応を追加
- `SaccAirVehicle` の `ExtraDrag`/`ExtraLift` へのアクセスを SFV 1.8 API に合わせて確認

### 6.2 DFUNC_ElevatorTrim

**元ファイル:** `EsnyaSFAddons/.../DFUNC/DFUNC_ElevatorTrim.cs`
**主な変更点:**
- `DFUNC_Base` 継承を廃止
- SFV 1.8 パターンに変更

### 6.3 DFUNC_AdvancedParkingBrake

**元ファイル:** `EsnyaSFAddons/.../DFUNC/DFUNC_AdvancedParkingBrake.cs`
**主な変更点:**
- `DFUNC_Base` 継承を廃止
- SFV 1.8 パターンに変更

### 6.4 DFUNC_AdvancedSpeedBrake

**元ファイル:** `EsnyaSFAddons/.../DFUNC/DFUNC_AdvancedSpeedBrake.cs`
**主な変更点:**
- `DFUNC_Base` 継承を廃止
- SFV 1.8 パターンに変更

### 6.5 SFEXT_AuxiliaryPowerUnit

**元ファイル:** `EsnyaSFAddons/.../SFEXT/SFEXT_AuxiliaryPowerUnit.cs`
**主な変更点:**
- SFV API 依存度が低いため変更は最小限
- 名前空間を `SFAdvEquipment.SFEXT` に変更
- `SFEXT_O_TakeOwnership` の戻り値を `void` に修正 (元は `bool` だが不正)

### 6.6 SFEXT_EngineFanDriver

**元ファイル:** `EsnyaSFAddons/.../SFEXT/SFEXT_EngineFanDriver.cs`
**主な変更点:**
- 名前空間変更のみ

### 6.7 GPWS

**元ファイル:** `EsnyaSFAddons/.../Avionics/GPWS.cs`
**主な変更点:**
- `DFUNC_Gear` / `DFUNC_Flaps` / `DFUNC_AdvancedFlaps` への参照取得方法を SFV 1.8 に合わせて修正
- `EsnyaSFAddons.DFUNC` 名前空間の参照を `SFAdvEquipment.DFUNC` に変更

### 6.8 SFEXT_Warning

**元ファイル:** `EsnyaSFAddons/.../SFEXT/SFEXT_Warning.cs`
**主な変更点:**
- 名前空間変更のみ

---

## 7. 実装スケジュール

```
Phase 1 (コア装備 8個)
 ├─ パッケージ基盤構築 (package.json, asmdef, ディレクトリ構造)
 ├─ SFAEUtil (共通ユーティリティクラス)
 ├─ DFUNC_AdvancedFlaps
 ├─ DFUNC_ElevatorTrim
 ├─ DFUNC_AdvancedParkingBrake
 ├─ DFUNC_AdvancedSpeedBrake
 ├─ SFEXT_AuxiliaryPowerUnit
 ├─ SFEXT_EngineFanDriver
 ├─ GPWS
 └─ SFEXT_Warning

Phase 2 (高度な車両システム 6個)
 ├─ SFEXT_AdvancedEngine ← 最大の移植対象 (670+行)
 ├─ SFEXT_AdvancedGear
 ├─ DFUNC_AdvancedThrustReverser
 ├─ DFUNC_AutoStarter
 ├─ SFEXT_AdvancedPropellerThrust
 └─ SFEXT_InstrumentsAnimationDriver

Phase 3 (アビオニクス・ユーティリティ 8個)
 ├─ AuralWarnings
 ├─ DFUNC_ThrustReverser
 ├─ DFUNC_SeatAdjuster
 ├─ DFUNCP_IHaveControl
 ├─ SFEXT_OutsideOnly
 ├─ SFEXT_PassengerOnly
 ├─ SFEXT_SeatsOnly
 └─ SFEXT_BoardingCollider

Phase 4 (その他 4個)
 ├─ DFUNC_AdvancedWaterRudder
 ├─ SFEXT_WakeTurbulence
 ├─ SFEXT_DihedralEffect
 └─ PickupChock
```

---

## 8. 技術的課題と対策

| 課題 | 対策 |
|------|------|
| `DFUNC_Base` 廃止 | 各 DFUNC に SFV 1.8 標準の VR トリガー処理を個別実装 |
| `UdonToolkit` 属性の除去 | `[SectionHeader]` → `[Header]`、`[HideIf]` → 削除、`[Popup]` → 削除 |
| `InariUdon` 依存の除去 | `SFEXT_Logger` は実装保留。他のスクリプトでは不使用 |
| `SFEXTP_*` イベント互換性 | `SAV_PassengerFunctionsController` のイベント発火を精査 |
| `SaccAirVehicle` フィールド名差異 | `ThrottleOverridden_`, `EngineOn`, `ControlsRoot` 等の存在を SFV 1.8 で確認 |
| ネットワーク同期の検証 | 各スクリプトの `BehaviourSyncMode` を SFV 1.8 の慣例に合わせて確認 |
| `Dial_Funcon` → `Dial_Funcon_Array` | SFV 1.8 標準に合わせ両方をサポート |
