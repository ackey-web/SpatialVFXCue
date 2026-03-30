# SpatialVFXCue UPM パッケージ化 設計書

**日付:** 2026-03-26
**対象リポジトリ:** https://github.com/ackey-web/SpatialVFXCue

---

## 概要

SpatialVFXCue を Unity Package Manager (UPM) の Git URL 方式でインストール可能なパッケージとして整備する。Spatial Creator Toolkit (`io.spatial.unitysdk`) への依存を宣言し、他の Unity プロジェクトから以下の URL で導入できるようにする。

```
https://github.com/ackey-web/SpatialVFXCue.git
```

---

## パッケージ情報

| フィールド | 値 |
|---|---|
| `name` | `io.github.ackey-web.spatialvfxcue` |
| `version` | `1.0.0` |
| `displayName` | `SpatialVFXCue` |
| `unity` | `2021.3`（最低バージョン） |
| Spatial SDK 依存 | `"io.spatial.unitysdk": "1.0.0"` |

---

## リポジトリ構成

### 変更前（Assets/SpatialVFXCue/ 内）

```
SpatialVFXCue/
├── Scripts/
├── Editor/
├── Prefabs/
├── Scenes/
├── Textures/
├── VisualizerParticle/
├── docs/
├── SpatialVFXCue.asmdef       ← ルートに置かれている
└── README.txt
```

### 変更後（リポジトリルート）

```
SpatialVFXCue.git/
├── package.json                ← 新規作成
├── README.md                   ← README.txt から変換
├── CHANGELOG.md                ← 新規作成
├── Runtime/                    ← Scripts/ をリネーム
│   ├── SpatialVFXCue.asmdef    ← ルートから移動
│   ├── VFXCueManager.cs
│   ├── VFXCueEffect.cs
│   ├── VFXCueBPMSync.cs
│   ├── VFXCueAudioVisualizer.cs
│   ├── VFXCueVJController.cs
│   ├── VFXCueVJPanel.cs
│   ├── VFXCueVJLayer.cs
│   ├── VFXCueVJPreset.cs
│   ├── VFXCueTriggerButton.cs
│   ├── VFXCueChain.cs
│   ├── VFXCueCountdown.cs
│   ├── VFXCueRandom.cs
│   ├── VFXCueEntry.cs
│   ├── VFXCueEffect.cs
│   └── VFXCueLog.cs
├── Editor/                     ← 変更なし
│   ├── SpatialVFXCue.Editor.asmdef
│   ├── VFXCueSetupWizard.cs
│   ├── VFXCueImportHelper.cs
│   ├── VFXCueManagerEditor.cs
│   └── VFXCueSceneRepair.cs
├── Prefabs/                    ← 変更なし
│   ├── VFX_Confetti.prefab
│   ├── VFX_Fireworks.prefab
│   ├── VFX_MeteorShower.prefab
│   ├── VFX_SmokeBurst.prefab
│   ├── VFX_SparkleRain.prefab
│   └── VFX_SpotlightBeam.prefab
├── Textures/                   ← 変更なし
├── VisualizerParticle/         ← 変更なし
├── Samples~/                   ← Scenes/ を移動（~ でUPM管理外）
│   └── Demo/
│       └── SpatialVFXCue_Demo.unity
└── Documentation~/             ← docs/ を移動
    └── superpowers/
        └── ...
```

---

## package.json

```json
{
  "name": "io.github.ackey-web.spatialvfxcue",
  "version": "1.0.0",
  "displayName": "SpatialVFXCue",
  "description": "Spatial メタバース空間でリアルタイムVFX演出を操作するUnityアセット。BPM同期・キーボードトリガー・VJコントロール対応。",
  "unity": "2021.3",
  "dependencies": {
    "io.spatial.unitysdk": "1.0.0"
  },
  "keywords": ["spatial", "vfx", "particles", "metaverse"],
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
```

---

## asmdef 変更点

| ファイル | 変更内容 |
|---|---|
| `SpatialVFXCue.asmdef` | `Runtime/` フォルダ内に移動（内容変更なし） |
| `SpatialVFXCue.Editor.asmdef` | `Editor/` のまま（変更なし） |

---

## インストール方法（利用者向け）

### 基本インストール

Unity の `Window > Package Manager > + > Add package from git URL` に以下を入力：

```
https://github.com/ackey-web/SpatialVFXCue.git
```

### バージョン固定インストール

```
https://github.com/ackey-web/SpatialVFXCue.git#v1.0.0
```

### manifest.json に直接記述

```json
{
  "dependencies": {
    "io.github.ackey-web.spatialvfxcue": "https://github.com/ackey-web/SpatialVFXCue.git",
    "io.spatial.unitysdk": "1.0.0"
  }
}
```

> **注意:** Spatial SDK (`io.spatial.unitysdk`) は OpenUPM 経由で別途インストールが必要。自動解決されない。

---

## 前提・制約

- `.meta` ファイルはリポジトリに含める（Unity が GUID を参照するため必須）
- `Samples~/` の `~` により Unity が自動インポートしない（Package Manager UI から手動インポート）
- `Documentation~/` の `~` も同様
- Spatial SDK は UPM のスコープレジストリ（OpenUPM）経由のため、依存の自動解決は利用者が手動で行う必要がある
