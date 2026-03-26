using System.Collections;
using UnityEngine;

namespace SpatialVFXCue
{
    /// <summary>
    /// VFX Prefab にアタッチする MonoBehaviour。
    /// スポーン時に ParticleSystem を再生し、タイマーで自動消滅する。
    /// </summary>
    public class VFXCueEffect : MonoBehaviour
    {
        [Tooltip("自動消滅までの秒数（VFXCueManager から上書きされる）")]
        public float autoDestroyTime = 5f;

        private ParticleSystem[] _particleSystems;
        private Coroutine _destroyCoroutine;

        // ベースパラメータ（初期化時にキャプチャ）
        private float[] _baseEmissionRates;
        private float[] _baseStartSpeeds;
        private float[] _baseStartSizes;
        private Color[] _baseStartColors;
        private int[] _baseMaxParticles;

        private void Start()
        {
            _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
            CaptureBaseParameters();
            PlayAllParticles();
            _destroyCoroutine = StartCoroutine(AutoDestroyCoroutine());
        }

        private void OnDestroy()
        {
            if (_destroyCoroutine != null)
            {
                StopCoroutine(_destroyCoroutine);
                _destroyCoroutine = null;
            }
        }

        private void PlayAllParticles()
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                _particleSystems[i].Play(true);
            }
        }

        private void StopAllParticles()
        {
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                _particleSystems[i].Stop(true, ParticleSystemStopBehavior.StopEmitting);
            }
        }

        /// <summary>
        /// 遅延初期化（外部からSetIntensity等が呼ばれた場合用）
        /// </summary>
        private void EnsureInitialized()
        {
            if (_particleSystems == null)
            {
                _particleSystems = GetComponentsInChildren<ParticleSystem>(true);
                CaptureBaseParameters();
            }
        }

        private void CaptureBaseParameters()
        {
            int count = _particleSystems.Length;
            _baseEmissionRates = new float[count];
            _baseStartSpeeds = new float[count];
            _baseStartSizes = new float[count];
            _baseStartColors = new Color[count];
            _baseMaxParticles = new int[count];

            for (int i = 0; i < count; i++)
            {
                var ps = _particleSystems[i];
                var emission = ps.emission;
                var main = ps.main;

                _baseEmissionRates[i] = emission.rateOverTime.constant;
                _baseStartSpeeds[i] = main.startSpeed.constant;
                _baseStartSizes[i] = main.startSize.constant;
                _baseStartColors[i] = main.startColor.color;
                _baseMaxParticles[i] = main.maxParticles;
            }
        }

        /// <summary>
        /// インテンシティを設定（0〜2）。emission rate と size を乗算で制御。
        /// </summary>
        public void SetIntensity(float intensity)
        {
            EnsureInitialized();
            intensity = Mathf.Clamp(intensity, 0f, 2f);
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var ps = _particleSystems[i];
                var emission = ps.emission;
                var main = ps.main;

                emission.rateOverTime = _baseEmissionRates[i] * intensity;
                main.startSize = _baseStartSizes[i] * Mathf.Sqrt(intensity);
                main.maxParticles = Mathf.Max(1, Mathf.RoundToInt(_baseMaxParticles[i] * intensity));
            }
        }

        /// <summary>
        /// スピード倍率を設定（0.1〜3）。simulationSpeed を制御。
        /// </summary>
        public void SetSpeedMultiplier(float speed)
        {
            EnsureInitialized();
            speed = Mathf.Clamp(speed, 0.1f, 3f);
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var main = _particleSystems[i].main;
                main.simulationSpeed = speed;
            }
        }

        /// <summary>
        /// カラーティントを設定。ベースカラーに乗算（alpha は保持）。
        /// </summary>
        public void SetColorTint(Color tint)
        {
            EnsureInitialized();
            for (int i = 0; i < _particleSystems.Length; i++)
            {
                var main = _particleSystems[i].main;
                Color baseCol = _baseStartColors[i];
                Color result = new Color(
                    baseCol.r * tint.r,
                    baseCol.g * tint.g,
                    baseCol.b * tint.b,
                    baseCol.a
                );
                main.startColor = result;
            }
        }

        private IEnumerator AutoDestroyCoroutine()
        {
            // パーティクルの放出時間
            float emitDuration = Mathf.Max(0f, autoDestroyTime - 1f);
            yield return new WaitForSeconds(emitDuration);

            // 放出停止
            StopAllParticles();

            // 残りのパーティクルが消えるまで待機
            yield return new WaitForSeconds(1f);

            // オブジェクト破棄
            Destroy(gameObject);
        }
    }
}
