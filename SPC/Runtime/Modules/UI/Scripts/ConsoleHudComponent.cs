using Cysharp.Threading.Tasks;
using HELIX.Extensions;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Utilities;
using Spookline.SPC.Ext;
using Spookline.SPC.Focus;
using Spookline.SPC.Input;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Spookline.SPC.UI {
  public class ConsoleHudComponent : SpookBehaviour<ConsoleHudComponent>, IFocusable, IFocusHandler {

    public InputActionReference toggleAction;

    public TextAnchor position = TextAnchor.LowerCenter;

    private void Awake() {
      this.PerformedInput(toggleAction, ctx => {
        if (FocusManager.Instance.HasFocus(this, out _)) return;
        Open();
      });
    }

    public int FocusFlags =>
      DefaultFocusFlags.EnableCursor | DefaultFocusFlags.DisableLook | DefaultFocusFlags.DisableMovement |
      DefaultFocusFlags.BlockInputActions | DefaultFocusFlags.EscapeCancelable;

    public void Open() => FocusManager.Instance.Focus(this, destroyCancellationToken);

    public void OnFocusGained(FocusContext context) {
      var scaffold = PlayerHudManager.Instance.scaffoldKey.Element;
      var focusKey = new GlobalKey();
      Alignment.AlignmentHelper.ToColumnAlignment(position, out var mainAxis, out var crossAxis);

      var overlay = scaffold.AddOverlay(
        new WidgetHostElement {
          Buildable = new HColumn(mainAxisAlign: mainAxis, crossAxisAlign: crossAxis) {
            new SpookConsoleView(cmdTextKey: focusKey).Size(50.Percent(), 50.Percent()).Tight()
          }.ToBuildable()
        }.Stretched()
      );

      context.CancellationToken.Register(() => scaffold.RemoveOverlay(overlay));
      FocusLater(focusKey).Forget();
    }

    public void OnFocusLost(FocusContext context) { }

    private static async UniTaskVoid FocusLater(GlobalKey key) {
      await UniTask.Delay(10);
      if (key.Target != null) key.Focus();
      else Debug.LogWarning($"Tried to focus console {key} but it was null");
    }

  }
}