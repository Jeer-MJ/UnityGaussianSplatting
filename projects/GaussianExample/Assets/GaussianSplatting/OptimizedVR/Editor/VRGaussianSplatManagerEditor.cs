// SPDX-License-Identifier: MIT
// Custom Inspector for VRGaussianSplatManager

using UnityEngine;
using UnityEditor;

namespace GaussianSplatting.OptimizedVR.Editor
{
    [CustomEditor(typeof(VRGaussianSplatManager))]
    public class VRGaussianSplatManagerEditor : UnityEditor.Editor
    {
        SerializedProperty m_SplatObjects;
        SerializedProperty m_ActiveSplatIndex;
        SerializedProperty m_MinSortDistance;
        SerializedProperty m_MaxSortDistance;
        SerializedProperty m_CameraPositionQuantization;
        SerializedProperty m_AlwaysUpdate;
        SerializedProperty m_SeparateEyeSorting;
        SerializedProperty m_IPD;
        SerializedProperty m_DebugLog;
        
        void OnEnable()
        {
            m_SplatObjects = serializedObject.FindProperty("splatObjects");
            m_ActiveSplatIndex = serializedObject.FindProperty("activeSplatIndex");
            m_MinSortDistance = serializedObject.FindProperty("minSortDistance");
            m_MaxSortDistance = serializedObject.FindProperty("maxSortDistance");
            m_CameraPositionQuantization = serializedObject.FindProperty("cameraPositionQuantization");
            m_AlwaysUpdate = serializedObject.FindProperty("alwaysUpdate");
            m_SeparateEyeSorting = serializedObject.FindProperty("separateEyeSorting");
            m_IPD = serializedObject.FindProperty("ipd");
            m_DebugLog = serializedObject.FindProperty("debugLog");
        }
        
        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            
            EditorGUILayout.Space();
            EditorGUILayout.LabelField("VR Gaussian Splatting Manager", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                "Manages optimized Gaussian Splatting rendering for VR (Quest 3). " +
                "Handles efficient sorting and stereo rendering.", 
                MessageType.Info);
            
            EditorGUILayout.Space();
            
            // Splat Objects
            EditorGUILayout.LabelField("Splat Objects", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SplatObjects, true);
            EditorGUILayout.PropertyField(m_ActiveSplatIndex);
            
            EditorGUILayout.Space();
            
            // Sorting Configuration
            EditorGUILayout.LabelField("Sorting Configuration", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_MinSortDistance);
            EditorGUILayout.PropertyField(m_MaxSortDistance);
            EditorGUILayout.PropertyField(m_CameraPositionQuantization);
            EditorGUILayout.PropertyField(m_AlwaysUpdate);
            
            if (m_AlwaysUpdate.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "⚠️ Always Update is enabled! This will re-sort every frame and impact performance. " +
                    "Only use for testing.",
                    MessageType.Warning);
            }
            
            EditorGUILayout.Space();
            
            // VR Optimization
            EditorGUILayout.LabelField("VR Optimization", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_SeparateEyeSorting);
            
            if (m_SeparateEyeSorting.boolValue)
            {
                EditorGUILayout.HelpBox(
                    "Separate eye sorting provides better quality but approximately doubles sorting cost.",
                    MessageType.Info);
            }
            
            EditorGUILayout.PropertyField(m_IPD);
            
            EditorGUILayout.Space();
            
            // Debug
            EditorGUILayout.LabelField("Debug", EditorStyles.boldLabel);
            EditorGUILayout.PropertyField(m_DebugLog);
            
            EditorGUILayout.Space();
            
            // Setup buttons
            EditorGUILayout.LabelField("Quick Setup", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Setup RadixSort Component"))
            {
                SetupRadixSort();
            }
            
            if (GUILayout.Button("Create Optimized RenderTextures"))
            {
                CreateRenderTextures();
            }
            
            // Runtime stats (only in play mode)
            if (Application.isPlaying)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Runtime Statistics", EditorStyles.boldLabel);
                
                var manager = (VRGaussianSplatManager)target;
                int sortCount;
                Vector3 lastPos;
                manager.GetSortingStats(out sortCount, out lastPos);
                
                EditorGUILayout.LabelField($"Total Sorts: {sortCount}");
                EditorGUILayout.LabelField($"Last Sort Position: {lastPos}");
                EditorGUILayout.LabelField($"Current Frame: {Time.frameCount}");
                
                if (GUILayout.Button("Force Sort Next Frame"))
                {
                    manager.ForceSortNextFrame();
                }
            }
            
            serializedObject.ApplyModifiedProperties();
        }
        
        void SetupRadixSort()
        {
            var manager = (VRGaussianSplatManager)target;
            var radixSort = manager.GetComponent<RadixSortVR>();
            
            if (radixSort == null)
            {
                Debug.LogError("RadixSortVR component not found!");
                return;
            }
            
            // Create materials if missing
            if (radixSort.computeKeyValues == null)
            {
                var shader = Shader.Find("GaussianSplatting/VR/ComputeKeyValue");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.name = "ComputeKeyValues";
                    
                    // Save to assets
                    string path = "Assets/GaussianSplatting/OptimizedVR/Resources/ComputeKeyValues.mat";
                    AssetDatabase.CreateAsset(mat, path);
                    
                    radixSort.computeKeyValues = mat;
                    EditorUtility.SetDirty(radixSort);
                }
                else
                {
                    Debug.LogError("Shader 'GaussianSplatting/VR/ComputeKeyValue' not found!");
                }
            }
            
            if (radixSort.radixSort == null)
            {
                var shader = Shader.Find("GaussianSplatting/VR/RadixSort");
                if (shader != null)
                {
                    var mat = new Material(shader);
                    mat.name = "RadixSort";
                    
                    string path = "Assets/GaussianSplatting/OptimizedVR/Resources/RadixSort.mat";
                    AssetDatabase.CreateAsset(mat, path);
                    
                    radixSort.radixSort = mat;
                    EditorUtility.SetDirty(radixSort);
                }
                else
                {
                    Debug.LogError("Shader 'GaussianSplatting/VR/RadixSort' not found!");
                }
            }
            
            AssetDatabase.SaveAssets();
            Debug.Log("RadixSort materials created successfully!");
        }
        
        void CreateRenderTextures()
        {
            var manager = (VRGaussianSplatManager)target;
            var radixSort = manager.GetComponent<RadixSortVR>();
            
            if (radixSort == null)
            {
                Debug.LogError("RadixSortVR component not found!");
                return;
            }
            
            RenderTexture keyValues0, keyValues1, prefixSums;
            RadixSortVR.CreateOptimizedRenderTextures(
                out keyValues0,
                out keyValues1,
                out prefixSums,
                1024 // Resolution
            );
            
            // Save to assets
            string basePath = "Assets/GaussianSplatting/OptimizedVR/Resources/";
            
            AssetDatabase.CreateAsset(keyValues0, basePath + "KeyValues0.renderTexture");
            AssetDatabase.CreateAsset(keyValues1, basePath + "KeyValues1.renderTexture");
            AssetDatabase.CreateAsset(prefixSums, basePath + "PrefixSums.renderTexture");
            
            radixSort.keyValues0 = keyValues0;
            radixSort.keyValues1 = keyValues1;
            radixSort.prefixSums = prefixSums;
            
            EditorUtility.SetDirty(radixSort);
            AssetDatabase.SaveAssets();
            
            Debug.Log("RenderTextures created successfully!");
        }
    }
}
