# SpatialVFXCue

Spatial メタバース空間でリアルタイム VFX 演出を操作する Unity アセットです。
キーボード操作で花火・紙吹雪・流星群などの演出を即座に発動できます。

## 機能

- キーボード 1〜6 で VFX をリアルタイム発動
- BPM 同期・VJ コントローラー・カウントダウンなど多彩なモード
- 全クライアントに同期されて自動消滅

---

## ⚠️ インストール方法（重要）

**Package Manager（git URL）からのインストールは動作しません。**  
必ず下記の手順で `Assets/` フォルダに直接コピーしてください。

---

## インストール手順

### ステップ 1：前提パッケージのインストール

Spatial Creator Toolkit を先にインストールしてください。

Unity の `Window > Package Manager > + > Add package from git URL` に入力：
```
https://openupm.com/packages/io.spatial.unitysdk/
```

### ステップ 2：このアセットをダウンロード

1. このページ右上の緑のボタン **「Code」→「Download ZIP」** をクリック
2. ZIP を解凍する

### ステップ 3：Unity プロジェクトにコピー

1. Unity のプロジェクトを開く
2. Project ウィンドウで `Assets` フォルダを右クリック →「Show in Finder（または Explorer）」
3. 開いたフォルダに **`SpatialVFXCue`** という名前のフォルダを新規作成
4. 解凍した ZIP の中身をすべて `Assets/SpatialVFXCue/` の中にコピー

コピー後の構成：
```
Assets/
  SpatialVFXCue/
    Runtime/
    Editor/
    Prefabs/
    Textures/
```

5. Unity に戻ると自動でインポートが始まります（しばらく待つ）

### ステップ 4：セットアップ

Unity メニューバーから実行：
```
SpatialVFXCue > One-Click Setup
```

### ステップ 5：デモシーンで確認

1. `Assets/SpatialVFXCue/Scenes/SpatialVFXCue_Demo.unity` を開く
2. Spatial SDK メニュー →「Test Active Scene」でサンドボックス起動
3. キー **1〜6** を押して演出が出ることを確認

---

## デフォルトキー割り当て

| キー | 演出 |
|------|------|
| 1 | 花火 |
| 2 | 紙吹雪 |
| 3 | 流星群 |
| 4 | スポットライト |
| 5 | スモーク爆発 |
| 6 | キラキラの雨 |

---

## 対応環境

- Unity 2021.3 LTS 以降
- Spatial Creator Toolkit 必須
- Universal Render Pipeline（URP）必須
