using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using HELIX.Coloring;
using Spookline.SPC.Debugging;
using Spookline.SPC.Ext;
using Unity.Mathematics;

namespace Spookline.SPC.Examples {
  public class ExampleOverlayBehaviour : SpookBehaviour<ExampleOverlayBehaviour> {

    public string entityTitle = "Drone-7";

    private List<float> _frameTimes = new();

    private void Awake() {
      On<GizmoEvt>().Do(OnGizmos);
    }


    private void Update() {
      _frameTimes.Add(Time.deltaTime);
      if (_frameTimes.Count > 100) _frameTimes.RemoveAt(0);
    }

    private void OnGizmos(ref GizmoEvt args) {
      if (args.WorldOverlayPass(out var world)) {
        var id = EntityId.ToULong(gameObject.GetEntityId());
        world.Box(id, entityTitle, transform.position)
          .Field("Status", "Active", color: Colors.Green)
          .Field("Health", 85, "%", color: Colors.Yellow)
          .Field("Distance", Vector3.Distance(transform.position, Camera.main.transform.position), 1, "m")
          .Field("Time", Time.time, 1, "s", Colors.LightBlue)
          .Field("Position", transform.position);

        world.Box(id + 1, "Head", transform.position + Vector3.up * 2);
      }

      if (args.ScreenOverlayPass(out var screen)) {
        screen.Section("Player Stats", 10)
          .Field("Status", "Active", color: Colors.Green)
          .Field("Health", 85, "%", color: Colors.Yellow)
          .Field("Distance", Vector3.Distance(transform.position, Camera.main.transform.position), 1, "m")
          .Field("Time", Time.time, 1, "s", Colors.LightBlue)
          .Field("Position", transform.position);

        screen.Section("Environment", 20)
          .Field("Temperature", 22.5f, 1, "°C")
          .Field("Wind Speed", 5.2f, 1, "m/s");

        var avgFrameTime = _frameTimes.Average();
        var averageFps = 1f / avgFrameTime;

        var targetFps = math.clamp(Application.targetFrameRate, 60, 144);
        var targetFrameTime = 1f / targetFps;

        var missed = 0;
        foreach (var frameTime in _frameTimes) {
          if (frameTime > targetFrameTime) missed++;
        }

        screen.Global()
          .Field("Frame Time", avgFrameTime * 1000f, 2, "ms")
          .Field("Target Frame Time", targetFrameTime * 1000f, 2, "ms")
          .Field("FPS", averageFps, 0, "")
          .Field("Target FPS", targetFps, 0, "")
          .Field("Missed Frames", missed, 0, "%");
      }
    }

  }
}