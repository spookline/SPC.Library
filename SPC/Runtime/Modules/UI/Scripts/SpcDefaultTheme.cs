using System.Collections.Generic;
using HELIX.Coloring;
using HELIX.Extensions;
using HELIX.Widgets;
using HELIX.Widgets.Elements;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Theme;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {

  [UxmlElement]
  public partial class SpcDefaultTheme : BaseElement {

    public static List<ThemeComponent> Generate(Color seedColor, bool dark, bool isEditor) {
      var colors = PrimitiveColorScheme.From(seedColor, dark ? Brightness.Dark : Brightness.Light);
      var spacing = isEditor ? new PrimitiveSpacingScheme { factor = 1f } : PrimitiveSpacingScheme.Default;
      var typography = isEditor ? new PrimitiveTypographyScheme { factor = 1f } : PrimitiveTypographyScheme.Default;
      var radius = isEditor ? new PrimitiveRadiusScheme { factor = 1f } : PrimitiveRadiusScheme.Default;
      return new List<ThemeComponent> {
        new PrimitiveBaseThemeComponent {
          colors = colors,
          spacing = spacing,
          typography = typography,
          radius = radius,
        }
      };
    }

    private readonly ThemeProviderElement _themeProvider;
    private Color _seedColor = Color.blue;
    private bool _dark;
    private bool _isEditor;

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


    public SpcDefaultTheme() {
      _themeProvider = new ThemeProviderElement().Fill();
      hierarchy.Add(_themeProvider);
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


    public override VisualElement contentContainer => _themeProvider;

  }
}