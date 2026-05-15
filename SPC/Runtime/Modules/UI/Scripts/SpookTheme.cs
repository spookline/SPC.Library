using System.Collections.Generic;
using HELIX.Widgets.Theming;

namespace Spookline.SPC.UI {
  public static class SpookTheme {

    public static readonly ThemeProperty<SpookConsoleStyle> Console = ThemeProperty.ExtractMaybe(
      "spook-console",
      SpookThemeComponent.Default,
      component => component.console
    ).Compute(provider => SpookConsoleStyle.DefaultOf(provider));

    public static readonly ThemeProperty<bool> IsEditor = ThemeProperty.ExtractMaybe(
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