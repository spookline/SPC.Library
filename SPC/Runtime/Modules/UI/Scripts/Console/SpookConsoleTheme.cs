using HELIX.Coloring;
using HELIX.Types;
using HELIX.Widgets.Theming;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Theme;
using Spookline.SPC.Console;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {

  public class SpookConsoleColors {

    public Color typeInfo;
    public Color typeInput;
    public Color typeWarning;
    public Color typeError;
    public Color typeBug;

    public Color infoActive;
    public Color infoWeak;
    public Color infoValid;
    public Color infoError;


    public CommandInfoRichTextStyle GetRichTextStyle =>
      new() {
        active = infoActive.ToHex(),
        weak = infoWeak.ToHex(),
        valid = infoValid.ToHex(),
        error = infoError.ToHex(),
      };

  }

  public class SpookConsoleStyle {

    public BoxConstraints constraints;
    public SubstanceLayers background;
    public StyleLength4 padding;
    public HTextFieldStyle commandLine;

    public TextStyle historyText;
    public StyleLength4 historyEntryPadding;
    public StyleLength historyIconSize;

    public SubstanceLayers aheadBackground;
    public StyleLength4 aheadPadding;
    public TextStyle aheadInfoText;
    public TextStyle aheadCompletionText;

    public SubstanceLayers viewerBackground;
    public StyleLength4 viewerPadding;
    public StyleLength4 viewerMargin;
    public float viewerHeaderGap;
    public HButtonStyle viewerHeaderButton;
    public TextStyle viewerHeaderText;
    public TextStyle viewerContentText;

    public SpookConsoleColors colors;

  }
}