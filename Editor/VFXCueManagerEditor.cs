#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

namespace SpatialVFXCue
{
    [CustomEditor(typeof(VFXCueManager))]
    public class VFXCueManagerEditor : Editor
    {
        private SerializedProperty _cues;
        private SerializedProperty _adminOnly;
        private SerializedProperty _maxConcurrentVFX;
        private bool[] _foldouts;

        private static readonly Color HeaderColor = new Color(0.2f, 0.6f, 0.9f, 1f);

        private void OnEnable()
        {
            _cues = serializedObject.FindProperty("cues");
            _adminOnly = serializedObject.FindProperty("adminOnly");
            _maxConcurrentVFX = serializedObject.FindProperty("maxConcurrentVFX");
            RefreshFoldouts();
        }

        private void RefreshFoldouts()
        {
            int count = _cues.arraySize;
            bool[] newFoldouts = new bool[count];
            if (_foldouts != null)
            {
                for (int i = 0; i < Mathf.Min(_foldouts.Length, count); i++)
                    newFoldouts[i] = _foldouts[i];
            }
            _foldouts = newFoldouts;
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            // ヘッダー
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 16,
                alignment = TextAnchor.MiddleCenter
            };
            Color prevColor = GUI.color;
            GUI.color = HeaderColor;
            EditorGUILayout.LabelField("SpatialVFXCue Manager", headerStyle);
            GUI.color = prevColor;
            EditorGUILayout.Space(4);

            // 設定セクション
            EditorGUILayout.LabelField("設定", EditorStyles.boldLabel);
            EditorGUI.indentLevel++;
            EditorGUILayout.PropertyField(_adminOnly, new GUIContent("管理者のみ操作可"));
            EditorGUILayout.PropertyField(_maxConcurrentVFX, new GUIContent("同時スポーン上限"));
            EditorGUI.indentLevel--;
            EditorGUILayout.Space(8);

            // キュー一覧ヘッダー
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.LabelField($"VFX キュー一覧 ({_cues.arraySize})", EditorStyles.boldLabel);
            if (GUILayout.Button("+ 追加", GUILayout.Width(60)))
            {
                _cues.InsertArrayElementAtIndex(_cues.arraySize);
                RefreshFoldouts();
                // 新規要素のデフォルト値設定
                SerializedProperty newCue = _cues.GetArrayElementAtIndex(_cues.arraySize - 1);
                newCue.FindPropertyRelative("cueName").stringValue = $"VFX_{_cues.arraySize}";
                newCue.FindPropertyRelative("autoDestroyTime").floatValue = 5f;
                newCue.FindPropertyRelative("cooldown").floatValue = 0.5f;
            }
            EditorGUILayout.EndHorizontal();
            EditorGUILayout.Space(2);

            if (_foldouts.Length != _cues.arraySize)
                RefreshFoldouts();

            // 各キューの描画
            int removeIndex = -1;
            for (int i = 0; i < _cues.arraySize; i++)
            {
                SerializedProperty cue = _cues.GetArrayElementAtIndex(i);
                string name = cue.FindPropertyRelative("cueName").stringValue;
                KeyCode key = (KeyCode)cue.FindPropertyRelative("triggerKey").intValue;

                EditorGUILayout.BeginVertical("box");

                EditorGUILayout.BeginHorizontal();
                _foldouts[i] = EditorGUILayout.Foldout(_foldouts[i], $"[{key}] {name}", true);
                if (GUILayout.Button("×", GUILayout.Width(24)))
                {
                    removeIndex = i;
                }
                EditorGUILayout.EndHorizontal();

                if (_foldouts[i])
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("cueName"), new GUIContent("名前"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("triggerKey"), new GUIContent("キー"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("vfxPrefab"), new GUIContent("VFX Prefab"));

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("位置設定", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("spawnAnchor"), new GUIContent("基準 Transform"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("worldPosition"), new GUIContent("ワールド座標"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("positionOffset"), new GUIContent("オフセット"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("rotationEuler"), new GUIContent("回転（Euler）"));

                    EditorGUILayout.Space(4);
                    EditorGUILayout.LabelField("タイミング", EditorStyles.miniLabel);
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("autoDestroyTime"), new GUIContent("自動消滅（秒）"));
                    EditorGUILayout.PropertyField(cue.FindPropertyRelative("cooldown"), new GUIContent("クールダウン（秒）"));
                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            if (removeIndex >= 0)
            {
                _cues.DeleteArrayElementAtIndex(removeIndex);
                RefreshFoldouts();
            }

            serializedObject.ApplyModifiedProperties();
        }
    }
}
#endif
