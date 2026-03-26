# Audio Visualizer Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Spatial空間内の音声をリアルタイムにパーティクルで可視化するVFXCueAudioVisualizerを追加する。

**Architecture:** AudioListener.GetSpectrumData()で64バンドのスペクトラムを毎フレーム取得し、低域→排出量・中域→速度・高域→サイズとしてParticleSystemを制御する。VFXCueBPMSyncのビートでブースト、VFXCueManagerのキュー発動時にバースト。

**Tech Stack:** Unity 2022.3 LTS (URP), C#, Unity ParticleSystem, AudioListener.GetSpectrumData, Spatial SDK v1.71.0

---

## 事前確認済み事項

- `VFXCueBPMSync.IsBeatStarted` (bool) → 行305に存在
- `VFXCueBPMSync.CurrentBeat` (int) → 行141に存在
- トーストAPI → `SpatialBridge.coreGUIService.DisplayToastMessage(string)` (既存コードと一致)
- Fキーの競合 → Task 2のStep 1で確認手順を追加

---

## ファイル構成

| 操作 | ファイルパス | 責務 |
|---|---|---|
| 修正 | `Assets/SpatialVFXCue/Scripts/VFXCueManager.cs` | OnCueTriggeredイベント追加 |
| 新規作成 | `Assets/SpatialVFXCue/Scripts/VFXCueAudioVisualizer.cs` | スペクトラム取得・PS制御・BPMSync/VFXCue連携 |

---

## Task 1: VFXCueManager にイベントを追加する

**Files:**
- Modify: `Assets/SpatialVFXCue/Scripts/VFXCueManager.cs`

- [ ] **Step 1: bpmSyncフィールドの直後にOnCueTriggeredイベントを追加する**

  `VFXCueManager.cs` の以下の行:
  ```csharp
  [System.NonSerialized]
  public VFXCueBPMSync bpmSync;
  ```
  の直後に追加:
  ```csharp
  /// <summary>キュー発動時に通知するイベント（AudioVisualizer連携用）</summary>
  public event System.Action<string> OnCueTriggered;
  ```

- [ ] **Step 2: TriggerCue()内のStartCoroutine直前にイベント発火を追加する**

  `private void TriggerCue(VFXCueEntry cue, bool ignoreCooldown = false)` 内の:
  ```csharp
  cue.lastTriggerTime = Time.time;
  StartCoroutine(SpawnVFXCoroutine(cue));
  ```
  を以下に変更:
  ```csharp
  cue.lastTriggerTime = Time.time;
  OnCueTriggered?.Invoke(cue.cueName);
  StartCoroutine(SpawnVFXCoroutine(cue));
  ```

- [ ] **Step 3: Unityエディタでコンパイルエラーがないことを確認する**

  Unity ConsoleにエラーやWarningが出ていないことを目視確認する。

- [ ] **Step 4: コミット**

  ```bash
  cd /Users/mataro/Projects/VFXtest
  git add Assets/SpatialVFXCue/Scripts/VFXCueManager.cs
  git commit -m "feat(VFXCueManager): add OnCueTriggered event for AudioVisualizer integration"
  ```

---

## Task 2: VFXCueAudioVisualizer のスケルトンを作成する

**Files:**
- Create: `Assets/SpatialVFXCue/Scripts/VFXCueAudioVisualizer.cs`

- [ ] **Step 1: Fキー競合をチェックする**

  Spatial SDKのデフォルトキーバインドを確認する（既知のキー: 数字1-5=エモート、Y=タップ、B=ビート、X=モード切替）。Fキーが競合する場合はtoggleKeyのデフォルト値を `KeyCode.H` に変更する。

- [ ] **Step 2: スクリプトファイルを作成する**

  以下の内容で `Assets/SpatialVFXCue/Scripts/VFXCueAudioVisualizer.cs` を作成する:

  ```csharp
  using UnityEngine;
  using SpatialSys.UnitySDK;

  namespace SpatialVFXCue
  {
      /// <summary>
      /// AudioListenerのスペクトラムデータをParticleSystemにマッピングするオーディオビジュアライザー。
      /// VFXCueManagerのキュー発動時にバースト、VFXCueBPMSyncのビートで強調する。
      /// </summary>
      public class VFXCueAudioVisualizer : MonoBehaviour
      {
          [Header("参照")]
          [Tooltip("シーン内のVFXCueManager")]
          public VFXCueManager cueManager;

          [Tooltip("シーン内のVFXCueBPMSync（任意）")]
          public VFXCueBPMSync bpmSync;

          [Tooltip("制御対象のParticleSystem")]
          public ParticleSystem targetParticleSystem;

          [Header("トグル")]
          [Tooltip("オン/オフキー（Spatialキーバインドと競合しないキーを選ぶ）")]
          public KeyCode toggleKey = KeyCode.F;

          [Tooltip("起動時にビジュアライザーを有効にするか")]
          public bool startEnabled = false;

          [Header("スペクトラム設定")]
          [Tooltip("GetSpectrumDataのサンプル数（2の累乗）")]
          public int spectrumSize = 64;

          [Tooltip("FFTウィンドウ関数")]
          public FFTWindow fftWindow = FFTWindow.Blackman;

          [Tooltip("スペクトラム値に掛けるゲイン（感度調整）")]
          [Min(1f)]
          public float sensitivity = 100f;

          [Header("フォールバック（Spatialで音が取れない場合ON）")]
          [Tooltip("ONにするとスペクトラムを使わずBPMSyncビートのみで固定値駆動する")]
          public bool useBPMFallback = false;

          [Header("低域 → 排出量")]
          public int lowBandStart = 0;
          public int lowBandEnd = 10;
          [Min(0f)] public float emissionMin = 0f;
          [Min(0f)] public float emissionMax = 200f;

          [Header("中域 → 速度")]
          public int midBandStart = 11;
          public int midBandEnd = 30;
          [Min(0f)] public float speedMin = 0.5f;
          [Min(0f)] public float speedMax = 5f;

          [Header("高域 → サイズ")]
          public int highBandStart = 31;
          public int highBandEnd = 63;
          [Min(0f)] public float sizeMin = 0.1f;
          [Min(0f)] public float sizeMax = 0.5f;

          [Header("BPMビート強調")]
          [Min(1f)] public float beatBoostMultiplier = 3f;
          [Min(0f)] public float beatBoostDuration = 0.1f;

          [Header("キュー発動バースト")]
          [Min(0)] public int burstCount = 50;

          private float[] _spectrum;
          private bool _isActive;
          private int _lastBeat = -1;
          private float _beatBoostTimer = 0f;

          private void Start()
          {
              _spectrum = new float[spectrumSize];
              _isActive = startEnabled;

              if (cueManager != null)
                  cueManager.OnCueTriggered += HandleCueTriggered;

              if (_isActive && targetParticleSystem != null)
                  targetParticleSystem.Play();
          }

          private void OnDestroy()
          {
              if (cueManager != null)
                  cueManager.OnCueTriggered -= HandleCueTriggered;
          }

          private void Update()
          {
              if (Input.GetKeyDown(toggleKey))
                  SetActive(!_isActive);

              if (!_isActive || targetParticleSystem == null) return;

              // --- スペクトラム取得 ---
              float low, mid, high;
              if (!useBPMFallback)
              {
                  AudioListener.GetSpectrumData(_spectrum, 0, fftWindow);
                  low  = Mathf.Clamp01(GetBandAverage(lowBandStart,  lowBandEnd)  * sensitivity);
                  mid  = Mathf.Clamp01(GetBandAverage(midBandStart,  midBandEnd)  * sensitivity);
                  high = Mathf.Clamp01(GetBandAverage(highBandStart, highBandEnd) * sensitivity);
              }
              else
              {
                  // フォールバック: 固定値（BPMブーストが乗る）
                  low  = 0.3f;
                  mid  = 0.5f;
                  high = 0.3f;
              }

              // --- ビートブースト係数を計算（PS書き込み前に確定させる）---
              if (bpmSync != null && bpmSync.IsBeatStarted)
              {
                  int currentBeat = bpmSync.CurrentBeat;
                  if (currentBeat != _lastBeat)
                  {
                      _lastBeat = currentBeat;
                      _beatBoostTimer = beatBoostDuration;
                  }
              }
              float boostMul = (_beatBoostTimer > 0f) ? beatBoostMultiplier : 1f;
              _beatBoostTimer = Mathf.Max(0f, _beatBoostTimer - Time.deltaTime);

              // --- ParticleSystem に1回だけ書き込む ---
              float emissionRate = Mathf.Min(
                  Mathf.Lerp(emissionMin, emissionMax, low) * boostMul,
                  emissionMax * beatBoostMultiplier  // 上限クランプ
              );

              var emission = targetParticleSystem.emission;
              var main     = targetParticleSystem.main;
              emission.rateOverTime = emissionRate;
              main.startSpeed       = new ParticleSystem.MinMaxCurve(Mathf.Lerp(speedMin, speedMax, mid));
              main.startSize        = new ParticleSystem.MinMaxCurve(Mathf.Lerp(sizeMin,  sizeMax,  high));
          }

          /// <summary>ビジュアライザーのオン/オフを切り替える（外部からも呼べる）</summary>
          public void SetActive(bool active)
          {
              _isActive = active;
              if (targetParticleSystem == null) return;

              if (_isActive)
                  targetParticleSystem.Play();
              else
                  targetParticleSystem.Stop(true, ParticleSystemStopBehavior.StopEmitting);

              try { SpatialBridge.coreGUIService.DisplayToastMessage(_isActive ? "Visualizer ON" : "Visualizer OFF"); } catch { }
          }

          /// <summary>指定バンド範囲の平均値を返す</summary>
          public float GetBandAverage(int start, int end)
          {
              if (_spectrum == null || start < 0 || end >= _spectrum.Length || start > end)
                  return 0f;
              float sum = 0f;
              for (int i = start; i <= end; i++)
                  sum += _spectrum[i];
              return sum / (end - start + 1);
          }

          private void HandleCueTriggered(string cueName)
          {
              if (!_isActive || targetParticleSystem == null || burstCount <= 0) return;
              targetParticleSystem.Emit(burstCount);
          }
      }
  }
  ```

- [ ] **Step 3: Unityエディタでコンパイルエラーがないことを確認する**

  Unity ConsoleにエラーやWarningが出ていないことを目視確認する。

- [ ] **Step 4: コミット**

  ```bash
  cd /Users/mataro/Projects/VFXtest
  git add Assets/SpatialVFXCue/Scripts/VFXCueAudioVisualizer.cs
  git commit -m "feat(AudioVisualizer): add full implementation with spectrum, BPM boost, and cue burst"
  ```

---

## Task 3: シーンにGameObjectを配置して動作確認する

**Files:**
- シーンファイル（手動操作）

- [ ] **Step 1: シーンにAudioVisualizerオブジェクトを配置する**

  1. Hierarchyで右クリック → Create Empty → 名前を「AudioVisualizer」とする
  2. ParticleSystemコンポーネントを追加し以下を設定する（**NetworkObjectは絶対に付けない**）:

  | 設定 | 値 |
  |---|---|
  | Transform.Position | ステージ床面中央（Y=0〜0.1付近） |
  | Shape | Box（Width: 10, Height: 0.1, Depth: 10）|
  | Start Lifetime | Random Between Two Constants: 2〜5 |
  | Start Color | Gradient（暖色〜寒色） |
  | Play On Awake | OFF |
  | Looping | ON |
  | Max Particles | 1000 |

  3. VFXCueAudioVisualizerコンポーネントをアタッチし、参照を設定する:
     - cueManager → シーン内のVFXCueManager
     - bpmSync → シーン内のVFXCueBPMSync（あれば）
     - targetParticleSystem → 上で追加したPS

- [ ] **Step 2: エディタPlay → 基本動作を確認する**

  1. Playボタンを押す
  2. toggleKeyを押す（デフォルトF） → 「Visualizer ON」トーストが出てパーティクルが流れることを確認
  3. もう一度押す → 「Visualizer OFF」トーストが出てパーティクルが止まることを確認

- [ ] **Step 3: BPMSync連携を確認する（bpmSyncが設定されている場合）**

  1. ビジュアライザーON状態で Yキーをタップ × 数回 → BPMが検出される
  2. Bキーでビート開始 → ビートごとにパーティクルの排出量が増加することを目視確認

- [ ] **Step 4: キュー発動バーストを確認する**

  1. ビジュアライザーON状態でVFXキューを発動
  2. 一瞬大量のパーティクルが出る（バースト）ことを目視確認

- [ ] **Step 5: コミット**

  ```bash
  cd /Users/mataro/Projects/VFXtest
  git add Assets/SpatialVFXCue/Scenes/
  git commit -m "feat(AudioVisualizer): add scene setup with AudioVisualizer GameObject"
  ```

---

## Task 4: Spatial本番環境での検証

- [ ] **Step 1: SpatialにパブリッシュしてAudioListenerの動作を確認する**

  1. File > Build Settings > Spatialでパブリッシュ
  2. Spatial空間内でBGMを流す
  3. FキーでビジュアライザーをON
  4. パーティクルが音に反応して変化しているか確認

- [ ] **Step 2: スペクトラムが常にゼロの場合はフォールバックモードに切り替える**

  AudioListenerに音が届いていない場合（パーティクルが反応しない）:
  1. InspectorでuseBPMFallbackをONに切り替える
  2. BPMSync有効時にビートに連動した演出が成立することを確認する
  3. このフラグは手動切り替え（自動検出は対象外）

- [ ] **Step 3: 統合テスト**

  | 操作 | 期待結果 |
  |---|---|
  | Fキー押下 | 「Visualizer ON」 → パーティクル開始 |
  | BPMSync有効 + ビート開始 | ビートごとにパーティクル量が増加 |
  | VFXキュー発動 | バーストが発生 |
  | Fキー再押下 | 「Visualizer OFF」 → パーティクル停止 |

- [ ] **Step 4: 最終コミット**

  ```bash
  cd /Users/mataro/Projects/VFXtest
  git add Assets/SpatialVFXCue/
  git commit -m "feat: complete audio visualizer - particle constellation synced to spectrum, BPM, and cue triggers"
  ```

---

## 補足: Fキー競合について

SpatialのキーバインドにFキーが割り当てられている場合、`VFXCueAudioVisualizer` の `toggleKey` をInspectorから変更する。推奨候補: `KeyCode.H`、`KeyCode.U`（BPMSyncでY/B/X使用済みのため避ける）。
