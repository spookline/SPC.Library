using System.Collections.Generic;
using HELIX.Coloring;
using HELIX.Extensions;
using HELIX.Widgets.Elements;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal.Theme;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
  [UxmlElement]
  public partial class SpcDefaultTheme : BaseElement {

    private readonly ThemeProviderElement _themeProvider;
    private bool _dark = true;
    private bool _isEditor;
    private Color _seedColor = Color.blue;


    public SpcDefaultTheme() {
      _themeProvider = new ThemeProviderElement().Fill();
      hierarchy.Add(_themeProvider);
    }

    [UxmlAttribute]
    public Color SeedColor {
      get => _seedColor;
      set {
        _seedColor = value;
        Regenerate();
      }
    }

    [UxmlAttribute]
    public bool Dark {
      get => _dark;
      set {
        _dark = value;
        Regenerate();
      }
    }

    [UxmlAttribute]
    public bool IsEditor {
      get => _isEditor;
      set {
        _isEditor = value;
        Regenerate();
      }
    }


    public override VisualElement contentContainer => _themeProvider;

    public static List<ThemeComponent> Generate(Color seedColor, bool dark, bool isEditor) {
      var colors = PrimitiveColorScheme.From(seedColor, dark ? Brightness.Dark : Brightness.Light);
      var spacing = isEditor ? new PrimitiveSpacingScheme { factor = 0.75f } : PrimitiveSpacingScheme.Default;
      var typography = isEditor ? new PrimitiveTypographyScheme { factor = 0.85f, lineHeightFactor = 1.1f } : PrimitiveTypographyScheme.Default;
      var radius = isEditor ? new PrimitiveRadiusScheme { factor = 0.75f } : PrimitiveRadiusScheme.Default;
      return new List<ThemeComponent> {
        new PrimitiveBaseThemeComponent {
          colors = colors,
          spacing = spacing,
          typography = typography,
          radius = radius
        },
        new SpookThemeComponent {
          isEditor = isEditor
        }
      };
    }

    protected override void OnAttached(AttachToPanelEvent evt) {
      base.OnAttached(evt);
      Regenerate();
    }

    public void Regenerate() {
      _themeProvider.Components = Generate(
        SeedColor,
        _dark,
        IsEditor
      );

      var colors = _themeProvider.GetThemed(PrimitiveBaseTheme.Colors, false);
      var typography = _themeProvider.GetThemed(PrimitiveBaseTheme.Typography, false);
      style.fontSize = typography.FontSize3;
      style.color = colors.surface.onMain;
    }

  }
}