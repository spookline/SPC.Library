using System;
using System.Collections.Generic;
using HELIX.Widgets;
using HELIX.Widgets.Modifiers;
using HELIX.Widgets.Scrolling;
using HELIX.Widgets.Universal;
using HELIX.Widgets.Universal.Styles;
using Spookline.SPC.Debugging;
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

    public override State<LogMessageViewer> CreateState() {
      return new State();
    }

    private class State : State<LogMessageViewer> {

      public bool wrapLines;

      public override Widget Build(BuildContext context) {
        var style = SpookTheme.Console.Get(context);

        return new HSubstanceBox(
          substances: style.viewerBackground,
          boxModifiers: new ModifierSet {
            new PaddingModifier(style.viewerPadding),
            new TextStyleModifier(style.viewerContentText)
          },
          builder: new HColumn(crossAxisAlign: Align.Stretch) {
            new HRow(crossAxisAlign: Align.FlexStart) {
              new HText("Log Message", style: style.viewerHeaderText),
              new HGap().Expand(),
              new HButton(
                style: style.viewerHeaderButton,
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
                    style: new TextStyle {
                      style = FontStyle.Bold
                    }
                  )
                }
              },
              new HButton(
                style: style.viewerHeaderButton,
                onClick: () => { GUIUtility.systemCopyBuffer = widget.entry.GetFullText().Trim(); }
              ) {
                new HIcon(FaSolidIcons.Copy, FaSolidIcons.FontDefinition)
              },
              new HButton(style: style.viewerHeaderButton, onClick: widget.onClose) {
                new HIcon(FaSolidIcons.Xmark, FaSolidIcons.FontDefinition)
              }
            },
            new HScrollView {
              new HGap(2),
              new HText(widget.entry.message ?? "", style: style.viewerContentText, selectable: true),
              new HGap(),
              new HText(
                widget.entry.stackTrace ?? "",
                style: wrapLines ? style.viewerContentText : style.viewerContentTextNoWrap,
                selectable: true
              )
            }.Fill()
          }.Fill()
        );
      }

    }

  }
}