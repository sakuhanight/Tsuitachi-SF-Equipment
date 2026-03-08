# TSFE Test Scenarios - 包括的テストシナリオ集

EngineTestBench Prefabを使用した包括的なテストシナリオです。実際の運用状況を再現し、SFEXT_AdvancedEngine、SFEXT_AuxiliaryPowerUnit、SFEXT_AutoStarter等の統合動作を確認します。

## 前提条件

- `EngineTestBench.prefab` がシーンに配置済み
- Play Mode で実行
- MockSAVControl Inspector で環境を操作

---

## シナリオ1: 地上始動（Normal Ground Start）

**目的**: 標準的な地上始動手順を確認

### 手順

1. **環境設定**
   - MockSAVControl Inspector → **"Ground (0m, 0kt)"** ボタン
   - 確認: Altitude=0m, AirSpeed=0kt, Atmosphere=1.0, Taxiing=true

2. **APU始動**
   - APU Inspector → **Toggle APU** ボタン（またはキー `A`）
   - 確認: APU Started=true

3. **エンジン始動（Engine 1）**
   - Engine 1 Inspector → **Starter ON**
   - 確認: EngineState: Off → Starting, N2上昇開始
   - Engine 1 Inspector → **Fuel ON**
   - 確認: N2 >= 15% で EngineState: Starting → Running
   - 確認: N1上昇開始、EGT上昇
   - 確認: 自動スターターカットオフ動作（N2 >= 95% idle）

4. **エンジン始動（Engine 2）**
   - Engine 2で同様の手順を実行

5. **APU停止**
   - APU Inspector → **Toggle APU**
   - 確認: APU Terminated=true

### 期待結果

- ✅ 両エンジンが正常に始動（EngineState=Running）
- ✅ N1/N2がidle値で安定
- ✅ EGT/ECTが正常範囲
- ✅ APUが正常停止

---

## シナリオ2: 自動始動シーケンス（AutoStarter）

**目的**: SFEXT_AutoStarterによる自動始動を確認

### 手順

1. **環境設定**
   - MockSAVControl Inspector → **"Ground"** ボタン

2. **自動始動開始**
   - AutoStarter Inspector → **Start Sequence** ボタン
   - 確認: シーケンス進行
     1. バッテリーON
     2. APU起動 → 起動完了待ち
     3. Engine 1起動 → Engine 2起動（順次）
     4. 全エンジン起動完了待ち
     5. APU停止
     6. 完了

3. **状態監視**
   - AutoStarter Inspectorで `state` と `statusMessage` を確認
   - 各エンジンのEngineStateを確認

### 期待結果

- ✅ 完全自動でバッテリー → APU → 両エンジン → APU停止
- ✅ EngineState遷移が正しい
- ✅ 最終state=Completed

### 中断テスト

1. シーケンス実行中に **Abort Sequence** ボタン
2. 確認: APU停止、全エンジンカット、state=Failed

---

## シナリオ3: Windmill始動（Air Restart）

**目的**: 空中エンジン停止後、Windmilling状態からの再始動を確認

### 手順

1. **巡航状態設定**
   - MockSAVControl Inspector → **"Windmill (FL150, 350kt)"** ボタン
   - 確認: Altitude=4572m (15,000ft), AirSpeed=350kt (180m/s), Atmosphere=0.64
   - 確認: IAS = TAS × √atmosphere = 350kt × √0.64 ≈ 280 KIAS（Windmill始動可能速度）

2. **エンジン停止（Engine 1）**
   - Engine 1 Inspector → **Fuel OFF**
   - 確認: EngineState: Running → Windmilling
   - 確認: N1が速度に応じて低下（windmillingN1Ratio * AirSpeed比）
   - 確認: N2がN1に機械的に追従（N1 * windmillingN2toN1Ratio）
   - 確認: Extra Drag増加（Windmilling抗力）

3. **Windmill始動**
   - Engine 1 Inspector → **Fuel ON** （**スターター不要**）
   - 確認: N2 >= 15% (minN2ForIgnition) なら即座に EngineState: Windmilling → Running
   - 確認: N1/N2が正常値まで上昇

4. **低速でのWindmill停止テスト**
   - MockSAVControl Inspector → Airspeed スライダーで **150 kt (77 m/s) 以下**に減速
   - 確認: EngineState: Windmilling → Off（minimumWindmillingSpeed以下）

### 期待結果

- ✅ 高速時: Fuel ONのみで再始動成功（スターター不要）
- ✅ N2 >= 15%がWindmill始動の条件
- ✅ 低速時: Windmillingが自動停止
- ✅ Extra Drag が速度²に比例

---

## シナリオ4: APU高度制限テスト

**目的**: APUの最大作動高度（FL200）制限を確認

### 手順

1. **地上でAPU起動**
   - MockSAVControl Inspector → **"Ground"** ボタン
   - APU Inspector → **Toggle APU**
   - 確認: APU Started=true

2. **上昇テスト**
   - MockSAVControl Inspector → Altitude スライダーで徐々に上昇
   - 確認: FL200 (6096m) まではAPU稼働継続
   - 確認: FL200超過で **APU自動停止**
   - Console確認: "Altitude xxxm exceeded limit 6096m - auto shutdown"

3. **高高度始動拒否テスト**
   - MockSAVControl Inspector → **"Cruise FL300"** ボタン（9144m）
   - APU Inspector → **Toggle APU** を試行
   - 確認: APU始動拒否
   - Console確認: "Cannot start: Altitude xxxm exceeds limit 6096m"

### 期待結果

- ✅ FL200以下: APU正常稼働
- ✅ FL200超過: APU自動停止
- ✅ 高高度: APU始動拒否

---

## シナリオ5: スターター電源不足テスト

**目的**: APU/バッテリー停止時、エンジン始動不可を確認

### 手順

1. **環境設定**
   - MockSAVControl Inspector → **"Ground"** ボタン

2. **電源なし始動テスト**
   - PowerBus Inspector → Battery OFF（または APU停止状態）
   - Engine Inspector → **Starter ON** を試行
   - 確認: N2上昇せず（StarterPowerAvailable=false）

3. **APU起動後に始動テスト**
   - APU Inspector → **Toggle APU**
   - 確認: BleedAirBus → Pressurized=true
   - Engine Inspector → **Starter ON**
   - 確認: N2上昇開始（StarterPowerAvailable=true）

### 期待結果

- ✅ 電源なし: スターター動作せず
- ✅ APU起動後: スターター動作

---

## シナリオ6: エンジン火災・消火シミュレーション

**目的**: 火災発生、消火器、火災ハンドル動作を確認

### 手順

1. **環境設定**
   - MockSAVControl Inspector → **"Ground"** ボタン
   - Engine 1を始動、アイドル安定

2. **手動火災発生（Inspector直接操作）**
   - Engine 1 Inspector → Settingsセクション展開
   - `fire` フラグを手動でtrueに変更（デバッグ用）
   - 確認: Fire表示が赤色、EGT急上昇（fireEGT=1812°C）

3. **火災警報ミュート**
   - Engine Inspector → **Fire Alarm: UNMUTED** ボタン
   - 確認: ボタン表示が **Fire Alarm: MUTED** に変化

4. **火災ハンドル操作**
   - Engine Inspector → **Fire Handle: NORMAL** ボタン
   - 確認: ボタン表示が **Fire Handle: PULLED** (赤色)
   - 確認: Fuel/Starter強制OFF（fireHandleCutsFuel=true時）
   - 確認: エンジン停止

5. **消火器作動**
   - Engine Inspector → **Discharge Extinguisher** ボタン
   - 確認: Fire=false、EGT減少開始

### 期待結果

- ✅ 火災時: EGT急上昇、Fire表示
- ✅ 火災ハンドル: エンジン強制停止
- ✅ 消火器: Fire=false

---

## シナリオ7: INOP（使用不可）エンジンのスキップ

**目的**: 火災ハンドルが引かれたエンジンをAutoStarterがスキップすることを確認

### 手順

1. **環境設定**
   - MockSAVControl Inspector → **"Ground"** ボタン

2. **Engine 1を INOP 状態に**
   - Engine 1 Inspector → **Fire Handle: PULLED**
   - 確認: fireHandlePulled=true

3. **AutoStarter実行**
   - AutoStarter Inspector → **Start Sequence**
   - 確認: シーケンス進行中、Engine 1がスキップされる
   - Console確認: "Engine 0 is INOP (fire handle pulled) - skipping"
   - 確認: Engine 2のみ始動

4. **完了確認**
   - 確認: state=Completed
   - 確認: statusMessage="1/1 operable running"（Engine 2のみ稼働）

### 期待結果

- ✅ INOPエンジンは自動的にスキップ
- ✅ 稼働可能エンジンのみ起動
- ✅ シーケンス正常完了

---

## シナリオ8: 燃料消費シミュレーション

**目的**: 燃料システム動作確認（MockSAVControl拡張機能）

### 手順

1. **環境設定**
   - MockSAVControl Inspector → **"Ground"** ボタン
   - Quick Controls → **Fuel: 25%** ボタン
   - 確認: Fuel=2500kg, FullFuel=10000kg

2. **燃料切れテスト**
   - 両エンジン始動、スロットル50%
   - MockSAVControl Inspector → Fuel を手動で `0` に設定
   - 確認: エンジン停止（実際のSFVではfuelカットが自動発生）

3. **燃料復旧**
   - Quick Controls → **Fuel: Full** ボタン
   - エンジン再始動テスト

### 期待結果

- ✅ Fuel値がリアルタイム表示
- ✅ 燃料切れで動作停止（実機では自動カット）
- ✅ 燃料復旧で再始動可能

---

## シナリオ9: 高出力連続運転制限（Continuous Power Limit）

**目的**: 高出力連続運転による故障確率上昇を確認

**注意**: このテストは実際の制限時間（300秒 = 5分）で実施します。短時間テストは行いません。

### 手順

1. **設定確認**
   - Engine Inspector → Settings → Faults
   - `enableContinuousPowerLimit = true` を確認
   - `continuousPowerTimeLimit = 300`（5分間）を確認
   - `continuousPowerThreshold = 0.9`（90%以上の推力）を確認

2. **高出力運転開始**
   - MockSAVControl Inspector → "Takeoff (0m, 150kt)" ボタン
   - Engine始動、スロットル 100%
   - 確認: 推力比率 >= 90%（takeoffまたはafterburner領域）
   - タイマー開始（外部ストップウォッチ推奨）

3. **5分間の連続運転**
   - スロットル 100% を維持
   - Engine Inspector で以下を監視:
     - N1: takeOffN1付近で安定
     - EGT: takeOffEGT付近で安定
     - ECT: 継続的に上昇
   - 確認: continuousPowerTimeが増加（Inspector では直接見えないが内部で蓄積）

4. **300秒（5分）経過後**
   - 確認: 火災確率発生開始（MTBF = 2592000秒 = 30日）
   - 注: 実際の火災発生は確率的で、30日の平均故障間隔のため、5分では発生しない可能性が高い
   - 確認: ECT継続上昇、overheatECT (343°C) 超過で火災確率大幅増加

5. **出力低下でリセット確認**
   - スロットル 50% に減少（推力比率 < 90%）
   - 確認: continuousPowerTime がリセット（次回の5分カウントは0から）
   - 確認: ECT減少開始

### 期待結果

- ✅ 高出力連続でcontinuousPowerTime蓄積（5分間）
- ✅ 制限時間超過で故障確率開始（実際の火災は確率的）
- ✅ 出力低下でタイマーリセット
- ✅ ECTの継続上昇・減少が正しく動作

### 補足: 火災発生の確率モデル

**現実的なMTBF値**:
- `mtbFireAtContinuousPower = 2592000` (30日)
- `mtbFireAtOverheat = 90` (ECT > overheatECT時)

5分間の高出力運転では火災発生確率は極めて低いため、以下のケースでテストします：

**高温運転による火災テスト**:
1. 5分間の連続高出力運転でECTを上昇させる
2. ECT > continuousECT (274°C) で `mtbFireAtContinuous = 2592000` の確率開始
3. ECT > overheatECT (343°C) で `mtbFireAtOverheat = 90` の確率に急増
4. 90秒程度でoverheat領域での火災発生確率が上昇

**このシナリオの目的**:
- 高出力連続運転制限の「タイマー機能」を確認
- 出力低下でのリセット機能を確認
- 実際の火災発生はシナリオ6で確認済み

---

## シナリオ10: 多重エンジンスロットル制御

**目的**: SFEXT_AdvancedEngineTest による複数エンジン一括制御を確認

### 手順

1. **環境設定**
   - MockSAVControl Inspector → **"Takeoff (0m, 150kt)"** ボタン

2. **両エンジン始動**
   - 手動またはAutoStarter使用

3. **スロットル一括操作**
   - EngineTestController Inspector使用（またはキーボード操作）
   - RightShift長押し: スロットル 0 → 100%
   - 確認: 両エンジンのN1が同期して上昇
   - 確認: ThrottleStrength = Engine1推力 + Engine2推力

4. **リバーサー一括操作**
   - `R`キー: Reversing ON
   - 確認: 両エンジンのreversingがtrue
   - 確認: ThrottleStrengthが負値（逆推力）

### 期待結果

- ✅ 複数エンジンが同期制御
- ✅ 推力が合算される
- ✅ リバーサーが一括動作

---

## トラブルシューティング

### EngineStateが遷移しない

- **Off → Starting にならない**: starterPowerSource確認、StarterPowerAvailable確認
- **Starting → Running にならない**: N2 >= 15%確認、fuel=true確認
- **Windmilling → Running にならない**: AirSpeed >= 128m/s確認、N2 >= 15%確認

### APUが始動しない

- **高度確認**: FL200以下か確認
- **電源確認**: powerSource (Battery/GPU) がactiveか確認

### AutoStarterが進まない

- **APU確認**: APUが正しく参照されているか
- **Engine確認**: enginesリストが正しく設定されているか
- **INOP確認**: 火災ハンドルが引かれていないか

### Windmilling抗力が発生しない

- **EngineState確認**: Windmilling状態か
- **速度確認**: AirSpeed > 0か
- **SAVControl確認**: MockSAVControl.ExtraDrag を確認

---

## 次のステップ

これらのシナリオを実行後、以下を試してください：

1. **実機パラメータ調整**: 737-800, A320, T-4等の実機データでパラメータ調整
2. **サウンド・エフェクト追加**: AudioSource, ParticleSystem設定
3. **実際のSaccFlightAndVehicles統合**: 実機体への組み込み
4. **VRChat内テスト**: マルチプレイヤー環境での同期確認

---

**注**: これらは開発テスト用シナリオです。実際の運用では適切なパラメータ調整とVRChat内テストが必要です。
