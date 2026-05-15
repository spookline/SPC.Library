using HELIX.Coloring;
using HELIX.Coloring.Material;
using HELIX.Extensions;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Substances;
using HELIX.Widgets.Universal.Theme;
using Spookline.SPC.Console;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
  public class SpookConsoleColors {

    public Color infoActive;
    public Color infoError;
    public Color infoValid;
    public Color infoWeak;
    public Color typeBug;
    public Color typeError;

    public Color typeInfo;
    public Color typeInput;
    public Color typeWarning;


    public CommandInfoRichTextStyle GetRichTextStyle =>
      new() {
        active = infoActive.ToHex(),
        weak = infoWeak.ToHex(),
        valid = infoValid.ToHex(),
        error = infoError.ToHex()
      };

  }

  public class SpookConsoleStyle {

    public SubstanceLayers aheadBackground;
    public TextStyle aheadCompletionText;
    public TextStyle aheadInfoText;
    public StyleLength4 aheadPadding;
    public TextAnchor anchor;
    public SubstanceLayers background;

    public SpookConsoleColors colors;

    public HTextFieldStyle commandLine;
    public float commandLineHeight;

    public BoxConstraints constraints;
    public StyleLength4 historyEntryPadding;
    public StyleLength historyIconSize;

    public TextStyle historyText;
    public StyleLength4 margin;
    public StyleLength4 padding;

    public SubstanceLayers viewerBackground;
    public TextStyle viewerContentText;
    public TextStyle viewerContentTextNoWrap;
    public HButtonStyle viewerHeaderButton;
    public TextStyle viewerHeaderText;
    public StyleLength4 viewerMargin;
    public StyleLength4 viewerPadding;


    public static SpookConsoleStyle DefaultOf(IThemeProvider provider, float scaling = -1f) {
      var colors = PrimitiveBaseTheme.Colors.Get(provider);
      var isEditor = SpookTheme.IsEditor.Get(provider);

      if (scaling < 0) scaling = isEditor ? 1f : 0.9f;

      var typo = PrimitiveBaseTheme.Typography.Get(provider);
      var spacing = PrimitiveBaseTheme.Spacing.Get(provider);
      var radius = PrimitiveBaseTheme.Radius.Get(provider);

      typo = new PrimitiveTypographyScheme {
        em = typo.em,
        factor = scaling * typo.factor,
        lineHeightFactor = typo.lineHeightFactor * scaling
      };
      spacing = new PrimitiveSpacingScheme {
        basis = spacing.basis,
        factor = scaling * spacing.factor
      };
      radius = new PrimitiveRadiusScheme {
        factor = scaling * radius.factor
      };

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

      var viewerTextNoWrap = new TextStyle();
      viewerTextNoWrap.Merge(viewerText);
      viewerTextNoWrap.wrap = WhiteSpace.NoWrap;

      var textField = new HTextFieldStyle {
        textStyle = new TextStyle {
          color = colors.surface.onMain,
          fontSize = typo.FontSize2
        },
        constraints = BoxConstraints.Tight(StyleKeyword.Auto, typo.LineHeight2),
        padding = EdgeInsets.Symmetric(spacing.Space2, 0),
        layers = new SubstanceBuilder(provider).Outline().Build()
      };

      var primary = colors.primary.main;
      var green = Colors.Green.Harmonize(primary);
      var yellow = Colors.Yellow.Harmonize(primary);
      var blue = Colors.Blue.Harmonize(primary);
      var red = Colors.Red.Harmonize(primary);
      var weak = colors.surface.onMain.WithOpacity(0.5f);

      return new SpookConsoleStyle {
        background = new BoxSubstance {
          background = new BackgroundStyle {
            color = isEditor ? colors.surface.main : colors.surface.main.WithOpacity(0.998f)
          },
          borderRadius = isEditor ? BorderRadius.None : BorderRadius.All(radius.Radius3)
        },
        padding = EdgeInsets.All(spacing.Space3),
        margin = EdgeInsets.All(spacing.Space3),
        anchor = TextAnchor.LowerCenter,

        viewerBackground = new BoxSubstance {
          background = new BackgroundStyle {
            color = colors.surface.container
          },
          borderRadius = BorderRadius.All(radius.Radius3)
        },
        viewerPadding = EdgeInsets.All(spacing.Space3),
        viewerMargin = EdgeInsets.Only(spacing.Space3),
        viewerHeaderText = baseText,
        viewerContentText = viewerText,
        viewerContentTextNoWrap = viewerTextNoWrap,
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
          borderRadius = BorderRadius.Only(radius.Radius3, radius.Radius3)
        },
        aheadPadding = EdgeInsets.All(spacing.Space2),
        aheadInfoText = aheadInfo,
        aheadCompletionText = aheadComplete,
        constraints = BoxConstraints.Tight(95.Percent(), 95.Percent()),
        commandLine = textField,
        commandLineHeight = typo.FontSize3 + spacing.Space3 * 2,

        colors = new SpookConsoleColors {
          infoError = red,
          infoActive = primary,
          infoValid = green,
          infoWeak = weak,

          typeError = red,
          typeBug = red,
          typeInfo = blue,
          typeInput = green,
          typeWarning = yellow
        }
      };
    }

  }
}