# SF-AdvancedEquipment 現状調査報告書

## 1. 概要

本報告書は、EsnyaSFAddons に実装されていた装備品を SaccFlightAndVehicles (以下 SFV) の最新版に対応させた新パッケージ **SF-AdvancedEquipment (SF-AdvEquipment)** を開発するにあたり、既存コードベースの現状を調査した結果をまとめたものである。

### 1.1 背景

| 項目 | 内容 |
|------|------|
| EsnyaSFAddons 対応バージョン | SaccFlightAndVehicles **1.63** |
| 現在主流のバージョン | SaccFlightAndVehicles **1.71** |
| 最新バージョン | SaccFlightAndVehicles **1.8.0** |
| EsnyaSFAddons パッケージバージョン | 5.0.0-beta.49 |
| ライセンス | MIT |

EsnyaSFAddons は SFV 1.63 を前提として開発されており、1.71 および 1.8 で導入された API 変更・新機能に追従していない。そのため、最新版 SFV と組み合わせた際に互換性の問題が発生する。

---

## 2. EsnyaSFAddons の構成

### 2.1 パッケージ構造

```
EsnyaSFAddons/
├── Packages/
│   ├── com.nekometer.esnya.esnya-sf-addons/         # メインランタイムパッケージ
│   │   ├── Scripts/
│   │   │   ├── Accesories/    (8 スクリプト)
│   │   │   ├── Annotations/   (2 スクリプト)
│   │   │   ├── Avionics/      (3 スクリプト)
│   │   │   ├── DFUNC/         (18 スクリプト)
│   │   │   ├── SFEXT/         (17 スクリプト)
│   │   │   ├── Weather/       (3 スクリプト)
│   │   │   └── ESFASceneSetup.cs
│   │   ├── Editor/            (5 スクリプト)
│   │   ├── Prefabs/           (18 プレハブ)
│   │   ├── Materials/         (5 マテリアル)
│   │   └── package.json
│   │
│   └── com.nekometer.esnya.esnya-sf-addons-ucs/     # UdonChips 連携パッケージ
│       ├── Scripts/           (2 スクリプト)
│       ├── Prefabs/           (3 プレハブ)
│       └── package.json
└── package.json (ルート v7.0.1)
```

### 2.2 依存関係

**メインパッケージの依存:**
- `com.nekometer.esnya.inari-udon`: 9.1.0
- `com.unity.textmeshpro`: 2.1.6
- `sh.orels.udontoolkit`: 1.1.2
- `com.vrchat.worlds`: 3.1.1 (VPM)
- `com.vrchat.udonsharp`: 1.1.7 (VPM)

---

## 3. 装備品一覧 (全54スクリプト)

### 3.1 DFUNC (ダイヤルファンクション) - 18スクリプト

ダイヤルファンクションはコクピット内のダイヤル操作に紐づくインタラクティブ制御機能である。

| スクリプト名 | 機能概要 | 複雑度 | SFV API依存度 |
|------------|---------|--------|-------------|
| `DFUNC_Base` | 全DFUNCの基底クラス。VRトリガー入力処理 | 低 | 低 |
| `DFUNC_AdvancedFlaps` | 多段階フラップ制御 (9段デテント、速度制限、破損モデル、ハプティクス) | **高** | **高** |
| `DFUNC_AdvancedCatapult` | カタパルト発艦用ランチフック制御 | 中 | 中 |
| `DFUNC_AdvancedParkingBrake` | パーキングブレーキ制御 | 低 | 中 |
| `DFUNC_AdvancedSpeedBrake` | スピードブレーキ/スポイラー制御 | 中 | 中 |
| `DFUNC_AdvancedThrustReverser` | エンジン逆噴射制御 (インターロック付き) | 中 | **高** |
| `DFUNC_AdvancedWaterRudder` | 水上ラダー制御 (水陸両用機) | 低 | 中 |
| `DFUNC_AutoStarter` | 自動エンジン始動シーケンス | 中 | **高** |
| `DFUNC_BuddyPod` | 空中給油バディポッド (ドローグ接触検出) | **高** | 中 |
| `DFUNC_ElevatorTrim` | エレベータトリム制御 | 低 | 中 |
| `DFUNC_Empty` | 空のプレースホルダー | 低 | 低 |
| `DFUNC_LandingLight` | ランディングライト制御 | 低 | 低 |
| `DFUNC_SeatAdjuster` | パイロットシート位置調整 | 低 | 中 |
| `DFUNC_SendCustomEvent` | 汎用カスタムイベント送信 | 低 | 低 |
| `DFUNC_Slider` | 汎用スライダー制御 | 低 | 低 |
| `DFUNC_ThrustReverser` | 標準逆噴射制御 | 中 | 中 |
| `DFUNC_WingFold` | 翼折りたたみ機構 (空母艦載機用) | 中 | 中 |
| `DFUNCP_IHaveControl` | 操縦権限表示/移譲 (パッセンジャー用) | 中 | 中 |

### 3.2 SFEXT (システムエクステンション) - 17スクリプト

エクステンションは `SaccEntity.ExtensionUdonBehaviours` に登録され、ビークルのライフサイクルイベントを受信する。

| スクリプト名 | 機能概要 | 行数 | 複雑度 | SFV API依存度 |
|------------|---------|------|--------|-------------|
| `SFEXT_AbstractVehicle` | SaccAirVehicleの完全代替。飛行物理・燃料・G力・大気モデル等 | 1400+ | **極高** | **極高** |
| `SFEXT_AdvancedEngine` | リアルタービファンエンジンシミュレーション (N1/N2スプール、EGT/ECT、始動シーケンス、エンジン火災、FOD) | 670+ | **極高** | **高** |
| `SFEXT_AdvancedGear` | 高度な脚システム (サスペンション、タイヤバースト、操舵、速度超過故障) | 300+ | **高** | **高** |
| `SFEXT_AdvancedPropellerThrust` | プロペラ推力シミュレーション | 中 | 中 | **高** |
| `SFEXT_AuxiliaryPowerUnit` | APUシステム (始動/停止シーケンション、排気エフェクト) | 190+ | 中 | 低 |
| `SFEXT_BoardingCollider` | 搭乗システムコライダー | 低 | 低 | 中 |
| `SFEXT_DihedralEffect` | 上反角効果シミュレーション | 低 | 中 | **高** |
| `SFEXT_EngineFanDriver` | エンジンファンアニメーション駆動 | 低 | 低 | 中 |
| `SFEXT_FloodedEngineKiller` | 浸水によるエンジン停止 | 低 | 低 | 中 |
| `SFEXT_InstrumentsAnimationDriver` | コクピット計器アニメーション駆動 | 中 | 中 | **高** |
| `SFEXT_KillPenalty` | キル時ペナルティシステム | 低 | 低 | 中 |
| `SFEXT_Logger` | イベントロガー | 低 | 低 | 低 |
| `SFEXT_OutsideOnly` | 車両外限定機能制御 | 低 | 低 | 低 |
| `SFEXT_PassengerOnly` | パッセンジャー限定機能制御 | 低 | 低 | 低 |
| `SFEXT_SeatsOnly` | 特定シート限定機能制御 | 低 | 低 | 低 |
| `SFEXT_WakeTurbulence` | 後方乱気流シミュレーション | 中 | 中 | **高** |
| `SFEXT_Warning` | 警告システム | 低 | 低 | 中 |

### 3.3 Avionics (アビオニクス) - 3スクリプト

| スクリプト名 | 機能概要 | 行数 | 複雑度 | SFV API依存度 |
|------------|---------|------|--------|-------------|
| `GPWS` | 対地接近警報装置 (モード1-6実装、高度コールアウト、バンク角警告) | 358 | **高** | **高** |
| `AuralWarnings` | 音響警報システム | 中 | 中 | 中 |
| `WindIndicator` | 風向風速計 | 低 | 低 | 中 |

### 3.4 Accessories (アクセサリ) - 8スクリプト

| スクリプト名 | 機能概要 | 複雑度 | SFV API依存度 |
|------------|---------|--------|-------------|
| `CatapultController` | カタパルト射出システム (発艦テンション管理、速度制御) | **高** | 中 |
| `DrogueHead` | 空中給油ドローグプローブ | 中 | 低 |
| `GroundWindIndicator` | 地上風向表示 | 低 | 低 |
| `PickupChock` | 車輪止め (Pickup対応) | 低 | 低 |
| `RemoteThrottle` | リモートスロットル制御 | 低 | 中 |
| `WheelDriver` | 車輪駆動制御 | 低 | 中 |
| `WheelLock` | ホイールロックシステム | 低 | 中 |
| `Windsock` | 吹流し (風向表示) | 低 | 低 |

### 3.5 Weather (気象) - 3スクリプト

| スクリプト名 | 機能概要 | 複雑度 | SFV API依存度 |
|------------|---------|--------|-------------|
| `FogController` | 霧/視程制御 | 低 | 低 |
| `RandomWindChanger` | 動的風向風速変化 | 低 | 中 |
| `RVR_Controller` | 滑走路視距離(RVR)制御 | 低 | 低 |

### 3.6 UdonChips連携 - 2スクリプト

| スクリプト名 | 機能概要 | 複雑度 | SFV API依存度 |
|------------|---------|--------|-------------|
| `SFEXT_UdonChips` | UdonChips報酬システム統合 (着陸/キル/環境ボーナス) | 中 | 中 |
| `GetMoneyOnExplode` | 爆発時通貨獲得 | 低 | 低 |

### 3.7 Editor拡張 - 5スクリプト

| スクリプト名 | 機能概要 |
|------------|---------|
| `SaccEntityEditor` | SaccEntity用カスタムインスペクタ (バリデーション、自動入力) |
| `SAV_KeyboardControlsEditor` | キーボード設定エディタ |
| `SAV_PassengerFunctionsControllerEditor` | パッセンジャー機能エディタ |
| `SFGizmoDrawer` | シーンビューギズモ描画 |
| `ESFADebugTools / ESFAMenu / ESFAUI` | デバッグ・メニュー・UI補助 |

---

## 4. SaccFlightAndVehicles 1.8 のアーキテクチャ

### 4.1 コアコンポーネント (全81スクリプト)

```
SaccFlightAndVehicles/
├── Scripts/
│   ├── SaccEntity.cs                    # 車両エンティティ基盤
│   ├── SaccVehicleSeat.cs               # シート管理
│   ├── SaccAirVehicle/
│   │   ├── SaccAirVehicle.cs            # 航空機物理
│   │   ├── SaccSeaVehicle.cs            # 水上機物理
│   │   ├── SAV_Extensions/ (14個)       # 標準エクステンション
│   │   ├── SSV_Extensions/ (1個)        # 水上機エクステンション
│   │   ├── EXT/ (2個)                   # タレット等
│   │   └── Weapons/ (4個)               # 兵装コントローラ
│   ├── SaccGroundVehicle/
│   │   ├── SaccGroundVehicle.cs         # 地上車両物理
│   │   ├── SaccWheel.cs                 # ホイール
│   │   ├── DFUNC/ (7個)                 # 地上車両DFUNC
│   │   └── SGV_Extensions/ (4個)        # 地上車両エクステンション
│   ├── DFUNC/ (21個)                    # 標準ダイヤルファンクション
│   ├── Other/ (15個)                    # ユーティリティ
│   └── SaccFlightEditorScripts/ (1個)
```

### 4.2 エクステンションシステム

SFVのエクステンションシステムは以下のイベントプレフィックスでカテゴリ分けされる:

| プレフィックス | スコープ | 説明 |
|-------------|---------|------|
| `SFEXT_L_` | Local | 全クライアントで実行されるローカルイベント |
| `SFEXT_G_` | Global | 全クライアントに送信されるグローバルイベント |
| `SFEXT_O_` | Owner | オーナー（操縦者）のみで実行されるイベント |
| `SFEXT_P_` | Passenger | パッセンジャーのみで実行されるイベント |
| `DFUNC_` | DFUNC | ダイヤルファンクション固有イベント |

### 4.3 主要イベント一覧 (SFV 1.8)

**ライフサイクルイベント:**
- `SFEXT_L_EntityStart` - エンティティ初期化
- `SFEXT_L_OnEnable` / `SFEXT_L_OnDisable`
- `SFEXT_L_WakeUp` / `SFEXT_L_FallAsleep`
- `SFEXT_L_OwnershipTransfer`

**パイロットイベント:**
- `SFEXT_O_PilotEnter` / `SFEXT_O_PilotExit`
- `SFEXT_G_PilotEnter` / `SFEXT_G_PilotExit`
- `SFEXT_P_PassengerEnter` / `SFEXT_P_PassengerExit`
- `SFEXT_O_TakeOwnership` / `SFEXT_O_LoseOwnership`

**飛行状態イベント:**
- `SFEXT_G_TouchDown` / `SFEXT_G_TakeOff` / `SFEXT_G_TouchDownWater`
- `SFEXT_O_Grounded` / `SFEXT_O_Airborne`
- `SFEXT_G_GearUp` / `SFEXT_G_GearDown`
- `SFEXT_O_FlapsOn` / `SFEXT_O_FlapsOff`

**エンジンイベント:**
- `SFEXT_G_EngineOn` / `SFEXT_G_EngineOff`
- `SFEXT_G_EngineStartup` / `SFEXT_G_EngineStartupCancel`
- `SFEXT_G_AfterburnerOn` / `SFEXT_G_AfterburnerOff`

**ダメージ/リセット:**
- `SFEXT_G_Explode` / `SFEXT_G_ReAppear`
- `SFEXT_G_BulletHit` / `SFEXT_G_MissileHit`
- `SFEXT_G_ReSupply` / `SFEXT_G_ReArm` / `SFEXT_G_ReFuel` / `SFEXT_G_RePair`
- `SFEXT_G_Dead` / `SFEXT_G_NotDead`

**その他:**
- `SFEXT_G_CatapultLockIn` / `SFEXT_G_CatapultLockOff` / `SFEXT_G_LaunchFromCatapult`
- `SFEXT_G_EnterWater` / `SFEXT_G_ExitWater`
- `SFEXT_O_VehicleTeleported`

### 4.4 DFUNCの実装パターン (SFV 1.8)

SFV 1.8のDFUNCは以下のパターンに従う:

```csharp
namespace SaccFlightAndVehicles
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.NoVariableSync)]
    public class DFUNC_Example : UdonSharpBehaviour
    {
        public UdonSharpBehaviour SAVControl;           // SaccAirVehicleへの参照
        public GameObject Dial_Funcon;                  // MFD表示用オブジェクト
        public GameObject[] Dial_Funcon_Array;          // MFD表示用配列

        [System.NonSerialized] public bool LeftDial = false;
        [System.NonSerialized] public int DialPosition = -999;
        [System.NonSerialized] public SaccEntity EntityControl;

        // SaccEntityから自動的に呼ばれるイベント
        public void SFEXT_L_EntityStart() { }
        public void SFEXT_O_PilotEnter() { }
        public void SFEXT_O_PilotExit() { }

        // ダイヤル選択イベント
        public void DFUNC_Selected() { }
        public void DFUNC_Deselected() { }

        // デスクトップ用キー入力
        public void KeyboardInput() { }
    }
}
```

**重要な変数 (SaccEntity からの自動注入):**
- `EntityControl` - SaccEntity への参照（自動設定）
- `LeftDial` - 左右どちらのダイヤルか（自動設定）
- `DialPosition` - ダイヤル上の位置（自動設定）

**SaccAirVehicle の変数アクセス (GetProgramVariable/SetProgramVariable):**
- `ExtraDrag`, `ExtraLift`, `ExtraVelLift` - 追加の空力パラメータ
- `MaxLift` - 最大揚力
- `VelStraightenStrPitch`, `VelStraightenStrYaw` - 速度による直進安定性
- `PitchDown` - ピッチダウン状態

---

## 5. EsnyaSFAddons と SFV 1.8 の互換性分析

### 5.1 非互換項目

#### 5.1.1 SFEXT_AbstractVehicle (最重要)
`SFEXT_AbstractVehicle` は SaccAirVehicle のほぼ完全な代替実装(1400+行)であり、SFV 1.63 の SaccAirVehicle のフィールド構成・物理計算ロジックに強く依存している。SFV 1.8 では SaccAirVehicle 自体に多数の変更・追加があるため、**直接の移植は不可能**と判断される。

**判定:** SF-AdvEquipment では移植対象外とし、SFV 1.8 の SaccAirVehicle をそのまま使用する方針が妥当。

#### 5.1.2 SFV API へのアクセスパターンの変更
EsnyaSFAddons の多くのスクリプト(46ファイル中)が `using SaccFlightAndVehicles` を使用し、`SaccEntity` や `SaccAirVehicle` のフィールドに直接アクセスしている。1.63 → 1.8 間でフィールド名やシグネチャが変更されている可能性がある。

ただし、`GetProgramVariable`/`SetProgramVariable` による間接アクセスの使用は **4ファイル・5箇所のみ** と限定的であり、大部分は直接フィールドアクセスまたはイベントコールバックのみで構成されている。

#### 5.1.3 独自の DFUNC_Base 基底クラス
EsnyaSFAddons は独自の `DFUNC_Base` クラスを定義しているが、SFV 1.8 の標準 DFUNC パターンはこの基底クラスを使用しない。SF-AdvEquipment では SFV 1.8 の標準パターンに合わせる必要がある。

#### 5.1.4 カスタムイベント名
EsnyaSFAddons 独自のイベント:
- `SFEXT_L_BoardingEnter` / `SFEXT_L_BoardingEnxit` (typo含む)

これらは SFV 1.8 の標準イベントには存在しない。

### 5.2 互換性のある項目

以下のイベントは SFV 1.8 でも引き続きサポートされている:

- `SFEXT_L_EntityStart`, `SFEXT_G_PilotEnter/Exit`, `SFEXT_O_PilotEnter/Exit`
- `SFEXT_P_PassengerEnter/Exit`, `SFEXT_G_Explode`, `SFEXT_G_RespawnButton`
- `SFEXT_O_TakeOwnership`, `SFEXT_O_LoseOwnership`
- `SFEXT_G_TouchDown`, `SFEXT_G_TouchDownWater`
- `SFEXT_L_WakeUp`, `SFEXT_L_FallAsleep`
- `DFUNC_Selected`, `DFUNC_Deselected`

---

## 6. 移植優先度の評価

### 6.1 優先度マトリクス

装備品を **実用性** と **移植難易度** の2軸で評価する。

#### 優先度A (高実用性・低〜中難易度) - 優先的に移植

| スクリプト | 理由 |
|-----------|------|
| `DFUNC_AdvancedFlaps` | フラップの多段制御は非常に実用的。SFV 1.8標準は ON/OFF のみで破損判定なし |
| `DFUNC_ElevatorTrim` | トリム制御は基本的かつ重要 |
| `DFUNC_AdvancedParkingBrake` | 地上操作に必須 |
| `DFUNC_AdvancedSpeedBrake` | 降下・着陸に重要 |
| `SFEXT_AuxiliaryPowerUnit` | 独立性が高く移植容易 |
| `SFEXT_EngineFanDriver` | シンプルなアニメーション駆動 |
| `GPWS` | 高い実用性、安全性向上 |
| `SFEXT_Warning` | 汎用的な警告システム |

#### 優先度B (高実用性・高難易度) - 段階的に移植

| スクリプト | 理由 |
|-----------|------|
| `SFEXT_AdvancedEngine` | 非常に高機能だが複雑 |
| `SFEXT_AdvancedGear` | SFV 1.8標準のDFUNC_Gearに破損判定・速度制限が無く、Advanced版の需要が高い |
| `DFUNC_AdvancedThrustReverser` | エンジンシステムとの連携が必要 |
| `DFUNC_AutoStarter` | エンジンシステム依存 |
| `SFEXT_AdvancedPropellerThrust` | 物理計算の精査が必要 |
| `SFEXT_InstrumentsAnimationDriver` | SaccAirVehicle の変数名に依存 |

#### 優先度C (特殊用途) - 必要に応じて移植

| スクリプト | 理由 |
|-----------|------|
| `DFUNC_BuddyPod` | 空中給油は特殊用途 |
| `DFUNC_WingFold` | 空母運用限定 |
| `DFUNC_AdvancedCatapult` | 空母運用限定 |
| `CatapultController` | 空母運用限定 |
| `SFEXT_WakeTurbulence` | 高度なシミュレーション |
| `SFEXT_DihedralEffect` | 空力の補正 |
| `SFEXT_FloodedEngineKiller` | 水陸両用機限定 |
| `DFUNC_AdvancedWaterRudder` | 水陸両用機限定 |

#### 優先度D (移植対象外)

| スクリプト | 理由 |
|-----------|------|
| `SFEXT_AbstractVehicle` | SaccAirVehicle の完全代替であり、1.8 では不要/非現実的 |
| `ESFASceneSetup` | EsnyaSFAddons 固有のシーン設定 |
| UdonChips連携 | 外部依存が多く、別パッケージとして独立管理が妥当 |

### 6.2 移植不要 (SFV 1.8 に同等機能が存在)

以下の機能は SFV 1.8 に標準で含まれているため、再実装の必要はない:

| EsnyaSFAddons | SFV 1.8 同等機能 | 備考 |
|--------------|-----------------|------|
| `SFEXT_AbstractVehicle` の基本飛行物理 | `SaccAirVehicle` | SFV 1.8 標準を直接使用 |
| カタパルト制御 | `DFUNC_Catapult` | 標準で十分 |
| フック制御 | `DFUNC_Hook` | 標準で十分 |
| `SFEXT_KillPenalty` の基本的なキル追跡 | `SAV_KillTracker` | 標準で十分 |
| `DFUNC_LandingLight` | `DFUNC_ToggleBool` | ToggleObjects にライトを設定するだけで完全代用可能 |
| `DFUNC_Empty` | (不要) | ダイヤルに未登録とすることで代用 |
| `DFUNC_Slider` | `DFUNC_SlideAnimation` | SFV 1.8 標準の float スライダーで代用可能 |
| `DFUNC_SendCustomEvent` | `DFUNC_ToggleBool` (部分的) | 単純な ON/OFF イベントは ToggleBool で代用可能。任意イベント送信は不可だが用途が限定的 |

> **注記: `DFUNC_ToggleBool` (SFV 1.8 標準) の汎用性**
>
> SFV 1.8 の `DFUNC_ToggleBool` は非常に高機能な汎用トグルスクリプトであり、以下を単一スクリプトでカバーする:
> - GameObject の ON/OFF (`ToggleObjects[]` / `ToggleObjects_Off[]`)
> - Animator Bool パラメータ制御
> - ParticleSystem emission 制御
> - 状態条件による制限 (飛行中/地上/水上/AB中/エンジン状態)
> - マスター/スレーブ連動、ドア開閉連携、ネットワーク同期
>
> EsnyaSFAddons で個別スクリプトとして実装されていた単純なオブジェクト切り替え系の機能は、この `DFUNC_ToggleBool` で広くカバーされる。

### 6.3 SFV 1.8 標準は基本機能のみ - Advanced版の再実装が必要

以下の機能は SFV 1.8 に標準実装が存在するが、**破損モデル・多段制御・速度制限といったリアリズム要素が欠落**しているため、SF-AdvEquipment で Advanced 版を改めて実装する:

| SFV 1.8 標準 | 標準の制限事項 | SF-AdvEquipment で実装する機能 |
|-------------|--------------|---------------------------|
| `DFUNC_Flaps` (ON/OFF のみ) | 多段制御なし、速度制限なし、破損判定なし | `DFUNC_AdvancedFlaps`: 9段デテント制御、各段速度制限、速度超過時アクチュエータ/翼破損、ハプティクス |
| `DFUNC_Gear` (UP/DOWN のみ) | 速度制限なし、タイヤバースト無し、故障判定なし | `SFEXT_AdvancedGear`: 展開/格納/展開中の速度制限、タイヤバースト、MTBF確率的故障、サスペンション制御 |

---

## 7. SFV 1.8 の新機能・変更点 (移植時の考慮事項)

### 7.1 SaccEntity の主要変更

SFV 1.8 の `SaccEntity` には以下の注目すべきフィールド/機能が確認された:

- `ExtensionUdonBehaviours[]` - エクステンション登録配列 (従来通り)
- `Dial_Functions_L[]` / `Dial_Functions_R[]` - 左右ダイヤル登録 (従来通り)
- `PassengerFunctionControllers[]` - パッセンジャー機能コントローラ配列
- `AAMTargetsLayer` / `AAMTargets[]` - ターゲティングシステム
- `ArmorStrength` / `NoDamageBelow` - ダメージモデル
- `ExternalSeats[]` - 外部シートサポート
- `EnableInteract` / `CustomPickup` 系 - インタラクト/ピックアップシステム
- `SendEventToExtensions()` - イベント送信メソッド (従来通り)

### 7.2 SaccAirVehicle の主要変更

SFV 1.8 で追加・変更されたと思われるフィールド:
- `Dial_Funcon_Array[]` - DFUNC 表示用配列 (1.63 では単体の `Dial_Funcon` のみだった可能性)
- `ExtraLift`, `ExtraDrag`, `ExtraVelLift` - 外部からの空力パラメータ追加機構
- `MaxLift` - 最大揚力
- `GroundDetectorLayers` / `GroundDetectorRayDistance` - 地面検出の細かい設定
- `ThrottleAfterburnerPoint` - アフターバーナー閾値

---

## 8. SF-AdvEquipment の設計方針 (提案)

### 8.1 パッケージ構造

```
SF-AdvEquipment/
├── Packages/
│   └── com.sakuha.sf-advequipment/
│       ├── Runtime/
│       │   ├── DFUNC/          # ダイヤルファンクション
│       │   ├── SFEXT/          # システムエクステンション
│       │   ├── Avionics/       # アビオニクス
│       │   ├── Accessories/    # アクセサリ
│       │   └── Weather/        # 気象
│       ├── Editor/             # エディタ拡張
│       ├── Prefabs/            # プレハブ
│       └── package.json
├── README.md
└── docs/
```

### 8.2 設計原則

1. **SFV 1.8 の標準パターンに準拠** - 独自の基底クラスは作らず、SFV 1.8 の DFUNC/SFEXT パターンをそのまま踏襲
2. **名前空間の統一** - `SFAdvEquipment.DFUNC`, `SFAdvEquipment.SFEXT` 等
3. **EsnyaSFAddons の独自DFUNC_Base を廃止** - SFV 1.8 標準の DFUNC パターン(基底クラスなし、直接 UdonSharpBehaviour 継承)に変更
4. **SaccAirVehicle 直接参照** - `UdonSharpBehaviour SAVControl` として参照し、`GetProgramVariable`/`SetProgramVariable` は最小限に
5. **外部依存の最小化** - InariUdon、UdonToolkit への依存を排除または Optional 化

### 8.3 移植時の技術的課題

| 課題 | 対策 |
|------|------|
| DFUNC_Base 廃止に伴う VR トリガー入力処理 | 各 DFUNC に SFV 1.8 標準のトリガー処理を実装 |
| SaccAirVehicle のフィールド名変更 | 1.8 の SaccAirVehicle を精査し、マッピング表を作成 |
| SFEXT_AdvancedEngine の N1/N2 モデル | SFV 1.8 のエンジンイベント体系に合わせて再設計 |
| SFEXT_AdvancedGear と DFUNC_Gear の競合 | SFV 1.8 の DFUNC_Gear を拡張する形式に変更 |
| ネットワーク同期モードの確認 | 各スクリプトの同期方式を SFV 1.8 に合わせて再評価 |

---

## 9. まとめ

EsnyaSFAddons は 54 個のスクリプトからなる大規模なアドオンであり、SFV 1.63 向けに精巧な航空機システムを実装している。SF-AdvEquipment としての移植にあたっては:

1. **SFEXT_AbstractVehicle は移植対象外** - SFV 1.8 の SaccAirVehicle を直接使用
2. **SFV 1.8 標準で代用可能なものは移植しない** - `DFUNC_LandingLight` → `DFUNC_ToggleBool`、`DFUNC_Slider` → `DFUNC_SlideAnimation`、`DFUNC_Empty` → 不要 等
3. **SFV 1.8 標準に不足するリアリズム要素は再実装** - `DFUNC_AdvancedFlaps` (多段制御・破損)、`SFEXT_AdvancedGear` (速度制限・タイヤバースト・故障) 等
4. **優先度A の 8 スクリプトから着手** - フラップ、トリム、GPWS 等の実用的な装備
5. **SFV 1.8 の標準パターンに完全準拠** - 独自基底クラスの廃止、標準イベント体系の使用
6. **段階的リリース** - 優先度A → B → C の順で段階的に実装

移植対象となるスクリプトは約 **35 個** (AbstractVehicle、SceneSetup、UdonChips 連携、SFV 1.8 標準で代用可能なもの等を除く) であり、SFV 1.8 のイベントシステムとの互換性は概ね良好であるため、個別のAPI差異を修正すれば移植は十分に実現可能である。
