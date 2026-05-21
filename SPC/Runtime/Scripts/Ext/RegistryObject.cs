using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using JetBrains.Annotations;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

namespace Spookline.SPC.Ext {
    public abstract class RegistryObject : SerializedScriptableObject {

        [HideInInspector]
        public string assetGuid;

        protected override void OnBeforeSerialize() {
            base.OnBeforeSerialize();
#if UNITY_EDITOR
            UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(GetEntityId(), out assetGuid, out _);
#endif
        }

    }

    public interface IObjectRegistry {

        public string AddressableLabel { get; }

        public Type GetObjectType();

    }

    public abstract class ObjectRegistry<TSelf, TObject> : Singleton<TSelf>, IDisposable, IObjectRegistry
        where TObject : RegistryObject {

        private readonly List<Action> _disposeActions = new();

        public IReadOnlyList<TObject> Objects { get; private set; } = new List<TObject>();

        public IReadOnlyDictionary<string, TObject> GuidLookup { get; private set; } =
            new Dictionary<string, TObject>();

        public IReadOnlyDictionary<string, TObject> NameLookup { get; private set; } =
            new Dictionary<string, TObject>();

        public Dictionary<Enum, TObject> EnumLookup { get; } = new();

        public void Dispose() {
            foreach (var action in _disposeActions) action.Invoke();
            GuidLookup = null;
            Objects = null;
        }

        public abstract string AddressableLabel { get; }

        public Type GetObjectType() {
            return typeof(TObject);
        }

        public TObject GetByGuid(string guid) {
            return GuidLookup.GetValueOrDefault(guid);
        }

        public bool TryGetByGuid(string guid, out TObject obj) {
            return GuidLookup.TryGetValue(guid, out obj);
        }

        public TObject GetByEnum(Enum key) {
            return EnumLookup.GetValueOrDefault(key);
        }

        public TObject GetByName(string name) {
            return NameLookup.GetValueOrDefault(name);
        }

        protected virtual UniTask BeforeLoad() {
            return UniTask.CompletedTask;
        }

        protected virtual UniTask AfterLoad() {
            return UniTask.CompletedTask;
        }

        public async UniTask Load([CanBeNull] Type enumType = null) {
            await BeforeLoad();

            var handle = Addressables.LoadResourceLocationsAsync(AddressableLabel, typeof(TObject));
            var locations = await handle;
            Debug.Log($"Loaded resource locations ({locations.Count})...");

            var objectList = new List<TObject>();
            var guidLookupDict = new Dictionary<string, TObject>();
            var nameLookupDict = new Dictionary<string, TObject>();

            var childOperations = new List<AsyncOperationHandle>();
            foreach (var location in locations) {
                //ResourceLocationData
                try {
                    Debug.Log(
                        $"Location: {location.PrimaryKey}, Type: {location.ResourceType}, Data: {location.Data ?? "null"}, {location.GetType()}");
                    var childHandle = Addressables.LoadAssetAsync<TObject>(location);
                    childHandle.Completed += x => {
                        var obj = x.Result;
                        if (obj.assetGuid == null) {
#if UNITY_EDITOR
                            if (!UnityEditor.AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
                                    obj, out var guid, out _)) {
                                Debug.LogError(
                                    $"Failed to retrieve asset guid for object {obj.name} at location {location.PrimaryKey}, " +
                                    "even in editor. Skipping.");
                                return;
                            }

                            obj.assetGuid = guid;
                            Debug.LogWarning(
                                $"Asset guid for {obj.name} was null, and was set to {guid} after AssetDatabase lookup. " +
                                "This will only work in the editor, in runtime builds this issue should not occur.");
#else
                            Debug.LogError(
                                $"Loaded object from location {location.PrimaryKey} '{obj.name}' does not have a valid assetGuid. Skipping.");
                            return;
#endif
                        }

                        objectList.Add(obj);
                        guidLookupDict[obj.assetGuid] = obj;
                        nameLookupDict[obj.name] = obj;
                    };
                    _disposeActions.Add(() => Addressables.Release(childHandle));
                    childOperations.Add(childHandle);
                } catch (Exception e) {
                    Debug.LogError($"Failed to load asset from location {location.PrimaryKey}: {e.Message}");
                }
            }

            var groupOperation = Addressables.ResourceManager.CreateGenericGroupOperation(childOperations);
            await groupOperation;

            // Assign
            Objects = objectList.AsReadOnly();
            GuidLookup = guidLookupDict;
            NameLookup = nameLookupDict;
            EnumLookup.Clear();

            if (enumType != null) AddEnumLookup(enumType);

            Addressables.Release(handle);

            await AfterLoad();
        }

        public void AddEnumLookup(Type enumType) {
            foreach (var value in Enum.GetValues(enumType)) {
                var name = Enum.GetName(enumType, value);
                if (name == null) continue;
                if (NameLookup.TryGetValue(name, out var obj))
                    EnumLookup[(Enum)value] = obj;
                else
                    Debug.LogWarning($"Enum value '{name}' not found in registry for type '{enumType.Name}'.");
            }
        }

    }
}