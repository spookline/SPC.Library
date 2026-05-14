using System;
using System.Collections.Generic;
using HELIX.Types;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Scrolling;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Styles;
using HELIX.Widgets.Universal.Theme;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.UI {
  public class LogMessageViewer : StatefulWidget<LogMessageViewer> {

    public ExtendedLogEntry entry;
    public Action onClose;

    public LogMessageViewer(
      ExtendedLogEntry entry,
      Action onClose,
      Key key = default,
      object[] constants = null,
      IReadOnlyCollection<Modifier> modifiers = null
    ) : base(key, constants, modifiers) {
      this.entry = entry;
      this.onClose = onClose;
    }

    public override State<LogMessageViewer> CreateState() => new State();

    private class State : State<LogMessageViewer> {

      public bool wrapLines = false;

      public override Widget Build(BuildContext context) {
        var colors = PrimitiveBaseTheme.Colors.Get(context);
        var radius = PrimitiveBaseTheme.Radius.Get(context);
        var typography = PrimitiveBaseTheme.Typography.Get(context);

        var general = new TextStyle {
          wrap = WhiteSpace.Normal,
          generator = TextGeneratorType.Standard,
        };

        var message = new TextStyle {
          wrap = WhiteSpace.Normal,
          generator = TextGeneratorType.Standard,
          fontSize = typography.FontSize1
        };

        var wrapped = new TextStyle {
          wrap = wrapLines ? WhiteSpace.Normal : WhiteSpace.NoWrap,
          generator = TextGeneratorType.Standard,
          fontSize = typography.FontSize1
        };

        return new HColumn(
          crossAxisAlign: Align.Stretch,
          modifiers: new Modifier[] {
            new TextStyleModifier(general),
            new PaddingModifier(8f),
            new BackgroundStyleModifier(colors.surface.container),
            new BorderModifier(Border.None, BorderRadius.All(radius.Radius3))
          }
        ) {
          new HRow(crossAxisAlign: Align.FlexStart) {
            new HText("Log Message").Body(context),
            new HGap().Expand(),
            new HButton(
              HButtonVariant.Ghost,
              size: HButtonSize.Small,
              selected: wrapLines,
              onClick: SetState(() => wrapLines = !wrapLines)
            ) {
              new HRow(gap: 4f) {
                new HIcon(
                  FaSolidIcons.TextWidth,
                  FaSolidIcons.FontDefinition
                ),
                new HText(
                  "Wrap",
                  style: new TextStyle() {
                    style = FontStyle.Bold
                  }
                )
              }
            },
            new HButton(
              HButtonVariant.Ghost,
              size: HButtonSize.Small,
              onClick: () => { GUIUtility.systemCopyBuffer = widget.entry.GetFullText().Trim(); }
            ) {
              new HIcon(FaSolidIcons.Copy, FaSolidIcons.FontDefinition)
            },
            new HButton(HButtonVariant.Ghost, onClick: widget.onClose, size: HButtonSize.Small) {
              new HIcon(FaSolidIcons.Xmark, FaSolidIcons.FontDefinition)
            }
          },
          new HScrollView {
            new HText(widget.entry.message ?? "", style: message, selectable: true),
            new HGap(),
            new HText(widget.entry.stackTrace ?? "", style: wrapped, selectable: true)
          }.Fill()
        };
      }

    }

  }
}