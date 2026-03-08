# TSFE Test Bench UI ガイド

EngineTestBench PrefabのInspector UIガイドです。Play Mode中に利用可能な包括的なテスト機能を説明します。

## MockSAVControl Inspector（メインコントロール）

MockSAVControl InspectorはPlay Mode中に最も重要なテストUIです。飛行環境全体を制御します。

### Test Scenarios (Presets) セクション

**場所**: Inspector上部、折りたたみ可能

**機能**: ワンクリックで飛行状態を設定

| ボタン | 設定値 | 用途 |
|--------|--------|------|
| **Ground (0m, 0kt)** | Alt=0m, Speed=0kt, Atm=1.0, Taxiing=true | 地上始動テスト |
| **Takeoff (0m, 150kt)** | Alt=0m, Speed=150kt, Atm=1.0, Taxiing=false | 離陸テスト |
| **Cruise FL100 (300kt)** | Alt=3048m, Speed=300kt, Atm=0.74 | 低高度巡航 |
| **Cruise FL300 (450kt)** | Alt=9144m, Speed=450kt, Atm=0.37 | 高高度巡航 |
| **Windmill (FL150, 350kt)** | Alt=4572m, Speed=350kt, Atm=0.64 | Windmill始動テスト |
| **APU Limit (FL200, 250kt)** | Alt=6096m, Speed=250kt, Atm=0.53 | APU高度制限テスト |

**使用例**:
```
1. Play Mode開始
2. MockSAVControl Inspector → "Windmill (FL150, 350kt)" ボタン
3. エンジンInspector → Fuel ON
4. Windmill始動成功を確認
```

### Quick Controls セクション

**場所**: Test Scenariosの下、折りたたみ可能

**Airspeed コントロール**:
- **スライダー**: 0-500 KIAS、リアルタイム表示（例: "280 KIAS"）
- **クイックボタン**: 0kt / 150kt / 250kt / 350kt
- **効果**: AirSpeedとAirVelが自動同期

**Altitude コントロール**:
- **スライダー**: 0-FL400（フィートで表示、例: "FL250"）
- **クイックボタン**: Ground / FL100 / FL200 / FL300
- **効果**: 高度に応じて **Atmosphere が自動調整**（Exp(-Alt/8400m)）

**Fuel コントロール**:
- **ボタン**: Empty / 25% / 50% / Full
- **効果**: Fuel値が即座に変更（例: Full → 10000kg）

### Current Status セクション

**場所**: Quick Controlsの下、リアルタイム更新

**表示項目**:
```
Airspeed: 144.3 m/s (280 KIAS)
Altitude: 7620 m (FL250)
Atmosphere: 0.43 (43%)
Fuel: 5000 / 10000 kg (50%)
Throttle Input: 0.50
Throttle Strength: 65204 N
Extra Drag: 0.087
Extra Lift: 0.000
Taxiing: NO
```

**用途**: テスト中の環境状態を一目で確認

---

## SFEXT_AdvancedEngine Inspector（エンジンコントロール）

個々のエンジンを詳細に制御・監視します。

### Engine Controls (Play Mode Only)

**場所**: Inspector上部、Play Mode中のみ表示

**ボタン**:
- **Starter: ON/OFF** (高さ30px) - スターター制御
- **Fuel: ON/OFF** (高さ30px) - 燃料バルブ制御
- **Reverser: ON/OFF** (高さ30px) - スラストリバーサー

**Fire Control**:
- **Fire Handle: NORMAL/PULLED** (赤色表示) - 火災ハンドル
- **Discharge Extinguisher** (Fire時のみ有効) - 消火器作動
- **Fire Alarm: UNMUTED/MUTED** (黄色表示) - 警報ミュート

### Engine State (Real-time)

**場所**: Engine Controlsの下、リアルタイム更新

**N1/N2 表示**:
```
N1 (Low Pressure Spool)
4586.0 RPM (100.0%)
[████████████████████] 100.0%

N2 (High Pressure Spool)
20171.0 RPM (100.0%)
[████████████████████] 100.0%
```
- プログレスバー付き
- 100%超過時は赤色表示

**Temperatures**:
```
EGT (Exhaust Gas Temp): 1038 °C
ECT (Engine Case Temp): 274 °C
```
- 警告色: 黄色（連続限界超過）、赤色（オーバーヒート）

**Status**:
```
Engine Running: YES (緑) / NO (灰色)
Fire: YES (赤) / NO (白)
```

**Starter System**:
```
Mode: Standalone (Self-Start)
または
Starter Power: AVAILABLE (緑) / NOT AVAILABLE (赤)
Power Source: BleedAirBus
Auto Cutoff: Enabled at 8155 RPM (95% of idle)
```

**Output**:
```
Thrust: 130408.5 N (6.86 m/s²)
```
- 機体質量から加速度換算表示

**Input**:
```
Throttle Input: 1.00 (100%)
```

### Response Time Calculator

**場所**: Engine Stateの下、折りたたみ可能

**機能**: 時間（秒）とResponseパラメータを相互変換

**表示例**:
```
N2 Startup (Starter → 25%)
時間 (秒): [12.0]
Response: 0.0050

N2 Response (25% → 50% Idle)
時間 (秒): [30.0]
Response: 0.0500
```

**使い方**:
1. 「時間 (秒)」を変更 → Responseが自動計算・即座に反映
2. Settingsで Responseを変更 → 時間が自動計算

---

## SFEXT_AuxiliaryPowerUnit Inspector

APU制御・監視用です。

### Play Mode Controls

**ボタン**:
- **Toggle APU** - APU始動/停止

**State表示**:
```
Started: YES / NO
Terminated: YES / NO
```

**Altitude Limit（Play Mode）**:
```
Current: 7620 m (FL250)
Max: 6096 m (FL200)
Status: EXCEEDED (赤) / OK (白)
```

---

## SFEXT_AdvancedEngineTest Inspector

複数エンジン一括制御用です。

### Play Mode Controls

**Throttle Control**:
```
Throttle Input: [0.00] ← リアルタイム表示
```
- キーボード: RightShift（増加）/ RightControl（減少）

**Reverser Control**:
```
Reversing: OFF ← ボタンで一括トグル
```
- キーボード: R

**個別操作の指示**:
```
Individual engine controls (Starter/Fuel):
Use each engine's Inspector to control individually
```

---

## UI Text デバッグ表示（オプション）

### Canvas + Text 作成（従来のUI Text使用時）

**手順**:
1. Hierarchy右クリック → UI → Canvas
2. Canvas右クリック → UI → Text (Legacy) または TextMeshPro
3. 名前: `DebugText`

**Rect Transform設定**:
- Anchor Presets: **Top Left**
- Pos X: `185`, Pos Y: `-180`
- Width: `350`, Height: `340`

**Text設定**:
- Font Size: `14`
- Color: `White`
- Alignment: `Left` + `Top`

**SFEXT_AdvancedEngineTest に設定**:
1. EngineTestController Inspector
2. Debug Text フィールドに DebugText をドラッグ

## 表示内容

Play Modeで以下が表示されます：

```
SFEXT_AdvancedEngine Test

Controls:
I: Starter [OFF]
F: Fuel [OFF]
RightShift/RightControl: Throttle [0.00]
R: Reverser [OFF]

Engine State:
N1: 0.0 RPM (0.0%)
N2: 0.0 RPM (0.0%)
EGT: 0 C
ECT: 0 C
Fire: NO
Engine On: NO
```

## UI不要の場合

Debug Text フィールドを空欄にすれば、Inspector だけで確認できます：

1. `Engine_Test` オブジェクトを選択
2. Inspector で `SFEXT_AdvancedEngine` コンポーネントの値を確認
   - N1, N2, EGT, ECT, fire, starter, fuel, reversing

3. `EngineTestController` オブジェクトを選択
4. Inspector で `SFEXT_AdvancedEngineTest` コンポーネントの値を確認
   - throttleInput, starter, fuel, reversing

5. `MockSAVControl` オブジェクトを選択
6. Inspector で値を確認
   - ThrottleStrength (推力)

## トラブルシューティング

### UI Textが表示されない

1. Canvas の Render Mode が `Screen Space - Overlay` か確認
2. Text の Color が White になっているか確認
3. Text オブジェクトが Canvas の子になっているか確認

### テキストが更新されない

1. `SFEXT_AdvancedEngineTest` の `debugText` フィールドが設定されているか確認
2. `engine` フィールドが設定されているか確認
3. Play Mode中に Inspector で `debugText.text` の値を確認

### UI が小さすぎる/大きすぎる

1. Canvas Scaler コンポーネント追加 (Canvas に)
2. UI Scale Mode: `Scale With Screen Size`
3. Reference Resolution: `1920 x 1080`
