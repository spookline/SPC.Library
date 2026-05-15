using System;
using System.Collections.Generic;
using System.Linq;
using HELIX.Widgets;
using HELIX.Widgets.Navigation;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Universal;
using Spookline.SPC.Ext;
using UnityEngine.UIElements;
using Key = HELIX.Widgets.Key;

namespace Spookline.SPC.UI {
  public class PlayerHudManager : SpookManagerBehaviour<PlayerHudManager> {

    public static ValueSignal<PlayerHudManager> Signal = new(equality: false);

    [NonSerialized]
    public readonly List<(string, Widget)> components = new();

    [NonSerialized]
    public readonly GlobalKey<ScaffoldElement> scaffoldKey = new();

    [NonSerialized]
    public readonly GlobalKey<NavStackElement> navStackKey = new();

    protected override void Awake() {
      base.Awake();
    }

    protected override void OnEnable() {
      base.OnEnable();
      SetDirty();
    }

    protected override void OnDisable() {
      base.OnDisable();
      Signal.SetValue(null);
    }

    public void SetDirty() {
      Signal.SetValue(this);
    }

  }


  public class HudWidget : StatefulWidget<HudWidget> {

    public HudWidget(
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) { }

    public override State<HudWidget> CreateState() => new HudWidgetState();

    private class HudWidgetState : State<HudWidget> {

      public override Widget Build(BuildContext context) {
        var manager = PlayerHudManager.Signal.Value;
        if (!manager) return new HText("No Hud Manager");
        return new HScaffold(key: manager.scaffoldKey) {
          new HNavStack(key: manager.navStackKey) {
            new HStack {
              manager.components.Select(x => x.Item2).Spread()
            }.Stretch()
          }.Stretch()
        }.Stretch();
      }

    }

  }

  [UxmlElement]
  public partial class HudWidgetElement : WidgetHostElement {

    public HudWidgetElement() {
      Buildable = new HudWidget().ToBuildable();
    }

  }
}