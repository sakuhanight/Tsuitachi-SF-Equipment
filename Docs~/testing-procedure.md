# TSFE統合テスト手順書

## 前提条件
- Unity 2022.3+、VRChat Worlds SDK 3.7.0+、SFV 1.8.0+がインストール済み
- テスト用機体にSaccAirVehicleコンポーネントがアタッチ済み
- VRヘッドセットと通常デスクトップ環境の両方でテスト可能

---

## Phase 1: 基礎システム（依存関係なし）

### 1.1 DFUNC_ElevatorTrim
**目的**: 昇降舵トリム制御の基本動作確認

1. 機体の子オブジェクトに空のGameObjectを作成 → `DFUNC_ElevatorTrim`
2. スクリプトアタッチ、パラメータ設定（`trimRate`, `maxTrim`）
3. `Dial_Funcon`用UIオブジェクト作成・割り当て
4. **テスト項目**:
   - VR: トリガー押下中にコントローラー前後移動でトリム変化
   - Desktop: キー入力でトリム変化
   - `PitchTrim`変数がSaccAirVehicleに正しく反映
   - ハプティクスフィードバック（VR）

### 1.2 DFUNC_AdvancedFlaps
**目的**: マルチdetentフラップと速度制限

1. 空のGameObject → `DFUNC_AdvancedFlaps`
2. `detents`配列設定（例: 0°, 10°, 20°, 30°）
3. `speedLimits`配列設定（例: 230, 200, 180, 160 KIAS）
4. **テスト項目**:
   - detent間の段階的移動
   - 速度超過時の警告音/ハプティクス
   - `overspeedDamageRate`による破損進行
   - 破損時の非対称展開（片側故障）

### 1.3 DFUNC_AdvancedSpeedBrake
**目的**: スピードブレーキの抗力制御

1. 空のGameObject → `DFUNC_AdvancedSpeedBrake`
2. `dragIncrement`, `maxDrag`設定
3. **テスト項目**:
   - 展開/収納で`ExtraDrag`が正しく加算/減算
   - アニメーション同期（`VehicleAnimator`の`speedbrake`パラメータ）
   - VR/Desktopの両操作モード

### 1.4 SFEXT_AuxiliaryPowerUnit (APU)
**目的**: 補助動力装置の基本動作

1. 機体の子オブジェクト → `SFEXT_AuxiliaryPowerUnit`
2. AudioSource、ParticleSystem設定
3. **テスト項目**:
   - `APU_Active`フラグの切り替え
   - 起動シーケンス（`startupTime`経過後に稼働）
   - 音響/パーティクルエフェクト
   - ネットワーク同期（手動sync）

### 1.5 GPWS（基本モード）
**目的**: 地形接近警報（標準gear/flapsとの連携）

1. 機体の子オブジェクト → `GPWS`
2. AudioSource複数設定（各警報音用）
3. **テスト項目**:
   - Mode 1: 高沈下率警報（>1500 fpm降下）
   - Mode 2: 地形接近警報（ラジオ高度<500 ft）
   - Mode 4: ギア未展開警報（<500 ft AGL、gear retracted）
   - Mode 5: フラップ未展開警報（<200 ft AGL、flaps retracted）
   - レイキャストによるラジオ高度計測

---

## Phase 2: エンジン・ギアシステム

### 2.1 SFEXT_AdvancedEngine
**目的**: 双軸ターボファンエンジンシミュレーション

1. エンジンごとに空のGameObject → `SFEXT_AdvancedEngine`
2. Execution Order: 1000に設定（Edit > Project Settings > Script Execution Order）
3. `intakeTransform`（吸気口）、`exhaustTransform`（排気口）設定
4. ParticleSystem（ジェットブラスト）設定
5. **テスト項目**:
   - 起動シーケンス: N2 → N1 → EGT推移
   - スロットル応答（`N1`, `N2`のspool up/down）
   - 逆噴射機構（`reverserDeployed`フラグ）
   - 火災/溶融モデリング（`EGT` > `meltdownTemp`）
   - プレイヤー吸引判定（`intakeRadius`）
   - 8変数の同期確認

### 2.2 DFUNC_AdvancedThrustReverser
**目的**: エンジン連携の逆噴射制御

1. 空のGameObject → `DFUNC_AdvancedThrustReverser`
2. `AdvancedEngines`配列に2.1のエンジン割り当て
3. **テスト項目**:
   - VRトリガー/Desktopキーで展開/収納
   - 地上のみ動作制限（`Taxiing`フラグ確認）
   - 各エンジンの`reverserDeployed`同期

### 2.3 SFEXT_AdvancedGear
**目的**: 着陸装置の展開/収納と地上判定

1. 機体の子オブジェクト → `SFEXT_AdvancedGear`
2. `gearColliders`配列に各車輪のCollider割り当て
3. **テスト項目**:
   - 展開/収納アニメーション（`VehicleAnimator.gear`）
   - `IsGearDown`フラグの状態管理
   - `gearColliders`の有効/無効切り替え
   - GPWS Mode 4との連携確認

### 2.4 DFUNC_AdvancedParkingBrake
**目的**: パーキングブレーキ（手動sync）

1. 空のGameObject → `DFUNC_AdvancedParkingBrake`
2. **テスト項目**:
   - ON時にリジッドボディ拘束（`isKinematic = true`）
   - ネットワーク同期（手動`RequestSerialization`）

### 2.5 DFUNC_AutoStarter
**目的**: APU→エンジン自動起動シーケンス

1. 空のGameObject → `DFUNC_AutoStarter`
2. Execution Order: 1000
3. `APU`、`Engines`配列割り当て
4. **テスト項目**:
   - ダイヤル選択でAPU起動 → 各エンジン順次起動
   - 完了後の自動ダイヤル解除
   - 中断機能（再度ダイヤル回転）

### 2.6 SFEXT_EngineFanDriver
**目的**: エンジンN1値に基づくファン回転

1. エンジンファンメッシュの親 → `SFEXT_EngineFanDriver`
2. `Engine`に2.1のAdvancedEngine割り当て
3. **テスト項目**:
   - N1値に応じた回転速度（`maxRPM`まで）
   - 視覚的フィードバック確認

### 2.7 SFEXT_InstrumentsAnimationDriver
**目的**: 10種アナログ計器の駆動

1. コックピット計器パネル → `SFEXT_InstrumentsAnimationDriver`
2. Animator設定（ADI、ASI、Altimeter等のパラメータ）
3. **テスト項目**:
   - 各計器の正確な指示値（対気速度、高度、姿勢角など）
   - 電源モード切り替え（vacuum/electric/pitot）
   - 電源喪失時のフラグ動作

---

## Phase 3: 高度なアビオニクス

### 3.1 AuralWarnings
**目的**: 音響警報統合システム

1. 機体の子オブジェクト → `AuralWarnings`
2. AudioSource配列（各警報音）設定
3. `AdvancedFlaps`（オプション）割り当て
4. **テスト項目**:
   - 失速警報（低速 + 高迎角）
   - オーバースピード警報
   - ギア警報（着陸形態未完了）
   - 高度コールアウト（500/100/50/10 ft）

### 3.2 GPWS（全モード統合テスト）
**前提**: Phase 2のAdvancedGear/AdvancedFlapsと連携

1. `gearExtension`に2.3、`flapsExtension`に1.2を割り当て
2. **追加テスト項目**:
   - Mode 3: 着陸後の高度喪失警報
   - Mode 6: 低高度での異常降下率
   - Advanced gear/flaps状態の正確な検出

### 3.3 ユーティリティスクリプト
**個別テスト**:

- **SFEXT_OutsideOnly**: 機外でのみ有効化
- **SFEXT_PassengerOnly**: 乗客時のみ有効化
- **SFEXT_SeatsOnly**: 着座時のみ有効化
- **SFEXT_BoardingCollider**: 搭乗時のコライダー制御

---

## Phase 4: 専門システム

### 4.1 DFUNC_AdvancedWaterRudder
**水上機専用**: 水上方向舵制御

### 4.2 SFEXT_WakeTurbulence
**物理効果**: 後方乱気流生成

### 4.3 SFEXT_DihedralEffect
**物理効果**: 上反角効果シミュレーション

### 4.4 PickupChock
**地上装備**: VRC Pickup連携の車輪止め

---

## 統合テストシナリオ

### シナリオ1: 冷機起動→離陸
1. APU起動（SFEXT_AuxiliaryPowerUnit）
2. 自動エンジン起動（DFUNC_AutoStarter）
3. フラップ離陸位置（DFUNC_AdvancedFlaps）
4. パーキングブレーキ解除（DFUNC_AdvancedParkingBrake）
5. 滑走→離陸（GPWS Mode 3監視）

### シナリオ2: 着陸アプローチ
1. 高度500 ft通過（AuralWarnings高度コール）
2. ギア展開（SFEXT_AdvancedGear → GPWS Mode 4解除）
3. フラップ着陸位置（速度制限遵守）
4. 接地→逆噴射（DFUNC_AdvancedThrustReverser）

### シナリオ3: 緊急事態
1. エンジン火災（SFEXT_AdvancedEngine.isOnFire）
2. GPWS Mode 2警報（低高度地形接近）
3. フラップ速度超過破損（DFUNC_AdvancedFlaps）

---

## デバッグツール

### Udon Log確認
```csharp
Debug.Log($"[TSFE] N1={N1:F1}% EGT={EGT:F0}°C");
```

### VRChat Client Debug Menu
- Udon Manager → 各コンポーネントの変数値監視
- Performance Stats → フレームレート影響確認

### ネットワーク同期テスト
- 複数クライアント接続
- Owner権限移譲時の状態維持確認

---

## チェックリスト

**Phase 1完了条件**:
- [ ] 全DFUNCがVR/Desktopで操作可能
- [ ] GPWS基本警報が正常動作
- [ ] APU起動/停止が同期

**Phase 2完了条件**:
- [ ] エンジン起動シーケンス完全動作
- [ ] ギア展開/収納が物理的に機能
- [ ] 自動起動シーケンスがエラーなし

**Phase 3完了条件**:
- [ ] 全音響警報が適切なタイミングで発動
- [ ] 計器が正確な値を表示

**Phase 4完了条件**:
- [ ] 専門システムが該当環境で動作
