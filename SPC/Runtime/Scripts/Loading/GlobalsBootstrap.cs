using System.Collections.Generic;
using System.IO;
using Cysharp.Threading.Tasks;
using Sirenix.OdinInspector;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Spookline.SPC.Loading {

    [HideMonoScript]
    [DefaultExecutionOrder(-400)]
    public class GlobalsBootstrap : MonoBehaviour {

        [ValueDropdown("GetSceneNames")]
        public string globalsScene = "none";

        private static IEnumerable<string> GetSceneNames() {
            yield return "none";
            for (var i = 0; i < SceneManager.sceneCountInBuildSettings; i++) {
                var path = SceneUtility.GetScenePathByBuildIndex(i);
                var name = Path.GetFileNameWithoutExtension(path);
                yield return name;
            }
        }

        public void Awake() {
            if (Globals.Instance) {
                Destroy(gameObject);
                return;
            }

            var currentScene = SceneManager.GetActiveScene().name;
            if (globalsScene != "none") {
                Load(globalsScene, currentScene).Forget();
                return;
            }

            Debug.Log("Creating globals instance automatically...");
            var obj = new GameObject("Injected Globals");
            DontDestroyOnLoad(obj);
            obj.AddComponent<Globals>();
            Debug.Log("Globals prepared, reloading scene..." );
            SceneManager.LoadScene(currentScene);
        }

        private static async UniTaskVoid Load(string globalsScene, string nextScene) {
            Debug.Log("Loading globals scene...");
            await SceneManager.LoadSceneAsync(globalsScene, LoadSceneMode.Single);
            await Globals.UntilStarted(); // Wait until Globals is fully initialized
            await UniTask.DelayFrame(1); // Ensure the scene is fully loaded
            Debug.Log("Globals scene loaded, switching to next scene...");
            await SceneManager.LoadSceneAsync(nextScene, LoadSceneMode.Single);
        }

    }
}