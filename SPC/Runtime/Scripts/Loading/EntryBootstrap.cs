using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spookline.SPC.Loading {
    [HideMonoScript]
    public class EntryBootstrap : MonoBehaviour {

        [ValueDropdown("GetSceneNames")]
        public string globalsSceneName = "globals";

        [ValueDropdown("GetSceneNames")]
        public string nextSceneName = "menu";
        
        private void Start() {
            Load(globalsSceneName, nextSceneName).Forget();
        }

        private static async UniTaskVoid Load(string globalsScene, string nextScene) {
            Debug.Log("Loading globals scene...");
            await SceneManager.LoadSceneAsync(globalsScene, LoadSceneMode.Additive);
            await Globals.UntilStarted(); // Wait until Globals is fully initialized
            await UniTask.DelayFrame(1); // Ensure the scene is fully loaded
            Debug.Log("Globals scene loaded, switching to main menu...");
            await SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
        }

        private static IEnumerable<string> GetSceneNames() {
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = Path.GetFileNameWithoutExtension(path);
                yield return name;
            }
        }
    }
}