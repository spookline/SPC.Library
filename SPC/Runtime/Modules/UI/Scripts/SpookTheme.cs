using System.Collections.Generic;
using HELIX.Coloring;
using HELIX.Coloring.Material;
using HELIX.Extensions;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Substances;
using HELIX.Widgets.Universal.Theme;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
  public class SpookTheme {

    public static ThemeProperty<SpookConsoleStyle> Console = ThemeProperty.ExtractMaybe(
      "spook-console",
      SpookThemeComponent.Default,
      component => component.console
    ).Compute(provider => {
        var colors = PrimitiveBaseTheme.Colors.Get(provider);
        var typo = PrimitiveBaseTheme.Typography.Get(provider);
        var spacing = PrimitiveBaseTheme.Spacing.Get(provider);
        var radius = PrimitiveBaseTheme.Radius.Get(provider);
        var textField = PrimitiveTheme.TextField.Get(provider);
        var isEditor = IsEditor.Get(provider);

        var baseText = new TextStyle {
          generator = TextGeneratorType.Standard,
          wrap = WhiteSpace.Normal,
          fontSize = typo.FontSize3,
          color = colors.surface.onMain
        };

        var aheadComplete = new TextStyle();
        aheadComplete.Merge(baseText);
        aheadComplete.fontSize = typo.FontSize1;

        var aheadInfo = new TextStyle();
        aheadInfo.Merge(aheadComplete);
        aheadInfo.fontSize = typo.FontSize2;

        var viewerText = new TextStyle();
        viewerText.Merge(baseText);
        viewerText.fontSize = typo.FontSize1;

        var primary = colors.primary.main;
        var green = Colors.Green.Harmonize(primary);
        var yellow = Colors.Yellow.Harmonize(primary);
        var blue = Colors.Blue.Harmonize(primary);
        var weak = colors.surface.onMain.WithOpacity(0.5f);

        return new SpookConsoleStyle {
          background = new BoxSubstance {
            background = new BackgroundStyle {
              color = colors.surface.main
            },
            borderRadius = isEditor ? BorderRadius.None : BorderRadius.All(radius.Radius3)
          },
          padding = EdgeInsets.All(spacing.Space3),

          viewerBackground = new BoxSubstance {
            background = new BackgroundStyle {
              color = colors.surface.container
            },
            borderRadius = BorderRadius.All(radius.Radius3)
          },
          viewerPadding = EdgeInsets.All(spacing.Space3),
          viewerMargin = EdgeInsets.Only(left: spacing.Space3),
          viewerHeaderText = baseText,
          viewerContentText = viewerText,
          viewerHeaderGap = spacing.Space1,
          viewerHeaderButton = DefaultButtonStyles.DefaultStyleOf(
            provider,
            HButtonVariant.Ghost,
            HButtonSize.Small,
            HInputRadius.Medium
          ),

          historyEntryPadding = EdgeInsets.Symmetric(spacing.Space1, spacing.Space1 * 0.5f),
          historyText = baseText,
          historyIconSize = typo.FontSize3,

          aheadBackground = new BoxSubstance {
            background = new BackgroundStyle {
              color = colors.surface.container
            },
            borderRadius = BorderRadius.Only(topLeft: radius.Radius3, topRight: radius.Radius3)
          },
          aheadPadding = EdgeInsets.All(spacing.Space2),
          aheadInfoText = aheadInfo,
          aheadCompletionText = aheadComplete,

          constraints = BoxConstraints.Tight(50.Percent(), 50.Percent()),
          commandLine = textField,
          colors = new SpookConsoleColors {
            infoError = colors.error.main,
            infoActive = primary,
            infoValid = green,
            infoWeak = weak,

            typeError = colors.error.main,
            typeBug = colors.error.main,
            typeInfo = blue,
            typeInput = green,
            typeWarning = yellow,
          }
        };
      }
    );

    public static ThemeProperty<bool> IsEditor = ThemeProperty.ExtractMaybe(
      "spook-is-editor",
      SpookThemeComponent.Default,
      component => component.isEditor
    );

    public static readonly IReadOnlyList<ThemeProperty> Properties = new ThemeProperty[] {
      Console, IsEditor
    };

  }

  public class SpookThemeComponent : ThemeComponent {

    public static readonly SpookThemeComponent Default = new() {
      console = new ThemeOptional<SpookConsoleStyle>(),
      isEditor = false
    };

    public SpookThemeComponent() {
      lookupScope = SpookTheme.Properties;
    }

    public ThemeOptional<SpookConsoleStyle> console;
    public ThemeOptional<bool> isEditor;

  }
}