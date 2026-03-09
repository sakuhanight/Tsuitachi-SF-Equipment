# TSFE アーキテクチャ図

**パッケージ**: Tsuitachi-SF-Equipment (TSFE)
**更新日**: 2026-03-09

---

## コンポーネント依存関係

```mermaid
graph TD
    subgraph "VR Dial Control (DFUNC)"
        Dial[VR Dial Input]
        MethodCaller[DFUNC_MethodCaller]
    end

    subgraph "Engine Control System"
        EngineToggle[SFEXT_EngineToggle]
        AutoStarter[SFEXT_AutoStarter]
    end

    subgraph "Power & APU System"
        PowerBus[TSFE_PowerBus]
        APU[SFEXT_AuxiliaryPowerUnit]
        BleedAir[TSFE_BleedAirBus]
    end

    subgraph "Engine System"
        Engine1[SFEXT_AdvancedEngine #1]
        Engine2[SFEXT_AdvancedEngine #2]
    end

    Dial -->|Trigger| MethodCaller
    MethodCaller -->|Toggle| EngineToggle
    EngineToggle -->|StartSequence| AutoStarter

    AutoStarter -->|SetBatteryOn| PowerBus
    AutoStarter -->|StartAPU| APU
    AutoStarter -->|StartEngines| Engine1
    AutoStarter -->|StartEngines| Engine2

    APU -.->|apuRunningIndicator| PowerBus
    APU -.->|apuRunningIndicator| BleedAir
    Engine1 -.->|engineRunningIndicator| PowerBus
    Engine1 -.->|engineRunningIndicator| BleedAir
    Engine2 -.->|engineRunningIndicator| PowerBus
    Engine2 -.->|engineRunningIndicator| BleedAir

    PowerBus -.->|starterPowerSource| Engine1
    PowerBus -.->|starterPowerSource| Engine2
    BleedAir -.->|starterPowerSource| Engine1
    BleedAir -.->|starterPowerSource| Engine2

    style Dial fill:#e1f5ff
    style MethodCaller fill:#bbdefb
    style EngineToggle fill:#90caf9
    style AutoStarter fill:#64b5f6
    style PowerBus fill:#fff9c4
    style APU fill:#ffeb3b
    style BleedAir fill:#fdd835
    style Engine1 fill:#ffccbc
    style Engine2 fill:#ffccbc
```

**凡例**:
- 実線矢印 (→): 直接的なメソッド呼び出し
- 点線矢印 (-.->): GameObject参照による間接的な状態通知

---

## DFUNC_MethodCallerパターン（推奨）

### 従来のパターン（非推奨）

```
DFUNC_AutoStarter
  - VRダイヤル入力処理
  - APU起動処理
  - エンジン起動処理
  - 型安全でない（UdonSharpBehaviour[]）
  - INOP判定なし
  - Windmill始動未対応
```

### 新パターン（推奨）

```
VR Dial
  ↓ Trigger
DFUNC_MethodCaller (methodName="Toggle")
  ↓ Toggle()
SFEXT_EngineToggle
  ↓ StartSequence()
SFEXT_AutoStarter
  - 全機能対応（型安全、INOP判定、Windmill始動）
  - 保守コスト削減
```

### 設定例

```yaml
GameObject: "EngineStartDial"
Components:
  - DFUNC_MethodCaller:
      targetComponent: SFEXT_EngineToggle (参照)
      methodName: "Toggle"
  - 必須: EntityControl (SaccEntity参照)
  - 必須: LeftDial (true/false)
```

---

## 状態管理パターン

### APU State Management

```mermaid
stateDiagram-v2
    [*] --> Off
    Off --> Starting: run=true
    Starting --> Running: N >= targetN
    Running --> Stopping: run=false
    Stopping --> Off: N = 0
    Stopping --> Starting: run=true (再始動)
```

**プロパティ**:
- `State` (APUState enum): 読み取り専用
- `IsOff`, `IsStarting`, `IsRunning`, `IsStopping`: ヘルパー
- `CanStart`: 始動可能判定

### Engine State Management

```mermaid
stateDiagram-v2
    [*] --> Off
    Off --> Windmilling: AirSpeed > threshold
    Off --> Starting: starter=true
    Windmilling --> Starting: starter=true または fuel=true
    Starting --> Running: N2 >= minN2 && fuel=true
    Running --> Windmilling: fuel=false
    Windmilling --> Off: AirSpeed < threshold
    Running --> Seized: Fire/Overheat failure
    Seized --> [*]
```

**プロパティ**:
- `State` (EngineState enum): 読み取り専用
- `IsOff`, `IsWindmilling`, `IsStarting`, `IsRunning`, `IsSeized`: ヘルパー
- `CanStart`: 始動可能判定
- `IsInoperable`: INOP判定（fireHandlePulled || Seized）

---

## 電源システムアーキテクチャ

### PowerBus (電力バス)

```
電源ソース → PowerBus → 電力消費機器

電源ソース:
  - Battery (手動ON/OFF)
  - APU (apuRunningIndicator)
  - Engine (engineRunningIndicator[])
  - GPU (gpuObject)

出力:
  - batteryPoweredIndicator (Battery || BusPower)
  - busPoweredIndicator (BusPower)

用途:
  - APU始動（Battery必須）
  - エンジンスターター（PowerBusまたはBleedAir）
  - 計器、照明、フラップ油圧
```

### BleedAirBus (ブリード空気バス)

```
空気源 → BleedAirBus → スターター

空気源:
  - APU (apuRunningIndicator)
  - Engine (engineRunningIndicator[], クロスブリード)
  - ASU (asuObject, 地上空調車)

出力:
  - bleedAirIndicator (任意)

用途:
  - エンジン空気タービンスターター
  - 空調システム（将来の拡張）
```

### GameObject参照パターンの妥当性

UdonSharp制約により、以下の理由でGameObject参照パターンは実用的です：

**メリット**:
- 疎結合（実装の詳細を隠蔽）
- Prefab再利用性向上
- Inspector設定が容易（ドラッグ&ドロップ）
- UdonSync不要（ローカル処理のみ）

**制約**:
- インターフェース未サポート
- SendCustomEventは文字列ベース（型安全でない）
- 継承制限

**代替案の問題**:
- 直接参照 → 結合度が高まり、Prefab再利用性が低下
- イベント駆動 → SendCustomEventは型安全でない
- インターフェース → UdonSharpが未サポート

---

## Windmill始動シーケンス

```mermaid
sequenceDiagram
    participant AutoStarter
    participant Engine
    participant PowerBus

    Note over AutoStarter: 全エンジンチェック
    AutoStarter->>Engine: State == Windmilling?
    AutoStarter->>Engine: N2 >= minN2ForIgnition?

    alt 全エンジンWindmill始動可能
        Note over AutoStarter: APUスキップ
        AutoStarter->>Engine: fuel = true (starterなし)
        Note over Engine: 燃料のみで再始動
    else 一部でもWindmill不可
        AutoStarter->>PowerBus: SetBatteryOn()
        AutoStarter->>APU: StartAPU()
        Note over APU: APU始動完了待ち
        AutoStarter->>Engine: starter=true, fuel=true
        Note over Engine: 通常始動
    end
```

**条件**:
- `State == Windmilling`
- `N2 >= takeOffN2 * minN2ForIgnition` (通常15%)

**効果**:
- APU不要（燃料のみで再始動）
- 飛行中のエンジン再始動が高速化

---

## Editor UIアーキテクチャ

### TSFEEditorUtil（共通ユーティリティ）

```csharp
// 色定数定義
StateOnColor = Color.green
StateOffColor = Color.red
StateWarningColor = Color.yellow
StateTransitionColor = new Color(1f, 0.5f, 0f) // オレンジ

// ColorScope（自動復元）
using (new TSFEEditorUtil.ColorScope(color))
{
    EditorGUILayout.LabelField("Status", "ON");
} // 自動でGUI.color復元

// State表示ヘルパー
TSFEEditorUtil.DrawAPUStateLabel(apu.State);
TSFEEditorUtil.DrawEngineStateLabel(engine.State);
TSFEEditorUtil.DrawStateLabel("Battery", isOn);

// Foldout永続化
bool show = TSFEEditorUtil.DrawPersistentFoldout("TSFE.EngineEditor.ShowControls", "Engine Controls");
```

**メリット**:
- GUI.color復元忘れがない
- 色の意味が明確（定数名で自己文書化）
- State表示ロジックの重複解消
- Foldout状態の永続化（ユーザー体験向上）

---

## 実装済みリファクタリング

### Phase 1 (Runtime)

- ✅ DFUNC_AutoStarter.cs削除
- ✅ APU/EngineにState判定ヘルパープロパティ追加
  - `IsRunning`, `IsOff`, `CanStart`等
- ✅ Engine.IsInoperableプロパティ追加
  - INOP判定の一元化
- ✅ AutoStarter/EngineToggleでIsInoperable使用に移行

### Phase 1E (Editor)

- ✅ TSFEEditorUtil.cs作成
  - 色定数、ColorScope、State表示ヘルパー
- ✅ PowerBusEditorにFoldout永続化適用
- ✅ PowerBusEditorのGUI色設定を移行

---

## 今後の拡張予定

### Phase 2（条件付き実施）

- 命名統一（Obsolete移行で後方互換性維持）
- MockSAVControl完全化
- テストシナリオのスクリプト化

### 非推奨（UdonSharp制約により効果限定的）

- ❌ イベント駆動化
- ❌ PowerBus/BleedAirBus直接参照化
- ❌ 型安全性の完全化

---

**作成者**: Claude Code
**レビュー**: 必要に応じてTSFE開発チームでレビュー
**更新頻度**: 大きな機能追加時、または四半期ごと
