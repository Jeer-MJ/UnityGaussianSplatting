// SPDX-License-Identifier: MIT
// Automatic setup wizard for VR Gaussian Splatting

using UnityEngine;
using UnityEditor;
using System.IO;

namespace GaussianSplatting.OptimizedVR.Editor
{
    public class VRGaussianSplattingSetup : EditorWindow
    {
        string m_ResourcesPath = "Assets/GaussianSplatting/OptimizedVR/Resources/";
        int m_TextureResolution = 1024;
        bool m_CreateExampleScene = true;
        
        [MenuItem("Tools/Gaussian Splatting/VR Setup Wizard")]
        static void ShowWindow()
        {
            var window = GetWindow<VRGaussianSplattingSetup>("VR GS Setup");
            window.minSize = new Vector2(400, 500);
            window.Show();
        }
        
        void OnGUI()
        {
            EditorGUILayout.Space(10);
            
            // Header
            GUIStyle headerStyle = new GUIStyle(EditorStyles.boldLabel);
            headerStyle.fontSize = 16;
            headerStyle.alignment = TextAnchor.MiddleCenter;
            
            EditorGUILayout.LabelField("VR Gaussian Splatting", headerStyle);
            EditorGUILayout.LabelField("Setup Wizard for Meta Quest 3", EditorStyles.centeredGreyMiniLabel);
            
            EditorGUILayout.Space(10);
            EditorGUILayout.HelpBox(
                "This wizard will set up the VR-optimized Gaussian Splatting system for your project.",
                MessageType.Info);
            
            EditorGUILayout.Space(10);
            
            // Configuration
            EditorGUILayout.LabelField("Configuration", EditorStyles.boldLabel);
            
            m_ResourcesPath = EditorGUILayout.TextField("Resources Path", m_ResourcesPath);
            
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.PrefixLabel("Texture Resolution");
            m_TextureResolution = EditorGUILayout.IntPopup(
                m_TextureResolution,
                new string[] { "512x512 (~250K splats)", "1024x1024 (~1M splats)", "2048x2048 (~4M splats)" },
                new int[] { 512, 1024, 2048 }
            );
            EditorGUILayout.EndHorizontal();
            
            m_CreateExampleScene = EditorGUILayout.Toggle("Create Example Scene", m_CreateExampleScene);
            
            EditorGUILayout.Space(20);
            
            // Setup Steps
            EditorGUILayout.LabelField("Setup Steps", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            EditorGUILayout.LabelField("✓ Create folder structure", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("✓ Create shader materials", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("✓ Create render textures", EditorStyles.miniLabel);
            EditorGUILayout.LabelField("✓ Setup VR manager prefab", EditorStyles.miniLabel);
            if (m_CreateExampleScene)
                EditorGUILayout.LabelField("✓ Create example scene", EditorStyles.miniLabel);
            EditorGUILayout.EndVertical();
            
            EditorGUILayout.Space(20);
            
            // Setup Button
            GUI.backgroundColor = Color.green;
            if (GUILayout.Button("Run Complete Setup", GUILayout.Height(40)))
            {
                RunSetup();
            }
            GUI.backgroundColor = Color.white;
            
            EditorGUILayout.Space(10);
            
            // Individual steps
            EditorGUILayout.LabelField("Individual Steps", EditorStyles.boldLabel);
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("1. Create Materials"))
            {
                CreateMaterials();
            }
            if (GUILayout.Button("2. Create RenderTextures"))
            {
                CreateRenderTextures();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("3. Create Manager Prefab"))
            {
                CreateManagerPrefab();
            }
            if (GUILayout.Button("4. Validate Setup"))
            {
                ValidateSetup();
            }
            EditorGUILayout.EndHorizontal();
            
            EditorGUILayout.Space(10);
            
            // Documentation
            EditorGUILayout.LabelField("Documentation", EditorStyles.boldLabel);
            if (GUILayout.Button("Open Integration Guide"))
            {
                string guidePath = Path.Combine(Application.dataPath, "GaussianSplatting/OptimizedVR/INTEGRATION_GUIDE.md");
                if (File.Exists(guidePath))
                {
                    EditorUtility.OpenWithDefaultApp(guidePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("File Not Found", 
                        "Integration guide not found at: " + guidePath, "OK");
                }
            }
            
            if (GUILayout.Button("Open README"))
            {
                string readmePath = Path.Combine(Application.dataPath, "GaussianSplatting/OptimizedVR/README.md");
                if (File.Exists(readmePath))
                {
                    EditorUtility.OpenWithDefaultApp(readmePath);
                }
                else
                {
                    EditorUtility.DisplayDialog("File Not Found", 
                        "README not found at: " + readmePath, "OK");
                }
            }
        }
        
        void RunSetup()
        {
            EditorUtility.DisplayProgressBar("VR GS Setup", "Creating folders...", 0.1f);
            CreateFolders();
            
            EditorUtility.DisplayProgressBar("VR GS Setup", "Creating materials...", 0.3f);
            CreateMaterials();
            
            EditorUtility.DisplayProgressBar("VR GS Setup", "Creating render textures...", 0.5f);
            CreateRenderTextures();
            
            EditorUtility.DisplayProgressBar("VR GS Setup", "Creating manager prefab...", 0.7f);
            CreateManagerPrefab();
            
            if (m_CreateExampleScene)
            {
                EditorUtility.DisplayProgressBar("VR GS Setup", "Creating example scene...", 0.9f);
                CreateExampleScene();
            }
            
            EditorUtility.ClearProgressBar();
            
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            
            EditorUtility.DisplayDialog("Setup Complete!", 
                "VR Gaussian Splatting system has been set up successfully!\n\n" +
                "Next steps:\n" +
                "1. Check the Integration Guide for detailed instructions\n" +
                "2. Add the VRGaussianSplatManager prefab to your scene\n" +
                "3. Assign your Gaussian splat renderers\n" +
                "4. Build and test on Quest 3", 
                "OK");
        }
        
        void CreateFolders()
        {
            if (!Directory.Exists(m_ResourcesPath))
            {
                Directory.CreateDirectory(m_ResourcesPath);
                Debug.Log($"Created folder: {m_ResourcesPath}");
            }
        }
        
        void CreateMaterials()
        {
            CreateFolders();
            
            // ComputeKeyValues material
            var keyValueShader = Shader.Find("GaussianSplatting/VR/ComputeKeyValue");
            if (keyValueShader != null)
            {
                var mat = new Material(keyValueShader);
                mat.name = "ComputeKeyValues";
                string path = Path.Combine(m_ResourcesPath, "ComputeKeyValues.mat");
                AssetDatabase.CreateAsset(mat, path);
                Debug.Log($"Created material: {path}");
            }
            else
            {
                Debug.LogError("Shader 'GaussianSplatting/VR/ComputeKeyValue' not found!");
            }
            
            // RadixSort material
            var sortShader = Shader.Find("GaussianSplatting/VR/RadixSort");
            if (sortShader != null)
            {
                var mat = new Material(sortShader);
                mat.name = "RadixSort";
                string path = Path.Combine(m_ResourcesPath, "RadixSort.mat");
                AssetDatabase.CreateAsset(mat, path);
                Debug.Log($"Created material: {path}");
            }
            else
            {
                Debug.LogError("Shader 'GaussianSplatting/VR/RadixSort' not found!");
            }
        }
        
        void CreateRenderTextures()
        {
            CreateFolders();
            
            // KeyValues0
            var keyValues0 = new RenderTexture(m_TextureResolution, m_TextureResolution, 0, 
                RenderTextureFormat.RGFloat)
            {
                name = "KeyValues0",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            string path0 = Path.Combine(m_ResourcesPath, "KeyValues0.renderTexture");
            AssetDatabase.CreateAsset(keyValues0, path0);
            Debug.Log($"Created RenderTexture: {path0}");
            
            // KeyValues1
            var keyValues1 = new RenderTexture(m_TextureResolution, m_TextureResolution, 0, 
                RenderTextureFormat.RGFloat)
            {
                name = "KeyValues1",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = false,
                autoGenerateMips = false
            };
            string path1 = Path.Combine(m_ResourcesPath, "KeyValues1.renderTexture");
            AssetDatabase.CreateAsset(keyValues1, path1);
            Debug.Log($"Created RenderTexture: {path1}");
            
            // PrefixSums (with mipmaps!)
            var prefixSums = new RenderTexture(m_TextureResolution, m_TextureResolution, 0, 
                RenderTextureFormat.RFloat)
            {
                name = "PrefixSums",
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                useMipMap = true,
                autoGenerateMips = false
            };
            string pathPrefix = Path.Combine(m_ResourcesPath, "PrefixSums.renderTexture");
            AssetDatabase.CreateAsset(prefixSums, pathPrefix);
            Debug.Log($"Created RenderTexture: {pathPrefix}");
        }
        
        void CreateManagerPrefab()
        {
            // Create GameObject
            var go = new GameObject("VRGaussianSplatManager");
            
            // Add components
            var manager = go.AddComponent<VRGaussianSplatManager>();
            var radixSort = go.GetComponent<RadixSortVR>();
            
            // Configure RadixSort
            radixSort.sortingPasses = 3;
            
            // Assign materials
            string matPath = Path.Combine(m_ResourcesPath, "ComputeKeyValues.mat");
            radixSort.computeKeyValues = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            
            matPath = Path.Combine(m_ResourcesPath, "RadixSort.mat");
            radixSort.radixSort = AssetDatabase.LoadAssetAtPath<Material>(matPath);
            
            // Assign RenderTextures
            string rtPath = Path.Combine(m_ResourcesPath, "KeyValues0.renderTexture");
            radixSort.keyValues0 = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            
            rtPath = Path.Combine(m_ResourcesPath, "KeyValues1.renderTexture");
            radixSort.keyValues1 = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            
            rtPath = Path.Combine(m_ResourcesPath, "PrefixSums.renderTexture");
            radixSort.prefixSums = AssetDatabase.LoadAssetAtPath<RenderTexture>(rtPath);
            
            // Configure Manager defaults
            manager.minSortDistance = 0f;
            manager.maxSortDistance = 50f;
            manager.cameraPositionQuantization = 0.05f;
            manager.alwaysUpdate = false;
            manager.separateEyeSorting = false;
            manager.ipd = 0.064f;
            manager.debugLog = true;
            
            // Save as prefab
            string prefabPath = "Assets/GaussianSplatting/OptimizedVR/VRGaussianSplatManager.prefab";
            PrefabUtility.SaveAsPrefabAsset(go, prefabPath);
            
            DestroyImmediate(go);
            
            Debug.Log($"Created prefab: {prefabPath}");
        }
        
        void CreateExampleScene()
        {
            // This would create a complete example scene
            // For now, just log a message
            Debug.Log("Example scene creation not yet implemented. " +
                      "Please follow the Integration Guide to set up your scene manually.");
        }
        
        void ValidateSetup()
        {
            bool allValid = true;
            string report = "Setup Validation:\n\n";
            
            // Check shaders
            var shader1 = Shader.Find("GaussianSplatting/VR/ComputeKeyValue");
            var shader2 = Shader.Find("GaussianSplatting/VR/RadixSort");
            
            if (shader1 != null)
                report += "✓ ComputeKeyValue shader found\n";
            else
            {
                report += "✗ ComputeKeyValue shader NOT found\n";
                allValid = false;
            }
            
            if (shader2 != null)
                report += "✓ RadixSort shader found\n";
            else
            {
                report += "✗ RadixSort shader NOT found\n";
                allValid = false;
            }
            
            // Check materials
            string matPath = Path.Combine(m_ResourcesPath, "ComputeKeyValues.mat");
            if (File.Exists(matPath))
                report += "✓ ComputeKeyValues material exists\n";
            else
            {
                report += "✗ ComputeKeyValues material NOT found\n";
                allValid = false;
            }
            
            matPath = Path.Combine(m_ResourcesPath, "RadixSort.mat");
            if (File.Exists(matPath))
                report += "✓ RadixSort material exists\n";
            else
            {
                report += "✗ RadixSort material NOT found\n";
                allValid = false;
            }
            
            // Check RenderTextures
            string rtPath = Path.Combine(m_ResourcesPath, "KeyValues0.renderTexture");
            if (File.Exists(rtPath))
                report += "✓ KeyValues0 RenderTexture exists\n";
            else
            {
                report += "✗ KeyValues0 RenderTexture NOT found\n";
                allValid = false;
            }
            
            rtPath = Path.Combine(m_ResourcesPath, "KeyValues1.renderTexture");
            if (File.Exists(rtPath))
                report += "✓ KeyValues1 RenderTexture exists\n";
            else
            {
                report += "✗ KeyValues1 RenderTexture NOT found\n";
                allValid = false;
            }
            
            rtPath = Path.Combine(m_ResourcesPath, "PrefixSums.renderTexture");
            if (File.Exists(rtPath))
                report += "✓ PrefixSums RenderTexture exists\n";
            else
            {
                report += "✗ PrefixSums RenderTexture NOT found\n";
                allValid = false;
            }
            
            // Check prefab
            string prefabPath = "Assets/GaussianSplatting/OptimizedVR/VRGaussianSplatManager.prefab";
            if (File.Exists(prefabPath))
                report += "✓ VRGaussianSplatManager prefab exists\n";
            else
            {
                report += "✗ VRGaussianSplatManager prefab NOT found\n";
                allValid = false;
            }
            
            report += "\n";
            
            if (allValid)
            {
                report += "✓ All components are set up correctly!";
                EditorUtility.DisplayDialog("Validation Passed", report, "OK");
            }
            else
            {
                report += "✗ Some components are missing. Run 'Complete Setup' to fix.";
                EditorUtility.DisplayDialog("Validation Failed", report, "OK");
            }
            
            Debug.Log(report);
        }
    }
}
