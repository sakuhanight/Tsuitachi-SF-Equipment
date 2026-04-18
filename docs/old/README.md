# Tsuitachi-SF-Equipment ドキュメント

## 概要

**Tsuitachi-SF-Equipment (TSFE)** は、SaccFlightAndVehicles 1.8 (SFV) 向けの高度な装備システムを提供する Unity パッケージです。VRChat ワールド向けに、フラップ、ランディングギア、エンジン、アビオニクス、コックピット計器などのリアルな航空機システムを実装します。

- **パッケージ名**: `net.tsuitachi.sf-equipment`
- **Unity バージョン**: 2022.3+
- **依存関係**: VRChat Worlds SDK 3.7.0+, SaccFlightAndVehicles 1.8.0+, UdonSharp 1.x
- **名前空間**: `TSFE`
- **ライセンス**: MIT

## 機能

### 飛行制御システム
- **DFUNC_AdvancedFlaps** - 多段デテントフラップ（速度制限、超過速度損傷、MTBF 故障モデリング）
- **DFUNC_ElevatorTrim** - エレベータトリム（荷重倍数リミッター付き）
- **DFUNC_AdvancedSpeedBrake** - スピードブレーキ（展開制限付き）
- **DFUNC_AdvancedParkingBrake** - パーキングブレーキシステム
- **DFUNC_AdvancedWaterRudder** - 水上ラダー（水上機用）

### 推進システム
- **SFEXT_AdvancedEngine** - ターボファンシミュレーション（デュアルスプール N1/N2、EGT/ECT 温度、始動シーケンス、逆推力）
- **SFEXT_AdvancedPropellerThrust** - プロペラ推力モデリング
- **DFUNC_AdvancedThrustReverser** - 逆推力装置（AdvancedEngine 用）
- **DFUNC_ThrustReverser** - 標準逆推力装置

### ランディングギア
- **SFEXT_AdvancedGear** - 高度なランディングギア（損傷モデリング付き）

### 補助システム
- **SFEXT_AuxiliaryPowerUnit** - APU（始動/停止シーケンス付き）
- **SFEXT_AutoStarter** - 自動エンジン始動シーケンス（バッテリー → APU → エンジン → APU 停止）
- **SFEXT_EngineToggle** - エンジン ON/OFF トグル（AutoStarter 使用）

### アビオニクス
- **GPWS** - 対地接近警報装置（6 モード地形/高度警報）
- **AuralWarnings** - オーラル警報システム（設定可能なサウンド）
- **SFEXT_InstrumentsAnimationDriver** - 10 個のアナログ計器駆動（ADI、HI、ASI、高度計など）

### 視覚効果
- **SFEXT_EngineFanDriver** - エンジンファン回転アニメーション
- **SFEXT_WakeTurbulence** - 後方乱気流生成
- **SFEXT_DihedralEffect** - 上反角効果シミュレーション

### ユーティリティシステム
- **TSFE_PowerBus** - 電力配電（バッテリー、APU、発電機）
- **TSFE_BleedAirBus** - ブリード空気配給
- **TSFE_HydraulicBus** - 油圧システム
- **TSFE_HydraulicPump** - 油圧ポンプ
- **TSFE_ParameterTransform** - Transform パラメータマッピング
- **TSFE_ParameterText** - テキストパラメータ表示
- **DFUNC_MethodCaller** - 汎用メソッド呼び出し（DFUNC 統合用）

### その他ユーティリティ
- **SFEXT_BoardingCollider** - 搭乗エリアコライダー
- **SFEXT_OutsideOnly** - 外部専用オブジェクト
- **SFEXT_PassengerOnly** - 乗客専用オブジェクト
- **SFEXT_SeatsOnly** - 座席専用オブジェクト
- **SFEXT_Warning** - 汎用警報システム
- **PickupChock** - 車輪止めピックアップオブジェクト

## ドキュメント構成

- **[README.md](README.md)** - このファイル
- **[architecture.md](architecture.md)** - システムアーキテクチャと設計パターン
- **[API_REFERENCE.md](API_REFERENCE.md)** - 全コンポーネントの完全な API リファレンス（予定）
- **[SETUP_GUIDE.md](SETUP_GUIDE.md)** - セットアップと設定ガイド（予定）
- **[COMPONENTS/](COMPONENTS/)** - 詳細なコンポーネントドキュメント（予定）
  - [DFUNC.md](COMPONENTS/DFUNC.md) - ダイアル機能コンポーネント
  - [SFEXT.md](COMPONENTS/SFEXT.md) - SaccEntity 拡張
  - [Avionics.md](COMPONENTS/Avionics.md) - アビオニクスシステム
  - [Utilities.md](COMPONENTS/Utilities.md) - ユーティリティコンポーネント

## クイックスタート

1. SaccFlightAndVehicles 1.8.0+ と VRChat SDK をインポート
2. Tsuitachi-SF-Equipment パッケージをインポート
3. SaccEntity に必要な SFEXT/DFUNC コンポーネントを追加
4. Unity Inspector でコンポーネントパラメータを設定
5. 詳細なセットアップ手順は [SETUP_GUIDE.md](SETUP_GUIDE.md) を参照（予定）

## サポート

問題や機能リクエストは GitHub の issue トラッカーをご利用ください。

## ライセンス

MIT ライセンス - 詳細は LICENSE ファイルを参照してください。
