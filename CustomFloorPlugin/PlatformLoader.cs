using UnityEngine;
using System.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;

namespace CustomFloorPlugin
{
    /// <summary>
    /// Loads AssetBundles containing CustomPlatforms and handles cycling between them
    /// </summary>
    class PlatformLoader
    {
        private const string customFolder = "CustomPlatforms";
        
        private List<string> bundlePaths;
        private List<CustomPlatform> platforms;

        /// <summary>
        /// Loads AssetBundles and populates the platforms array with CustomPlatform objects
        /// </summary>
        public CustomPlatform[] CreateAllPlatforms(Transform parent)
        {

            string customPlatformsFolderPath = Path.Combine(Environment.CurrentDirectory, customFolder);

            // Create the CustomPlatforms folder if it doesn't already exist
            if (!Directory.Exists(customPlatformsFolderPath))
            {
                Directory.CreateDirectory(customPlatformsFolderPath);
            }

            // Find AssetBundles in our CustomPlatforms directory
            string[] allBundlePaths = Directory.GetFiles(customPlatformsFolderPath, "*.plat");

            platforms = new List<CustomPlatform>();
            bundlePaths = new List<string>();

            // Create a dummy CustomPlatform for the original platform
            CustomPlatform defaultPlatform = new GameObject("Default Platform").AddComponent<CustomPlatform>();
            defaultPlatform.transform.parent = parent;
            defaultPlatform.platName = "Default Environment";
            defaultPlatform.platAuthor = "Beat Saber";
            Texture2D texture = Resources.FindObjectsOfTypeAll<Texture2D>().First(x => x.name == "LvlInsaneCover");
            defaultPlatform.icon = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            platforms.Add(defaultPlatform);
            bundlePaths.Add("");
            // Populate the platforms array
            for (int i = 0; i < allBundlePaths.Length; i++)
            {

                CustomPlatform newPlatform = LoadPlatformBundle(allBundlePaths[i],parent);
            }

            return platforms.ToArray();
        }

        public CustomPlatform LoadPlatformBundle(string bundlePath, Transform parent)
        {

            AssetBundle bundle = AssetBundle.LoadFromFile(bundlePath);

            if (bundle == null) return null;

            CustomPlatform newPlatform = LoadPlatform(bundle, parent);
            if (newPlatform != null)
            {
                bundlePaths.Add(bundlePath);
                platforms.Add(newPlatform);
                Plugin.logger.Info("Loaded: " + newPlatform.name);
            }

            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(bundlePath))
                {
                    byte[] hash = md5.ComputeHash(stream);
                    newPlatform.platHash = BitConverter
                        .ToString(hash)
                        .Replace("-", string.Empty)
                        .ToLower();
                }
            }

            return newPlatform;
        }
        
        /// <summary>
        /// Instantiate a platform from an assetbundle.
        /// </summary>
        /// <param name="bundle">An AssetBundle containing a CustomPlatform</param>
        /// <returns></returns>
        private CustomPlatform LoadPlatform(AssetBundle bundle, Transform parent)
        {

            GameObject platformPrefab = bundle.LoadAsset<GameObject>("_CustomPlatform");

            if (platformPrefab == null)
            {
                Plugin.logger.Info("Assetbundle didnt contain a Custom Platform");
                return null;
            }

            GameObject newPlatform =  GameObject.Instantiate(platformPrefab.gameObject);

            newPlatform.transform.parent = parent;

            bundle.Unload(false);
            
            // Collect author and name
            CustomPlatform customPlatform = newPlatform.GetComponent<CustomPlatform>();

            if (customPlatform == null)
            {
                // Check for old platform 
                global::CustomPlatform legacyPlatform = newPlatform.GetComponent<global::CustomPlatform>();
                if(legacyPlatform != null)
                {
                    Plugin.logger.Info("legacy version of customPlatform detected, updating");
                    // Replace legacyplatform component with up to date one
                    customPlatform = newPlatform.AddComponent<CustomPlatform>();
                    customPlatform.platName = legacyPlatform.platName;
                    customPlatform.platAuthor = legacyPlatform.platAuthor;
                    customPlatform.hideDefaultPlatform = true;
                    // Remove old platform data
                    GameObject.Destroy(legacyPlatform);
                }
                else
                {
                    // no customplatform component, abort
                    Plugin.logger.Info("Loaded object had no customplatform attached, skipping");
                    GameObject.Destroy(newPlatform);
                    return null;
                }
            }

            newPlatform.name = customPlatform.platName + " by " + customPlatform.platAuthor;

            if (customPlatform.icon == null) customPlatform.icon = Resources.FindObjectsOfTypeAll<Sprite>().Where(x => x.name == "FeetIcon").FirstOrDefault();

            AddManagers(newPlatform);

            newPlatform.SetActive(false);

            return customPlatform;
        }
        internal void AddManagers(GameObject go) {
            AddManagers(go, go);
        }
        private void AddManagers(GameObject go, GameObject root) {
            // Replace materials for this object
            MaterialSwapper.ReplaceMaterialsForGameObject(go);

            // Rotation effect manager
            if(go.GetComponentInChildren<RotationEventEffect>(true) != null) {
                RotationEventEffectManager rotManager = root.GetComponent<RotationEventEffectManager>();
                if(rotManager == null) {
                    rotManager = root.AddComponent<RotationEventEffectManager>();
                    //PlatformManager.SpawnedComponents.Add(rotManager);
                }
                rotManager.CreateEffects(go);
                Plugin.Log("Added RotationEventEffectManagers");
            }

            // Add a trackRing controller if there are track ring descriptors
            if(go.GetComponentInChildren<TrackRings>(true) != null) {
                foreach(TrackRings trackRings in go.GetComponentsInChildren<TrackRings>(true)) {
                    GameObject ringPrefab = trackRings.trackLaneRingPrefab;

                    // Add managers to prefabs (nesting)
                    AddManagers(ringPrefab, root);
                }

                TrackRingsManagerSpawner trms = root.GetComponent<TrackRingsManagerSpawner>();
                if(trms == null) {
                    trms = root.AddComponent<TrackRingsManagerSpawner>();
                    //PlatformManager.SpawnedComponents.Add(trms);
                }
                trms.CreateTrackRings(go);
                Plugin.Log("Added TrackRingsManagerSpawners");
            }

            // Add spectrogram manager
            if(go.GetComponentInChildren<Spectrogram>(true) != null) {
                foreach(Spectrogram spec in go.GetComponentsInChildren<Spectrogram>(true)) {
                    GameObject colPrefab = spec.columnPrefab;
                    AddManagers(colPrefab, root);
                }

                SpectrogramColumnManager specManager = go.GetComponent<SpectrogramColumnManager>();
                if(specManager == null) {
                    specManager = go.AddComponent<SpectrogramColumnManager>();
                    //PlatformManager.SpawnedComponents.Add(specManager);
                }
                specManager.CreateColumns(go);
                Plugin.Log("Added SpectrogramColumnManagers");
            }

            if(go.GetComponentInChildren<SpectrogramMaterial>(true) != null) {
                // Add spectrogram materials manager
                SpectrogramMaterialManager specMatManager = go.GetComponent<SpectrogramMaterialManager>();
                if(specMatManager == null) {
                    specMatManager = go.AddComponent<SpectrogramMaterialManager>();
                    //PlatformManager.SpawnedComponents.Add(specMatManager);
                }
                specMatManager.UpdateMaterials();
                Plugin.Log("Added SpectrogramMaterialManagers");
            }


            if(go.GetComponentInChildren<SpectrogramAnimationState>(true) != null) {
                // Add spectrogram animation state manager
                SpectrogramAnimationStateManager specAnimManager = go.GetComponent<SpectrogramAnimationStateManager>();
                if(specAnimManager == null) {
                    specAnimManager = go.AddComponent<SpectrogramAnimationStateManager>();
                    //PlatformManager.SpawnedComponents.Add(specAnimManager);
                }
                specAnimManager.UpdateAnimationStates();
                Plugin.Log("Added SpectrogramAnimationStateManagers");
            }

            // Add Song event manager
            if(go.GetComponentInChildren<SongEventHandler>(true) != null) {
                foreach(SongEventHandler handler in go.GetComponentsInChildren<SongEventHandler>()) {
                    SongEventManager manager = handler.gameObject.AddComponent<SongEventManager>();
                    //PlatformManager.SpawnedComponents.Add(manager);
                    manager._songEventHandler = handler;
                }
                Plugin.Log("Added SongEventManagers");
            }

            // Add EventManager 
            if(go.GetComponentInChildren<EventManager>(true) != null) {
                foreach(EventManager em in go.GetComponentsInChildren<EventManager>()) {
                    PlatformEventManager pem = em.gameObject.AddComponent<PlatformEventManager>();
                    //PlatformManager.SpawnedComponents.Add(pem);
                    pem._EventManager = em;
                }
                Plugin.Log("Added PlatformEventManagers");
            }
        }
    }
}
