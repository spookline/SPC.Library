using Cysharp.Threading.Tasks;
using HELIX.Extensions;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
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

    private void Awake() {
      this.PerformedInput(
        toggleAction,
        ctx => {
          if (FocusManager.Instance.HasFocus(this, out _)) return;
          Open();
        }
      );
    }

    public int FocusFlags =>
      DefaultFocusFlags.EnableCursor | DefaultFocusFlags.DisableLook | DefaultFocusFlags.DisableMovement |
      DefaultFocusFlags.BlockInputActions | DefaultFocusFlags.EscapeCancelable;

    public void OnFocusGained(FocusContext context) {
      var scaffold = PlayerHudManager.Instance.scaffoldKey.Element;
      var focusKey = new GlobalKey();

      var overlay = scaffold.AddOverlay(
        new WidgetHostElement {
          Buildable = new HStatefulBuilder((ctx, _) => {
              var style = SpookTheme.Console.Get(ctx);
              Alignment.AlignmentHelper.ToColumnAlignment(style.anchor, out var mainAxis, out var crossAxis);
              return new HColumn(mainAxis, crossAxis) {
                new SpookConsoleView(
                  focusKey,
                  modifiers: new Modifier[] {
                    new SizeModifier(style.constraints)
                  }
                )
              }.Margin(style.margin).Fill();
            }
          ).Fill().ToBuildable()
        }.Stretched()
      );

      context.CancellationToken.Register(() => scaffold.RemoveOverlay(overlay));
      FocusLater(focusKey).Forget();
    }

    public void OnFocusLost(FocusContext context) { }

    public void Open() {
      FocusManager.Instance.Focus(this, destroyCancellationToken);
    }

    private static async UniTaskVoid FocusLater(GlobalKey key) {
      await UniTask.Delay(10);
      if (key.Target != null) key.Focus();
      else Debug.LogWarning($"Tried to focus console {key} but it was null");
    }

  }
}