//This project is open source. Anyone can use any part of this code however they wish
//Feel free to use this code in your own projects, or expand on this code
//If you have any improvements to the code itself, please visit
//https://github.com/Dharengo/Spriter2UnityDX and share your suggestions by creating a fork
//-Dengar/Dharengo

using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Spriter2UnityDX.Prefabs
{
    using Importing;
    using Animations;
    using Entity;

    using UnityEngine.Rendering;
    using Spriter2UnityDX.Extensions;

    public class PrefabBuilder : UnityEngine.Object
    {
        private ScmlProcessingInfo ProcessingInfo;
        private List<string> _previousActiveMapNames;

        public PrefabBuilder(ScmlProcessingInfo info)
        {
            ProcessingInfo = info;
        }

        public IEnumerator Build(ScmlObject obj, string scmlPath, IBuildTaskContext buildCtx)
        {
            // The process begins by loading up all the textures
            var directory = Path.GetDirectoryName(scmlPath);

            // I find these slightly more useful than Lists because you can be 100% sure
            // that the ids match and items can be added in any order
            var folders = new Dictionary<int, IDictionary<int, Sprite>>();
            var fileInfo = new Dictionary<int, IDictionary<int, File>>();

            AssetDatabase.StartAssetEditing();

            foreach (var folder in obj.folders)
            {
                foreach (var file in folder.files)
                {
                    if (file.objectType == ObjectType.sprite)
                    {
                        var path = string.Format("{0}/{1}", directory, file.name);

                        if (buildCtx.IsCanceled) { yield break; }
                        yield return $"{buildCtx.MessagePrefix}: Setting texture import options for {path}";

                        SetTextureImportSettings(path, file);
                    }
                }
            }

            AssetDatabase.StopAssetEditing();

            foreach (var folder in obj.folders)
            {
                var files = folders[folder.id] = new Dictionary<int, Sprite>();
                var fi = fileInfo[folder.id] = new Dictionary<int, File>();

                foreach (var file in folder.files)
                {
                    if (file.objectType == ObjectType.sprite)
                    {
                        var path = string.Format("{0}/{1}", directory, file.name);

                        if (buildCtx.IsCanceled) { yield break; }
                        yield return $"{buildCtx.MessagePrefix}: Getting sprite at {path}";

                        files[file.id] = GetSpriteAtPath(path);
                        fi[file.id] = file;
                    }
                }
            }

            foreach (var entity in obj.entities)
            {   // Now begins the real prefab build process
                var prefabPath = string.Format("{0}/{1}.prefab", directory, entity.name);

                if (buildCtx.IsCanceled) { yield break; }
                yield return $"{buildCtx.MessagePrefix}: Getting/creating prefab at {prefabPath}";

                var prefab = (GameObject)AssetDatabase.LoadAssetAtPath(prefabPath, typeof(GameObject));
                GameObject instance;

                if (prefab == null)
                {   // Creates an empty prefab if one doesn't already exists
                    instance = new GameObject(entity.name);
                    prefab = PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
                    ProcessingInfo.NewPrefabs.Add(prefab);
                }
                else
                {
                    instance = (GameObject)PrefabUtility.InstantiatePrefab(prefab); //instantiates the prefab if it does exist
                    ProcessingInfo.ModifiedPrefabs.Add(prefab);
                }

                SaveAndRemoveCharacterMaps(instance);

                var prefabBuildProcess =
                    IteratorUtils.SafeEnumerable(
                        () => TryBuild(entity, prefab, instance, directory, prefabPath, folders, fileInfo, buildCtx),
                        ex =>
                        {
                            DestroyImmediate(instance);
                            Debug.LogErrorFormat("Build failed for '{0}': {1}", entity.name, ex);
                        });

                while (prefabBuildProcess.MoveNext())
                {
                    yield return prefabBuildProcess.Current;
                }

                if (buildCtx.IsCanceled)
                {
                    DestroyImmediate(instance);
                    yield break;
                }
            }
        }

        private IEnumerator TryBuild(Entity entity, GameObject prefab, GameObject instance, string directory, string prefabPath,
            IDictionary<int, IDictionary<int, Sprite>> folders, Dictionary<int, IDictionary<int, File>> fileInfo, IBuildTaskContext buildCtx)
        {
            if (buildCtx.IsCanceled) { yield break; }
            yield return $"{buildCtx.MessagePrefix}: Processing entity '{entity.name}'";

            buildCtx.EntityName = entity.name;

            // SpriterEntityInfo will initialize and gather info about the bones and sprites for this entity.
            SpriterEntityInfo entityInfo = new SpriterEntityInfo();

            var entityInfoProcess = entityInfo.Process(entity, fileInfo, buildCtx);
            while (entityInfoProcess.MoveNext())
            {
                yield return entityInfoProcess.Current;
            }

            if (buildCtx.IsCanceled) { yield break; }

            var controllerPath = string.Format("{0}/{1}.controller", directory, entity.name);
            var animator = instance.GetOrAddComponent<Animator>(); // Fetches/creeates the prefab's Animator

            AnimatorController controller = null;

            if (animator.runtimeAnimatorController != null)
            {   // The controller we use is hopefully the controller attached to the animator
                controller = animator.runtimeAnimatorController as AnimatorController ?? //Or the one that's referenced by an OverrideController
                    (AnimatorController)((AnimatorOverrideController)animator.runtimeAnimatorController).runtimeAnimatorController;
            }

            if (controller == null)
            {   // Otherwise we have to check the AssetDatabase for our controller
                controller = (AnimatorController)AssetDatabase.LoadAssetAtPath(controllerPath, typeof(AnimatorController));
                if (controller == null)
                {
                    controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath); //Or create a new one if it doesn't exist.
                    ProcessingInfo.NewControllers.Add(controller);
                }

                animator.runtimeAnimatorController = controller;
            }

            var transforms = new Dictionary<string, Transform>(); //All of the bones and sprites, identified by TimeLine.name, because those are truly unique
            transforms["rootTransform"] = instance.transform; //The root GameObject needs to be part of this hierarchy as well
            var defaultBones = new Dictionary<string, SpatialInfo>(); //These are basically the object states on the first frame of the first animation
            var defaultSprites = new Dictionary<string, SpriteInfo>(); //They are used as control values in determining whether something has changed
            var animBuilder = new AnimationBuilder(ProcessingInfo, folders, transforms, defaultBones, defaultSprites, prefabPath, controller, entityInfo);
            var firstAnim = true; //The prefab's graphic will be determined by the first frame of the first animation

            foreach (var animation in entity.animations)
            {
                buildCtx.AnimationName = animation.name;

                if (buildCtx.IsCanceled) { yield break; }
                yield return $"{buildCtx.MessagePrefix}: processing";

                var timeLines = new Dictionary<int, TimeLine>();
                foreach (var timeLine in animation.timelines) //TimeLines hold all the critical data such as positioning and graphics used
                {
                    timeLines[timeLine.id] = timeLine;
                }

                foreach (var key in animation.mainlineKeys)
                {
                    var parents = new Dictionary<int, string>(); //Parents are referenced by different IDs V_V
                    parents[-1] = "rootTransform"; //This is where "-1 == no parent" comes in handy

                    if (buildCtx.IsCanceled) { yield break; }
                    yield return $"{buildCtx.MessagePrefix}, mainline key time: {key.time_s}, processing bones";

                    ProcessBones(parents, transforms, timeLines, key, defaultBones, entityInfo);

                    if (buildCtx.IsCanceled) { yield break; }
                    yield return $"{buildCtx.MessagePrefix}, mainline key time: {key.time_s}, processing sprites";

                    ProcessSprites(parents, transforms, timeLines, key, defaultBones, defaultSprites, entityInfo, folders, firstAnim);

                    firstAnim = false;
                }

                var animBuildProcess =
                    IteratorUtils.SafeEnumerable(
                        () => animBuilder.Build(animation, timeLines, buildCtx),
                        ex =>
                        {
                            Debug.LogErrorFormat("Unable to build animation '{0}' for '{1}', reason: {2}", animation.name, entity.name, ex);
                        });

                while (animBuildProcess.MoveNext())
                {
                    yield return animBuildProcess.Current;
                }

                if (buildCtx.IsCanceled) { yield break; }
            }

            buildCtx.AnimationName = "";

            instance.GetOrAddComponent<SortingGroup>();

            FinalizeVirtualParentProcessing(entityInfo, transforms);
            ProcessCharacterMaps(entity, instance, folders);

            EditorUtility.SetDirty(instance);

            PrefabUtility.SaveAsPrefabAssetAndConnect(instance, prefabPath, InteractionMode.AutomatedAction);
            DestroyImmediate(instance); //Apply the instance's changes to the prefab, then destroy the instance.

            buildCtx.EntityName = "";

            if (buildCtx.ImportedPrefabs.Contains(prefabPath))
            {
                Debug.LogWarning($"The prefab at '{prefabPath}' has been imported more than once in the same import " +
                    "session.  This is likely due to 1) multiple .scml files in the same folder that have entities " +
                    "that share the same name, or 2) a single .scml file with duplicate entity names.");
            }

            buildCtx.ImportedPrefabs.Add(prefabPath);
        }

        private void FinalizeVirtualParentProcessing(SpriterEntityInfo entityInfo, Dictionary<string, Transform> transforms)
        {
            // Add 'possible parents' to all of the virtual parent components.
            foreach (var info in entityInfo.boneInfo.Values.Cast<SpriterEntityInfo.SpriterInfoBase>()
                .Concat(entityInfo.objectInfo.Values))
            {
                if (info.hasVirtualParent && info.virtualParentTransform != null)
                {
                    var vp = info.virtualParentTransform.GetComponent<VirtualParent>();

                    if (vp != null)
                    {
                        vp.possibleParents.Clear();

                        foreach (var parentName in info.parentBoneNames)
                        {
                            var possibleParentTransform = transforms[parentName];
                            vp.possibleParents.Add(possibleParentTransform);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"VirtualParent missing on: {info.name}");
                    }
                }
            }
        }

        private void SaveAndRemoveCharacterMaps(GameObject instance)
        {
            // If the prefab already exists during an import AND it has a character map controller, all of the active
            // maps have to be disabled during the import and re-applied afterward.
            var characterController = instance.GetComponent<CharacterMapController>();
            if (characterController != null)
            {
                _previousActiveMapNames = characterController.activeMapNames.ToList();
                characterController.Clear();
            }
            else
            {
                _previousActiveMapNames = null;
            }
        }

        private void ProcessCharacterMaps(Entity entity, GameObject instance, IDictionary<int, IDictionary<int, Sprite>> folders)
        {
            if (entity.characterMaps.Count == 0 || ScmlImportOptions.options == null || !ScmlImportOptions.options.createCharacterMaps)
            {   // Either the feature is disabled or this entity doesn't have any character maps.
                var c = instance.GetComponent<CharacterMapController>();
                if (c != null)
                {
                    DestroyImmediate(c);
                }

                return;
            }

            var characterMapController = instance.GetOrAddComponent<CharacterMapController>();

            // Build characterMapController.baseMap...

            characterMapController.baseMap.Clear();

            // Note: This code here is the reason why all active maps have to be temporarily removed.
            foreach (var renderer in instance.GetComponentsInChildren<SpriteRenderer>(includeInactive: true))
            {
                // Map sprites to the appropriate transform and, if appropriate, the texture controller index.

                Transform targetTransform = renderer.transform;
                var textureController = targetTransform.GetComponent<TextureController>();

                if (textureController)
                {
                    for (int i = 0; i < textureController.Sprites.Length; ++i)
                    {
                        var sprite = textureController.Sprites[i];
                        characterMapController.baseMap.Add(sprite, new SpriteMapTarget(targetTransform, i));
                    }
                }
                else
                {
                    characterMapController.baseMap.Add(renderer.sprite, new SpriteMapTarget(targetTransform, 0));
                }
            }

            characterMapController.Refresh(); // Apply _just_ the base map.

            // Build characterMapController.availableMaps...

            characterMapController.availableMaps.Clear();

            foreach (var characterMap in entity.characterMaps)
            {
                var charMap = new CharacterMapping(characterMap.name);

                foreach (var mapInstruction in characterMap.maps)
                {
                    Sprite srcSprite = TryGetSprite(folders, mapInstruction.folder, mapInstruction.file);

                    if (srcSprite == null)
                    {
                        Debug.LogWarning($"Spriter2UnityDX: ProcessCharacterMaps(): For entity '{entity.name}', " +
                            $"character map '{characterMap.name}', the source sprite at folder: {mapInstruction.folder}, " +
                            $"file: {mapInstruction.file} wasn't found.");

                        continue;
                    }

                    Sprite targetSprite = null;

                    if (mapInstruction.target_folder != -1 && mapInstruction.target_file != -1)
                    {
                        targetSprite = TryGetSprite(folders, mapInstruction.target_folder, mapInstruction.target_file);

                        if (targetSprite == null)
                        {
                            Debug.LogWarning($"Spriter2UnityDX: ProcessCharacterMaps(): For entity '{entity.name}', " +
                                $"character map '{characterMap.name}', the target sprite at folder: {mapInstruction.folder}, " +
                                $"file: {mapInstruction.file} wasn't found.");

                            continue;
                        }
                    }

                    var spriteMapping = characterMapController.baseMap.spriteMaps.Find(s => s.sprite == srcSprite);

                    if (spriteMapping != null)
                    {
                        foreach (var target in spriteMapping.targets)
                        {
                            charMap.Add(targetSprite, target);
                        }
                    }
                    else
                    {
                        Debug.LogWarning($"Spriter2UnityDX: ProcessCharacterMaps(): For entity '{entity.name}', " +
                            $"character map '{characterMap.name}', the source sprite at folder: {mapInstruction.folder}, " +
                            $"file: {mapInstruction.file} doesn't exist in the base map.");
                    }
                }

                characterMapController.availableMaps.Add(charMap);
            }

            if (_previousActiveMapNames != null)
            {
                // Remove any invalid character map names from _previousActiveMapNames (from pre-existing
                // character map controllers.)
                _previousActiveMapNames.RemoveAll(name =>
                {
                    return characterMapController.availableMaps.Find(m => m.name == name) == null;
                });

                characterMapController.activeMapNames = _previousActiveMapNames.ToList();
                characterMapController.Refresh(); // Apply any user-defined mappings.

                _previousActiveMapNames = null;
            }
        }

        private Sprite TryGetSprite(IDictionary<int, IDictionary<int, Sprite>> folders, int folderIdx, int fileIdx)
        {
            try
            {
                return folders[folderIdx][fileIdx];
            }
            catch (Exception)
            {
                return null;
            }
        }

        private void ProcessBones(Dictionary<int, string> parents, Dictionary<string, Transform> transforms,
            Dictionary<int, TimeLine> timeLines, MainLineKey key, Dictionary<string, SpatialInfo> defaultBones,
            SpriterEntityInfo entityInfo)
        {
            var boneRefs = new Queue<Ref>(key.boneRefs);

            while (boneRefs.Count > 0)
            {
                var bone = boneRefs.Dequeue();
                var timeLine = timeLines[bone.timeline];
                parents[bone.id] = timeLine.name;

                if (!transforms.ContainsKey(timeLine.name))
                {   //We only need to go through this once, so ignore it if it's already in the dict

                    SpriterEntityInfo.SpriterBoneInfo spriterBoneInfo;
                    entityInfo.boneInfo.TryGetValue(timeLine.name, out spriterBoneInfo);

                    if (spriterBoneInfo == null || spriterBoneInfo.type != ObjectType.bone)
                    {
                        Debug.LogWarning($"Spriter2UnityDX: ProcessBones() was unable to find bone info for bone '{timeLine.name}'.");
                        continue;
                    }

                    if (parents.ContainsKey(bone.parent))
                    {   //If the parent cannot be found, it will probably be found later, so save it
                        var parentName = parents[bone.parent];
                        var parentTransform = transforms[parentName];

                        ProcessVirtualParent(parentName, ref parentTransform, spriterBoneInfo);

                        var child = parentTransform.Find(timeLine.name); //Try to find the child transform if it exists
                        if (child == null)
                        {   //Or create a new one
                            child = new GameObject(timeLine.name).transform;
                            child.SetParent(parentTransform);
                        }

                        transforms[timeLine.name] = child;
                        var spatialInfo = defaultBones[timeLine.name] = timeLine.keys.Find(x => x.id == bone.key).info;

                        if (!spatialInfo.processed)
                        {
                            SpatialInfo parentInfo;
                            defaultBones.TryGetValue(parentName, out parentInfo); // 'parentName' may be grandparent if a virtual parent was created.
                            spatialInfo.Process(parentInfo);
                        }

                        child.localPosition = new Vector3(spatialInfo.x, spatialInfo.y, 0f);
                        child.localRotation = Quaternion.Euler(0, 0, spatialInfo.angle);
                        child.localScale = new Vector3(spatialInfo.scale_x, spatialInfo.scale_y, 1f);
                    }
                    else
                    {
                        boneRefs.Enqueue(bone);
                    }
                }
            }
        }

        private void ProcessSprites(Dictionary<int, string> parents, Dictionary<string, Transform> transforms,
            Dictionary<int, TimeLine> timeLines, MainLineKey key, Dictionary<string, SpatialInfo> defaultBones,
            Dictionary<string, SpriteInfo> defaultSprites, SpriterEntityInfo entityInfo,
            IDictionary<int, IDictionary<int, Sprite>> folders, bool firstAnim)
        {
            foreach (var oref in key.objectRefs)
            {
                var timeLine = timeLines[oref.timeline];

                SpriterEntityInfo.SpriterObjectInfo spriterObjectInfo;
                entityInfo.objectInfo.TryGetValue(timeLine.name, out spriterObjectInfo);

                if (spriterObjectInfo == null || spriterObjectInfo.type != ObjectType.sprite)
                {   // Don't log a warning if this was one of the unsupported Spriter object types.
                    if (spriterObjectInfo == null)
                    {
                        Debug.LogWarning($"Spriter2UnityDX: ProcessSprites() was unable to find object info for sprite '{timeLine.name}'.");
                    }

                    continue;
                }

                if (transforms.ContainsKey(timeLine.name))
                {
                    continue;
                }

                var parentName = parents[oref.parent];
                var parentTransform = transforms[parentName];

                ProcessVirtualParent(parentName, ref parentTransform, spriterObjectInfo);

                // 'child' is the name without a suffix, so will be a pivot or a renderer (without a pivot parent.)
                var child = parentTransform.Find(timeLine.name);
                if (child == null)
                {
                    child = new GameObject(timeLine.name).transform;
                }

                child.SetParent(parentTransform);
                transforms[timeLine.name] = child; // Note that virtual parents and renderers w/ a pivot parent aren't added to this.

                var spriteInfo = defaultSprites[timeLine.name] = (SpriteInfo)timeLine.keys[0].info;

                if (!spriteInfo.processed)
                {
                    SpatialInfo parentInfo;
                    defaultBones.TryGetValue(parentName, out parentInfo); // 'parentName' may be grandparent if a virtual parent was created.
                    spriteInfo.Process(parentInfo);
                }

                // If this sprite (for any animation of the entity) has one or more non-default
                // pivots then a pivot controller will need to be created for it.  Otherwise,
                // don't create one and be sure to remove any that might already exist.

                bool needsPivotController = spriterObjectInfo.hasPivotController;

                var pivotController = child.GetComponent<DynamicPivot2D>();

                if (needsPivotController)
                {
                    if (pivotController == null)
                    {
                        pivotController = child.gameObject.AddComponent<DynamicPivot2D>();
                    }

                    pivotController.pivot = new Vector2(spriteInfo.pivot_x, spriteInfo.pivot_y);
                }
                else if (pivotController != null)
                {
                    DestroyImmediate(pivotController);
                }

                child.localPosition = new Vector3(spriteInfo.x, spriteInfo.y, spriteInfo.z_index); // Z-index (sprite sorting order / -1000f) is stored in z.
                child.localEulerAngles = new Vector3(0f, 0f, spriteInfo.angle);
                child.localScale = new Vector3(spriteInfo.scale_x, spriteInfo.scale_y, 1f);

                // Get or create a Sorting Order Updater.  If a pivot controller was created then it must be on the
                // same game object.
                child.GetOrAddComponent<SortingOrderUpdater>();

                // If a pivot controller is used then the sprite renderer has to go on a child game object.
                string spriteRendererName = spriterObjectInfo.spriteRenderTransformName;
                var rendererTransform = needsPivotController ? child.Find(spriteRendererName) : child;

                if (needsPivotController && rendererTransform == null)
                {
                    rendererTransform = new GameObject(spriteRendererName).transform;
                    rendererTransform.SetParent(child);
                }

                var renderer = rendererTransform.GetOrAddComponent<SpriteRenderer>(); // Get or create a Sprite Renderer

                renderer.sprite = folders[spriteInfo.folder][spriteInfo.file];
                renderer.sortingOrder = spriteInfo.SortingOrder;

                if (needsPivotController)
                {
                    rendererTransform.localPosition = Vector3.zero; // The pivot script will adjust this.
                    rendererTransform.localEulerAngles = Vector3.zero;
                    rendererTransform.localScale = Vector3.one;
                }

                var color = renderer.color;
                color.a = spriteInfo.a;
                renderer.color = color;

                var spriteVisibility = rendererTransform.GetOrAddComponent<SpriteVisibility>();

                // Disable the Sprite Renderer if this isn't the first frame of the first animation
                renderer.enabled = firstAnim;
                spriteVisibility.isVisible = firstAnim ? 1f : 0f;
            }
        }

        private void ProcessVirtualParent(string parentName, ref Transform parentTransform,
            SpriterEntityInfo.SpriterInfoBase virtualParentInfo)
        {
            // The hierarchy for a sprite will look like the following:
            //
            //     Virtual Parent (optional)
            //     └── Pivot (optional) or Sprite Render
            //         └── Sprite Renderer w/ pivot parent
            //
            // The naming will look like one of the following examples, in this case,
            // for a sprite with a name of "lower_leg":
            //
            //     lower_leg virtual parent     lower_leg virtual parent   lower_leg
            //     └── lower_leg                └── lower_leg              └── lower_leg renderer
            //         └── lower_leg renderer
            //
            // ...or just a transform named "lower_leg" in the case where there isn't a virtual parent
            // and there isn't a pivot controller.  (This will be the most common case.)
            //
            // Bones can have virtual parents but can't have sprite renderers or pivots so their
            // hierarchy will look like this:
            //
            //     Virtual Parent (optional)
            //     └── Bone
            //
            // The bone transform will have the name of the bone from the Spriter file.  The virtual
            // parent, if any, will be named boneName + " virtual parent";

            if (virtualParentInfo.hasVirtualParent)
            {   // Find or create a transform for the virtual parent.

                if (virtualParentInfo.parentBoneNames[0] != parentName)
                {   // The bone/sprite's first parent isn't the bone that would actually be its parent when the
                    // character is in the bind/default pose.  (The "bind pose" refers to the character’s
                    // default bone and sprite positions, etc., which are defined by the first frame of the
                    // entity's first animation.)  Make it the first so that it shows up at index zero of
                    // the virtual parent component's 'possibleParents' list.
                    var swapIdx = virtualParentInfo.parentBoneNames.FindIndex(x => x == parentName);

                    var tmp = virtualParentInfo.parentBoneNames[0];
                    virtualParentInfo.parentBoneNames[0] = virtualParentInfo.parentBoneNames[swapIdx];
                    virtualParentInfo.parentBoneNames[swapIdx] = tmp;
                }

                var virtualParentTransform = parentTransform.Find(virtualParentInfo.virtualParentTransformName);
                if (virtualParentTransform == null)
                {
                    virtualParentTransform = new GameObject(virtualParentInfo.virtualParentTransformName).transform;
                }

                virtualParentInfo.virtualParentTransform = virtualParentTransform; // Post proccessing needs this.

                virtualParentTransform.SetParent(parentTransform, false);
                parentTransform = virtualParentTransform; // The virtual parent will be the next stage's parent.

                var virtualParentComponent = virtualParentTransform.GetOrAddComponent<VirtualParent>();

                virtualParentComponent.possibleParents.Clear();
                virtualParentComponent.parentIndex = 0; // We know this is the bind pose parent index from above.
            }
            else
            {   // If a virtual parent exists, remove it.
                var virtualParentTransform = parentTransform.Find(virtualParentInfo.virtualParentTransformName);
                if (virtualParentTransform != null)
                {
                    DestroyImmediate(virtualParentTransform);
                }
            }
        }

        private void SetTextureImportSettings(string path, File file)
        {
            var importer = TextureImporter.GetAtPath(path) as TextureImporter;

            if (importer == null)
            {   // If no TextureImporter exists, there's no texture to be found
                return;
            }

            bool requiresSettingsUpdate =
                importer.textureType != TextureImporterType.Sprite
                || importer.spritePivot.x != file.pivot_x
                || importer.spritePivot.y != file.pivot_y
                || importer.spriteImportMode != SpriteImportMode.Single
                || (ScmlImportOptions.options != null && importer.spritePixelsPerUnit != ScmlImportOptions.options.pixelsPerUnit);

            if (requiresSettingsUpdate)
            {
                // Make sure the texture has the required settings...

                var settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);

                settings.ApplyTextureType(TextureImporterType.Sprite);
                settings.spriteAlignment = (int)SpriteAlignment.Custom;
                settings.spritePivot = new Vector2(file.pivot_x, file.pivot_y);

                if (ScmlImportOptions.options != null)
                {
                    settings.spritePixelsPerUnit = ScmlImportOptions.options.pixelsPerUnit;
                }

                importer.SetTextureSettings(settings);

                importer.spriteImportMode = SpriteImportMode.Single; // Set this last!  It won't work in some cases otherwise.

                importer.SaveAndReimport();
            }
        }

        private Sprite GetSpriteAtPath(string path)
        {
            Sprite result = (Sprite)AssetDatabase.LoadAssetAtPath(path, typeof(Sprite));
            if (result == null)
            {
                Debug.LogWarning($"The Spriter .scml file references a sprite at '{path}' but it was not found.  " +
                    "The sprite may not be needed so the import will continue.");
            }

            return result;
        }
    }
}
