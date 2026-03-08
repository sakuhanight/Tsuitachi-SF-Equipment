# SFEXT_AdvancedEngine - エンジン状態管理・Windmilling実装計画

## 背景と目的

### 課題
- 現在のエンジン実装はBool管理（`engineOn`, `meltdown`）で状態が不明瞭
- 飛行中のエンジン停止時の物理挙動（Windmilling）が未実装
- スターター方式が固定（N2軸のみ）で、T-4等のN1軸スターター機に対応できない

### 目標
1. **状態管理の明確化**: Enum導入で5つの状態を明確に定義
2. **Windmilling実装**: 飛行中エンジン停止時のN1/N2回転と抗力を再現
3. **汎用スターター対応**: N1/N2両方のスターター方式に対応（737, Citation, T-4等）

---

## 1. EngineState Enum設計

### 定義
```csharp
public enum EngineState
{
    Off = 0,         // 完全停止（地上、N1/N2 = 0）
    Windmilling = 1, // 風車回転（飛行中、意図的停止後）
    Starting = 2,    // 始動中（スターター稼働）
    Running = 3,     // 正常運転中
    Seized = 4       // 破損・固着（MTBF/バードストライク等）
}
```

### UdonSync対応
```csharp
[UdonSynced] private int _engineStateInt = 0;
private EngineState engineState
{
    get => (EngineState)_engineStateInt;
    set { _engineStateInt = (int)value; }
}
```

---

## 2. スターター方式の汎用対応

### n1Start Bool
- **false（デフォルト）**: N2軸スターター（CFM56, 737, Citation等）
- **true**: N1軸スターター（F3-IHI-30/T-4等）

### 駆動関係マトリックス

| 状態 | n1Start=false | n1Start=true |
|------|--------------|--------------|
| **Starting** | N2（スターター）→ N1 | **N1（スターター）→ N2** |
| **Windmilling** | N1（空気）→ N2 | N1（空気）→ N2 |
| **Running** | N2（燃焼）→ N1 | N2（燃焼）→ N1 |
| **Off/Seized** | 独立減衰 | 独立減衰 |

---

## 3. Windmilling物理モデル

### N1（ファン）挙動
- **駆動要因**: 空気速度（AirSpeed）
- **計算式**:
  ```
  targetN1 = takeOffN1 * (airSpeed / windmillingReferenceSpeed)
             * windmillingN1Ratio * atmosphere
  ```
- **制限**: 最大20% N1

### N2（高圧タービン）挙動
- **駆動要因**: N1との機械的結合
- **計算式**:
  ```
  targetN2 = N1 * windmillingN2toN1Ratio
  ```
- **特徴**: 燃焼なしのため機械損失が大きい（通常比の0.5-0.7倍）

### 抗力計算
```csharp
float speedRatio = airSpeed / windmillingDragReferenceSpeed;
float drag = windmillingDragCoefficient * (speedRatio^2) * atmosphere;
```

**適用先**: `SAVControl.ExtraDrag`（累積方式）

---

## 4. 状態遷移図

```
Off
  → [starter=true] → Starting

Starting (N2軸)
  → [fuel && N2 >= minN2ForIgnition] → Running

Starting (N1軸)
  → [N1 >= autoIgnitionN1Threshold] → 自動燃料投入
  → [fuel && N2 >= minN2ForIgnition] → Running

Running
  → [fuel=false && airSpeed > minimumWindmillingSpeed] → Windmilling
  → [fuel=false && airSpeed <= minimumWindmillingSpeed] → Off
  → [meltdown] → Seized

Windmilling
  → [airSpeed < minimumWindmillingSpeed && N1 < 1%] → Off
  → [starter=true] → Starting（再始動可能）

Seized
  → （固定、Respawn/Explodeまで）
```

---

## 5. N1/N2更新ロジック

### 実装構造
```csharp
void UpdateN1N2(float dt)
{
    switch (engineState)
    {
        case EngineState.Off:
            N1 = N2 = 0f;
            break;

        case EngineState.Starting:
            if (n1Start)
            {
                UpdateN1FromStarter(dt);      // N1駆動
                UpdateN2FromN1Starting(dt);   // N2追従
            }
            else
            {
                UpdateN2FromStarter(dt);      // N2駆動
                UpdateN1FromN2Starting(dt);   // N1追従
            }
            break;

        case EngineState.Windmilling:
            UpdateN1FromAirSpeed(dt);         // N1駆動
            UpdateN2FromN1Windmill(dt);       // N2追従
            break;

        case EngineState.Running:
            UpdateN2FromThrottle(dt);         // N2駆動（燃焼）
            UpdateN1FromN2(dt);               // N1追従
            break;

        case EngineState.Seized:
            UpdateN1Decay(dt);                // 急速減衰
            UpdateN2Decay(dt);
            break;
    }
}
```

### 各メソッド

#### N1軸スターター用（新規）
```csharp
void UpdateN1FromStarter(float dt)
{
    float targetN1 = takeOffN1 * starterTargetN1Ratio;
    N1 = Mathf.MoveTowards(N1, targetN1, n1StartupResponse * Mathf.Abs(N1 - targetN1) * dt);
}

void UpdateN2FromN1Starting(float dt)
{
    float targetN2 = N1 * startingN2toN1Ratio;
    N2 = Mathf.MoveTowards(N2, targetN2, n2StartupResponse * Mathf.Abs(N2 - targetN2) * dt);
}
```

#### N2軸スターター用（既存ロジック）
```csharp
void UpdateN2FromStarter(float dt)
{
    bool effectiveFuel = fuel && !fireHandlePulled;
    float target = effectiveFuel ? idleN2 : takeOffN2 * starterTargetN2Ratio;
    float resp = effectiveFuel ? n2Response : n2StartupResponse;
    N2 = Mathf.MoveTowards(N2, target, resp * Mathf.Abs(target - N2) * dt);
}

void UpdateN1FromN2Starting(float dt)
{
    float targetN1 = TSFEUtil.ClampedRemap(N2, 0f, idleN2, 0f, idleN1);
    N1 = Mathf.MoveTowards(N1, targetN1, n1StartupResponse * Mathf.Abs(N1 - targetN1) * dt);
}
```

#### Windmilling用（新規）
```csharp
void UpdateN1FromAirSpeed(float dt)
{
    float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
    float atmosphere = (float)SAVControl.GetProgramVariable("Atmosphere");

    float targetN1 = takeOffN1 * (airSpeed / windmillingReferenceSpeed)
                     * windmillingN1Ratio * atmosphere;
    targetN1 = Mathf.Clamp(targetN1, 0f, takeOffN1 * 0.2f);

    N1 = Mathf.MoveTowards(N1, targetN1, n1DecreaseResponse * Mathf.Abs(N1 - targetN1) * dt);
}

void UpdateN2FromN1Windmill(float dt)
{
    float targetN2 = N1 * windmillingN2toN1Ratio;
    N2 = Mathf.MoveTowards(N2, targetN2, n2DecreaseResponse * Mathf.Abs(N2 - targetN2) * dt);
}
```

#### Running用（既存ロジック）
```csharp
void UpdateN2FromThrottle(float dt)
{
    float n2FromThrottle = Mathf.Lerp(idleN2, takeOffN2, throttleInput);
    float n2FromN1 = TSFEUtil.ClampedRemap(N1, idleN1, takeOffN1, idleN2, takeOffN2);
    float target = Mathf.Max(n2FromThrottle, n2FromN1);
    N2 = Mathf.MoveTowards(N2, target, n2Response * Mathf.Abs(target - N2) * dt);
}

void UpdateN1FromN2(float dt)
{
    float target = Mathf.Lerp(idleN1, takeOffN1, throttleInput);
    float n2Min = idleN2 * 0.99f;
    target = Mathf.Min(target, TSFEUtil.ClampedRemap(N2, n2Min, takeOffN2, idleN1, takeOffN1));
    float resp = target > N1 ? n1Response : n1DecreaseResponse;
    N1 = Mathf.MoveTowards(N1, target, resp * Mathf.Abs(target - N1) * dt);
}
```

#### Seized用（新規）
```csharp
void UpdateN1Decay(float dt)
{
    N1 = Mathf.MoveTowards(N1, 0f, n1DecreaseResponse * N1 * 10f * dt);
}

void UpdateN2Decay(float dt)
{
    N2 = Mathf.MoveTowards(N2, 0f, n2DecreaseResponse * N2 * 10f * dt);
}
```

---

## 6. Windmilling抗力計算

### 実装
```csharp
private float appliedWindmillingDrag = 0f;

void UpdateWindmillingDrag(float dt)
{
    if (engineState == EngineState.Windmilling && N1 > takeOffN1 * 0.01f)
    {
        float airSpeed = (float)SAVControl.GetProgramVariable("AirSpeed");
        float atmosphere = (float)SAVControl.GetProgramVariable("Atmosphere");

        // 速度²に比例する抗力
        float speedRatio = airSpeed / windmillingDragReferenceSpeed;
        float drag = windmillingDragCoefficient * (speedRatio * speedRatio) * atmosphere;

        // ExtraDragに累積適用
        float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
        SAVControl.SetProgramVariable("ExtraDrag",
            currentDrag + drag - appliedWindmillingDrag);
        appliedWindmillingDrag = drag;
    }
    else if (appliedWindmillingDrag != 0f)
    {
        // 抗力解除
        float currentDrag = (float)SAVControl.GetProgramVariable("ExtraDrag");
        SAVControl.SetProgramVariable("ExtraDrag", currentDrag - appliedWindmillingDrag);
        appliedWindmillingDrag = 0f;
    }
}
```

---

## 7. Inspectorパラメータ一覧

### 始動システム
```csharp
[Header("始動システム")]
public GameObject starterPowerSource;
public bool n1Start = false;  // N1軸スターター方式切替

[Header("N2軸スターター設定（n1Start=false時）")]
[Range(0.15f, 0.35f)]
public float starterTargetN2Ratio = 0.25f;  // CFM56: 25%, FJ44: 20%
[Range(0.15f, 0.35f)]
public float minN2ForIgnition = 0.25f;

[Header("N1軸スターター設定（n1Start=true時）")]
[Range(0.1f, 0.3f)]
public float starterTargetN1Ratio = 0.15f;  // T-4: 15%程度
[Range(0.05f, 0.15f)]
public float autoIgnitionN1Threshold = 0.10f;  // T-4: 10%
[Range(0.8f, 1.5f)]
public float startingN2toN1Ratio = 1.2f;
public bool autoFuelInjection = true;

[Header("共通始動設定")]
public bool autoStarterCutoff = true;
[Range(0.9f, 1.0f)]
public float starterCutoffThreshold = 0.95f;
```

### Windmilling設定
```csharp
[Header("Windmilling設定")]
[Range(0.05f, 0.25f)]
public float windmillingN1Ratio = 0.15f;
[Range(0.4f, 0.8f)]
public float windmillingN2toN1Ratio = 0.6f;
public float windmillingReferenceSpeed = 250f;  // m/s
public float minimumWindmillingSpeed = 30f;     // m/s

[Header("Windmilling抗力")]
[Range(0.05f, 0.5f)]
public float windmillingDragCoefficient = 0.15f;
public float windmillingDragReferenceSpeed = 250f;  // m/s
```

---

## 8. 実装タスク一覧

### 完了済み ✅
1. EngineState enum定義とUdonSync対応
2. n1Start boolと両スターター方式用パラメータ追加
3. Windmillingパラメータ追加
4. 状態遷移ロジック実装（UpdateEngineStateメソッド）
5. N1/N2更新ロジックを状態別メソッドに分割
   - UpdateN1N2()メソッド実装
   - 各状態別メソッド実装（9個）
6. Windmilling時の抗力計算とExtraDrag適用
   - UpdateWindmillingDrag()メソッド実装
   - ResetEngine()でappliedWindmillingDragクリア
7. 既存のengineOn/meltdown参照をengineStateに置き換え
   - UpdateEngine()の条件分岐書き換え
   - 推力計算ロジック調整
   - 温度計算ロジック調整
   - UpdateDamage()の更新
   - UpdatePlayerHazards()の更新
   - 互換フラグの保持（外部スクリプト用）

---

## 9. 機体別パラメータ例

### Boeing 737-800 (CFM56-7B27)
```
n1Start: false
starterTargetN2Ratio: 0.25
minN2ForIgnition: 0.25
windmillingN1Ratio: 0.15
windmillingN2toN1Ratio: 0.6
```

### Kawasaki T-4 (F3-IHI-30)
```
n1Start: true
starterTargetN1Ratio: 0.15
autoIgnitionN1Threshold: 0.10
startingN2toN1Ratio: 1.2
windmillingN1Ratio: 0.12
windmillingN2toN1Ratio: 0.65
```

### Cessna Citation (FJ44)
```
n1Start: false
starterTargetN2Ratio: 0.20
minN2ForIgnition: 0.20
windmillingN1Ratio: 0.18
windmillingN2toN1Ratio: 0.55
```

---

## 10. テスト計画

### 単体テスト
- [ ] 各EngineState遷移の確認
- [ ] N1軸スターター始動シーケンス（T-4）
- [ ] N2軸スターター始動シーケンス（737）
- [ ] Windmilling状態でのN1/N2挙動
- [ ] Windmilling抗力の適用/解除

### 統合テスト
- [ ] 飛行中エンジン停止→Windmilling→再始動
- [ ] 片発停止時の非対称抗力
- [ ] 火災→Seized状態→RPM急速減衰
- [ ] 自動燃料投入（N1軸スターター）

### パフォーマンステスト
- [ ] 複数エンジン同時動作
- [ ] ネットワーク同期確認

---

## 11. 互換性保証

### 既存機体への影響
- `n1Start=false`（デフォルト）で既存の挙動を維持
- `engineOn`フラグは互換性のため保持
- 既存のパラメータ（starterTargetN2Ratio等）は引き続き有効

### 移行パス
1. 既存機体: 何も変更不要（デフォルトでN2軸スターター動作）
2. T-4等: `n1Start=true`に変更、N1軸パラメータ調整
3. Windmilling効果: すべての機体で自動有効化

---

## 12. 参考資料

### 技術情報源
- T-4元搭乗員からの証言（N1軸スターター、10%自動燃料投入）
- CFM56エンジン始動手順（N2軸スターター、25%目標）
- 一般的なターボファンスターター方式（N2軸が主流）

### 物理モデル
- Windmilling N1: 空気速度依存、大気密度補正
- Windmilling N2: N1機械結合、燃焼なし損失大
- 抗力: 速度²比例、大気密度補正

---

**作成日**: 2026-03-08
**最終更新**: 2026-03-08
**ステータス**: 実装完了（全7タスク完了、テスト待ち）
