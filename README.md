# SpatialVFXCue

Spatial メタバース空間でリアルタイム VFX 演出を操作する Unity アセットです。
キーボード操作で花火・紙吹雪・流星群などの演出を即座に発動できます。

## 機能

- キーボード 1〜6 で VFX をリアルタイム発動
- BPM 同期・VJ コントローラー・カウントダウンなど多彩なモード
- 全クライアントに同期されて自動消滅

---

## インストール手順

### ステップ 1：前提パッケージのインストール

Spatial Creator Toolkit を先にインストールしてください。

Unity の `Window > Package Manager > + > Add package from git URL` に入力：
```
https://openupm.com/packages/io.spatial.unitysdk/
```

---

### ステップ 2：このアセットをダウンロード

1. このページ右上の緑のボタン **「Code」→「Download ZIP」** をクリック
2. ZIP を解凍する

---

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

---

### ステップ 4：セットアップ

Unity メニューバーから実行：
```
SpatialVFXCue > Setup Sample Assets
```

テクスチャ・マテリアル・VFX Prefab・デモシーンが自動生成されます。

---

## 新規プロジェクト向け（デモシーンをそのまま使う場合）

1. 上記ステップ 1〜4 を完了する
2. `Assets/SpatialVFXCue/Scenes/SpatialVFXCue_Demo.unity` を開く
3. Spatial SDK の **Space Package Config** を開き、以下を設定：
   - **Scene**: `SpatialVFXCue_Demo` を指定
   - **C# Assembly**: `SpatialVFXCue` を指定
   - **Network Prefabs**: 以下の 6 つを登録
     - `VFX_Fireworks`
     - `VFX_Confetti`
     - `VFX_MeteorShower`
     - `VFX_SpotlightBeam`
     - `VFX_SmokeBurst`
     - `VFX_SparkleRain`
4. Spatial SDK メニュー →「Test Active Scene」でサンドボックス起動
5. キー **1〜6** を押して演出が出ることを確認

---

## 既存空間向け（既存シーンに VFX 機能を追加する場合）

1. 上記ステップ 1〜4 を完了する（サンプルアセット生成まで）
2. 既存シーンを開いた状態で、メニューから実行：
   ```
   SpatialVFXCue > Repair Scene Components
   ```
   VFXCueManager 等の必要なコンポーネントが自動追加されます
3. プロジェクトの **Space Package Config** を開き、以下を手動登録：
   - **C# Assembly**: `SpatialVFXCue` を指定
   - **Network Prefabs**: 以下の 6 つを登録
     - `VFX_Fireworks`
     - `VFX_Confetti`
     - `VFX_MeteorShower`
     - `VFX_SpotlightBeam`
     - `VFX_SmokeBurst`
     - `VFX_SparkleRain`

> **注意**: Space Config のシーン設定は変更しないでください。既存空間のシーン設定がそのまま維持されます。

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
