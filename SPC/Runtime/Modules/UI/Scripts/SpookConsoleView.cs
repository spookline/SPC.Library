using System.Collections.Generic;
using HELIX.Extensions;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Universal;
using Spookline.SPC.Debugging;

namespace Spookline.SPC.UI {
  public class SpookConsoleView : StatefulWidget<SpookConsoleView> {

    public readonly bool isEditor;
    public GlobalKey cmdTextKey;

    public SpookConsoleView(
      GlobalKey cmdTextKey = null,
      bool isEditor = false,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.isEditor = isEditor;
      this.cmdTextKey = cmdTextKey ?? new GlobalKey();
    }

    public override State<SpookConsoleView> CreateState() {
      return new State();
    }

    private class State : State<SpookConsoleView> {

      public ValueSignal<ExtendedLogEntry?> selectedEntry;

      public override void InitState() {
        selectedEntry = AddDisposable(new ValueSignal<ExtendedLogEntry?>());
      }


      public override Widget Build(BuildContext context) {
        var theme = SpookTheme.Console.Get(context);

        return new HSubstanceBox(
          substances: theme.background,
          boxModifiers: new ModifierSet {
            new PaddingModifier(theme.padding),
            new TextStyleModifier(theme.historyText)
          },
          builder: new HStack {
            new HRow {
              new SpookConsoleHistory(
                selectedEntry,
                true
              ).Fill(),
              new LogMessageViewer(
                selectedEntry.Value ?? default,
                () => { selectedEntry.Value = null; },
                modifiers: new Modifier[] {
                  new MarginModifier(theme.viewerMargin),
                  new DisplayModifier(selectedEntry.Value.HasValue),
                  new SizeModifier(BoxConstraints.Tight(50.Percent(), 100.Percent()))
                }
              )
            }.Positioned(EdgeInsets.Only(0, 0, 0, theme.commandLineHeight)),
            new SpookConsoleCommandLine(widget.cmdTextKey, theme.colors.GetRichTextStyle)
              .Positioned(EdgeInsets.Only(bottom: 0, left: 0, right: 0))
          }.Fill()
        );
      }

    }

  }
}