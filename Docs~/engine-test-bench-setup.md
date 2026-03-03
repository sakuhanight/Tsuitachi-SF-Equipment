# エンジンテストベンチ セットアップガイド

各種エンジンの検証用テストベンチ構成です。実機搭載時とは独立した構成で、様々なエンジンをテスト可能です。

## 推奨構成

```
EngineTestBench (親GameObject)
├── MockSAVControl (テスト用SAV)
│   └── MockSAVControl component
├── TestController (テストコントローラー)
│   └── SFEXT_AdvancedEngineTest component
└── Engines (エンジンコンテナ)
    ├── Engine_CFM56_Left (テスト対象エンジン 1)
    │   └── SFEXT_AdvancedEngine component
    ├── Engine_CFM56_Right (テスト対象エンジン 2)
    │   └── SFEXT_AdvancedEngine component
    └── Engine_GE90 (別のエンジンタイプ)
        └── SFEXT_AdvancedEngine component
```

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

### 3. TestController 作成

1. `EngineTestBench` の子として空の GameObject 作成: `TestController`
2. `SFEXT_AdvancedEngineTest` コンポーネント追加
3. 設定:
   - Engine: **後でエンジンを選択**
   - Debug Text: なし (任意、UI Text使用時のみ)
   - キーバインド: デフォルトでOK

### 4. Engines コンテナ作成

1. `EngineTestBench` の子として空の GameObject 作成: `Engines`
2. このフォルダ内にテスト対象エンジンを配置

### 5. エンジン作成 (例: CFM56)

1. `Engines` の子として空の GameObject 作成: `Engine_CFM56_Left`
2. `SFEXT_AdvancedEngine` コンポーネント追加
3. 設定:
   - **SAV Control**: `MockSAVControl`
   - **Entity Control**: 空欄 (テスト時は不要)
   - 動力系: デフォルト (CFM56-7B27相当)
   - N1/N2 RPM: デフォルト
   - 温度: デフォルト
   - 逆噴射: デフォルト
   - 故障 MTBF: デフォルト
   - コンポーネント:
     - Vehicle Animator: なし (任意)
     - サウンド: なし (任意)
     - エフェクト: なし (任意)

4. TestController の設定に戻る:
   - **Engine**: `Engine_CFM56_Left`

### 6. 複数エンジンテスト（任意）

異なるエンジンをテストする場合:

1. `Engines` 内に新しいエンジン作成: `Engine_GE90`
2. SFEXT_AdvancedEngine を追加
3. パラメータを GE90 用に調整:
   - maxThrust: 510000 (GE90-115Bの場合)
   - idleN1/N2: GE90のデータ
   - 等

4. テスト時は `TestController` の `Engine` フィールドを切り替え

### 7. UI デバッグ表示（任意）

1. Hierarchy 右クリック → UI → Canvas
2. Canvas 右クリック → UI → Text (Legacy)
3. Text 設定:
   - Anchor: Top Left
   - Position: (185, -180)
   - Size: (350, 340)
   - Font Size: 14
4. `TestController` の `Debug Text` に設定

## テスト手順

### 基本テスト

1. Play Mode 開始
2. `TestController` を選択
3. Inspector で操作:
   - **Starter ON** → N2 が 30% まで上昇 (約10秒)
   - **Fuel ON** → N2 が 50% (Idle) まで上昇 (約20秒)
   - **Throttle スライダー** → N1 が上昇、推力発生
   - **Reverser ON** → 推力が負に反転

### エンジン切り替えテスト

1. Play Mode 停止
2. `TestController` の `Engine` を別のエンジンに変更
3. Play Mode 再開
4. 同じ手順でテスト

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

## 実機搭載時の構成

実際の機体にエンジンを搭載する場合:

```
Aircraft (SaccEntity付き)
└── VehicleModel
    └── Engines
        ├── LeftEngine
        │   └── SFEXT_AdvancedEngine
        └── RightEngine
            └── SFEXT_AdvancedEngine
```

設定:
- **SAV Control**: Aircraft の `SaccAirVehicle`
- **Entity Control**: 空欄 (SaccEntity が自動注入)
- **Vehicle Animator**: Aircraft の Animator
- **サウンド/エフェクト**: 実機用アセット

## トラブルシューティング

### エンジンが初期化されない

- `Start()` が呼ばれているか確認
- EntityControl が null でも動作する（テスト用）

### ThrottleStrength が更新されない

- MockSAVControl の SAVControl 参照が正しいか確認
- Inspector で MockSAVControl.ThrottleStrength を確認

### 複数エンジンで推力が重複

- 各エンジンが異なる MockSAVControl を参照していないか確認
- 同じ MockSAVControl を共有すること

## 次のステップ

1. 各種エンジンパラメータのプリセット作成
2. サウンド/エフェクトの統合テスト
3. DFUNC_AdvancedThrustReverser の実装
4. DFUNC_AutoStarter の実装
5. 実機への統合

---

**注意**: このテストベンチは開発・検証専用です。VRChat にアップロードする際は不要なので削除してください。
