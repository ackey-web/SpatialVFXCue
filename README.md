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

### ステップ 2：ZIP を展開して Unity にコピー

1. 受け取った ZIP を解凍する
2. Unity のプロジェクトを開く
3. Project ウィンドウで `Assets` フォルダを右クリック →「Show in Finder（または Explorer）」
4. 開いたフォルダに **`SpatialVFXCue`** という名前のフォルダを新規作成
5. 解凍した ZIP の中身をすべて `Assets/SpatialVFXCue/` の中にコピー

コピー後の構成：
```
Assets/
  SpatialVFXCue/
    Runtime/
    Editor/
    Prefabs/
    Textures/
```

6. Unity に戻ると自動でインポートが始まります（しばらく待つ）

---

### ステップ 3：サンプルアセット生成

Unity メニューバーから実行：
```
SpatialVFXCue > Setup Sample Assets
```

テクスチャ・マテリアル・VFX Prefab・デモシーンが自動生成されます。

---

## 新規プロジェクト向け（デモシーンをそのまま使う場合）

1. 上記ステップ 1〜3 を完了する
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

1. 上記ステップ 1〜3 を完了する（サンプルアセット生成まで）
2. **既存シーンを開いた状態で**、メニューから実行：
   ```
   SpatialVFXCue > Add VFXCue to Current Scene
   ```
   VFXCueManager・BPM同期・VJコントローラー等が自動でシーンに配置されます。
   キュー設定（キー1〜6 → VFX Prefab 6種）も自動で構成されます。
3. シーンを保存する（Ctrl+S / Cmd+S）
4. プロジェクトの **Space Package Config** を開き、以下を設定：
   - **C# Assembly**: `SpatialVFXCue` を指定
   - **Network Prefabs**: 以下の 6 つを登録
     - `VFX_Fireworks`
     - `VFX_Confetti`
     - `VFX_MeteorShower`
     - `VFX_SpotlightBeam`
     - `VFX_SmokeBurst`
     - `VFX_SparkleRain`
5. Spatial SDK メニュー →「Test Active Scene」で動作確認

> **注意**: このメニューは既存の Space Config のシーン設定を変更しません。既存空間の設定がそのまま維持されます。

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
| 7 | チェイン演出（花火→紙吹雪→キラキラの雨） |
| 8 | ランダム VFX |
| 9 | カウントダウン（3...2...1...GO!） |
| T | タップテンポ |
| B | ビート同期 開始/停止 |

---

## 対応環境

- Unity 2021.3 LTS 以降
- Spatial Creator Toolkit 必須
- Universal Render Pipeline（URP）必須
