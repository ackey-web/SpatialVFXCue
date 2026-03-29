#if UNITY_EDITOR
using System.IO;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using SpatialSys.UnitySDK;

namespace SpatialVFXCue
{
    public static class VFXCueSetupWizard
    {
        private const string BasePath = "Assets/SpatialVFXCue";
        private const string TexturePath = BasePath + "/Textures";
        private const string MaterialPath = BasePath + "/Materials";
        private const string PrefabPath = BasePath + "/Prefabs";
        private const string ScenePath = BasePath + "/Scenes";

        [MenuItem("SpatialVFXCue/Setup Sample Assets")]
        public static void SetupSampleAssets()
        {
            EnsureDirectories();
            CreateTextures();
            AssetDatabase.Refresh();
            CreateMaterials();
            AssetDatabase.Refresh();
            CreatePrefabs();
            AssetDatabase.Refresh();
            CreateDemoScene();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            string message = "サンプルアセットの生成が完了しました！\n\n" +
                "【生成されたもの】\n" +
                "- テクスチャ: 4種\n" +
                "- マテリアル: 6種\n" +
                "- VFX Prefab: 6種\n" +
                "- デモシーン: 1つ（全機能設定済み）\n\n" +
                "【次のステップ】\n" +
                "Space Package Config を開き、以下を手動で設定してください：\n" +
                "- C# Assembly: SpatialVFXCue\n" +
                "- Scene: SpatialVFXCue_Demo（または既存シーン）\n" +
                "- Network Prefabs: VFX_Fireworks 等 6種を登録";

            UnityEditor.EditorUtility.DisplayDialog("SpatialVFXCue Setup", message, "OK");
        }

        [MenuItem("SpatialVFXCue/Repair Scene Components")]
        public static void RepairSceneComponents()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (!scene.IsValid() || !scene.isLoaded)
            {
                UnityEditor.EditorUtility.DisplayDialog("SpatialVFXCue", "シーンが開かれていません。", "OK");
                return;
            }

            int added = 0;
            var roots = scene.GetRootGameObjects();
            foreach (var root in roots)
            {
                // VFXCueManager が無いシーンにはマネージャーを追加しない（手動配置前提）
                // 既存の VFXCueManager があれば、関連コンポーネントの欠落をチェック
                var manager = root.GetComponentInChildren<VFXCueManager>(true);
                if (manager != null)
                {
                    if (manager.GetComponent<VFXCueLog>() == null)
                    {
                        manager.gameObject.AddComponent<VFXCueLog>();
                        added++;
                    }
                    if (manager.GetComponent<VFXCueBPMSync>() == null)
                    {
                        manager.gameObject.AddComponent<VFXCueBPMSync>();
                        added++;
                    }
                    if (manager.GetComponent<VFXCueVJController>() == null)
                    {
                        manager.gameObject.AddComponent<VFXCueVJController>();
                        added++;
                    }
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(scene);

            string message = added > 0
                ? $"修復完了: {added} 個のコンポーネントを追加しました。\nシーンを保存してください。"
                : "修復不要: すべてのコンポーネントが正常です。";
            UnityEditor.EditorUtility.DisplayDialog("SpatialVFXCue Repair", message, "OK");
        }

        private static void EnsureDirectories()
        {
            string[] dirs = { TexturePath, MaterialPath, PrefabPath, ScenePath };
            foreach (string dir in dirs)
            {
                if (!AssetDatabase.IsValidFolder(dir))
                {
                    string parent = Path.GetDirectoryName(dir).Replace("\\", "/");
                    string folderName = Path.GetFileName(dir);
                    AssetDatabase.CreateFolder(parent, folderName);
                }
            }
        }

        // =====================================================================
        //  ユーティリティ
        // =====================================================================

        private static Shader FindParticleShader()
        {
            Shader s = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (s == null) s = Shader.Find("Particles/Standard Unlit");
            if (s == null) Debug.LogError("[SpatialVFXCue] URP Particle Shader が見つかりません");
            return s;
        }

        private static Material CreateMat(string name, Shader shader, Texture2D tex, int blendMode, string keyword, int renderQueue = 3000)
        {
            string path = $"{MaterialPath}/{name}.mat";
            // 既存を削除
            if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
                AssetDatabase.DeleteAsset(path);

            Material mat = new Material(shader);
            mat.SetFloat("_Surface", 1f);
            mat.SetFloat("_Blend", blendMode);
            mat.EnableKeyword(keyword);
            mat.renderQueue = renderQueue;
            if (tex != null) mat.SetTexture("_BaseMap", tex);
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        private static ParticleSystem AddPS(GameObject parent, string childName, Material mat)
        {
            GameObject child = new GameObject(childName);
            child.transform.SetParent(parent.transform);
            child.transform.localPosition = Vector3.zero;
            child.transform.localRotation = Quaternion.identity;
            ParticleSystem ps = child.AddComponent<ParticleSystem>();
            var r = child.GetComponent<ParticleSystemRenderer>();
            r.sharedMaterial = mat;
            r.renderMode = ParticleSystemRenderMode.Billboard;
            var main = ps.main;
            main.playOnAwake = false;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            return ps;
        }

        private static GameObject CreateBasePrefab(string name)
        {
            GameObject go = new GameObject(name);
            go.AddComponent<SpatialNetworkObject>();
            go.AddComponent<VFXCueEffect>();
            return go;
        }

        private static void SavePrefab(GameObject go, string name)
        {
            string path = $"{PrefabPath}/{name}.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }

        private static Gradient MakeGradient(Color c1, float a1, Color c2, float a2)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c1, 0f), new GradientColorKey(c2, 1f) },
                new[] { new GradientAlphaKey(a1, 0f), new GradientAlphaKey(a2, 1f) }
            );
            return g;
        }

        private static Gradient MakeGradient3(Color c1, float a0, float a1, float a2)
        {
            Gradient g = new Gradient();
            g.SetKeys(
                new[] { new GradientColorKey(c1, 0f), new GradientColorKey(c1, 1f) },
                new[] { new GradientAlphaKey(a0, 0f), new GradientAlphaKey(a1, 0.3f), new GradientAlphaKey(a2, 1f) }
            );
            return g;
        }

        private static AnimationCurve CurveEase(float v0, float v1)
        {
            return AnimationCurve.EaseInOut(0f, v0, 1f, v1);
        }

        // =====================================================================
        //  テクスチャ生成（128x128 高品質）
        // =====================================================================

        #region Textures

        private static void CreateTextures()
        {
            CreateCircleTexture();
            CreateStarTexture();
            CreateStreakTexture();
            CreateSmokeTexture();
        }

        private static void CreateCircleTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float alpha = Mathf.Clamp01(1f - dist);
                    alpha = Mathf.Pow(alpha, 1.5f); // ソフトグロー
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, TexturePath + "/Particle_Circle.png");
        }

        private static void CreateStarTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - center) / center;
                    float dy = (y - center) / center;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    // 4本の光条
                    float angle = Mathf.Atan2(dy, dx);
                    float spike = Mathf.Abs(Mathf.Cos(angle * 2f));
                    spike = Mathf.Pow(spike, 8f) * 0.8f;
                    float core = Mathf.Pow(Mathf.Clamp01(1f - dist), 3f);
                    float rays = Mathf.Pow(Mathf.Clamp01(1f - dist * 0.7f), 2f) * spike;
                    float alpha = Mathf.Clamp01(core + rays);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, TexturePath + "/Particle_Star.png");
        }

        private static void CreateStreakTexture()
        {
            int width = 128, height = 16;
            Texture2D tex = new Texture2D(width, height, TextureFormat.RGBA32, false);
            float cy = height / 2f;
            for (int y = 0; y < height; y++)
            {
                float vy = 1f - Mathf.Abs(y - cy) / cy;
                vy = Mathf.Pow(vy, 2f);
                for (int x = 0; x < width; x++)
                {
                    float vx = 1f - (float)x / width;
                    vx = Mathf.Pow(vx, 0.7f);
                    float alpha = vx * vy;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, TexturePath + "/Particle_Streak.png");
        }

        private static void CreateSmokeTexture()
        {
            int size = 128;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            float center = size / 2f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), new Vector2(center, center)) / center;
                    float n1 = Mathf.PerlinNoise(x * 0.06f + 100f, y * 0.06f + 100f);
                    float n2 = Mathf.PerlinNoise(x * 0.12f + 50f, y * 0.12f + 50f) * 0.5f;
                    float n3 = Mathf.PerlinNoise(x * 0.25f, y * 0.25f) * 0.25f;
                    float noise = (n1 + n2 + n3) / 1.75f;
                    float falloff = Mathf.Pow(Mathf.Clamp01(1f - dist), 1.5f);
                    float alpha = Mathf.Clamp01(falloff * noise * 1.8f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
            tex.Apply();
            SaveTexture(tex, TexturePath + "/Particle_Smoke.png");
        }

        private static void SaveTexture(Texture2D tex, string path)
        {
            byte[] pngData = tex.EncodeToPNG();
            string fullPath = Path.Combine(Application.dataPath, "..", path);
            File.WriteAllBytes(fullPath, pngData);
            Object.DestroyImmediate(tex);
        }

        #endregion

        // =====================================================================
        //  マテリアル生成（VFX ごとに専用マテリアル）
        // =====================================================================

        #region Materials

        private static Material _matAdditiveCircle, _matAdditiveStar, _matAdditiveStreak;
        private static Material _matAlphaCircle, _matAlphaSmoke, _matSoftStreak;

        private static void CreateMaterials()
        {
            Shader shader = FindParticleShader();
            if (shader == null) return;

            Texture2D circle = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}/Particle_Circle.png");
            Texture2D star = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}/Particle_Star.png");
            Texture2D streak = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}/Particle_Streak.png");
            Texture2D smoke = AssetDatabase.LoadAssetAtPath<Texture2D>($"{TexturePath}/Particle_Smoke.png");

            _matAdditiveCircle = CreateMat("VFX_Additive_Circle", shader, circle, 1, "_BLENDMODE_ADD");
            _matAdditiveStar   = CreateMat("VFX_Additive_Star",   shader, star,   1, "_BLENDMODE_ADD");
            _matAdditiveStreak = CreateMat("VFX_Additive_Streak", shader, streak, 1, "_BLENDMODE_ADD");
            _matAlphaCircle    = CreateMat("VFX_Alpha_Circle",    shader, circle, 0, "_BLENDMODE_ALPHA");
            _matAlphaSmoke     = CreateMat("VFX_Alpha_Smoke",     shader, smoke,  0, "_BLENDMODE_ALPHA");
            _matSoftStreak     = CreateMat("VFX_Soft_Streak",     shader, streak, 4, "_BLENDMODE_PREMULTIPLY");
        }

        #endregion

        // =====================================================================
        //  Prefab 生成（本番品質）
        // =====================================================================

        #region Prefabs

        private static void CreatePrefabs()
        {
            // マテリアルをロード（CreateMaterials で static に保存済み、念のため再ロード）
            if (_matAdditiveCircle == null)
            {
                _matAdditiveCircle = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialPath}/VFX_Additive_Circle.mat");
                _matAdditiveStar   = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialPath}/VFX_Additive_Star.mat");
                _matAdditiveStreak = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialPath}/VFX_Additive_Streak.mat");
                _matAlphaCircle    = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialPath}/VFX_Alpha_Circle.mat");
                _matAlphaSmoke     = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialPath}/VFX_Alpha_Smoke.mat");
                _matSoftStreak     = AssetDatabase.LoadAssetAtPath<Material>($"{MaterialPath}/VFX_Soft_Streak.mat");
            }

            CreateFireworksPrefab();
            CreateConfettiPrefab();
            CreateMeteorShowerPrefab();
            CreateSpotlightBeamPrefab();
            CreateSmokeBurstPrefab();
            CreateSparkleRainPrefab();
        }

        // ── 花火 ──────────────────────────────────────────────────────────────
        private static void CreateFireworksPrefab()
        {
            GameObject go = CreateBasePrefab("VFX_Fireworks");

            // 1) 打ち上げコア（明るい芯）
            {
                ParticleSystem ps = AddPS(go, "LaunchCore", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 0.6f;
                m.startLifetime = 0.5f;
                m.startSpeed = 25f;
                m.startSize = 0.4f;
                m.startColor = new Color(1f, 0.95f, 0.8f);
                m.maxParticles = 5;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 3) });
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Cone;
                sh.angle = 3f;
                sh.radius = 0.05f;
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(1f, 0.1f));
            }

            // 2) 打ち上げトレイル（火花の尾）
            {
                ParticleSystem ps = AddPS(go, "LaunchTrail", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 0.7f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.2f, 0.5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(18f, 26f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.15f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.8f, 0.3f), new Color(1f, 0.6f, 0.1f));
                m.maxParticles = 80;
                var em = ps.emission;
                em.rateOverTime = 120f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Cone;
                sh.angle = 6f;
                sh.radius = 0.05f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient(new Color(1f, 0.9f, 0.5f), 1f, new Color(1f, 0.3f, 0f), 0f);
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(1f, 0f));
            }

            // 3) 開花バースト（メイン）
            {
                ParticleSystem ps = AddPS(go, "BurstMain", _matAdditiveStar);
                var m = ps.main;
                m.duration = 0.1f;
                m.startDelay = 0.7f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1.0f, 2.0f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(5f, 12f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.4f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.4f, 0.1f), new Color(1f, 0.9f, 0.3f));
                m.maxParticles = 250;
                m.gravityModifier = 0.6f;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 150) });
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.3f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] {
                        new GradientColorKey(new Color(1f, 1f, 0.8f), 0f),
                        new GradientColorKey(new Color(1f, 0.5f, 0.1f), 0.4f),
                        new GradientColorKey(new Color(0.8f, 0.1f, 0f), 1f)
                    },
                    new[] {
                        new GradientAlphaKey(1f, 0f),
                        new GradientAlphaKey(0.8f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = g;
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(0.8f, 0f));
            }

            // 4) 開花グロー（中心の光）
            {
                ParticleSystem ps = AddPS(go, "BurstGlow", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 0.1f;
                m.startDelay = 0.7f;
                m.startLifetime = 0.5f;
                m.startSpeed = 0f;
                m.startSize = 5f;
                m.startColor = new Color(1f, 0.8f, 0.4f, 0.6f);
                m.maxParticles = 1;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                var sh = ps.shape;
                sh.enabled = false;
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(1f, 3f));
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient(new Color(1f, 0.8f, 0.4f), 0.8f, new Color(1f, 0.3f, 0f), 0f);
            }

            // 5) 残り火（ゆっくり落ちる火花）
            {
                ParticleSystem ps = AddPS(go, "Embers", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 0.3f;
                m.startDelay = 0.9f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1.5f, 3f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 4f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.03f, 0.08f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.6f, 0.1f), new Color(1f, 0.3f, 0f));
                m.maxParticles = 200;
                m.gravityModifier = 1.2f;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 100) });
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 2f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient(new Color(1f, 0.5f, 0.1f), 0.8f, new Color(0.5f, 0.1f, 0f), 0f);
            }

            SavePrefab(go, "VFX_Fireworks");
        }

        // ── 紙吹雪 ────────────────────────────────────────────────────────────
        private static void CreateConfettiPrefab()
        {
            GameObject go = CreateBasePrefab("VFX_Confetti");

            // 1) メイン紙吹雪
            {
                ParticleSystem ps = AddPS(go, "ConfettiMain", _matAlphaCircle);
                var m = ps.main;
                m.duration = 1f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.08f, 0.2f);
                m.startSize3D = true;
                m.startSizeX = new ParticleSystem.MinMaxCurve(0.1f, 0.25f);
                m.startSizeY = new ParticleSystem.MinMaxCurve(0.06f, 0.12f);
                m.startSizeZ = 1f;
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.1f, 0.2f), new Color(0.1f, 0.5f, 1f));
                m.maxParticles = 500;
                m.gravityModifier = 0.6f;
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] {
                    new ParticleSystem.Burst(0f, 200),
                    new ParticleSystem.Burst(0.3f, 100)
                });
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Cone;
                sh.angle = 50f;
                sh.radius = 1.5f;
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.x = new ParticleSystem.MinMaxCurve(-200f, 200f);
                rot.y = new ParticleSystem.MinMaxCurve(-200f, 200f);
                rot.z = new ParticleSystem.MinMaxCurve(-300f, 300f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient3(Color.white, 1f, 1f, 0f);
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 1f;
                noise.frequency = 0.5f;
                noise.scrollSpeed = 0.3f;
            }

            // 2) 金色のキラキラ
            {
                ParticleSystem ps = AddPS(go, "GoldGlitter", _matAdditiveStar);
                var m = ps.main;
                m.duration = 0.5f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(3f, 8f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.85f, 0.3f), new Color(1f, 0.95f, 0.7f));
                m.maxParticles = 100;
                m.gravityModifier = 0.3f;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 60) });
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Cone;
                sh.angle = 60f;
                sh.radius = 0.5f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient(new Color(1f, 0.9f, 0.5f), 1f, new Color(1f, 0.7f, 0.2f), 0f);
            }

            SavePrefab(go, "VFX_Confetti");
        }

        // ── 流星群 ─────────────────────────────────────────────────────────────
        private static void CreateMeteorShowerPrefab()
        {
            GameObject go = CreateBasePrefab("VFX_MeteorShower");

            // 1) 流星本体
            {
                ParticleSystem ps = AddPS(go, "MeteorCore", _matAdditiveStreak);
                var r = ps.GetComponent<ParticleSystemRenderer>();
                r.renderMode = ParticleSystemRenderMode.Stretch;
                r.lengthScale = 4f;
                r.velocityScale = 0.1f;
                var m = ps.main;
                m.duration = 4f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.8f, 1.5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(18f, 30f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.15f, 0.35f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.9f, 0.7f), new Color(0.7f, 0.85f, 1f));
                m.maxParticles = 60;
                m.gravityModifier = 0.2f;
                var em = ps.emission;
                em.rateOverTime = 12f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = new Vector3(20f, 0.5f, 20f);
                sh.position = new Vector3(0f, 15f, 0f);
                sh.rotation = new Vector3(25f, 15f, 0f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] {
                        new GradientColorKey(new Color(0.8f, 0.9f, 1f), 0f),
                        new GradientColorKey(new Color(1f, 0.8f, 0.4f), 0.5f),
                        new GradientColorKey(new Color(1f, 0.3f, 0f), 1f)
                    },
                    new[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.1f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = g;
            }

            // 2) 流星の残光トレイル
            {
                ParticleSystem ps = AddPS(go, "MeteorGlow", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 4f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.8f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(15f, 28f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.4f, 0.8f);
                m.startColor = new Color(0.5f, 0.6f, 1f, 0.3f);
                m.maxParticles = 100;
                m.gravityModifier = 0.2f;
                var em = ps.emission;
                em.rateOverTime = 15f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = new Vector3(20f, 0.5f, 20f);
                sh.position = new Vector3(0f, 15f, 0f);
                sh.rotation = new Vector3(25f, 15f, 0f);
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(1f, 0f));
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient(new Color(0.6f, 0.7f, 1f), 0.4f, new Color(0.3f, 0.3f, 1f), 0f);
            }

            // 3) 背景の星屑
            {
                ParticleSystem ps = AddPS(go, "Stardust", _matAdditiveStar);
                var m = ps.main;
                m.duration = 4f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 3f);
                m.startSpeed = 0f;
                m.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.06f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 1f, 1f, 0.3f), new Color(0.7f, 0.8f, 1f, 0.5f));
                m.maxParticles = 200;
                var em = ps.emission;
                em.rateOverTime = 40f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = new Vector3(25f, 15f, 25f);
                sh.position = new Vector3(0f, 8f, 0f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient3(Color.white, 0f, 0.5f, 0f);
            }

            SavePrefab(go, "VFX_MeteorShower");
        }

        // ── スポットライト ─────────────────────────────────────────────────────
        private static void CreateSpotlightBeamPrefab()
        {
            GameObject go = CreateBasePrefab("VFX_SpotlightBeam");

            // シンプルな光球（ずっと同じサイズで光り続ける）
            {
                ParticleSystem ps = AddPS(go, "Light", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 10f;
                m.loop = true;
                m.startLifetime = 10f;
                m.startSpeed = 0f;
                m.startSize = 5f;
                m.startColor = new Color(0.4f, 0.7f, 1f, 0.9f);
                m.maxParticles = 1;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                var sh = ps.shape;
                sh.enabled = false;
            }

            SavePrefab(go, "VFX_SpotlightBeam");
        }

        // ── スモーク爆発 ───────────────────────────────────────────────────────
        private static void CreateSmokeBurstPrefab()
        {
            GameObject go = CreateBasePrefab("VFX_SmokeBurst");

            // 1) 中心フラッシュ
            {
                ParticleSystem ps = AddPS(go, "Flash", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 0.1f;
                m.startLifetime = 0.3f;
                m.startSpeed = 0f;
                m.startSize = 6f;
                m.startColor = new Color(1f, 0.95f, 0.85f, 0.8f);
                m.maxParticles = 1;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 1) });
                var sh = ps.shape;
                sh.enabled = false;
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(1f, 2.5f));
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient(Color.white, 1f, new Color(1f, 0.8f, 0.5f), 0f);
            }

            // 2) メインスモーク
            {
                ParticleSystem ps = AddPS(go, "SmokeMain", _matAlphaSmoke);
                var m = ps.main;
                m.duration = 0.3f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(2.5f, 4.5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(4f, 10f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.5f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(0.85f, 0.85f, 0.85f, 0.6f), new Color(0.5f, 0.5f, 0.5f, 0.4f));
                m.maxParticles = 200;
                m.gravityModifier = -0.15f;
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 100) });
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.8f;
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(0.4f, 2.5f));
                var rot = ps.rotationOverLifetime;
                rot.enabled = true;
                rot.z = new ParticleSystem.MinMaxCurve(-30f, 30f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] {
                        new GradientColorKey(new Color(0.9f, 0.9f, 0.9f), 0f),
                        new GradientColorKey(new Color(0.6f, 0.6f, 0.6f), 0.5f),
                        new GradientColorKey(new Color(0.3f, 0.3f, 0.3f), 1f)
                    },
                    new[] {
                        new GradientAlphaKey(0.5f, 0f),
                        new GradientAlphaKey(0.3f, 0.5f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = g;
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 1.5f;
                noise.frequency = 0.5f;
            }

            // 3) 衝撃波リング風の散らばり
            {
                ParticleSystem ps = AddPS(go, "ShockDebris", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 0.1f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(0.5f, 1.5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(8f, 15f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.9f, 0.7f), new Color(1f, 0.6f, 0.2f));
                m.maxParticles = 80;
                m.gravityModifier = 1f;
                var em = ps.emission;
                em.rateOverTime = 0;
                em.SetBursts(new[] { new ParticleSystem.Burst(0f, 50) });
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Sphere;
                sh.radius = 0.3f;
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient(new Color(1f, 0.8f, 0.4f), 1f, new Color(1f, 0.3f, 0f), 0f);
            }

            SavePrefab(go, "VFX_SmokeBurst");
        }

        // ── キラキラの雨 ───────────────────────────────────────────────────────
        private static void CreateSparkleRainPrefab()
        {
            GameObject go = CreateBasePrefab("VFX_SparkleRain");

            // 1) メインキラキラ
            {
                ParticleSystem ps = AddPS(go, "SparkleMain", _matAdditiveStar);
                var m = ps.main;
                m.duration = 4f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.5f, 2f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.04f, 0.18f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.95f, 0.7f), new Color(0.7f, 0.85f, 1f));
                m.maxParticles = 500;
                m.gravityModifier = 0.4f;
                m.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
                var em = ps.emission;
                em.rateOverTime = 80f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = new Vector3(15f, 0.5f, 15f);
                sh.position = new Vector3(0f, 12f, 0f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                Gradient g = new Gradient();
                g.SetKeys(
                    new[] {
                        new GradientColorKey(Color.white, 0f),
                        new GradientColorKey(new Color(1f, 0.95f, 0.8f), 0.5f),
                        new GradientColorKey(new Color(0.8f, 0.9f, 1f), 1f)
                    },
                    new[] {
                        new GradientAlphaKey(0f, 0f),
                        new GradientAlphaKey(1f, 0.05f),
                        new GradientAlphaKey(0.6f, 0.7f),
                        new GradientAlphaKey(0f, 1f)
                    }
                );
                col.color = g;
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 0.6f;
                noise.frequency = 0.8f;
                noise.scrollSpeed = 0.2f;
            }

            // 2) 大きめのグロー粒子
            {
                ParticleSystem ps = AddPS(go, "GlowOrbs", _matAdditiveCircle);
                var m = ps.main;
                m.duration = 4f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(3f, 5f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(0.3f, 1f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.3f, 0.7f);
                m.startColor = new ParticleSystem.MinMaxGradient(
                    new Color(1f, 0.9f, 0.5f, 0.15f), new Color(0.5f, 0.7f, 1f, 0.1f));
                m.maxParticles = 60;
                m.gravityModifier = 0.3f;
                var em = ps.emission;
                em.rateOverTime = 10f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = new Vector3(15f, 0.5f, 15f);
                sh.position = new Vector3(0f, 12f, 0f);
                var sol = ps.sizeOverLifetime;
                sol.enabled = true;
                sol.size = new ParticleSystem.MinMaxCurve(1f, CurveEase(0.5f, 1.5f));
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient3(new Color(1f, 0.95f, 0.8f), 0f, 0.15f, 0f);
                var noise = ps.noise;
                noise.enabled = true;
                noise.strength = 1f;
                noise.frequency = 0.5f;
            }

            // 3) 極小キラキラ（密度感）
            {
                ParticleSystem ps = AddPS(go, "TinySparkles", _matAdditiveStar);
                var m = ps.main;
                m.duration = 4f;
                m.startLifetime = new ParticleSystem.MinMaxCurve(1f, 2f);
                m.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f);
                m.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.04f);
                m.startColor = new Color(1f, 1f, 1f, 0.9f);
                m.maxParticles = 300;
                m.gravityModifier = 0.6f;
                var em = ps.emission;
                em.rateOverTime = 60f;
                var sh = ps.shape;
                sh.shapeType = ParticleSystemShapeType.Box;
                sh.scale = new Vector3(12f, 0.5f, 12f);
                sh.position = new Vector3(0f, 10f, 0f);
                var col = ps.colorOverLifetime;
                col.enabled = true;
                col.color = MakeGradient3(Color.white, 0f, 1f, 0f);
            }

            SavePrefab(go, "VFX_SparkleRain");
        }

        #endregion

        // =====================================================================
        //  デモシーン
        // =====================================================================

        #region Demo Scene

        private static void CreateDemoScene()
        {
            string scenePath = $"{ScenePath}/SpatialVFXCue_Demo.unity";
            var scene = EditorSceneManager.NewScene(NewSceneSetup.DefaultGameObjects, NewSceneMode.Single);

            // 環境を暗めに（VFX が映える）
            RenderSettings.ambientIntensity = 0.3f;

            GameObject managerObj = new GameObject("VFXCueManager");
            VFXCueManager manager = managerObj.AddComponent<VFXCueManager>();
            manager.adminOnly = false; // テスト用にオフ
            managerObj.AddComponent<VFXCueLog>();

            string[] prefabNames = {
                "VFX_Fireworks", "VFX_Confetti", "VFX_MeteorShower",
                "VFX_SpotlightBeam", "VFX_SmokeBurst", "VFX_SparkleRain"
            };
            string[] cueNames = {
                "花火", "紙吹雪", "流星群", "スポットライト", "スモーク爆発", "キラキラの雨"
            };
            KeyCode[] keys = {
                KeyCode.Alpha1, KeyCode.Alpha2, KeyCode.Alpha3,
                KeyCode.Alpha4, KeyCode.Alpha5, KeyCode.Alpha6
            };

            for (int i = 0; i < prefabNames.Length; i++)
            {
                string prefabAssetPath = $"{PrefabPath}/{prefabNames[i]}.prefab";
                SpatialNetworkObject prefab = null;
                GameObject prefabGo = AssetDatabase.LoadAssetAtPath<GameObject>(prefabAssetPath);
                if (prefabGo != null)
                    prefab = prefabGo.GetComponent<SpatialNetworkObject>();

                VFXCueEntry entry = new VFXCueEntry
                {
                    triggerKey = keys[i],
                    vfxPrefab = prefab,
                    cueName = cueNames[i],
                    worldPosition = new Vector3(0f, 3f, 0f),
                    autoDestroyTime = 6f,
                    cooldown = 0.5f
                };
                manager.cues.Add(entry);
            }

            // スポットライトは一瞬フラッシュなので消滅を短く
            if (manager.cues.Count > 3)
                manager.cues[3].autoDestroyTime = 3f;

            // ── BPM 同期（自動設定済み） ──
            GameObject bpmObj = new GameObject("VFXCueBPMSync");
            VFXCueBPMSync bpmSync = bpmObj.AddComponent<VFXCueBPMSync>();
            bpmSync.cueManager = manager;
            bpmSync.bpm = 128f;
            bpmSync.beatInterval = 1f;
            bpmSync.tapTempoKey = KeyCode.T;
            bpmSync.beatStartKey = KeyCode.B;
            bpmSync.showBeatFlash = true;
            // キー → キュー名のマッピングを自動設定
            for (int i = 0; i < cueNames.Length; i++)
            {
                VFXCueBPMSync.BPMCueMapping mapping = new VFXCueBPMSync.BPMCueMapping();
                mapping.key = keys[i];
                mapping.cueName = cueNames[i];
                bpmSync.cueMappings.Add(mapping);
            }

            // ── ランダム VFX（自動設定済み） ──
            GameObject randomObj = new GameObject("VFXCueRandom");
            VFXCueRandom vfxRandom = randomObj.AddComponent<VFXCueRandom>();
            vfxRandom.cueManager = manager;
            vfxRandom.triggerKey = KeyCode.Alpha8;
            vfxRandom.cooldown = 1f;
            vfxRandom.avoidRepeat = true;
            vfxRandom.cueNames = new System.Collections.Generic.List<string>(cueNames);

            // ── チェイン演出（自動設定済み） ──
            GameObject chainObj = new GameObject("VFXCueChain");
            VFXCueChain vfxChain = chainObj.AddComponent<VFXCueChain>();
            vfxChain.cueManager = manager;
            vfxChain.triggerKey = KeyCode.Alpha7;
            // 花火 → 紙吹雪 → キラキラの雨 の連続演出
            vfxChain.chainSteps = new System.Collections.Generic.List<VFXCueChain.ChainStep>();
            string[] chainCues = { "花火", "紙吹雪", "キラキラの雨" };
            float[] chainDelays = { 0f, 1.5f, 3f };
            for (int i = 0; i < chainCues.Length; i++)
            {
                VFXCueChain.ChainStep step = new VFXCueChain.ChainStep();
                step.cueName = chainCues[i];
                step.delay = chainDelays[i];
                vfxChain.chainSteps.Add(step);
            }

            // ── カウントダウン演出（自動設定済み） ──
            GameObject countdownObj = new GameObject("VFXCueCountdown");
            VFXCueCountdown vfxCountdown = countdownObj.AddComponent<VFXCueCountdown>();
            vfxCountdown.cueManager = manager;
            vfxCountdown.countdownFrom = 3;
            vfxCountdown.intervalSeconds = 1f;
            vfxCountdown.triggerKey = KeyCode.Alpha9;
            // クライマックスで全 VFX 一斉発動
            vfxCountdown.climaxCueNames = new System.Collections.Generic.List<string>(cueNames);
            vfxCountdown.climaxStagger = 0.15f;

            // ── VJ コントローラー（自動配置） ──
            GameObject vjObj = new GameObject("VFXCueVJController");
            VFXCueVJController vjCtrl = vjObj.AddComponent<VFXCueVJController>();
            VFXCueVJPanel vjPanel = vjObj.AddComponent<VFXCueVJPanel>();
            vjCtrl.cueManager = manager;
            vjPanel.vjController = vjCtrl;

            // デフォルトレイヤー4つ
            vjCtrl.layers = new System.Collections.Generic.List<VFXCueVJLayer>
            {
                new VFXCueVJLayer { layerName = "All", layerColor = Color.white },
                new VFXCueVJLayer { layerName = "BG", layerColor = new Color(0.3f, 0.5f, 1f) },
                new VFXCueVJLayer { layerName = "FG", layerColor = new Color(1f, 0.4f, 0.3f) },
                new VFXCueVJLayer { layerName = "Accent", layerColor = new Color(1f, 0.8f, 0.2f) }
            };

            // キューのレイヤー割り当て（花火/紙吹雪→FG, 流星群/スモーク→BG, ライト/キラキラ→Accent）
            // 0=All, 1=BG, 2=FG, 3=Accent
            int[] layerAssignments = { 2, 2, 1, 3, 1, 3 };
            for (int i = 0; i < manager.cues.Count && i < layerAssignments.Length; i++)
            {
                manager.cues[i].layerIndex = layerAssignments[i];
            }

            // VFXCueManager に VJController を登録
            manager.vjController = vjCtrl;

            // ステージ
            GameObject stage = GameObject.CreatePrimitive(PrimitiveType.Plane);
            stage.name = "StageFloor";
            stage.transform.position = Vector3.zero;
            stage.transform.localScale = new Vector3(3f, 1f, 3f);

            EditorSceneManager.SaveScene(scene, scenePath);
            Debug.Log($"[SpatialVFXCue] デモシーンを作成しました（全機能設定済み）: {scenePath}");
        }

        #endregion
    }
}
#endif
