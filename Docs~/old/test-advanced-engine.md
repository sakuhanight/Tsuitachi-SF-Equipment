# SFEXT_AdvancedEngine テスト手順

SFEXT_AdvancedEngineの動作確認手順です。EngineTestBench Prefabを使用した包括的なテスト環境を提供します。

## 前提条件

- Unity 2022.3+
- VRChat Worlds SDK 3.7.0+
- UdonSharp 1.x
- TSFE パッケージ

## クイックスタート（Prefab使用）

### 1. EngineTestBench Prefabを配置

1. `Packages/submodules/Tsuitachi-SF-Equipment/Sample/EngineTestBench.prefab` をシーンにドラッグ
2. Play Modeに入る
3. MockSAVControl InspectorでTest Scenariosボタンをクリック

**これだけで包括的なテストが可能です！**

### 2. テスト可能な項目

- ✅ 地上始動（Ground Start）
- ✅ Windmill始動（空中再始動）
- ✅ APU高度制限テスト
- ✅ エンジン状態遷移（Off/Windmilling/Starting/Running/Seized）
- ✅ 火災・故障シミュレーション
- ✅ 速度・高度・燃料システム

詳細なテストシナリオは `test-scenarios.md` を参照してください。

---

## 手動セットアップ（カスタム環境）

### 1. 空のシーン作成

1. Unity で新規シーンを作成: `SFEXT_AdvancedEngineTest`
2. VRChat Worlds SDK の設定を適用

### 2. テストオブジェクト構築

#### モック SAVControl 作成

1. 空の GameObject 作成: `MockSAVControl`
2. `MockSAVControl` コンポーネントを追加
3. 設定（デフォルト値）:
   - **推力・スロットル**:
     - Throttle Input: `0`
     - Throttle Strength: `0` (エンジンが自動設定)
   - **速度・高度**:
     - Air Speed: `0 m/s`
     - Altitude: `0 m`
     - Atmosphere: `1.0` (海面)
   - **燃料**:
     - Fuel: `5000 kg`
     - Full Fuel: `10000 kg`
   - **物理**:
     - Vehicle Rigidbody: (自動取得、または手動設定)
     - Extra Drag: `0`
     - Extra Lift: `0`
     - Taxiing: `✓`

#### エンジンオブジェクト作成

1. 空の GameObject 作成: `Engine_Test`
2. `SFEXT_AdvancedEngine` コンポーネントを追加
3. 基本設定:
   - **SAV Control**: `MockSAVControl`
   - Max Thrust: `130408.51` (デフォルト、CFM56-7B27相当)
   - Thrust Curve: `2.0`

4. N1/N2 パラメータ: デフォルト値でOK
   - Idle N1: `879.6`
   - Reference N1: `4397`
   - Take Off N1: `4586`
   - Idle N2: `8583.5`
   - Reference N2: `17167`
   - Take Off N2: `20171`

5. 温度パラメータ: デフォルト値でOK

6. コンポーネント:
   - Vehicle Animator: 後で追加（任意）
   - サウンド: 後で追加（任意）
   - エフェクト: 後で追加（任意）

#### テストコントローラー作成

1. 空の GameObject 作成: `EngineTestController`
2. `SFEXT_AdvancedEngineTest` コンポーネントを追加
3. 設定:
   - **Engine**: `Engine_Test` (SFEXT_AdvancedEngine)
   - Show Debug Info: `✓`
   - Debug UI Scale: `1.0`
   - キーバインド: デフォルトでOK

#### モック SaccEntity 作成

SFEXT_AdvancedEngine は `EntityControl` を必要とするため、ダミーを作成します。

1. `Engine_Test` に `SaccEntity` コンポーネントを追加（実際のSaccEntityがない場合は空のUdonBehaviourで代用）
2. SFEXT_AdvancedEngine の Inspector で:
   - Entity Control: `Engine_Test` (SaccEntity)

### 3. VRChat へアップロード（任意）

ローカルテストで十分ですが、VRChat でテストする場合:

1. VRChat SDK > Build & Test
2. Play Mode でテスト

## MockSAVControl Inspector (Play Mode)

Play Mode中、MockSAVControl Inspectorで以下の機能が使用可能です：

### Test Scenarios (Presets)

ワンクリックで飛行状態を設定：
- **Ground (0m, 0kt)** - 地上始動テスト
- **Takeoff (0m, 150kt)** - 離陸テスト
- **Cruise FL100 (300kt)** - 低高度巡航
- **Cruise FL300 (450kt)** - 高高度巡航
- **Windmill (FL150, 350kt)** - Windmill始動テスト
- **APU Limit (FL200, 250kt)** - APU高度制限テスト

### Quick Controls

スライダーで速度・高度・燃料を調整：
- **Airspeed**: 0-500 KIAS（ノット表示）
- **Altitude**: 0-FL400（高度に応じて大気密度自動調整）
- **Fuel**: Empty/25%/50%/Full

### Current Status

リアルタイムで以下を表示：
- 速度（m/s, KIAS）
- 高度（m, FL）
- 大気密度、燃料残量
- スロットル入力、推力
- 追加抗力・揚力（Windmilling時など）

---

## エンジンInspector (Play Mode)

SFEXT_AdvancedEngine Inspectorで以下の機能が使用可能です：

### Engine Controls

- **Starter ON/OFF** - スターター制御
- **Fuel ON/OFF** - 燃料制御
- **Reverser ON/OFF** - リバーサー制御
- **Fire Handle** - 火災ハンドル（PULLED時、エンジン停止）
- **Discharge Extinguisher** - 消火器作動

### Engine State (Real-time)

- **N1/N2** - RPMと%表示、プログレスバー
- **EGT/ECT** - 温度（°C）、警告色表示
- **Status** - EngineOn, Fire
- **Starter System** - 電源状態、自動カットオフ情報
- **Output** - 推力（N, m/s²）

### Response Time Calculator

時間とResponseパラメータを相互変換：
- N2 Startup (Starter → 25%)
- N2 Response (25% → Idle)
- N1 Response (Idle → Take Off)
- N1 Decrease (Take Off → Idle)

---

## 基本テスト項目

### テスト1: 地上始動シーケンス（N2軸スターター）

**目的**: N2 → N1 の順で正しく始動するか確認

**手順**:
1. Play Mode 開始
2. MockSAVControl Inspector → **"Ground (0m, 0kt)"** ボタン
3. Engine Inspector → **Starter ON**
   - EngineState: **Off → Starting**
   - N2 が 0 から上昇開始
   - N2 が 25% (starterTargetN2Ratio) で停止（燃料なし）
4. Engine Inspector → **Fuel ON**
   - N2 が idle まで上昇
   - N2 >= 15% (minN2ForIgnition) で EngineState: **Starting → Running**
   - N1 が idle まで上昇開始
5. Engine Inspector → **Starter OFF** (または自動カットオフ)
   - N2/N1 が idle で安定

**期待結果**:
- EngineState遷移: `Off → Starting → Running`
- N2 が先に上昇、15%到達で EngineOn=true
- N1 は EngineOn 後に追従
- EGT が idleEGT 付近に到達
- 自動スターターカットオフ機能が動作（N2 >= idleN2 * 0.95）

### テスト1-B: N1軸スターター（T-4型）

**前提**: Engine設定で `n1Start = true`

**手順**:
1. Play Mode 開始
2. MockSAVControl Inspector → **"Ground"** ボタン
3. Engine Inspector → **Starter ON**
   - EngineState: **Off → Starting**
   - N1 が上昇開始
   - N1 が 15% (starterTargetN1Ratio) で停止
   - N2 が N1 * startingN2toN1Ratio で追従
4. N1 >= 10% (autoIgnitionN1Threshold) で **自動燃料投入**
   - EngineState: **Starting → Running**
5. 自動スターターカットオフ

**期待結果**:
- N1が先に上昇、自動燃料投入が動作
- N2がN1に機械的に追従

### テスト2: スロットル操作

**目的**: N1 がスロットルに追従するか確認

**手順**:
1. エンジン始動済み状態から
2. `RightShift` キー長押し: スロットル増加
   - Throttle: 0.00 → 1.00 まで上昇
   - N1 が idle → Take Off まで追従
   - N2 も連動して上昇
   - EGT が 1038°C 付近まで上昇
3. `RightControl` キー長押し: スロットル減少
   - Throttle: 1.00 → 0.00 まで下降
   - N1/N2 が idle へ減少

**期待結果**:
- N1 が Throttle に応答（応答速度: n1Response = 0.1）
- N2 が N1 に追従（応答速度: n2Response = 0.05）
- EGT が N1 に連動
- ThrottleStrength が 0 → 130kN まで変化（Mock SAVControl ウィンドウで確認）

### テスト3: スラストリバーサー

**目的**: 逆噴射で推力が反転するか確認

**手順**:
1. エンジン始動、スロットル 50% 状態
2. Mock SAVControl の ThrottleStrength を確認（正の値）
3. `R` キー: Reversing ON
   - Reverser Position が 0 → 1 へ移行
   - ThrottleStrength が正 → 負へ反転
4. `R` キー: Reversing OFF
   - Reverser Position が 1 → 0 へ移行
   - ThrottleStrength が負 → 正へ復帰

**期待結果**:
- Reverser Position が reverserExtractResponse (0.5) で展開
- 推力が `-reverserRatio * 元の推力` に変化（デフォルト 50%）

### テスト4: エンジン停止

**目的**: 燃料カットで正しく停止するか確認

**手順**:
1. エンジン稼働中（スロットル任意）
2. `F` キー: Fuel OFF
   - N1/N2 が減少開始
   - EGT が減少開始
   - N2 が 50% 以下で「Engine On」が false
   - N1/N2 が 0 まで減少

**期待結果**:
- N1 が先に減少（n1DecreaseResponse = 0.08）
- N2 が遅れて減少（n2DecreaseResponse = 0.04）
- ThrottleStrength が 0 へ

### テスト5: 温度・故障モデル（任意）

**目的**: 過熱・火災モデルの動作確認（長時間テスト）

**手順**:
1. エンジン始動
2. スロットル 100% で連続運転
3. ECT を監視:
   - Continuous ECT (274°C) 到着: 火災確率開始（MTBF 2592000秒 = 30日）
   - Overheat ECT (343°C) 超過: 火災確率大幅増加（MTBF 90秒）
4. 火災発生時:
   - Fire が true
   - EGT が 1812°C へ急上昇
   - fireSound が再生（設定されていれば）
5. 火災発生後 90秒以内にメルトダウン確率
   - Fuel/Starter が強制 OFF
   - N1/N2 が停止

**期待結果**:
- 現実的な MTBF 値ではテストが困難（30日）
- テスト用に `mtbFireAtContinuous` を `10` などに変更して確認可能

**テスト用設定例**:
```
mtbFireAtContinuous: 10 (10秒で火災発生確率)
mtbFireAtOverheat: 5 (5秒で火災発生確率)
mtbMeltdownOnFire: 5 (火災後5秒でメルトダウン確率)
```

### テスト6: サウンド・エフェクト（任意）

**目的**: 音響・視覚エフェクトの確認

**手順**:
1. Engine_Test に AudioSource を追加:
   - `IdleSound`: ループ音源、Volume 0.5、Pitch 1.0
   - `InsideSound`: ループ音源、Volume 0.3、Pitch 1.0
   - `ThrustSound`: ループ音源、Volume 0.7、Pitch 1.0
   - `TakeoffSound`: ループ音源、Volume 0.4、Pitch 1.0

2. SFEXT_AdvancedEngine に AudioSource を設定

3. エンジン始動・スロットル操作
   - N2 上昇 → IdleSound の Volume/Pitch が上昇
   - N1 上昇 → ThrustSound の Volume/Pitch が上昇
   - N1 > 80% → TakeoffSound が再生開始

**期待結果**:
- 各音源が N1/N2 に連動して Volume/Pitch 変化
- UpdateSound() の式通り動作

### テスト7: プレイヤーハザード（VRChat内テスト推奨）

**目的**: 吸気・排気ハザードの動作確認

**手順**:
1. VRChat にアップロード
2. Engine_Test に Intake/Exhaust Transform を設定:
   - 新規 GameObject `IntakePoint` を作成、Engine_Test の子にする
   - 新規 GameObject `ExhaustPoint` を作成、Engine_Test の子にする（前方向き）
   - SFEXT_AdvancedEngine に設定
   - Intake Hazard Radius: `2`
   - Exhaust Hazard Radius: `3`
   - Exhaust Hazard Distance: `10`

3. エンジン始動後:
   - IntakePoint の半径 2m 以内に接近 → 吸い込まれる
   - ExhaustPoint の後方 10m, 半径 3m 以内に接近 → 吹き飛ばされる

**期待結果**:
- N1 に比例した力でプレイヤーが押される
- Engine On = false 時はハザード無効

## トラブルシューティング

### エンジンが始動しない

1. `EntityControl` が設定されているか確認
2. `SAVControl` が設定されているか確認
3. MockSAVControl の ThrottleInput が反映されているか確認

### N2 が上昇しない

1. Starter が ON になっているか確認
2. Fuel が ON になっているか確認
3. n2StartupResponse の値を確認（デフォルト 0.005）

### N1 が上昇しない

1. N2 が idle (50%) 以上か確認
2. ThrottleInput が増加しているか確認（RightShift キー）
3. n1Response の値を確認（デフォルト 0.1）

### ThrottleStrength が変化しない

1. SAVControl が正しく設定されているか確認
2. MockSAVControl に SFEXT_AdvancedEngine が GetProgramVariable でアクセスできているか確認

### デバッグUIが表示されない

1. SFEXT_AdvancedEngineTest の `showDebugInfo` が ON か確認
2. Engine 参照が設定されているか確認
3. EngineTestController が Ownership を持っているか確認

## 次のステップ

1. DFUNC_AdvancedThrustReverser 実装（VR/Desktop UI 連携）
2. DFUNC_AutoStarter 実装（自動始動シーケンス）
3. 実際の SaccFlightAndVehicles 機体への統合
4. サウンド・エフェクトの本格実装

---

**注意**: このテストは簡易的なものです。実際の機体統合では、SaccEntity/SaccAirVehicle の完全な環境が必要です。
