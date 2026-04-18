# TSFE セットアップ手順書

**Tsuitachi-SF-Equipment (TSFE)** をSaccFlightAndVehicles機体に導入する手順書。SF-1（大型ジェット機）を実例として解説。

**対象読者**: VRChat World制作者、SFV機体制作者
**難易度**: 中級～上級
**所要時間**: 2-4時間（機体の複雑さによる）

---

## 目次

- [前提条件](#前提条件)
- [パッケージインストール](#パッケージインストール)
- [Phase 1: コアシステム（DFUNC）](#phase-1-コアシステムdfunc)
- [Phase 2: エンジン・APUシステム](#phase-2-エンジンapuシステム)
- [Phase 3: 自動制御・補助システム](#phase-3-自動制御補助システム)
- [Phase 4: アニメーター設定](#phase-4-アニメーター設定)
- [Phase 5: テスト手順](#phase-5-テスト手順)
- [トラブルシューティング](#トラブルシューティング)
- [パフォーマンス最適化](#パフォーマンス最適化)

---

## 前提条件

### 必須パッケージ

| パッケージ | バージョン | 入手先 |
|----------|----------|--------|
| **Unity** | 2022.3.22f1+ | Unity Hub |
| **VRChat Worlds SDK** | 3.7.0+ | VCC (VRChat Creator Companion) |
| **UdonSharp** | 1.x | VCC（SDK同梱） |
| **SaccFlightAndVehicles** | 1.8.0+ | [GitHub](https://github.com/Sacchan-VRC/SaccFlightAndVehicles) |

### 既存セットアップ要件

TSFEを導入する前に、以下が完了している必要があります：

- ✅ SaccFlightAndVehicles 1.8の基本セットアップ完了
- ✅ SaccAirVehicleコンポーネント設定済み
- ✅ VehicleAnimatorセットアップ済み
- ✅ 基本的な飛行テスト完了
- ✅ 標準SFVのDFUNC動作確認済み

**確認方法**:
1. PlayModeで機体に搭乗できる
2. スロットル・ピッチ・ロール・ヨー操作が動作する
3. 離着陸ができる

---

## パッケージインストール

### 方法1: VCC経由（推奨）

```bash
# VCCでプロジェクトを開く
1. VCC（VRChat Creator Companion）を起動
2. プロジェクトを選択
3. "Manage Project" → "Add Package"
4. "Add Package from git URL" を選択
5. 以下のURLを入力:
   https://github.com/Tsuitachi/Tsuitachi-SF-Equipment.git
6. "Add" をクリック
```

### 方法2: UnityPackage手動インポート

```bash
1. GitHubからリリース版をダウンロード
   https://github.com/Tsuitachi/Tsuitachi-SF-Equipment/releases
2. Unity Editor → Assets → Import Package → Custom Package
3. ダウンロードした .unitypackage を選択
4. すべてのファイルにチェックを入れて "Import"
```

### インストール確認

インストール後、以下のフォルダが存在することを確認：

```
Packages/
  └── net.tsuitachi.sf-equipment/
      ├── Runtime/
      │   ├── DFUNC/
      │   ├── SFEXT/
      │   ├── Avionics/
      │   └── Utility/
      ├── Editor/
      ├── Sample/
      └── Docs~/
```

---

## Phase 1: コアシステム（DFUNC）

Phase 1では、パイロットが直接操作するコントロール（DFUNC）を追加します。

### 1.1 DFUNC_AdvancedFlaps（フラップ）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   Hierarchy:
   SF-1 (SaccEntity)
     └── SaccAirVehicle
         └── DFUNCs
             └── DFUNC_AdvancedFlaps  ← 新規作成
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `DFUNC` → `DFUNC_AdvancedFlaps`

3. **必須フィールド設定**

   | フィールド | 設定値 | 説明 |
   |----------|--------|------|
   | **SAVControl** | `SaccAirVehicle` | Hierarchyからドラッグ |
   | **Dial_Funcon_Array** | ダイヤル表示GameObject配列 | VRダイヤル選択時の表示 |

4. **Detents設定（SF-1の例）**

   フラップ展開角度（度）:
   ```
   Detents (Size: 9):
   [0] 0    → UP（格納）
   [1] 1    → Position 1
   [2] 2    → Position 2
   [3] 5    → Position 5
   [4] 10   → Position 10
   [5] 15   → Position 15
   [6] 25   → Position 25
   [7] 30   → Position 30
   [8] 40   → FULL（最大展開）
   ```

5. **速度制限設定（KIAS）**

   各detentの最大許容速度:
   ```
   Speed Limits (Size: 9):
   [0] 340  → UP時の最大速度
   [1] 250  → Pos 1
   [2] 250  → Pos 2
   [3] 250  → Pos 5
   [4] 210  → Pos 10
   [5] 200  → Pos 15
   [6] 190  → Pos 25
   [7] 175  → Pos 30
   [8] 162  → FULL時の最大速度（VFE）
   ```

6. **物理パラメータ設定**

   | パラメータ | 推奨値 | 説明 |
   |----------|--------|------|
   | **Drag Multiplier** | 1.4 | フラップ展開時の抗力倍率 |
   | **Lift Multiplier** | 1.35 | フラップ展開時の揚力倍率 |
   | **Response** | 1.0 | 展開速度（秒） |

7. **故障モデリング設定（オプション）**

   | パラメータ | 推奨値 | 説明 |
   |----------|--------|------|
   | **Mean Time Between Actuator Broken On Overspeed** | 120 | 過速度時のアクチュエータ故障MTBF（秒） |
   | **Mean Time Between Wing Broken On Overspeed** | 240 | 過速度時の翼破損MTBF（秒） |
   | **Overspeed Damage Multiplier** | 10 | 過速度時の故障確率倍率 |

8. **入力設定**

   | 設定 | デフォルト | 変更可能 |
   |-----|----------|---------|
   | **Desktop Key** | F | フラップ展開キー |
   | （Shift+F） | - | フラップ格納キー |
   | **VR Input Axis** | (0, 0, 1) | Z軸（前後） |
   | **Controller Sensitivity** | 0.02 | VR感度 |

#### アニメーター設定

`VehicleAnimator`に以下のパラメータを追加：

| パラメータ名 | 型 | 用途 |
|----------|---|------|
| `flapsangle` | Float | 現在のフラップ角度（0-1） |
| `flapstarget` | Float | 目標フラップ角度（0-1） |
| `flaps` | Bool | フラップ展開中フラグ |
| `flapsbroken` | Bool | フラップ故障フラグ |

#### テスト手順

1. PlayModeで搭乗
2. **デスクトップ**: `F`キーでフラップ展開、`Shift+F`で格納
3. **VR**: ダイヤルを選択してZ軸（前後）に手を動かす
4. Inspectorで`Detent Index`、`Angle`が変化することを確認
5. 速度制限を超えた状態で展開すると故障することを確認（Debug Mode有効時）

---

### 1.2 DFUNC_ElevatorTrim（エレベータートリム）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/SaccAirVehicle/DFUNCs/DFUNC_ElevatorTrim
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `DFUNC` → `DFUNC_ElevatorTrim`

3. **必須フィールド設定**

   | フィールド | 設定値 |
   |----------|--------|
   | **SAVControl** | `SaccAirVehicle` |
   | **Dial_Funcon_Array** | ダイヤル表示GameObject配列 |

4. **トリム範囲設定**

   | パラメータ | 推奨値 | 説明 |
   |----------|--------|------|
   | **Trim Range** | 0.15 | トリム範囲（±15%） |
   | **Response** | 2.0 | トリム変更速度 |
   | **VR Input Distance** | 0.15 | VR入力距離（m） |

5. **入力設定**

   | 設定 | キー | 説明 |
   |-----|-----|------|
   | **Trim Up Key** | UpArrow | 機首上げトリム |
   | **Trim Down Key** | DownArrow | 機首下げトリム |

#### アニメーター設定

| パラメータ名 | 型 | 用途 |
|----------|---|------|
| `trim` | Float | 現在のトリム値（-1 ～ +1） |

#### テスト手順

1. 飛行中、`↑`キーでトリムUP、`↓`キーでトリムDOWN
2. VRダイヤル選択中、前後に手を動かしてトリム調整
3. トリムを設定後、ジョイスティックから手を離してもピッチ姿勢が維持されることを確認

---

### 1.3 DFUNC_AdvancedSpeedBrake（スピードブレーキ）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/SaccAirVehicle/DFUNCs/DFUNC_AdvancedSpeedBrake
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `DFUNC` → `DFUNC_AdvancedSpeedBrake`

3. **必須フィールド設定**

   | フィールド | 設定値 |
   |----------|--------|
   | **SAVControl** | `SaccAirVehicle` |
   | **Dial_Funcon_Array** | ダイヤル表示GameObject配列 |

4. **物理パラメータ設定（重要）**

   **SF-1の場合（推力380,000N、質量19,000kg）**:

   | パラメータ | 推奨値 | 説明 |
   |----------|--------|------|
   | **Drag Multiplier** | **200-300** | 巡航速度で推力の40-60%相殺 |
   | **Lift Multiplier** | 0.6 | 揚力減少率 |
   | **Response** | 1.0 | 展開速度 |

   ⚠️ **重要**: 標準値1.4では効果が不十分です。大型機では**200以上**推奨。

   **計算式**:
   ```
   必要dragMultiplier = (推力 / (速度 × AirFriction × 質量)) - 1

   SF-1の例（100m/s巡航時）:
   = (380000 / (100 × 0.0004 × 19000)) - 1
   = (380000 / 760) - 1
   = 500 - 1
   = 499  ← 推力100%相殺に必要な値

   推奨値: 200-300（推力の40-60%相殺、現実的な減速効果）
   ```

5. **入力設定**

   | 設定 | キー | 動作 |
   |-----|-----|------|
   | **Desktop Key** | B | ホールドで展開、離すと格納 |
   | **VR Input Distance** | 0.1 | VR入力距離（m） |

#### アニメーター設定

| パラメータ名 | 型 | 用途 |
|----------|---|------|
| `speedbrake` | Float | 現在の展開率（0-1） |
| `speedbrakeinput` | Float | 目標展開率（0-1） |

#### テスト手順

1. 飛行中、`B`キー押下でスピードブレーキ展開
2. キーを離すと自動格納
3. **効果確認**: dragMultiplier=200で、100m/s巡航時に明確な減速効果を確認
4. VRダイヤル選択中、前後に手を動かして展開率調整

#### PlayMode Inspector デバッグ表示

PlayMode中、DFUNC_AdvancedSpeedBrakeのInspectorに以下が表示されます：

- Target Angle / Actual Angle（スライダー表示）
- SAVControl情報（ExtraDrag, ExtraLift, AirSpeed, Rigidbody速度）
- 期待抗力・揚力の計算値

**活用方法**:
1. `dragMultiplier`を調整
2. PlayModeで飛行しながらInspectorで`ExtraDrag`値を確認
3. 減速効果を体感しながら最適値を見つける

---

### 1.4 DFUNC_ThrustReverser（リバーサー）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/SaccAirVehicle/DFUNCs/DFUNC_ThrustReverser
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `DFUNC` → `DFUNC_ThrustReverser`

3. **必須フィールド設定**

   | フィールド | 設定値 |
   |----------|--------|
   | **SAVControl** | `SaccAirVehicle` |
   | **Dial_Funcon_Array** | ダイヤル表示GameObject配列 |

4. **入力設定**

   | 設定 | キー | 動作 |
   |-----|-----|------|
   | **Desktop Key** | V | トグル（ON/OFF） |

#### アニメーター設定

| パラメータ名 | 型 | 用途 |
|----------|---|------|
| `reverser` | Bool | リバーサー展開フラグ |

#### テスト手順

1. 着陸後、`V`キーでリバーサー展開
2. SAVControlの`InvertedThrottle`フラグが切り替わることを確認
3. スロットルを上げると逆推力が発生することを確認

**注意**: SFEXT_AdvancedEngineを使用する場合、エンジン側で自動的にリバーサー制御されます。

---

## Phase 2: エンジン・APUシステム

Phase 2では、複雑な内部状態を持つエンジン・APUシステムを追加します。

### 2.1 TSFE_PowerBus（電源バス）

エンジンとAPUの前提として、電源バスを先に設定します。

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/Systems/TSFE_PowerBus
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `Utility` → `TSFE_PowerBus`

3. **フィールド設定**

   | フィールド | 設定値 | 説明 |
   |----------|--------|------|
   | **SAVControl** | `SaccAirVehicle` | |
   | **Power Sources** | サイズ0（初期状態） | 後でAPU/Engineを追加 |
   | **Indicator** | 電源インジケータGameObject | オプション |

#### 動作原理

- 複数の電源（APU、エンジン、バッテリー等）を統合管理
- いずれかの電源がONなら`IsPowered() == true`
- 電源消費側コンポーネント（計器等）がこのバスを参照

---

### 2.2 TSFE_BleedAirBus（ブリードエアバス）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/Systems/TSFE_BleedAirBus
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `Utility` → `TSFE_BleedAirBus`

3. **フィールド設定**

   | フィールド | 設定値 |
   |----------|--------|
   | **SAVControl** | `SaccAirVehicle` |
   | **Bleed Air Sources** | サイズ0（初期状態） |

#### 動作原理

- APU・エンジンからのブリードエア供給を統合管理
- エンジン始動に必要（APUからブリードエア供給）
- 空調・与圧システム（将来実装）にも使用

---

### 2.3 SFEXT_AuxiliaryPowerUnit（APU）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/SaccAirVehicle/SFEXTs/SFEXT_AuxiliaryPowerUnit
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `SFEXT` → `SFEXT_AuxiliaryPowerUnit`

3. **必須フィールド設定**

   | フィールド | 設定値 | 説明 |
   |----------|--------|------|
   | **SAVControl** | `SaccAirVehicle` | |
   | **Power Bus** | `TSFE_PowerBus` | 電源バス |
   | **Bleed Air Bus** | `TSFE_BleedAirBus` | ブリードエアバス |

4. **APUパラメータ設定（SF-1の例）**

   | パラメータ | 推奨値 | 説明 |
   |----------|--------|------|
   | **Max RPM** | 100 | 最大RPM（%） |
   | **Idle RPM** | 95 | アイドルRPM（%） |
   | **Startup Time** | 60 | 始動時間（秒） |
   | **Cooldown Time** | 120 | 冷却時間（秒） |
   | **Max EGT** | 700 | 最大EGT（℃） |

5. **入力設定**

   APUはSAVControlの`StartEngine`ボタン経由で制御：
   - エンジン停止中に`StartEngine`押下 → APU起動シーケンス開始
   - APU起動中に再度押下 → APUシャットダウン

6. **サウンド設定（オプション）**

   | AudioSource | 用途 |
   |------------|------|
   | **Startup Sound** | APU始動音 |
   | **Running Sound** | APU運転音 |
   | **Shutdown Sound** | APUシャットダウン音 |

#### アニメーター設定

| パラメータ名 | 型 | 用途 |
|----------|---|------|
| `apurpm` | Float | APU RPM（0-1） |
| `apurunning` | Bool | APU運転中フラグ |
| `apuegt` | Float | APU EGT（0-1） |

#### テスト手順

1. PlayModeで搭乗
2. `Y`キー（StartEngineボタン）でAPU起動
3. ConsoleまたはInspectorで`APU State`が`Starting` → `Running`に遷移することを確認
4. 約60秒後、`Power Bus`の`IsPowered()`が`true`になることを確認
5. 再度`Y`キーでAPUシャットダウン

---

### 2.4 SFEXT_AdvancedEngine（エンジン）

SF-1は双発機なので、エンジンを2つセットアップします。

#### セットアップ手順（Engine_L）

1. **GameObjectの作成**
   ```
   SF-1/SaccAirVehicle/SFEXTs/SFEXT_AdvancedEngine_L
   ```

2. **Prefabからインスタンス化（推奨）**
   - ProjectビューでSample/SFEXT_AdvancedEngine_SF-1.prefabを探す
   - HierarchyのSFEXTsフォルダにドラッグ&ドロップ
   - 名前を`SFEXT_AdvancedEngine_L`に変更

3. **必須フィールド設定**

   | フィールド | 設定値 | 説明 |
   |----------|--------|------|
   | **SAVControl** | `SaccAirVehicle` | |
   | **Starter Power Source** | `TSFE_PowerBus` | APU電源 |
   | **Engine Position** | Engine_Lの Transform | 非対称推力計算用 |

4. **エンジンパラメータ（SF-1の例: 190,000N推力）**

   | パラメータ | 設定値 | 説明 |
   |----------|--------|------|
   | **Max Thrust** | 190000 | 最大推力（N） |
   | **Thrust Curve** | 1.8 | 推力カーブ指数 |
   | **Idle Thrust Ratio** | 0.05 | アイドル推力比率 |
   | **Idle N1** | 6000 | アイドルN1 RPM |
   | **Reference N1** | 11500 | 巡航N1 RPM |
   | **Takeoff N1** | 12000 | 離陸N1 RPM |
   | **Idle N2** | 8000 | アイドルN2 RPM |
   | **Reference N2** | 14000 | 巡航N2 RPM |
   | **Takeoff N2** | 15000 | 離陸N2 RPM |

5. **温度設定**

   | パラメータ | 設定値 | 説明 |
   |----------|--------|------|
   | **Ambient Temp** | 15 | 外気温度（℃、ISA標準） |
   | **Idle EGT** | 500 | アイドルEGT（℃） |
   | **Continuous EGT** | 850 | 連続運転EGT（℃） |
   | **Takeoff EGT** | 850 | 離陸EGT（℃） |
   | **Fire EGT** | 1150 | 火災発生EGT（℃） |

6. **アフターバーナー設定（戦闘機の場合）**

   | パラメータ | 設定値 | 説明 |
   |----------|--------|------|
   | **Has Afterburner** | true | AB有効化 |
   | **Afterburner Threshold** | 0.8 | AB点火スロットル閾値 |
   | **Afterburner Thrust Multiplier** | 1.62 | AB推力倍率 |

7. **故障システム設定（オプション）**

   | パラメータ | 推奨値 | 説明 |
   |----------|--------|------|
   | **Enable Failure System** | true | 故障システム有効化 |
   | **MTB Fire At Continuous** | 2592000 | 連続運転時の火災MTBF（秒、30日） |
   | **MTB Fire At Overheat** | 1800 | 過熱時の火災MTBF（秒、30分） |
   | **MTB Meltdown On Fire** | 90 | 火災発生後の破損MTBF（秒、1.5分） |

8. **質量設定**

   | パラメータ | 設定値 | 説明 |
   |----------|--------|------|
   | **Use Manual Mass** | false | 自動（SAVから取得） |
   | **Engine Calculation Mass** | 19000 | 手動設定する場合の質量（kg） |

9. **動的推力設定**

   | パラメータ | 設定値 | 説明 |
   |----------|--------|------|
   | **Enable Dynamic Thrust** | true | 速度・高度依存の推力減衰 |
   | **Enable Asymmetric Thrust** | true | 非対称推力（片発時） |

10. **サウンド設定**

    AudioSourceを9個子オブジェクトとして追加：

    | AudioSource名 | 用途 |
    |-------------|------|
    | IdleSound | エンジンアイドル音 |
    | InsideSound | コックピット内音 |
    | ThrustSound | 推力音 |
    | TakeoffSound | 離陸推力音 |
    | ReverserSound | リバーサー音 |
    | AfterburnerSound | アフターバーナー音 |
    | StartingSound | 始動音 |
    | WindmillingSound | 風車回転音 |
    | IngestionSound | エンジン吸入音（危険警告） |

11. **キャノピー連動音量（オプション）**

    | パラメータ | 設定値 | 説明 |
    |----------|--------|------|
    | **Canopy Parameter Name** | "canopy" | キャノピーアニメーターパラメータ |
    | **Canopy Invert** | false | 反転フラグ |
    | **Canopy Closed Volume Multiplier** | 0.3 | 閉鎖時の音量倍率 |

#### アニメーター設定

| パラメータ名 | 型 | 用途 |
|----------|---|------|
| `n1` | Float | N1 RPM（0-1） |
| `n2` | Float | N2 RPM（0-1） |
| `reverser` | Bool | リバーサー展開中 |
| `fire` | Bool | エンジン火災 |
| `afterburner` | Bool | アフターバーナー点火中 |

#### Engine_Rのセットアップ

1. `SFEXT_AdvancedEngine_L`を複製
2. 名前を`SFEXT_AdvancedEngine_R`に変更
3. **Engine Position**を右エンジンのTransformに変更
4. AudioSourcesを右エンジン位置に配置

#### テスト手順

1. APU起動完了後、`Y`キー（StartEngine）長押し
2. N2が約25%まで上昇（スターター稼働）
3. 自動的に燃料噴射開始、EGTが上昇
4. N2が約60%でアイドル安定
5. スロットルを上げるとN1/N2が追従
6. `T`キー（Afterburner）でAB点火（戦闘機の場合）
7. 着陸後、`V`キー（Reverser）で逆推力確認

---

### 2.5 SFEXT_AutoStarter（自動始動シーケンス）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/SaccAirVehicle/SFEXTs/SFEXT_AutoStarter
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `SFEXT` → `SFEXT_AutoStarter`

3. **必須フィールド設定**

   | フィールド | 設定値 | 説明 |
   |----------|--------|------|
   | **SAVControl** | `SaccAirVehicle` | |
   | **APU** | `SFEXT_AuxiliaryPowerUnit` | |
   | **Engines** | サイズ2 | Engine_L, Engine_Rを設定 |

4. **シーケンス設定**

   | パラメータ | 設定値 | 説明 |
   |----------|--------|------|
   | **Auto APU Start** | true | APU自動起動 |
   | **Auto Engine Start After APU** | true | APU起動後にエンジン自動始動 |
   | **Engine Start Delay** | 10 | APU起動後、エンジン始動までの待機時間（秒） |
   | **Engine Start Interval** | 5 | 左右エンジン始動の間隔（秒） |

#### 動作フロー

```
1. パイロット搭乗
2. Yキー押下
   ↓
3. APU自動起動開始
   ↓
4. APU起動完了（約60秒）
   ↓
5. 10秒待機
   ↓
6. Engine_L自動始動開始
   ↓
7. Engine_L起動完了（約20秒）
   ↓
8. 5秒待機
   ↓
9. Engine_R自動始動開始
   ↓
10. Engine_R起動完了
   ↓
11. APU自動シャットダウン
```

#### テスト手順

1. PlayModeで搭乗
2. `Y`キー1回押下のみ
3. 自動的にAPU → Engine_L → Engine_R → APUシャットダウンのシーケンスが実行されることを確認
4. 途中でキャンセルする場合、再度`Y`キー押下

---

## Phase 3: 自動制御・補助システム

### 3.1 SFEXT_AutoFlaps（自動フラップ）

#### セットアップ手順

1. **GameObjectの作成**
   ```
   SF-1/SaccAirVehicle/SFEXTs/SFEXT_AutoFlaps
   ```

2. **コンポーネント追加**
   - `Add Component` → `TSFE` → `SFEXT` → `SFEXT_AutoFlaps`

3. **必須フィールド設定**

   | フィールド | 設定値 | 説明 |
   |----------|--------|------|
   | **Advanced Flaps** | `DFUNC_AdvancedFlaps` | 制御対象 |
   | **SAVControl** | `SaccAirVehicle` | |

4. **モード設定**

   | モード | 説明 | 用途 |
   |-------|------|------|
   | **0: Civilian** | 速度ベース | 民間機 |
   | **1: Military** | AoA/G/Mach対応 | 戦闘機 |
   | **2: IDLC** | 統合デジタル飛行制御 | F-35風 |

5. **スケジュール設定（Civilianモードの例）**

   ```
   Schedule Flap Angle (deg):
   [0] 0    → UP
   [1] 1    → Pos 1
   [2] 5    → Pos 5
   [3] 15   → Pos 15
   [4] 30   → Pos 30
   [5] 40   → FULL

   Schedule Speed Max (KIAS):
   [0] -1   → 無制限
   [1] 250  → 250kt以下でPos 1
   [2] 210  → 210kt以下でPos 5
   [3] 200  → 200kt以下でPos 15
   [4] 175  → 175kt以下でPos 30
   [5] 162  → 162kt以下でFULL

   Schedule Priority:
   [0] 0
   [1] 1
   [2] 2
   [3] 3
   [4] 4
   [5] 5
   ```

6. **ヒステリシス設定**

   | パラメータ | 推奨値 | 説明 |
   |----------|--------|------|
   | **Extend Hysteresis Knots** | 5 | 展開時のヒステリシス（kt） |
   | **Retract Margin Knots** | 3 | 過速度保護マージン（kt） |

7. **脚連動設定（オプション）**

   | パラメータ | 設定値 | 説明 |
   |----------|--------|------|
   | **Inhibit On Gear Up** | true | 脚収納中は展開禁止 |
   | **Inhibit Max Angle** | 0 | 脚収納中の最大許容角度 |

#### テスト手順

1. PlayModeで離陸
2. 速度を250kt以下に減速 → 自動的にFlaps 1展開
3. さらに210kt以下に減速 → Flaps 5展開
4. 手動でフラップレバーを操作 → 自動制御が一時停止
5. レバーを離す → 自動制御が再開

---

### 3.2 SFEXT_Chock（車輪ブロック）

#### セットアップ手順

1. **Prefabからインスタンス化**
   - ProjectビューでSample/SFEXT_Chock.prefabを探す
   - Sceneビューの機体前方にドラッグ&ドロップ

2. **フィールド設定**

   | フィールド | 設定値 | 説明 |
   |----------|--------|------|
   | **Target Rigidbody** | `VehicleRigidbody` | 機体のRigidbody |
   | **Brake Force** | 100000 | ブレーキ力（N） |

3. **配置**

   - 機体の前輪または主脚の前方に配置
   - Rigidbodyコンポーネント追加済み
   - VRC_Pickupコンポーネント追加済み

#### テスト手順

1. PlayModeでChockに近づく
2. Interactでピックアップ
3. 機体の車輪前方に配置
4. ToggleChock()メソッドが呼ばれ、機体が固定されることを確認
5. 再度Interactで解除

---

## Phase 4: アニメーター設定

TSFEコンポーネントが使用するアニメーターパラメータを`VehicleAnimator`に追加します。

### 必須パラメータ一覧

| パラメータ名 | 型 | 用途 | 使用コンポーネント |
|----------|---|------|------------------|
| **flapsangle** | Float | フラップ角度（0-1） | DFUNC_AdvancedFlaps |
| **flapstarget** | Float | フラップ目標角度（0-1） | DFUNC_AdvancedFlaps |
| **flaps** | Bool | フラップ展開中 | DFUNC_AdvancedFlaps |
| **flapsbroken** | Bool | フラップ故障 | DFUNC_AdvancedFlaps |
| **trim** | Float | エレベータートリム（-1～+1） | DFUNC_ElevatorTrim |
| **speedbrake** | Float | スピードブレーキ展開率（0-1） | DFUNC_AdvancedSpeedBrake |
| **speedbrakeinput** | Float | スピードブレーキ入力（0-1） | DFUNC_AdvancedSpeedBrake |
| **reverser** | Bool | リバーサー展開中 | DFUNC_ThrustReverser |
| **n1** | Float | エンジンN1 RPM（0-1） | SFEXT_AdvancedEngine |
| **n2** | Float | エンジンN2 RPM（0-1） | SFEXT_AdvancedEngine |
| **fire** | Bool | エンジン火災 | SFEXT_AdvancedEngine |
| **afterburner** | Bool | アフターバーナー点火中 | SFEXT_AdvancedEngine |
| **apurpm** | Float | APU RPM（0-1） | SFEXT_AuxiliaryPowerUnit |
| **apurunning** | Bool | APU運転中 | SFEXT_AuxiliaryPowerUnit |
| **apuegt** | Float | APU EGT（0-1） | SFEXT_AuxiliaryPowerUnit |

### アニメーターセットアップ手順

1. `VehicleAnimator`を選択
2. Animator Controllerを開く
3. Parameters タブで上記パラメータを追加
4. Animationクリップでパラメータを使用してメッシュ変形・回転を制御

### アニメーション例

#### フラップ展開アニメーション

```
State: Flaps
  Motion: FlapsAnimation

FlapsAnimation:
  0.0s (flapsangle = 0): FlapMesh.localRotation = (0, 0, 0)
  1.0s (flapsangle = 1): FlapMesh.localRotation = (40, 0, 0)
```

#### スピードブレーキ展開アニメーション

```
State: SpeedBrake
  Motion: SpeedBrakeAnimation

SpeedBrakeAnimation:
  0.0s (speedbrake = 0): SpeedBrakeMesh.localPosition = (0, 0, 0)
  1.0s (speedbrake = 1): SpeedBrakeMesh.localPosition = (0, 0.5, 0)
```

---

## Phase 5: テスト手順

### 基本機能テスト

#### 1. 地上テスト

- [ ] APU起動（Yキー）
- [ ] APU RPMが100%到達
- [ ] PowerBusが電力供給状態（IsPowered = true）
- [ ] エンジン始動シーケンス開始
- [ ] 左エンジンN2が上昇
- [ ] 左エンジンアイドル到達（N2 約60%）
- [ ] 右エンジン始動
- [ ] 右エンジンアイドル到達
- [ ] APU自動シャットダウン

#### 2. 離陸テスト

- [ ] スロットル最大（W キー）
- [ ] N1/N2が上昇
- [ ] 推力発生（機体が前進）
- [ ] Vr（離陸速度）到達
- [ ] ピッチアップで離陸
- [ ] フラップ展開（F キー）→ 揚力・抗力変化確認
- [ ] フラップ格納（Shift+F キー）

#### 3. 巡航テスト

- [ ] トリム調整（↑↓ キー）
- [ ] ジョイスティック中立でピッチ姿勢維持
- [ ] スピードブレーキ展開（B キー）→ 減速効果確認
- [ ] 自動フラップが速度に応じて動作（AutoFlaps有効時）

#### 4. 着陸テスト

- [ ] 速度減速でフラップ自動展開（AutoFlaps有効時）
- [ ] 手動フラップ操作で自動制御が一時停止
- [ ] 接地後、リバーサー展開（V キー）
- [ ] スロットル上げで逆推力確認
- [ ] チョック配置で機体固定

### パフォーマンステスト

#### FPS確認

1. Statsウィンドウを開く（Window → Analysis → Stats）
2. PlayModeで以下を確認：
   - **FPS**: 60以上維持
   - **Batches**: 増加量を確認
   - **Tris**: ポリゴン数確認

#### Udon Heap確認

1. VRChat SDK Control Panelを開く
2. "Content Manager" → "Udon Heap Size"
3. PlayMode中のヒープ使用量確認
4. **推奨**: 総ヒープサイズの50%以下

### 故障システムテスト（オプション）

1. `Enable Failure System = true`に設定
2. フラップを速度制限超過で展開 → 故障発生確認
3. エンジンを連続最大推力で運転 → 過熱・火災発生確認
4. `Debug Mode = true`でConsoleログ確認

---

## トラブルシューティング

### 問題1: エンジンが始動しない

**症状**: Yキー押下してもN2が上昇しない

**原因と対策**:

| 原因 | 確認方法 | 対策 |
|-----|---------|------|
| APUが起動していない | APU RPMを確認 | APUを先に起動 |
| PowerBusが未接続 | `Starter Power Source`フィールド確認 | PowerBusを設定 |
| SAVControlが未設定 | Inspectorで確認 | SaccAirVehicleを設定 |
| Auto Fuel Injection = false | Inspectorで確認 | trueに変更 |

### 問題2: フラップが動かない

**症状**: Fキー押下しても反応なし

**原因と対策**:

| 原因 | 確認方法 | 対策 |
|-----|---------|------|
| SAVControlが未設定 | Inspectorで確認 | SaccAirVehicleを設定 |
| Detentsが空配列 | Inspectorで確認 | 最低2個のdetentを設定 |
| Speed Limitsが空配列 | Inspectorで確認 | Detentsと同じサイズに設定 |
| アニメーターパラメータ未設定 | Animator Controllerで確認 | `flapsangle`等を追加 |

### 問題3: スピードブレーキの減速効果が弱い

**症状**: スピードブレーキ全開でも速度がほとんど落ちない

**原因と対策**:

| 原因 | 対策 |
|-----|------|
| dragMultiplierが小さすぎる | **200-300**に増やす（SF-1の場合） |
| SaccAirVehicleのAirFrictionが小さい | SaccAirVehicleのAirFriction値を確認 |
| 機体質量が大きすぎる | 質量に応じてdragMultiplierを調整 |

**計算式**:
```
推奨dragMultiplier = (推力 / (巡航速度 × AirFriction × 質量)) × 0.5
```

### 問題4: APUが自動停止しない

**症状**: エンジン始動完了後もAPUが動き続ける

**原因と対策**:

| 原因 | 対策 |
|-----|------|
| SFEXT_AutoStarterが未設定 | AutoStarterを追加 |
| AutoStarterのAPUフィールドが未設定 | APUを設定 |
| Auto APU Shutdown = false | trueに変更 |

### 問題5: 自動フラップが動作しない

**症状**: 速度変化してもフラップが自動展開/格納されない

**原因と対策**:

| 原因 | 確認方法 | 対策 |
|-----|---------|------|
| Advanced Flapsが未設定 | Inspectorで確認 | DFUNC_AdvancedFlapsを設定 |
| Scheduleが空 | Inspectorで確認 | Speed Max等を設定 |
| 手動操作中 | isPilot || selectedを確認 | 手動操作を解除 |
| SAVControlが未設定 | Inspectorで確認 | SaccAirVehicleを設定 |

### 問題6: VRダイヤルが反応しない

**症状**: VRでダイヤルを選択しても操作できない

**原因と対策**:

| 原因 | 対策 |
|-----|------|
| Dial_Funcon_Arrayが未設定 | ダイヤル表示GameObjectを設定 |
| DFUNC_LeftDial/RightDial未実装 | 最新版のTSFEに更新 |
| trackingTarget未設定 | DFUNC_Selected()が正しく実装されているか確認 |

### 問題7: 音が鳴らない

**症状**: エンジン音・APU音が聞こえない

**原因と対策**:

| 原因 | 対策 |
|-----|------|
| AudioSourceが未設定 | AudioSourceを追加 |
| AudioClipが未設定 | AudioClipをアサイン |
| Volume Multiplierが0 | 1.0に設定 |
| Spatializationが有効 | Spatial Blend = 1.0（3D）に設定 |
| Max Distanceが小さい | 100m以上に設定 |

### 問題8: Udon同期エラー

**症状**: Console に "UdonSyncMode mismatch" エラー

**原因と対策**:

| 原因 | 対策 |
|-----|------|
| Sync Modeが間違っている | DFUNC: Continuous, SFEXT: 仕様に従う |
| UdonSharpコンパイルエラー | U# → "Compile All UdonSharp Programs" |
| VRChat SDKバージョン不一致 | SDK 3.7.0+に更新 |

---

## パフォーマンス最適化

### 1. AudioSource最適化

**問題**: 大量のAudioSourceがパフォーマンスに影響

**対策**:
- **Max Distance**を適切に設定（100m推奨）
- **Volume Rolloff**をLogarithmicに設定
- 不要なAudioSourceは削除
- PlayOnAwake = falseに設定

### 2. Update()最適化

**問題**: 毎フレームGetProgramVariable()呼び出しが重い

**対策**:
```csharp
// 悪い例
private void Update()
{
    float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed"); // 毎フレーム
}

// 良い例
private float airSpeed;
private void Update()
{
    airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed"); // キャッシュ
    // airSpeedを複数回使用
}
```

### 3. アニメーター最適化

**対策**:
- **Culling Mode**: `Always Animate`（SFV要件）
- 不要なアニメーションレイヤーを削除
- Write Defaults = trueに統一

### 4. Collider最適化

**対策**:
- MeshColliderをBox/Capsule Colliderに変更可能な箇所は変更
- Wheel Colliderの数を最小限に

---

## 次のステップ

### 追加実装推奨コンポーネント

Phase 1-3完了後、以下のコンポーネント追加を検討：

| コンポーネント | 優先度 | 効果 |
|-------------|-------|------|
| **SFEXT_InstrumentsAnimationDriver** | 高 | アナログ計器駆動 |
| **GPWS** | 高 | 対地接近警報 |
| **AuralWarnings** | 中 | 速度超過警報 |
| **SFEXT_AdvancedGear** | 中 | 高度なギアシステム |
| **SFEXT_DihedralEffect** | 低 | 上反効果（リアル飛行感） |
| **SFEXT_WakeTurbulence** | 低 | 後方乱気流 |

### カスタマイズ推奨パラメータ

機体特性に応じて調整推奨：

| パラメータ | 調整対象 | 理由 |
|----------|---------|------|
| **dragMultiplier** | DFUNC_AdvancedSpeedBrake | 機体質量・推力に応じて |
| **Speed Limits** | DFUNC_AdvancedFlaps | 機体のVFE/VFに合わせて |
| **Max Thrust** | SFEXT_AdvancedEngine | 実機データに基づいて |
| **MTBF値** | 各故障システム | ゲームバランスに応じて |

---

## 参考資料

### TSFE公式ドキュメント

- `Docs~/PATTERNS.md` - 設計パターン
- `Docs~/COMPONENTS.md` - コンポーネント一覧
- `Docs~/ARCHITECTURE.md` - アーキテクチャ設計

### 外部リソース

- [SaccFlightAndVehicles Wiki](https://github.com/Sacchan-VRC/SaccFlightAndVehicles/wiki)
- [VRChat Udon Documentation](https://creators.vrchat.com/worlds/udon/)
- [UdonSharp Documentation](https://udonsharp.docs.vrchat.com/)

---

**最終更新**: 2026-04-13
**対象バージョン**: TSFE 1.0.0, SFV 1.8.0
