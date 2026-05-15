using System.Collections.Generic;
using System.Threading;
using HELIX.Coloring;
using HELIX.Coloring.Material;
using HELIX.Extensions;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Navigation;
using HELIX.Widgets.Signals;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Theme;
using Spookline.SPC.Console;
using Spookline.SPC.Focus;
using UnityEngine;
using UnityEngine.UI;

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

    public override State<SpookConsoleView> CreateState() => new State();

    private class State : State<SpookConsoleView> {

      public ValueSignal<ExtendedLogEntry?> selectedEntry;

      public override void InitState() {
        selectedEntry = AddDisposable(new ValueSignal<ExtendedLogEntry?>());
      }


      public PrimitiveBaseThemeComponent GetComponent(BuildContext context) {
        if (!widget.isEditor)
          return new PrimitiveBaseThemeComponent() {
            colors = PrimitiveBaseTheme.Colors.Get(context),
            spacing = PrimitiveBaseTheme.Spacing.Get(context),
            typography = PrimitiveBaseTheme.Typography.Get(context),
            radius = PrimitiveBaseTheme.Radius.Get(context),
          };

        var colors = PrimitiveColorScheme.From(MaterialColors.Blue, Brightness.Dark);
        var spacing = new PrimitiveSpacingScheme { factor = 1f };

        return new PrimitiveBaseThemeComponent {
          colors = colors,
          spacing = spacing,
          typography = new PrimitiveTypographyScheme { factor = 1f },
          radius = new PrimitiveRadiusScheme { factor = 1f }
        };
      }

      public override Widget Build(BuildContext context) {
        var component = GetComponent(context);

        var primary = component.colors.value.primary.main;
        var infoColor = Colors.Blue.Harmonize(primary);
        var warningColor = Colors.Yellow.Harmonize(primary);
        var errorColor = Colors.Red.Harmonize(primary);
        var successColor = Colors.Green.Harmonize(primary);
        var weakColor = component.colors.value.surface.onMain.WithOpacity(0.5f);
        var activeColor = component.colors.value.primary.main;

        var consoleStyle = new CommandInfoRichTextStyle {
          weak = weakColor.ToHex(),
          active = activeColor.ToHex(),
          valid = successColor.ToHex(),
          error = errorColor.ToHex(),
        };

        return new HThemeProvider(
          new List<ThemeComponent> { component },
          modifiers: new Modifier[] {
            new PaddingModifier(component.spacing.value.Space3),
            new BackgroundStyleModifier(component.colors.value.surface.main),
            new TextStyleModifier(
              new TextStyle {
                color = component.colors.value.surface.onMain,
                generator = TextGeneratorType.Standard
              }
            )
          }
        ) {
          new HStack {
            new HRow {
              new SpookConsoleHistory(
                infoColor: infoColor,
                successColor: successColor,
                warningColor: warningColor,
                errorColor: errorColor,
                selectedEntry: selectedEntry,
                refreshing: widget.isEditor // TODO: Maybe later also for non-editors?
              ).Fill(),
              new LogMessageViewer(
                selectedEntry.Value ?? default,
                () => { selectedEntry.Value = null; },
                modifiers: new Modifier[] {
                  new MarginModifier(EdgeInsets.Only(left: 8f)),
                  new DisplayModifier(selectedEntry.Value.HasValue),
                  new SizeModifier(BoxConstraints.Tight(50.Percent(), 100.Percent()))
                }
              )
            }.Positioned(EdgeInsets.Only(0, 0, 0, 32)),
            new SpookConsoleCommandLine(widget.cmdTextKey, consoleStyle)
              .Positioned(EdgeInsets.Only(bottom: 0, left: 0, right: 0))
          }
        };
      }

    }

  }
}