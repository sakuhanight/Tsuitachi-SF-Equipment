# 電力・油圧システムのセットアップ手順

TSFE電力・油圧システムをSF-1などの機体にセットアップする手順です。

## 前提条件

- **SFEXT_AdvancedEngine実装済み**（N2パラメータが必要）
- DFUNC_AdvancedFlapsを使用する場合

## システム構成

```
DFUNC_AdvancedFlaps
    ↓ (powerSource)
TSFE_HydraulicBus
    ↓ (hydraulicPumps[])
TSFE_HydraulicPump (EngineDriven)
    ↓ (engineComponent)
SFEXT_AdvancedEngine (N2パラメータ)
```

または電動ポンプの場合:

```
DFUNC_AdvancedFlaps
    ↓ (powerSource)
TSFE_HydraulicBus
    ↓ (hydraulicPumps[])
TSFE_HydraulicPump (Electric)
    ↓ (powerBus)
TSFE_PowerBus
    ↓ (engineComponents[] / apuComponent / gpuObject)
SFEXT_AdvancedEngine / SFEXT_AuxiliaryPowerUnit / GPU GameObject
```

## Phase 1: 油圧システムの構築

### 1. エンジン駆動油圧ポンプ作成

SF-1は2基エンジンなので、左右に1つずつポンプを作成します。

**左エンジンポンプ:**
1. 新規GameObject作成: `TSFE_HydraulicPump_Left`
2. `TSFE_HydraulicPump` コンポーネント追加
3. 設定:
   - Pump Type: `EngineDriven`
   - Engine Component: `SFEXT_AdvancedEngine_Left`
   - Engine N2 Parameter Name: `N2`
   - Minimum N2: `50` (50%以上で動作)
   - Update Interval: `0.1`

**右エンジンポンプ:**
1. 新規GameObject作成: `TSFE_HydraulicPump_Right`
2. `TSFE_HydraulicPump` コンポーネント追加
3. 設定:
   - Pump Type: `EngineDriven`
   - Engine Component: `SFEXT_AdvancedEngine_Right`
   - Engine N2 Parameter Name: `N2`
   - Minimum N2: `50`

### 2. 油圧バス作成

1. 新規GameObject作成: `TSFE_HydraulicBus`
2. `TSFE_HydraulicBus` コンポーネント追加
3. 設定:
   - Hydraulic Pumps: Size = `2`
     - Element 0: `TSFE_HydraulicPump_Left`
     - Element 1: `TSFE_HydraulicPump_Right`
   - Minimum Running Pumps: `1` (1つ動作すればOK、冗長性)
   - Update Interval: `0.1`

## Phase 2: DFUNC_AdvancedFlaps接続

1. SF-1のFlapsオブジェクトを探す（または新規作成）
2. 既存のFlapsコンポーネント（あれば）を削除またはDisable
3. `DFUNC_AdvancedFlaps` コンポーネント追加
4. 設定:
   - **SAVControl**: SF-1のSaccAirVehicleコンポーネント
   - **Power Source**: `TSFE_HydraulicBus` (UdonSharpBehaviour)
   - **Power Source Legacy**: 空欄
   - **Detents**: `{0, 1, 2, 5, 10, 15, 25, 30, 40}` (Boeing 737風)
   - **Speed Limits**: `{340, 250, 250, 250, 210, 200, 190, 175, 162}` (KIAS)
   - **Response**: `1.0` (展開速度)

## Phase 3: EntityControlへの登録

1. SF-1の`SaccEntity`コンポーネントを開く
2. `Extensions` 配列に `DFUNC_AdvancedFlaps` を追加
3. `Dial Functions` 配列に `DFUNC_AdvancedFlaps` を追加

## Phase 4: アニメーター設定

1. SF-1の`VehicleAnimator`を開く
2. 以下のパラメータを追加:
   - `flaps` (Bool) - フラップ展開中
   - `flapsangle` (Float, 0-1) - 現在角度（正規化）
   - `flapstarget` (Float, 0-1) - 目標角度（正規化）
   - `flapsbroken` (Bool) - 翼破損状態

3. アニメーションステートでフラップメッシュを制御:
   ```
   flapsangle = 0.0 → フラップ格納
   flapsangle = 0.5 → 20°展開
   flapsangle = 1.0 → 40°展開（最大）
   ```

## Phase 5: 電力システム追加（オプション - 電動ポンプ用）

### 1. 電力バス作成

1. 新規GameObject作成: `TSFE_PowerBus`
2. `TSFE_PowerBus` コンポーネント追加
3. 設定:
   - APU Component: `SFEXT_AuxiliaryPowerUnit` (実装後)
   - APU Parameter Name: `Running`
   - Engine Components: Size = `2`
     - Element 0: `SFEXT_AdvancedEngine_Left`
     - Element 1: `SFEXT_AdvancedEngine_Right`
   - Engine Parameter Name: `EngineOn`
   - GPU Object: 地上電源用GameObject（任意）

### 2. 電動油圧ポンプ作成

1. 新規GameObject作成: `TSFE_HydraulicPump_Electric`
2. `TSFE_HydraulicPump` コンポーネント追加
3. 設定:
   - Pump Type: `Electric`
   - Power Bus: `TSFE_PowerBus`
   - Pump Switch: スイッチ用GameObject（activeで有効、任意）

4. `TSFE_HydraulicBus` の Hydraulic Pumps 配列に追加:
   - Size = `3`
   - Element 2: `TSFE_HydraulicPump_Electric`

## Phase 6: RAT（緊急用）追加（オプション）

1. 新規GameObject作成: `TSFE_HydraulicPump_RAT`
2. `TSFE_HydraulicPump` コンポーネント追加
3. 設定:
   - Pump Type: `RAT`
   - SAV Control: SaccAirVehicleコンポーネント
   - RAT Deployed Object: RAT展開状態GameObject
   - Minimum Speed: `50` (m/s、約100kt以上で動作)

4. `TSFE_HydraulicBus` の配列に追加

## テスト手順

### 1. 簡易テスト（油圧なしで動作確認）

1. `TSFE_HydraulicBus` の `Minimum Running Pumps` を `0` に設定
2. Play Modeでフラップ操作をテスト
3. 確認後、`1` に戻す

### 2. エンジン駆動テスト

1. Play Modeで機体に搭乗
2. エンジンスタート前: フラップが動かない
3. エンジン始動（N2 > 50%）: フラップが動く
4. 片方エンジン停止: 冗長性により動作継続
5. 両方停止: フラップ停止

### 3. 速度超過テスト

1. フラップを展開（例: Flaps 30°、速度制限175kt）
2. 速度を200ktまで加速
3. アクチュエータ故障または翼破損が発生
4. 破損時にアニメーションパラメータ `flapsbroken` が true になる

## トラブルシューティング

### フラップが動かない

1. `TSFE_HydraulicBus.Pressurized` が true か確認（Inspector）
2. `TSFE_HydraulicPump.Running` が true か確認
3. エンジンN2が50%以上か確認
4. powerSourceがnullでないか確認

### エンジン停止してもフラップが動く

1. `Minimum Running Pumps` が `0` になっていないか確認
2. エンジンコンポーネントへの参照が正しいか確認
3. N2パラメータ名が正しいか確認（`N2`）

### 速度超過で破損しない

1. Speed Limits配列が正しく設定されているか確認
2. `meanTimeBetweenActuatorBrokenOnOverspeed` / `meanTimeBetweenWingBrokenOnOverspeed` の値を下げてテスト（例: 10秒）

## 冗長性設計の例

**二重油圧系統:**
```
System A: Engine 1 Driven Pump + Electric Pump A
System B: Engine 2 Driven Pump + Electric Pump B
```

両系統を別々のHydraulicBusとして実装し、Flapsは両方を参照可能にする（DFUNC_AdvancedFlapsの拡張が必要）。

## 次のステップ

1. SFEXT_AdvancedEngineの実装
2. SFEXT_AuxiliaryPowerUnitの実装
3. 電力バスの活用（電動ポンプ、アビオニクス等）
4. RAT実装（緊急用油圧）

---

**注意:** 現在のSF-1は標準Saccエンジンを使用しているため、SFEXT_AdvancedEngineへの移行が必須です。
