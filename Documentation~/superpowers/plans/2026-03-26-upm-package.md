# SpatialVFXCue UPM パッケージ化 実装プラン

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** `ackey-web/SpatialVFXCue` GitHub リポジトリを UPM Git URL 方式でインストール可能なパッケージとして整備する。

**Architecture:** GitHub リポジトリを `/Users/mataro/Projects/SpatialVFXCue/` にクローンし、`/Users/mataro/Projects/VFXtest/Assets/SpatialVFXCue/` の最新コンテンツを UPM 標準レイアウト（Runtime/, Editor/, Samples~/, Documentation~/）で配置する。`.meta` ファイルはフォルダリネーム時に対応するものを併せてリネームし GUID を保持する。

**Tech Stack:** Unity 2021.3+, UPM, GitHub CLI (`gh`), Spatial Creator Toolkit (`io.spatial.unitysdk`)

---

## ファイルマップ

| 操作 | 変更前 | 変更後 |
|---|---|---|
| リネーム | `Scripts/` + `Scripts.meta` | `Runtime/` + `Runtime.meta` |
| 移動 | `SpatialVFXCue.asmdef` + `.meta` | `Runtime/SpatialVFXCue.asmdef` + `.meta` |
| リネーム→移動 | `Scenes/` → | `Samples~/Demo/`（.metaなし） |
| リネーム | `docs/` + `docs.meta` | `Documentation~/`（.metaなし） |
| 新規作成 | — | `package.json` |
| 新規作成 | — | `README.md` |
| 新規作成 | — | `CHANGELOG.md` |
| 変更なし | `Editor/`, `Prefabs/`, `Textures/`, `VisualizerParticle/` | そのまま |

---

## Task 1: リポジトリをクローンして初期状態を確認

**Files:**
- Working dir: `/Users/mataro/Projects/SpatialVFXCue/`

- [ ] **Step 1: リポジトリをクローンする**

```bash
gh repo clone ackey-web/SpatialVFXCue /Users/mataro/Projects/SpatialVFXCue
```

- [ ] **Step 2: 現在の内容を確認する**

```bash
ls -la /Users/mataro/Projects/SpatialVFXCue/
```

期待される結果: README.md や古い構成ファイル（Scripts/, Materials/ など）が存在する場合がある。

- [ ] **Step 3: 既存コンテンツをすべて削除する（.git は残す）**

```bash
cd /Users/mataro/Projects/SpatialVFXCue
find . -mindepth 1 -not -path './.git*' -delete
```

- [ ] **Step 4: 削除を確認する**

```bash
ls -la /Users/mataro/Projects/SpatialVFXCue/
```

期待される結果: `.git/` のみ残っている。

---

## Task 2: VFXtest からコンテンツをコピーする

**Files:**
- Source: `/Users/mataro/Projects/VFXtest/Assets/SpatialVFXCue/`
- Dest: `/Users/mataro/Projects/SpatialVFXCue/`

- [ ] **Step 1: 全コンテンツをコピーする**

```bash
rsync -av --exclude='.git' \
  /Users/mataro/Projects/VFXtest/Assets/SpatialVFXCue/ \
  /Users/mataro/Projects/SpatialVFXCue/
```

- [ ] **Step 2: コピー結果を確認する**

```bash
ls /Users/mataro/Projects/SpatialVFXCue/
```

期待される結果: `Scripts/`, `Editor/`, `Prefabs/`, `Scenes/`, `Textures/`, `VisualizerParticle/`, `docs/`, `SpatialVFXCue.asmdef`, `README.txt` などが存在する。

---

## Task 3: Scripts/ → Runtime/ にリネームする

**Files:**
- Rename: `Scripts/` → `Runtime/`
- Rename: `Scripts.meta` → `Runtime.meta`
- Move: `SpatialVFXCue.asmdef` → `Runtime/SpatialVFXCue.asmdef`
- Move: `SpatialVFXCue.asmdef.meta` → `Runtime/SpatialVFXCue.asmdef.meta`

- [ ] **Step 1: Scripts/ を Runtime/ にリネームする**

```bash
cd /Users/mataro/Projects/SpatialVFXCue
mv Scripts Runtime
mv Scripts.meta Runtime.meta
```

- [ ] **Step 2: asmdef を Runtime/ 内に移動する**

```bash
mv SpatialVFXCue.asmdef Runtime/SpatialVFXCue.asmdef
mv SpatialVFXCue.asmdef.meta Runtime/SpatialVFXCue.asmdef.meta
```

- [ ] **Step 3: Runtime/ の中身を確認する**

```bash
ls /Users/mataro/Projects/SpatialVFXCue/Runtime/
```

期待される結果: `SpatialVFXCue.asmdef`, `VFXCueManager.cs`, `VFXCueEffect.cs` など全 .cs ファイルと .meta ファイルが存在する。

---

## Task 4: Scenes/ → Samples~/Demo/ に移動する

**Files:**
- Create: `Samples~/Demo/`
- Move: `Scenes/SpatialVFXCue_Demo.unity` → `Samples~/Demo/`
- Move: `Scenes/SpatialVFXCue_Demo.unity.meta` → `Samples~/Demo/`
- Delete: `Scenes/`, `Scenes.meta`

> `Samples~/` の `~` は Unity が自動インポートしないディレクトリを示す UPM 規約。.meta ファイルは Samples~/ フォルダ自体には不要。

- [ ] **Step 1: Samples~/Demo/ ディレクトリを作成する**

```bash
cd /Users/mataro/Projects/SpatialVFXCue
mkdir -p "Samples~/Demo"
```

- [ ] **Step 2: デモシーンを移動する**

```bash
mv Scenes/SpatialVFXCue_Demo.unity "Samples~/Demo/"
mv Scenes/SpatialVFXCue_Demo.unity.meta "Samples~/Demo/"
```

- [ ] **Step 3: 空になった Scenes/ と .meta を削除する**

```bash
rm -rf Scenes
rm -f Scenes.meta
```

- [ ] **Step 4: 移動結果を確認する**

```bash
ls "Samples~/Demo/"
```

期待される結果: `SpatialVFXCue_Demo.unity`, `SpatialVFXCue_Demo.unity.meta` が存在する。

---

## Task 5: docs/ → Documentation~/ にリネームする

**Files:**
- Rename: `docs/` → `Documentation~/`
- Delete: `docs.meta`（Documentation~/ には .meta 不要）

- [ ] **Step 1: docs/ を Documentation~/ にリネームする**

```bash
cd /Users/mataro/Projects/SpatialVFXCue
mv docs "Documentation~"
rm -f docs.meta
```

- [ ] **Step 2: 確認する**

```bash
ls "Documentation~/"
```

期待される結果: `superpowers/` フォルダが存在する。

---

## Task 6: package.json を作成する

**Files:**
- Create: `/Users/mataro/Projects/SpatialVFXCue/package.json`

- [ ] **Step 1: package.json を作成する**

```bash
cat > /Users/mataro/Projects/SpatialVFXCue/package.json << 'EOF'
{
  "name": "io.github.ackey-web.spatialvfxcue",
  "version": "1.0.0",
  "displayName": "SpatialVFXCue",
  "description": "Spatial メタバース空間でリアルタイムVFX演出を操作するUnityアセット。BPM同期・キーボードトリガー・VJコントロール対応。",
  "unity": "2021.3",
  "dependencies": {
    "io.spatial.unitysdk": "1.0.0"
  },
  "keywords": [
    "spatial",
    "vfx",
    "particles",
    "metaverse",
    "music"
  ],
  "author": {
    "name": "ackey-web",
    "url": "https://github.com/ackey-web"
  },
  "samples": [
    {
      "displayName": "Demo Scene",
      "description": "SpatialVFXCue のデモシーン（Spatial SDK 必須）",
      "path": "Samples~/Demo"
    }
  ]
}
EOF
```

- [ ] **Step 2: 内容を確認する**

```bash
cat /Users/mataro/Projects/SpatialVFXCue/package.json
```

期待される結果: 上記 JSON が正しく出力される。

---

## Task 7: README.md と CHANGELOG.md を作成する

**Files:**
- Create: `/Users/mataro/Projects/SpatialVFXCue/README.md`
- Create: `/Users/mataro/Projects/SpatialVFXCue/CHANGELOG.md`
- Delete: `README.txt`, `README.txt.meta`（README.md に置換）

- [ ] **Step 1: README.md を作成する**

```bash
cat > /Users/mataro/Projects/SpatialVFXCue/README.md << 'EOF'
# SpatialVFXCue

Spatial メタバース空間でリアルタイム VFX 演出を操作する Unity パッケージ。

## 機能

- BPM 同期・キーボードトリガーによる VFX キュー発動
- VJ コントローラー（レイヤー・プリセット管理）
- カウントダウン・チェーン・ランダム発動モード
- オーディオビジュアライザー連携

## インストール

### 必要な前提

Spatial Creator Toolkit を先にインストールしてください（OpenUPM 経由）:

```
https://openupm.com/packages/io.spatial.unitysdk/
```

### このパッケージのインストール

Unity の `Window > Package Manager > + > Add package from git URL` に入力:

```
https://github.com/ackey-web/SpatialVFXCue.git
```

バージョン固定:

```
https://github.com/ackey-web/SpatialVFXCue.git#v1.0.0
```

## デモシーン

Package Manager の「Samples」タブから `Demo Scene` をインポートして確認できます。

## 対応環境

- Unity 2021.3 LTS 以降
- Spatial Creator Toolkit (io.spatial.unitysdk) 1.0.0 以降
EOF
```

- [ ] **Step 2: CHANGELOG.md を作成する**

```bash
cat > /Users/mataro/Projects/SpatialVFXCue/CHANGELOG.md << 'EOF'
# Changelog

## [1.0.0] - 2026-03-26

### Added
- 初回リリース
- VFXCueManager: キーボードトリガーによる VFX 発動
- VFXCueBPMSync: BPM 同期モード
- VFXCueVJController: VJ コントロールパネル
- VFXCueAudioVisualizer: オーディオビジュアライザー連携
- VFXCueChain / VFXCueRandom / VFXCueCountdown: 発動パターン制御
- 6種類の VFX Prefab（Confetti, Fireworks, MeteorShower, SmokeBurst, SparkleRain, SpotlightBeam）
- デモシーン (Samples~/Demo)
EOF
```

- [ ] **Step 3: 古い README.txt を削除する**

```bash
cd /Users/mataro/Projects/SpatialVFXCue
rm -f README.txt README.txt.meta
```

---

## Task 8: 不要ファイルを削除して最終構成を確認する

- [ ] **Step 1: 機能概要.txt（日本語ファイル）の扱いを確認する**

```bash
ls /Users/mataro/Projects/SpatialVFXCue/SpatialVFXCue*.txt 2>/dev/null
```

このファイルは Documentation~/ に移動するか削除する:

```bash
mv /Users/mataro/Projects/SpatialVFXCue/SpatialVFXCue_機能概要.txt \
   "/Users/mataro/Projects/SpatialVFXCue/Documentation~/" 2>/dev/null || true
rm -f /Users/mataro/Projects/SpatialVFXCue/SpatialVFXCue_機能概要.txt.meta
```

- [ ] **Step 2: 最終ディレクトリ構成を確認する**

```bash
find /Users/mataro/Projects/SpatialVFXCue -not -path '*/.git*' | sort
```

期待される最終構成:
```
SpatialVFXCue/
├── package.json
├── README.md
├── CHANGELOG.md
├── Runtime/
│   ├── SpatialVFXCue.asmdef
│   ├── SpatialVFXCue.asmdef.meta
│   ├── VFXCueManager.cs (+ .meta)
│   └── ... (全 .cs + .meta)
├── Editor/
│   ├── SpatialVFXCue.Editor.asmdef (+ .meta)
│   └── ... (全 .cs + .meta)
├── Prefabs/ (+ .meta)
├── Textures/ (+ .meta)
├── VisualizerParticle/ (+ .meta)
├── Samples~/
│   └── Demo/
│       ├── SpatialVFXCue_Demo.unity
│       └── SpatialVFXCue_Demo.unity.meta
└── Documentation~/
    └── superpowers/
```

---

## Task 9: コミットしてプッシュする

- [ ] **Step 1: git status で変更を確認する**

```bash
cd /Users/mataro/Projects/SpatialVFXCue
git status
```

- [ ] **Step 2: 全ファイルをステージする**

```bash
git add -A
```

- [ ] **Step 3: コミットする**

```bash
git commit -m "$(cat <<'EOF'
feat: UPM パッケージ構成に移行 (v1.0.0)

- Scripts/ → Runtime/ にリネーム
- SpatialVFXCue.asmdef を Runtime/ 内に移動
- Scenes/ → Samples~/Demo/ に移動
- docs/ → Documentation~/ にリネーム
- package.json を追加 (io.github.ackey-web.spatialvfxcue)
- README.md, CHANGELOG.md を追加

Co-Authored-By: Claude Sonnet 4.6 <noreply@anthropic.com>
EOF
)"
```

- [ ] **Step 4: v1.0.0 タグを打つ**

```bash
git tag v1.0.0
```

- [ ] **Step 5: プッシュする**

```bash
git push origin main --tags
```

- [ ] **Step 6: GitHub でリポジトリを確認する**

```bash
gh repo view ackey-web/SpatialVFXCue --web
```

---

## Task 10: 別プロジェクトからインストールテストする（任意）

インストール確認は別の Unity プロジェクトで以下を `manifest.json` に追記して行う:

```json
{
  "dependencies": {
    "io.github.ackey-web.spatialvfxcue": "https://github.com/ackey-web/SpatialVFXCue.git#v1.0.0"
  }
}
```

Unity を開き、Package Manager に `SpatialVFXCue` が表示されること、`Samples` タブに `Demo Scene` が表示されることを確認する。
