using System;
using UnityEngine;
using UnityEngine.Rendering;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class TemporalProvider : MonoBehaviour
{
    private static readonly int TemporalIndexID = Shader.PropertyToID("_TemporalFrameIndex");
    private static readonly int TemporalIntensityID = Shader.PropertyToID("_TemporalIntensity");
    private static GlobalKeyword TemporalEffects = default;
    private int _fakeFrameCount = 0;
    public bool enableTemporalEffects = true;

    private void Awake() {
        TemporalEffects = GlobalKeyword.Create("ENABLE_TEMPORAL_EFFECTS");
    }

    void OnEnable()
    {
#if UNITY_EDITOR
        // Hook into the global editor update loop so it runs even outside of Play Mode
        EditorApplication.update += EditorUpdate;
#endif
    }

    void OnDisable()
    {
#if UNITY_EDITOR
        EditorApplication.update -= EditorUpdate;
#endif
    }

    void Update()
    {
        // This handles runtime (Play Mode)
        if (Application.isPlaying) {
            var frameCount = Time.frameCount % 8;
            Refresh(frameCount);
        }
    }

    private void Refresh(int frameCount) {
        if (!enableTemporalEffects) {
            Shader.SetGlobalInt(TemporalIndexID, 0);
            Shader.SetGlobalFloat(TemporalIntensityID, 0f);
            Shader.DisableKeyword(TemporalEffects);
            return;
        }

        var intensity = frameCount / 8.0f;
        Shader.SetGlobalInt(TemporalIndexID, frameCount);
        Shader.SetGlobalFloat(TemporalIntensityID, intensity);
        Shader.EnableKeyword(TemporalEffects);
    }

    void EditorUpdate()
    {
        // This handles Edit Mode
        if (!Application.isPlaying)
        {
            _fakeFrameCount++;
            Refresh(_fakeFrameCount % 4);

            // OPTIONAL: Forces the scene view to repaint constantly so you can see the dithering animate
#if UNITY_EDITOR
            SceneView.RepaintAll();
#endif
        }
    }
}