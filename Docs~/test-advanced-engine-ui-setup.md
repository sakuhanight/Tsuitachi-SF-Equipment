# SFEXT_AdvancedEngine デバッグUI作成手順

UdonSharpではOnGUIが使えないため、UI Textを使ったデバッグ表示を作成します。

## UI Canvas + Text の作成

### 1. Canvas作成

1. Hierarchy右クリック → UI → Canvas
2. Canvas設定:
   - Render Mode: `Screen Space - Overlay`
   - (デフォルトでOK)

### 2. Text作成

1. Canvas右クリック → UI → Text (Legacy)
   - または UI → Text - TextMeshPro (推奨)
2. 名前: `DebugText`

### 3. Text設定

**Rect Transform**:
- Anchor Presets: **Top Left** (左上クリック、Alt+Shift押しながら)
- Pos X: `185`
- Pos Y: `-180`
- Width: `350`
- Height: `340`

**Text (または TextMeshPro)**:
- Font Size: `14`
- Color: `White`
- Alignment: `Left` + `Top`
- (TextMeshProの場合) Vertex Color: `White`

### 4. SFEXT_AdvancedEngineTest に設定

1. `EngineTestController` オブジェクトを選択
2. Inspector で `SFEXT_AdvancedEngineTest` コンポーネントを確認
3. **Debug Text** フィールドに `DebugText` オブジェクトをドラッグ

## 表示内容

Play Modeで以下が表示されます：

```
SFEXT_AdvancedEngine Test

Controls:
I: Starter [OFF]
F: Fuel [OFF]
RightShift/RightControl: Throttle [0.00]
R: Reverser [OFF]

Engine State:
N1: 0.0 RPM (0.0%)
N2: 0.0 RPM (0.0%)
EGT: 0 C
ECT: 0 C
Fire: NO
Engine On: NO
```

## UI不要の場合

Debug Text フィールドを空欄にすれば、Inspector だけで確認できます：

1. `Engine_Test` オブジェクトを選択
2. Inspector で `SFEXT_AdvancedEngine` コンポーネントの値を確認
   - N1, N2, EGT, ECT, fire, starter, fuel, reversing

3. `EngineTestController` オブジェクトを選択
4. Inspector で `SFEXT_AdvancedEngineTest` コンポーネントの値を確認
   - throttleInput, starter, fuel, reversing

5. `MockSAVControl` オブジェクトを選択
6. Inspector で値を確認
   - ThrottleStrength (推力)

## トラブルシューティング

### UI Textが表示されない

1. Canvas の Render Mode が `Screen Space - Overlay` か確認
2. Text の Color が White になっているか確認
3. Text オブジェクトが Canvas の子になっているか確認

### テキストが更新されない

1. `SFEXT_AdvancedEngineTest` の `debugText` フィールドが設定されているか確認
2. `engine` フィールドが設定されているか確認
3. Play Mode中に Inspector で `debugText.text` の値を確認

### UI が小さすぎる/大きすぎる

1. Canvas Scaler コンポーネント追加 (Canvas に)
2. UI Scale Mode: `Scale With Screen Size`
3. Reference Resolution: `1920 x 1080`
