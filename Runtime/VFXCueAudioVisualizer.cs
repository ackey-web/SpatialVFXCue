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
            {
                Debug.Log($"[AudioVisualizer] Fキー検知 → SetActive({!_isActive}), PS={targetParticleSystem}");
                SetActive(!_isActive);
            }

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
            // デクリメント先行 → 新ビートで上書き → 係数確定（持続時間が正確に機能する）
            _beatBoostTimer = Mathf.Max(0f, _beatBoostTimer - Time.deltaTime);
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
            if (targetParticleSystem == null)
            {
                Debug.LogWarning("[AudioVisualizer] targetParticleSystem が未設定です！ Inspector で参照を設定してください。");
                return;
            }

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
