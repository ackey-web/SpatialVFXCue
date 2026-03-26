# SpatialVFXCue オーディオビジュアライザー 設計ドキュメント

**日付**: 2026-03-24
**プロジェクト**: SpatialVFXCue
**対象Unity**: 2022.3 LTS (URP)
**SDK**: io.spatial.unitysdk v1.71.0

---

## 概要

Spatial空間内で流れる全音声（BGM・SE含む）を`AudioListener.GetSpectrumData()`でリアルタイム取得し、周波数スペクトラムに応じてパーティクルが反応するオーディオビジュアライザーを追加する。

スタイルはパーティクルコンステレーション（星雲型）。ステージ床面に沿って広がるように配置し、VFXCueManagerおよびVFXCueBPMSyncと連携する。

---

## 設計方針

- **実装スコープを最小限に**: 新規スクリプト1つ（`VFXCueAudioVisualizer.cs`）＋VFXCueManagerへの変更1行
- **Spatialで確実に動く**: VFX Graphは使わずUnity標準のParticleSystemを使用
- **既存システムと干渉しない**: VFXCueBPMSyncはポーリングで参照（イベント追加なし）

---

## アーキテクチャ

```
AudioListener.GetSpectrumData(64バンド)
  │
  ├─ 低域 (band 0-10)  → EmissionRate（パーティクル排出量）
  ├─ 中域 (band 11-30) → StartSpeed（パーティクル速度）
  └─ 高域 (band 31-63) → StartSize（パーティクルサイズ）

VFXCueBPMSync.CurrentBeat (ポーリング)
  └─ ビート検出 → EmissionRate を beatBoostMultiplier 倍に一時ブースト

VFXCueManager.OnCueTriggered (新規イベント)
  └─ キュー発動 → ParticleSystem.Emit(burstCount) でバースト放出
```

---

## 変更ファイル

### 新規: `Scripts/VFXCueAudioVisualizer.cs`

**責務**: スペクトラム取得 → ParticleSystem制御 → BPMSync/VFXCue連携

**Inspector公開プロパティ**:

| グループ | フィールド | デフォルト値 | 説明 |
|---|---|---|---|
| 参照 | cueManager | - | VFXCueManager参照 |
| 参照 | bpmSync | - | VFXCueBPMSync参照（任意） |
| 参照 | targetParticleSystem | - | 制御対象のParticleSystem |
| トグル | toggleKey | KeyCode.F | オン/オフキー |
| トグル | startEnabled | false | 起動時の状態 |
| スペクトラム | spectrumSize | 64 | GetSpectrumDataのサイズ |
| スペクトラム | fftWindow | Blackman | FFTウィンドウ種別 |
| 低域→排出量 | lowBandStart/End | 0 / 10 | 低域バンド範囲 |
| 低域→排出量 | emissionMin/Max | 0 / 200 | 排出量の最小/最大 |
| 中域→速度 | midBandStart/End | 11 / 30 | 中域バンド範囲 |
| 中域→速度 | speedMin/Max | 0.5 / 5.0 | 速度の最小/最大 |
| 高域→サイズ | highBandStart/End | 31 / 63 | 高域バンド範囲 |
| 高域→サイズ | sizeMin/Max | 0.1 / 0.5 | サイズの最小/最大 |
| BPMビート強調 | beatBoostMultiplier | 3.0 | ビート時の排出量倍率 |
| BPMビート強調 | beatBoostDuration | 0.1秒 | ブースト持続時間 |
| キューバースト | burstCount | 50 | キュー発動時の一括放出数 |

**主要メソッド**:
- `Update()`: スペクトラム取得 → バンド集約 → PS制御 → ビート検出 → キー入力処理
- `GetBandAverage(int start, int end)`: 指定バンド範囲の平均値を返す
- `SetActive(bool)`: オン/オフ切り替え（外部からも呼べる）
- `OnCueTriggered(string cueName)`: キュー発動時コールバック

### 修正: `Scripts/VFXCueManager.cs`

追加内容（最小限）:

```csharp
// イベント宣言を追加
public event System.Action<string> OnCueTriggered;

// TriggerCue() 内、スポーン成功後に追加
OnCueTriggered?.Invoke(cue.cueName);
```

---

## ParticleSystem 推奨設定

シーンに手動でGameObjectを作成し、以下の設定でParticleSystemを追加する。

| 設定項目 | 推奨値 |
|---|---|
| Position | ステージ床面中央（Y=0付近） |
| Shape | Box または Cone（上向き） |
| Start Lifetime | 2〜5秒 |
| Start Color | グラデーション（低音=暖色、高音=寒色） |
| Play On Awake | OFF |
| Looping | ON |
| Max Particles | 1000 |

---

## データフロー

```
毎フレーム:
  AudioListener.GetSpectrumData(_spectrum, 0, FFTWindow.Blackman)
  low  = Average(_spectrum[0..10])
  mid  = Average(_spectrum[11..30])
  high = Average(_spectrum[31..63])

  emission.rateOverTime = Lerp(emissionMin, emissionMax, low * sensitivity)
  main.startSpeed       = Lerp(speedMin, speedMax, mid * sensitivity)
  main.startSize        = Lerp(sizeMin, sizeMax, high * sensitivity)

ビート検出（bpmSync != null）:
  if (bpmSync.CurrentBeat != _lastBeat)
    _beatBoostTimer = beatBoostDuration
  if (_beatBoostTimer > 0)
    emission.rateOverTime *= beatBoostMultiplier

キュー発動時:
  targetParticleSystem.Emit(burstCount)
```

---

## 制限事項・注意点

- **[要実機確認] AudioListenerへの音声到達**: Spatial SDKが独自ブリッジ経由でオーディオを再生する場合、`GetSpectrumData()`が常にゼロ配列を返す可能性がある。本番前にSpatial上でスペクトラム値が0以外になるか必ず検証すること
- **フォールバックモード**: スペクトラム取得が機能しない場合は`bpmSync.CurrentBeat`のみで駆動するモードに切り替える（`useBPMFallback`フラグをInspectorに追加）
- **スペクトラムはチャンネル0のみ**取得（ステレオ対応は対象外）
- **ParticleSystem配置**: NetworkObjectコンポーネントを付けない。Spatial Space Configのネットワーク登録対象外とすること（ローカル描画のみ）
- **OnCueTriggeredの発火**: VFXCueManagerのTriggerCue()はローカルで実行されるため、イベントはローカルコールバックとなり二重バーストは発生しない。ただし実装時に確認すること
- **Fキーの競合**: Spatialのキーバインドと競合する可能性がある。既存のBPMSyncと同様にSpatial非対応キーを確認した上でデフォルトキーを決定すること（現時点ではFをデフォルトとするが変更可能）
- adminOnlyの制限はかけない（全ユーザーに見える演出として機能する）

---

## 実装順序

1. `VFXCueManager.cs` と `VFXCueAudioVisualizer.cs` を同時に作成（イベント型を先に確定させてコンパイルエラーを防ぐ）
2. シーンにParticleSystem GameObjectを手動配置・設定（NetworkObjectなし）
3. VFXCueAudioVisualizerをアタッチして参照を設定
4. エディタでFキー動作確認
5. **[重要] Spatial本番環境でGetSpectrumDataの値確認**（ゼロ配列ならフォールバックモードに切り替え）
6. Spatial本番環境で全機能テスト
