# エンジンテストベンチ セットアップガイド

各種エンジンの検証用テストベンチ構成です。1基ずつテストし、調整後に実機に搭載します。

## 推奨構成

```
EngineTestBench (親GameObject)
├── MockSAVControl (テスト用SAV)
│   └── MockSAVControl component
├── PowerBus (電力バス)
│   └── TSFE_PowerBus component
├── BleedAirBus (ブリード空気バス)
│   └── TSFE_BleedAirBus component
├── EngineTestController (エンジンテストコントローラー)
│   └── SFEXT_AdvancedEngineTest component
├── APUTestController (APUテストコントローラー)
│   └── SFEXT_AuxiliaryPowerUnitTest component
├── Engine_Test (テスト対象エンジン 1基)
│   └── SFEXT_AdvancedEngine component
└── APU_Test (テスト対象APU)
    └── SFEXT_AuxiliaryPowerUnit component
```

**ワークフロー**:
1. 各コンポーネントを個別にテスト・調整
2. パラメータが確定したらPrefab化
3. 実機に必要な数だけ複製して搭載

## 詳細セットアップ

### 1. EngineTestBench 作成

1. 空の GameObject を作成: `EngineTestBench`
2. Position: (0, 0, 0)

### 2. MockSAVControl 作成

1. `EngineTestBench` の子として空の GameObject 作成: `MockSAVControl`
2. `MockSAVControl` コンポーネント追加
3. 設定:
   - Throttle Input: `0`
   - Throttle Strength: `0` (自動更新)
   - Air Speed: `0`
   - Vehicle Animator: なし (任意)
   - Controls Root: `MockSAVControl` 自身

### 3. PowerBus 作成

1. `EngineTestBench` の子として空の GameObject 作成: `PowerBus`
2. `TSFE_PowerBus` コンポーネント追加
3. 設定:
   - APU Component: **後でAPUを選択**
   - APU Parameter Name: `started`
   - Engine Components: **後でエンジンを選択** (配列、サイズ1)
   - Engine Parameter Name: `EngineOn`
   - GPU Object: なし (任意、地上電源車)
   - Update Interval: 0.1秒

### 3b. BleedAirBus 作成

1. `EngineTestBench` の子として空の GameObject 作成: `BleedAirBus`
2. `TSFE_BleedAirBus` コンポーネント追加
3. 設定:
   - APU Component: **後でAPUを選択**
   - APU Parameter Name: `started`
   - Engine Components: **後でエンジンを選択** (配列、サイズ1)
   - Engine Parameter Name: `EngineOn`
   - ASU Object: なし (任意、地上空調車)
   - Update Interval: 0.1秒

### 3c. EngineTestController 作成

1. `EngineTestBench` の子として空の GameObject 作成: `EngineTestController`
2. `SFEXT_AdvancedEngineTest` コンポーネント追加
3. 設定:
   - Engine: **後でエンジンを選択**
   - Debug Text: なし (任意、UI Text使用時のみ)
   - キーバインド: デフォルトでOK

### 3d. APUTestController 作成

1. `EngineTestBench` の子として空の GameObject 作成: `APUTestController`
2. `SFEXT_AuxiliaryPowerUnitTest` コンポーネント追加
3. 設定:
   - APU: **後でAPUを選択**
   - Debug Text: なし (任意、UI Text使用時のみ)
   - キーバインド: デフォルト (A: Toggle APU)

### 4. エンジン作成 (例: CFM56)

1. `EngineTestBench` の子として空の GameObject 作成: `Engine_Test`
2. `SFEXT_AdvancedEngine` コンポーネント追加
3. 設定:
   - **SAV Control**: `MockSAVControl`
   - **Entity Control**: 空欄 (テスト時は不要)
   - 動力系: デフォルト (CFM56-7B27相当)
   - N1/N2 RPM: デフォルト
   - 温度: デフォルト
   - 逆噴射: デフォルト
   - 故障 MTBF: デフォルト
   - **始動システム**:
     - Electric Starter: ON (電動スターター)
     - Standalone Start: ON (テスト用、単独始動モード)
     - Power Bus: 空欄 (Standalone Start有効時は不要)
     - Bleed Air Bus: 空欄
   - コンポーネント:
     - Vehicle Animator: なし (任意)
     - サウンド: なし (任意)
     - エフェクト: なし (任意)

4. EngineTestController の設定に戻る:
   - **Engine**: `Engine_Test`

**注意**: Standalone Start を ON にすると、PowerBus/BleedAirBus無しでエンジン始動可能（テスト用）。統合テスト時は OFF にしてください。

### 4b. APU作成

1. `EngineTestBench` の子として空の GameObject 作成: `APU_Test`
2. `SFEXT_AuxiliaryPowerUnit` コンポーネント追加
3. 設定:
   - APU Audio Source: なし (任意)
   - APU Start/Loop/Stop: なし (任意、AudioClip)
   - Cross Fade Duration: 3.0秒
   - Default APU Start Duration: 30秒
   - Default APU Stop Duration: 10秒
   - Exhaust Effect: なし (任意、ParticleSystem)

4. APUTestController の設定に戻る:
   - **APU**: `APU_Test`

### 5. UI デバッグ表示（任意）

1. Hierarchy 右クリック → UI → Canvas
2. Canvas 右クリック → UI → Text (Legacy)
3. Text 設定:
   - Anchor: Top Left
   - Position: (185, -180)
   - Size: (350, 340)
   - Font Size: 14
4. `EngineTestController` と `APUTestController` の `Debug Text` に設定

## テスト手順

### エンジン単体テスト

1. Play Mode 開始
2. `Engine_Test` または `EngineTestController` を選択
3. Inspector で操作:
   - **Starter ON** → N2 が 25% まで上昇 (約10秒)
   - **Fuel ON** → N2 が 60% (Idle) まで上昇 (約20秒)
   - **Throttle 100%** → N1/N2 が 100% まで上昇 (約5秒)
   - **Reverser ON** → 推力が負に反転 (-50%)
4. パラメータ調整:
   - Response Time Calculator で時間調整
   - Settings で詳細パラメータ調整

### APU単体テスト

1. Play Mode 開始
2. `APU_Test` または `APUTestController` を選択
3. Inspector で操作:
   - **Toggle APU** または **Start APU** → APU起動開始 (約30秒)
   - **Started** が YES になるまで待つ
   - **Stop APU** → APU停止開始 (約10秒)
   - **Terminated** が YES になるまで待つ

### APUとエンジンの統合テスト (電動スターター)

**事前準備**:
1. `Engine_Test` の **Standalone Start を OFF** に設定
2. `Engine_Test` の **Power Bus** に `PowerBus` を設定
3. `PowerBus` の **APU Component** に `APU_Test` を設定
4. `PowerBus` の **Engine Components** (配列サイズ1) に `Engine_Test` を設定

**テスト手順**:
1. Play Mode 開始
2. `Engine_Test` を選択 → **Starter System** セクション確認:
   - Starter Type: `Electric`
   - Electric Power: `NOT AVAILABLE` (赤)
3. APU起動:
   - `APU_Test` を選択 → **Start APU**
   - **Started** が YES になるまで待つ (約30秒)
4. `Engine_Test` を選択 → Electric Power が `AVAILABLE` (緑) になることを確認
5. エンジン始動 (APU電源使用):
   - **Starter ON** → N2 が 25% まで上昇 (約10秒)
   - **Fuel ON** → エンジンがIdle (60% N2) に到達 (約20秒)
6. APU停止:
   - `APU_Test` を選択 → **Stop APU**
   - エンジンは継続運転（自己発電）
   - `Engine_Test` の Electric Power が `AVAILABLE` のまま (エンジンからの電力)
7. エンジン出力テスト:
   - **Throttle 100%** → フル推力確認 (N1/N2 100%)

### APUとエンジンの統合テスト (空気タービン式スターター)

**事前準備**:
1. `Engine_Test` の **Electric Starter を OFF** に設定
2. `Engine_Test` の **Standalone Start を OFF** に設定
3. `Engine_Test` の **Bleed Air Bus** に `BleedAirBus` を設定
4. `BleedAirBus` の **APU Component** に `APU_Test` を設定
5. `BleedAirBus` の **Engine Components** (配列サイズ1) に `Engine_Test` を設定

**テスト手順**:
1. Play Mode 開始
2. `Engine_Test` を選択 → **Starter System** セクション確認:
   - Starter Type: `Air Turbine`
   - Bleed Air: `NOT AVAILABLE` (赤)
3. APU起動:
   - `APU_Test` を選択 → **Start APU**
   - **Started** が YES になるまで待つ
4. `Engine_Test` を選択 → Bleed Air が `AVAILABLE` (緑) になることを確認
5. エンジン始動 (APUブリード空気使用):
   - **Starter ON** → N2 が 25% まで上昇
   - **Fuel ON** → エンジンがIdle (60% N2) に到達
6. APU停止:
   - `APU_Test` を選択 → **Stop APU**
   - エンジンは継続運転
   - `Engine_Test` の Bleed Air が `AVAILABLE` のまま (エンジンからのブリード)
7. エンジン出力テスト:
   - **Throttle 100%** → フル推力確認

### 別エンジンのテスト

1. Play Mode 停止
2. `Engine_Test` のパラメータを変更:
   - 例: GE90 用に maxThrust, N1/N2 値を変更
3. Play Mode 再開してテスト
4. 確定したら Prefab 化して保存

## プリセット例

### CFM56-7B27 (Boeing 737-800)

```
Max Thrust: 130408.51 N (27,300 lbf)
Idle N1: 879.6 RPM (19.2%)
Reference N1: 4397 RPM (95.9%)
Take Off N1: 4586 RPM (100%)
Idle N2: 8583.5 RPM (50%)
Reference N2: 17167 RPM (100%)
Idle EGT: 725°C
Take Off EGT: 1038°C
```

### GE90-115B (Boeing 777-300ER)

```
Max Thrust: 510000 N (115,000 lbf)
Idle N1: 1200 RPM (約20%)
Reference N1: 5800 RPM (約97%)
Take Off N1: 6000 RPM (100%)
Idle N2: 4800 RPM (約50%)
Reference N2: 9600 RPM (100%)
Idle EGT: 650°C
Take Off EGT: 950°C
```

### PW4000 (Boeing 747-400)

```
Max Thrust: 281570 N (63,300 lbf)
Idle N1: 950 RPM (約20%)
Reference N1: 4550 RPM (約96%)
Take Off N1: 4750 RPM (100%)
Idle N2: 5200 RPM (約50%)
Reference N2: 10400 RPM (100%)
Idle EGT: 700°C
Take Off EGT: 1000°C
```

## 実機搭載ワークフロー

### 1. テストベンチで調整

1. `Engine_Test` でパラメータを調整
2. 動作確認完了
3. GameObject を右クリック → Create Empty Prefab
4. `Prefabs/Engines/Engine_CFM56.prefab` として保存

### 2. 実機への搭載

```
Aircraft (SaccEntity付き)
└── VehicleModel
    └── Engines
        ├── LeftEngine (Engine_CFM56.prefab から作成)
        │   └── SFEXT_AdvancedEngine
        └── RightEngine (Engine_CFM56.prefab から作成)
            └── SFEXT_AdvancedEngine
```

### 3. 実機用設定変更

各エンジンの設定を変更:
- **SAV Control**: `MockSAVControl` → Aircraft の `SaccAirVehicle`
- **Entity Control**: 空欄のまま (SaccEntity が自動注入)
- **Vehicle Animator**: Aircraft の Animator
- **サウンド/エフェクト**: 実機用アセット

### 4. テストベンチの削除

VRChat アップロード前に `EngineTestBench` GameObject を削除または非アクティブ化

## トラブルシューティング

### エンジンが初期化されない

- `Start()` が呼ばれているか確認
- EntityControl が null でも動作する（テスト用）

### ThrottleStrength が更新されない

- Engine_Test の SAV Control が MockSAVControl を参照しているか確認
- Inspector で MockSAVControl.ThrottleStrength を確認

## 次のステップ

1. 各種エンジンパラメータのプリセット作成
2. サウンド/エフェクトの統合テスト
3. DFUNC_AdvancedThrustReverser の実装
4. DFUNC_AutoStarter の実装
5. 実機への統合

---

**注意**: このテストベンチは開発・検証専用です。VRChat にアップロードする際は不要なので削除してください。
