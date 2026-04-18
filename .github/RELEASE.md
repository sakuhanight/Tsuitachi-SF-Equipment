# リリース手順

## 自動リリース（推奨）

GitHub Actionsを使用した完全自動リリースプロセス。

### 手順

1. **GitHubリポジトリページにアクセス**
   - https://github.com/sakuhanight/Tsuitachi-SF-Equipment

2. **Actionsタブを開く**
   - 上部メニューから「Actions」をクリック

3. **"Create Release with Version Update"ワークフローを選択**
   - 左サイドバーから選択

4. **"Run workflow"をクリック**
   - 右上の「Run workflow」ボタンをクリック

5. **バージョン番号を入力**
   - 形式: `X.Y.Z` （例: `1.0.0`, `0.2.1`）
   - ❌ `v1.0.0` のように`v`プレフィックスは**不要**

6. **"Run workflow"を実行**
   - 緑色のボタンをクリック

### 自動実行される処理

1. ✅ バージョン形式の検証（`X.Y.Z`形式）
2. ✅ `packages/net.tsuitachi.sf-equipment/package.json`のバージョン更新
3. ✅ 前回リリースからのコミット履歴でリリースノート生成
4. ✅ 変更をコミット（"Bump version to X.Y.Z"）
5. ✅ `vX.Y.Z`形式のGitタグ作成
6. ✅ タグとコミットをGitHubにプッシュ
7. ✅ パッケージZIPファイル作成
8. ✅ GitHub Releaseの作成（リリースノート付き）
9. ✅ VPMリポジトリへの自動登録（`sakuhanight/vpm.t7i.io`）

### 実行例

```
Input version: 0.2.0

↓

1. package.json: "version": "0.1.0" → "0.2.0"
2. Commit: "Bump version to 0.2.0"
3. Tag: v0.2.0
4. Release: https://github.com/sakuhanight/Tsuitachi-SF-Equipment/releases/tag/v0.2.0
5. VPM: 自動登録
```

---

## 手動リリース（従来方式）

タグプッシュによる半自動リリース。

### 前提条件

- `packages/net.tsuitachi.sf-equipment/package.json`のバージョンを**手動で更新済み**
- 変更をコミット・プッシュ済み

### 手順

```bash
# 1. package.jsonのバージョンを手動更新
vim packages/net.tsuitachi.sf-equipment/package.json
# "version": "0.2.0" に変更

# 2. コミット
git add packages/net.tsuitachi.sf-equipment/package.json
git commit -m "Bump version to 0.2.0"
git push origin master

# 3. タグ作成・プッシュ
git tag v0.2.0
git push origin v0.2.0
```

### 自動実行される処理

1. ✅ パッケージZIPファイル作成
2. ✅ GitHub Releaseの作成（自動生成リリースノート）
3. ✅ VPMリポジトリへの自動登録

---

## リリースノートのカスタマイズ

自動リリースでは前回タグからのコミット履歴を自動生成します。

手動でカスタマイズする場合：

1. GitHub Releasesページにアクセス
2. 該当リリースの「Edit release」をクリック
3. リリースノートを編集
4. 「Update release」で保存

---

## トラブルシューティング

### ワークフローが失敗する

**エラー: "Version must be in format X.Y.Z"**
- バージョン番号から`v`プレフィックスを削除
- 正しい形式: `1.0.0` ❌ `v1.0.0`

**エラー: "Permission denied"**
- リポジトリのSettings → Actions → General
- "Workflow permissions"を"Read and write permissions"に設定

### VPM登録が失敗する

- `secrets.GITHUB_TOKEN`の権限を確認
- VPMリポジトリ（`sakuhanight/vpm.t7i.io`）のアクセス権を確認

### タグが既に存在する

```bash
# ローカルタグ削除
git tag -d v0.2.0

# リモートタグ削除（注意: GitHubリリースも手動削除必要）
git push origin :refs/tags/v0.2.0
```

---

## バージョニング規則

[Semantic Versioning 2.0.0](https://semver.org/)に準拠：

- **MAJOR** (`X.0.0`): 破壊的変更
- **MINOR** (`0.X.0`): 後方互換性のある機能追加
- **PATCH** (`0.0.X`): 後方互換性のあるバグ修正

### 例

- `0.1.0` → `0.2.0`: 新機能追加（DFUNC_AutoFlaps実装）
- `0.2.0` → `0.2.1`: バグ修正（GPWS警告音修正）
- `0.9.0` → `1.0.0`: 安定版リリース（破壊的変更含む）

---

## チェックリスト

リリース前に確認：

- [ ] すべてのテストが通過している
- [ ] `COMPONENTS.md`の実装状況が最新
- [ ] `REFACTORING-SUMMARY.md`が更新済み
- [ ] 破壊的変更がある場合、`CHANGELOG.md`に記載（推奨）
- [ ] Unity 2022.3でパッケージが正常動作することを確認
