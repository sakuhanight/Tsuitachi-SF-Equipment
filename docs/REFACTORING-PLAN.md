# TSFE リファクタリング計画

**現状分析とパターン適用によるコード品質向上計画**

作成日: 2026-04-11

---

## 目次

- [現状分析](#現状分析)
- [検出された問題](#検出された問題)
- [リファクタリング優先順位](#リファクタリング優先順位)
- [Phase 1: DFUNC共通化](#phase-1-dfunc共通化)
- [Phase 2: SFEXT共通化](#phase-2-sfext共通化)
- [Phase 3: ユーティリティ抽出](#phase-3-ユーティリティ抽出)
- [Phase 4: ドキュメント整備](#phase-4-ドキュメント整備)
- [実施スケジュール](#実施スケジュール)

---

## 現状分析

### コンポーネント統計

| カテゴリ | 総数 | PATTERNS.md準拠 | 要リファクタ |
|---------|------|----------------|------------|
| DFUNC | 8 | 3 (38%) | 5 (62%) |
| SFEXT | 18 | 12 (67%) | 6 (33%) |
| Avionics | 2 | 2 (100%) | 0 (0%) |
| Utility | 9 | 9 (100%) | 0 (0%) |
| **合計** | **37** | **26 (70%)** | **11 (30%)** |

### GetProgramVariable使用状況

- 総呼び出し回数: **116回** (25ファイル)
- キャッシュ可能: 推定40-50回（初期化時に取得して保持すべき変数）

---

## 検出された問題

### 1. DFUNC共通パターン違反

#### 問題A: 必須メソッドの不統一実装

**影響コンポーネント:**
- `DFUNC_AdvancedWaterRudder` - DFUNC_LeftDial/RightDial未実装
- `DFUNC_AdvancedSpeedBrake` - DFUNC_Selected()でtrackingTarget保険設定なし

**問題の影響:**
- VR使用時に手の追跡が正しく動作しない可能性
- 一部のSaccFlightコールバックが機能しない

**修正方針:**
```csharp
// 標準パターンに統一
public void DFUNC_LeftDial()
{
    trackingTarget = VRCPlayerApi.TrackingDataType.LeftHand;
}

public void DFUNC_RightDial()
{
    trackingTarget = VRCPlayerApi.TrackingDataType.RightHand;
}

public void DFUNC_Selected()
{
    selected = true;

    // 保険設定（DFUNC_LeftDial/RightDialが呼ばれない場合）
    trackingTarget = LeftDial
        ? VRCPlayerApi.TrackingDataType.LeftHand
        : VRCPlayerApi.TrackingDataType.RightHand;

    // Ownership取得
    if (!isOwner)
    {
        Networking.SetOwner(Networking.LocalPlayer, gameObject);
    }
}
```

#### 問題B: 状態変数の命名不統一

**不統一な変数名:**
- `isPilot` vs `piloting` vs 未使用
- `isSelected` vs `selected`
- `isOwner` vs `Networking.IsOwner()`直接使用

**修正方針:**
すべてのDFUNCで以下の標準状態変数を使用：
```csharp
private bool isPilot;      // ローカルプレイヤーがパイロット
private bool isOwner;      // ローカルプレイヤーがOwner
private bool selected;     // ダイヤル選択中
private bool hasPilot;     // 誰かがパイロット中（グローバル）
```

#### 問題C: ライフサイクルイベントの実装漏れ

**パターン:**
```csharp
public void SFEXT_O_TakeOwnership() { isOwner = true; }
public void SFEXT_O_LoseOwnership() { isOwner = false; }
```

**未実装コンポーネント:**
- DFUNC_AdvancedSpeedBrake
- DFUNC_AdvancedWaterRudder

#### 問題D: ResetStatusメソッドの命名不統一

**現状:**
- `ResetStatus()` - DFUNC_AdvancedFlaps, DFUNC_ElevatorTrim, DFUNC_AdvancedParkingBrake
- `SFEXT_G_ReAppear()` - DFUNC_AdvancedSpeedBrake
- `SFEXT_G_Reappear()` - DFUNC_AdvancedWaterRudder

**修正方針:**
すべて`ResetStatus()`に統一し、以下から呼び出す：
```csharp
public void SFEXT_G_Explode() { ResetStatus(); }
public void SFEXT_G_RespawnButton() { ResetStatus(); }
```

---

### 2. SFEXT共通パターン違反

#### 問題A: SAVControl参照のキャッシュ不足

**頻繁に使用される変数の例:**
```csharp
// 毎フレーム呼び出し（非効率）
float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
Vector3 airVel = (Vector3)SAVControl.GetProgramVariable("AirVel");
```

**修正方針:**
初期化時にキャッシュできるものは保持：
```csharp
// SFEXT_L_EntityStartで1回だけ取得
vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
controlsRoot = (Transform)SAVControl.GetProgramVariable("ControlsRoot");
```

#### 問題B: 状態管理の不統一

**修正方針:**
すべてのSFEXTで以下の標準状態変数を使用：
```csharp
private bool initialized;
private bool isPilot;
private bool isPassenger;
private bool isOwner;
private Animator vehicleAnimator;
private Rigidbody vehicleRigidbody;
```

---

### 3. Tooltipの不足

**現状:**
- 37コンポーネント中、Tooltipが不十分なpublicフィールドが多数存在
- 特にDFUNC系コンポーネントで顕著

**修正方針:**
すべてのpublicフィールドに日本語Tooltipを追加：
```csharp
[Tooltip("VRコントローラーの感度（1デテント移動に必要な距離、メートル）")]
public float controllerSensitivity = 0.02f;
```

---

## リファクタリング優先順位

### 優先度A（高）: 機能に影響する問題

1. **DFUNC_AdvancedWaterRudder** - DFUNC_LeftDial/RightDial未実装
2. **DFUNC_AdvancedSpeedBrake** - DFUNC_Selected()の保険設定なし
3. **Ownership管理の不統一** - 全DFUNCで`isOwner`変数を使用

### 優先度B（中）: 保守性に影響する問題

4. **状態変数の命名統一** - isPilot, selected等の標準化
5. **ResetStatusメソッドの統一** - 命名とイベントハンドラ
6. **ライフサイクルイベントの実装漏れ** - SFEXT_O_TakeOwnership等

### 優先度C（低）: ドキュメント整備

7. **Tooltip追加** - すべてのpublicフィールド
8. **XMLドキュメントコメント** - public API

---

## Phase 1: DFUNC共通化

### 対象コンポーネント (5個)

1. ✅ DFUNC_AdvancedFlaps - 既に準拠
2. ✅ DFUNC_ElevatorTrim - 既に準拠
3. ❌ DFUNC_AdvancedSpeedBrake - 要修正
4. ❌ DFUNC_AdvancedWaterRudder - 要修正
5. ✅ DFUNC_AdvancedParkingBrake - 既に準拠
6. ❌ DFUNC_ThrustReverser - 要修正（軽微）
7. ❌ DFUNC_AdvancedThrustReverser - 要修正（軽微）
8. ❌ DFUNC_MethodCaller - 要修正（軽微）

### 修正項目チェックリスト

各DFUNCコンポーネントで以下を確認・修正：

- [ ] **必須フィールド**
  ```csharp
  public UdonSharpBehaviour SAVControl;
  public GameObject Dial_Funcon;
  public GameObject[] Dial_Funcon_Array;

  [System.NonSerialized] public bool LeftDial = false;
  [System.NonSerialized] public int DialPosition = -999;
  [System.NonSerialized] public SaccEntity EntityControl;
  ```

- [ ] **標準状態変数**
  ```csharp
  private bool isPilot, isOwner, selected, hasPilot;
  private VRCPlayerApi.TrackingDataType trackingTarget;
  private Transform controlsRoot;
  private Animator vehicleAnimator;
  ```

- [ ] **必須メソッド実装**
  ```csharp
  public void DFUNC_LeftDial() { ... }
  public void DFUNC_RightDial() { ... }
  public void DFUNC_Selected() { ... }
  public void DFUNC_Deselected() { ... }
  ```

- [ ] **ライフサイクルイベント**
  ```csharp
  public void SFEXT_L_EntityStart() { ... }
  public void SFEXT_O_PilotEnter() { isPilot = true; isOwner = true; selected = false; }
  public void SFEXT_O_PilotExit() { isPilot = false; }
  public void SFEXT_O_TakeOwnership() { isOwner = true; }
  public void SFEXT_O_LoseOwnership() { isOwner = false; }
  public void SFEXT_G_PilotEnter() { hasPilot = true; gameObject.SetActive(true); }
  public void SFEXT_G_PilotExit() { hasPilot = false; }
  public void SFEXT_G_Explode() { ResetStatus(); }
  public void SFEXT_G_RespawnButton() { ResetStatus(); }
  ```

- [ ] **ResetStatusメソッド**
  ```csharp
  private void ResetStatus()
  {
      // 状態リセット処理
  }
  ```

### 実装順序

1. **DFUNC_AdvancedWaterRudder** (優先度A)
2. **DFUNC_AdvancedSpeedBrake** (優先度A)
3. **DFUNC_ThrustReverser** (優先度B)
4. **DFUNC_AdvancedThrustReverser** (優先度B)
5. **DFUNC_MethodCaller** (優先度B)

---

## Phase 2: SFEXT共通化

### 対象コンポーネント (6個)

主要な要修正コンポーネント：

1. SFEXT_AdvancedEngine - SAVControl参照のキャッシュ最適化
2. SFEXT_AdvancedGear - 状態変数の統一
3. SFEXT_AutoStarter - ライフサイクルイベントの追加
4. SFEXT_Warning - 軽微な調整
5. SFEXT_EngineFanDriver - 軽微な調整
6. SFEXT_Chock - 軽微な調整

### 修正項目チェックリスト

- [ ] **標準状態変数**
  ```csharp
  private bool initialized;
  private bool isPilot, isPassenger, isOwner;
  private Animator vehicleAnimator;
  private Rigidbody vehicleRigidbody;
  ```

- [ ] **SAVControl参照のキャッシュ**
  ```csharp
  public void SFEXT_L_EntityStart()
  {
      vehicleAnimator = (Animator)SAVControl.GetProgramVariable("VehicleAnimator");
      vehicleRigidbody = (Rigidbody)SAVControl.GetProgramVariable("VehicleRigidbody");
      // 他の頻繁に使う変数...

      initialized = true;
  }
  ```

- [ ] **ライフサイクルイベント**
  ```csharp
  public void SFEXT_O_PilotEnter() { isPilot = true; }
  public void SFEXT_O_PilotExit() { isPilot = false; }
  public void SFEXT_O_PassengerEnter() { isPassenger = true; }
  public void SFEXT_O_PassengerExit() { isPassenger = false; }
  public void SFEXT_G_Explode() { ResetStatus(); }
  public void SFEXT_G_RespawnButton() { ResetStatus(); }
  ```

- [ ] **ResetStatusメソッド**
  ```csharp
  private void ResetStatus()
  {
      // 状態リセット処理
  }
  ```

---

## Phase 3: ユーティリティ抽出

### TSFEUtilへの追加候補

#### 1. サウンド管理ヘルパー

現状、複数のコンポーネントで重複実装：

```csharp
// 初期化時の音量キャッシュ（重複コード）
audioVolumes = new float[audioSources.Length];
audioPitches = new float[audioSources.Length];
for (var i = 0; i < audioSources.Length; i++)
{
    var src = audioSources[i];
    if (!src) continue;
    audioVolumes[i] = src.volume;
    audioPitches[i] = src.pitch;
}
```

**提案:**
```csharp
// TSFEUtil.cs
public static void CacheAudioProperties(AudioSource[] sources, out float[] volumes, out float[] pitches)
{
    volumes = new float[sources.Length];
    pitches = new float[sources.Length];
    for (int i = 0; i < sources.Length; i++)
    {
        if (!sources[i]) continue;
        volumes[i] = sources[i].volume;
        pitches[i] = sources[i].pitch;
    }
}
```

#### 2. Animator安全呼び出し

現状パターン：
```csharp
if (vehicleAnimator) vehicleAnimator.SetFloat("param", value);
if (vehicleAnimator) vehicleAnimator.SetBool("param", value);
```

**提案:**
```csharp
// TSFEUtil.cs
public static void SetAnimatorFloat(Animator animator, string paramName, float value)
{
    if (animator) animator.SetFloat(paramName, value);
}

public static void SetAnimatorBool(Animator animator, string paramName, bool value)
{
    if (animator) animator.SetBool(paramName, value);
}
```

**採用判断:** 現状のパターンで十分簡潔なため、優先度は低い。

#### 3. キャノピー連動音量制御

SFEXT_AdvancedEngine、SFEXT_AuxiliaryPowerUnitで実装済みの共通ロジック：

```csharp
// 重複コード
float canopyFactor = 1f;
if (enableCanopyAttenuation && vehicleAnimator != null)
{
    bool canopyOpen = vehicleAnimator.GetBool(canopyParameterName);
    if (invertCanopyParameter) canopyOpen = !canopyOpen;
    canopyFactor = canopyOpen ? 1f : canopyClosedVolumeMultiplier;
}
```

**提案:**
```csharp
// TSFEUtil.cs
public static float GetCanopyVolumeMultiplier(
    Animator animator,
    string paramName,
    bool invert,
    float closedMultiplier)
{
    if (!animator) return 1f;

    bool canopyOpen = animator.GetBool(paramName);
    if (invert) canopyOpen = !canopyOpen;

    return canopyOpen ? 1f : closedMultiplier;
}
```

**採用判断:** 有用。Phase 3で実装。

---

## Phase 4: ドキュメント整備

### 1. Tooltip追加

**基準:**
- すべてのpublicフィールドに日本語Tooltipを追加
- 単位がある場合は明記（KIAS, m/s, 秒, メートル等）
- デフォルト値の意味を説明

**例:**
```csharp
[Tooltip("VRコントローラーの感度（1デテント移動に必要な距離、メートル）")]
public float controllerSensitivity = 0.02f;

[Tooltip("速度制限 (KIAS) - 各デテント位置での最大許容速度")]
public float[] speedLimits = { 340, 250, 210, 175, 162 };
```

### 2. XMLドキュメントコメント

public APIに追加：

```csharp
/// <summary>
/// フラップの現在の角度（度）を取得します。
/// </summary>
public float CurrentAngle => currentAngle;

/// <summary>
/// フラップのデテント位置を変更します。
/// </summary>
/// <param name="index">デテントインデックス（0-based）</param>
public void SetDetent(int index) { ... }
```

---

## 実施スケジュール

### Phase 1: DFUNC共通化（推定: 2-3時間）

1. ✅ リファクタリング計画作成（完了）
2. DFUNC_AdvancedWaterRudder修正（30分）
3. DFUNC_AdvancedSpeedBrake修正（30分）
4. DFUNC_ThrustReverser修正（20分）
5. DFUNC_AdvancedThrustReverser修正（20分）
6. DFUNC_MethodCaller修正（20分）
7. テスト・動作確認（30分）
8. コミット（10分）

### Phase 2: SFEXT共通化（推定: 2-3時間）

1. SFEXT_AdvancedEngine修正（30分）
2. SFEXT_AdvancedGear修正（30分）
3. SFEXT_AutoStarter修正（20分）
4. その他SFEXT修正（40分）
5. テスト・動作確認（30分）
6. コミット（10分）

### Phase 3: ユーティリティ抽出（推定: 1-2時間）

1. TSFEUtilにキャノピー音量制御追加（20分）
2. 既存コンポーネントを新ユーティリティ使用に移行（40分）
3. テスト・動作確認（20分）
4. コミット（10分）

### Phase 4: ドキュメント整備（推定: 2-3時間）

1. 全publicフィールドへのTooltip追加（90分）
2. XMLドキュメントコメント追加（60分）
3. コミット（10分）

**総推定時間: 7-11時間**

---

## リスク管理

### リスクA: 動作不良の発生

**対策:**
- 各Phase完了後に動作確認
- git commitを細かく分割（機能単位）
- 問題発生時は即座にrevert可能

### リスクB: 既存設定との非互換

**対策:**
- publicフィールドの型・名前は変更しない
- 新しいフィールド追加時はデフォルト値で後方互換を保つ

### リスクC: VRChatワールドへの影響

**対策:**
- Prefabの更新は慎重に行う
- Sample/以下のPrefabで動作確認

---

## 成功基準

### Phase 1完了基準

- [ ] すべてのDFUNCがPATTERNS.mdの標準パターンに準拠
- [ ] 命名規則が統一されている
- [ ] ビルドエラー・実行エラーが無い

### Phase 2完了基準

- [ ] すべてのSFEXTがPATTERNS.mdの標準パターンに準拠
- [ ] SAVControl参照の最適化完了
- [ ] ビルドエラー・実行エラーが無い

### Phase 3完了基準

- [ ] 共通処理がTSFEUtilに集約
- [ ] 重複コードが削減
- [ ] ビルドエラー・実行エラーが無い

### Phase 4完了基準

- [ ] すべてのpublicフィールドにTooltipあり
- [ ] public APIにXMLドキュメントコメントあり
- [ ] ドキュメントが最新状態

---

## 次のステップ

1. ✅ この計画書をレビュー
2. Phase 1の実施開始
3. 各Phase完了時にコミット
4. 全Phase完了後、COMPONENTS.md/PATTERNS.mdを更新
