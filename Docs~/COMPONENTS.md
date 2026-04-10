# TSFE コンポーネント一覧

**Tsuitachi-SF-Equipment (TSFE)** パッケージのすべてのコンポーネントを分類・整理したリファレンスドキュメント。

最終更新: 2026-04-11

---

## 目次

- [概要](#概要)
- [DFUNC (Dial Functions)](#dfunc-dial-functions)
- [SFEXT (SaccEntity Extensions)](#sfext-saccentity-extensions)
- [Avionics (航空電子機器)](#avionics-航空電子機器)
- [Utility (ユーティリティ)](#utility-ユーティリティ)
- [依存関係マップ](#依存関係マップ)

---

## 概要

TSFEは、SaccFlightAndVehicles 1.8向けの高度な航空機システムパッケージです。以下の5つのカテゴリに分類されます：

| カテゴリ | 説明 | コンポーネント数 |
|---------|------|----------------|
| **DFUNC** | VR/デスクトップ対応のダイヤル機能（コントロール） | 7 |
| **SFEXT** | SaccEntityに追加するシステム拡張 | 14 |
| **Avionics** | 警告・計器システム | 2 |
| **Utility** | 共通ヘルパー・バスシステム | 9 |

---

## DFUNC (Dial Functions)

VR/デスクトップ入力対応のコントロール類。`DFUNC_Selected()`/`DFUNC_Deselected()`などのイベントを実装。

| コンポーネント | 説明 | Sync | ExecOrder | 実装状況 |
|--------------|------|------|-----------|---------|
| **DFUNC_AdvancedFlaps** | 多段フラップ制御（速度制限・故障モデリング） | Continuous | 0 | ✅ 完成 |
| **DFUNC_ElevatorTrim** | エレベータートリム制御（VR軸入力対応） | Continuous | 0 | ✅ 完成 |
| **DFUNC_AdvancedSpeedBrake** | 高度なスピードブレーキ制御 | Continuous | 0 | ✅ 完成 |
| **DFUNC_AdvancedThrustReverser** | SFEXT_AdvancedEngine連携のリバーサー | Manual | 0 | ✅ 完成 |
| **DFUNC_ThrustReverser** | 標準SFV用リバーサー（SFEXT不要） | Manual | 0 | ✅ 完成 |
| **DFUNC_AdvancedParkingBrake** | SFEXT_AdvancedGear連携の駐車ブレーキ | Manual | 0 | ✅ 完成 |
| **DFUNC_AdvancedWaterRudder** | 水上機用ウォーターラダー制御 | Manual | 0 | ✅ 完成 |
| **DFUNC_MethodCaller** | 任意のUdonメソッドを呼び出すジェネリックDFUNC | None | 0 | ✅ 完成 |

### 共通パターン

すべてのDFUNCは以下の自動注入フィールドを持つ：
- `EntityControl` (SaccEntity)
- `LeftDial` (bool) - VRどちらの手か
- `DialPosition` (int) - ダイヤル位置（廃止予定）

VRトリガー入力は `TSFEUtil.GetTriggerInput(LeftDial)` で取得。

---

## SFEXT (SaccEntity Extensions)

車両に追加するシステム拡張。`SFEXT_L_EntityStart`, `SFEXT_O_PilotEnter`などのイベントを受信。

### エンジン・推進系

| コンポーネント | 説明 | Sync | ExecOrder | 依存 | 実装状況 |
|--------------|------|------|-----------|------|---------|
| **SFEXT_AdvancedEngine** | ターボファンエンジン（N1/N2/EGT/リバーサー） | Continuous | 1000 | APU, ThrustReverser | ✅ 完成 |
| **SFEXT_AuxiliaryPowerUnit** | 補助動力装置（APU、電源・空圧・始動） | Manual | 0 | PowerBus, BleedAirBus | ✅ 完成 |
| **SFEXT_AdvancedPropellerThrust** | 可変ピッチプロペラ推力 | Continuous | 0 | - | ✅ 完成 |
| **SFEXT_EngineFanDriver** | エンジンファン回転アニメーション | None | 0 | AdvancedEngine | ✅ 完成 |
| **SFEXT_AutoStarter** | 自動エンジンスターター（APU→エンジン起動） | Manual | 1000 | APU, AdvancedEngine | ⚠️ 要検証 |
| **SFEXT_EngineToggle** | シンプルなエンジンON/OFFトグル | None | 0 | - | ✅ 完成 |

### 揚力・操縦系

| コンポーネント | 説明 | Sync | ExecOrder | 依存 | 実装状況 |
|--------------|------|------|-----------|------|---------|
| **SFEXT_AutoFlaps** | 自動フラップスケジューリング（速度・AoA・G対応） | None | 0 | DFUNC_AdvancedFlaps | ✅ 完成 |
| **SFEXT_AdvancedGear** | 高度なランディングギア（油圧・故障モデル） | Continuous | 0 | HydraulicBus | ⚠️ 要検証 |
| **SFEXT_DihedralEffect** | 上反効果（横滑り時の復元モーメント） | None | 0 | - | ✅ 完成 |
| **SFEXT_WakeTurbulence** | ウェイクタービュランス（後方乱気流） | None | 0 | - | ✅ 完成 |

### 計器・表示

| コンポーネント | 説明 | Sync | ExecOrder | 依存 | 実装状況 |
|--------------|------|------|-----------|------|---------|
| **SFEXT_InstrumentsAnimationDriver** | アナログ計器駆動（ADI/ASI/Altimeter等10種） | None | 0 | PowerBus | ✅ 完成 |
| **SFEXT_Warning** | エンジン警告灯制御（MASTER CAUTION等） | None | 0 | AdvancedEngine | ✅ 完成 |

### その他システム

| コンポーネント | 説明 | Sync | ExecOrder | 依存 | 実装状況 |
|--------------|------|------|-----------|------|---------|
| **SFEXT_Chock** | 車両固定用チョック（車輪ブロック） | None | 0 | - | ✅ 完成 |
| **SFEXT_BoardingCollider** | 搭乗判定コライダー制御 | None | 0 | - | ✅ 完成 |
| **SFEXT_OutsideOnly** | 外部視点時のみ有効化 | None | 0 | - | ✅ 完成 |
| **SFEXT_PassengerOnly** | 乗客時のみ有効化 | None | 0 | - | ✅ 完成 |
| **SFEXT_SeatsOnly** | 座席使用時のみ有効化 | None | 0 | - | ✅ 完成 |

### テスト用

| コンポーネント | 説明 | 用途 |
|--------------|------|------|
| **SFEXT_AdvancedEngineTest** | AdvancedEngineテストベンチ | テストシーン専用 |
| **SFEXT_AuxiliaryPowerUnitTest** | APUテストベンチ | テストシーン専用 |
| **MockSAVControl** | SaccAirVehicleモックオブジェクト | ユニットテスト用 |

---

## Avionics (航空電子機器)

警告・計器システム。

| コンポーネント | 説明 | Sync | ExecOrder | 依存 | 実装状況 |
|--------------|------|------|-----------|------|---------|
| **GPWS** | 対地接近警報（6モード：Sink Rate/Terrain/Gear等） | NoVariableSync | 1100 | AdvancedFlaps（任意） | ✅ 完成 |
| **AuralWarnings** | 速度超過警報（Overspeed/Clacker/Stall） | NoVariableSync | 0 | AdvancedFlaps（任意） | ✅ 完成 |

---

## Utility (ユーティリティ)

共通ヘルパー・バスシステム。

### 共通ヘルパー

| コンポーネント | 説明 | 実装状況 |
|--------------|------|---------|
| **TSFEUtil** | 静的ヘルパー（単位変換/数学/MTBF/DFUNC補助） | ✅ 完成 |
| **TSFE_ParameterTransform** | Udon変数→Transform位置/回転 | ✅ 完成 |
| **TSFE_ParameterText** | Udon変数→TextMeshPro表示 | ✅ 完成 |
| **TSFE_InteractProxy** | Interactイベントを別オブジェクトに転送 | ✅ 完成 |

### バスシステム

| コンポーネント | 説明 | 実装状況 |
|--------------|------|---------|
| **TSFE_PowerBus** | 電源バス（複数電源の統合管理） | ✅ 完成 |
| **TSFE_HydraulicBus** | 油圧バス（複数ポンプの統合管理） | ✅ 完成 |
| **TSFE_HydraulicPump** | 油圧ポンプ（電源/エンジン駆動） | ✅ 完成 |
| **TSFE_BleedAirBus** | ブリードエアバス（エンジン/APU空圧管理） | ✅ 完成 |

### テスト用

| コンポーネント | 説明 | 用途 |
|--------------|------|------|
| **TestScenario** | テストシナリオ定義 | ユニットテスト用 |
| **TestScenarioRunner** | テストシナリオ実行 | ユニットテスト用 |

---

## 依存関係マップ

### Phase 1: コアシステム（独立動作可能）

```
DFUNC_AdvancedFlaps
DFUNC_ElevatorTrim
DFUNC_AdvancedSpeedBrake
SFEXT_AuxiliaryPowerUnit → TSFE_PowerBus, TSFE_BleedAirBus
GPWS → DFUNC_AdvancedFlaps (optional)
AuralWarnings → DFUNC_AdvancedFlaps (optional)
```

### Phase 2: エンジン・ギアシステム

```
SFEXT_AdvancedEngine → SFEXT_AuxiliaryPowerUnit
                      → TSFE_PowerBus, TSFE_BleedAirBus
DFUNC_AdvancedThrustReverser → SFEXT_AdvancedEngine
SFEXT_EngineFanDriver → SFEXT_AdvancedEngine
SFEXT_Warning → SFEXT_AdvancedEngine
SFEXT_AutoStarter → SFEXT_AuxiliaryPowerUnit, SFEXT_AdvancedEngine

SFEXT_AdvancedGear → TSFE_HydraulicBus
DFUNC_AdvancedParkingBrake → SFEXT_AdvancedGear

SFEXT_AutoFlaps → DFUNC_AdvancedFlaps
```

### Phase 3: 計器・ユーティリティ

```
SFEXT_InstrumentsAnimationDriver → TSFE_PowerBus (optional)
DFUNC_ThrustReverser (標準SFV用、SFEXT不要)
DFUNC_MethodCaller (汎用)
SFEXT_OutsideOnly, SFEXT_PassengerOnly, SFEXT_SeatsOnly (独立)
SFEXT_BoardingCollider (独立)
```

### Phase 4: 特殊システム

```
DFUNC_AdvancedWaterRudder (独立)
SFEXT_DihedralEffect (独立)
SFEXT_WakeTurbulence (独立)
SFEXT_Chock (独立)
```

---

## 実装状況サマリー

| カテゴリ | 完成 | 要検証 | 未実装 | 合計 |
|---------|------|--------|--------|------|
| DFUNC | 8 | 0 | 0 | 8 |
| SFEXT | 16 | 2 | 0 | 18 |
| Avionics | 2 | 0 | 0 | 2 |
| Utility | 9 | 0 | 0 | 9 |
| **合計** | **35** | **2** | **0** | **37** |

**要検証コンポーネント:**
- SFEXT_AutoStarter - APU連携のテスト
- SFEXT_AdvancedGear - 油圧バス統合のテスト

---

## 次のステップ

リファクタリングの方針検討時は、以下の観点で整理してください：

1. **共通パターンの抽出**: 複数コンポーネントで重複しているロジック
2. **依存関係の整理**: 循環参照や不要な依存の削減
3. **テスト容易性**: Mock/Stubの導入可能性
4. **パフォーマンス**: `GetProgramVariable`呼び出しの最適化
5. **保守性**: 命名規則・コメント・ドキュメントの統一

詳細は `Docs~/old/refactoring-analysis.md` を参照。
