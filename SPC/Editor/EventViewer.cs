using System.Linq;
using Spookline.SPC.Events;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace Spookline.SPC.Editor {
    public class EventViewer : EditorWindow {

        private void CreateGUI() {
            var types = EventManager.Instance.reactors.Keys.Select(x => x.FullName).ToList();
            var dropdown = new DropdownField(types, types.FirstOrDefault());
            rootVisualElement.Add(dropdown);

            var view = new VisualElement {
                style = {
                    flexDirection = FlexDirection.Column,
                    flexGrow = 1,
                    paddingBottom = 8,
                    paddingTop = 8,
                    paddingLeft = 8,
                    paddingRight = 8
                }
            };
            if (types.Count > 0) {
                var scrollView = BuildVisualization(types.FirstOrDefault());
                view.Add(scrollView);
            }

            dropdown.RegisterValueChangedCallback(evt => {
                view.Clear();
                var scrollView = BuildVisualization(evt.newValue);
                view.Add(scrollView);
            });
            rootVisualElement.Add(view);
        }

        [MenuItem("Window/Event Viewer")]
        public static void ShowWindow() {
            GetWindow<EventViewer>("Event Viewer");
        }

        public VisualElement BuildVisualization(string eventName) {
            var scrollView = new ScrollView();
            var eventReactor = EventManager.Instance.reactors.FirstOrDefault(x => x.Key.FullName == eventName).Value;
            var info = eventReactor.CreateInfo();
            var yHeight = 64;
            var column = new VisualElement { style = { flexDirection = FlexDirection.Column } };
            foreach (var eventInfo in info.Rows) {
                var priorityLabel = new Label("Priority: " + eventInfo.Priority) {
                    style = {
                        paddingBottom = 4,
                        color = new Color(1, 1, 1, 0.5f)
                    }
                };

                column.Add(priorityLabel);

                var row = new VisualElement {
                    style = {
                        flexDirection = FlexDirection.Row,
                        height = yHeight,
                        marginBottom = 8
                    }
                };

                foreach (var handler in eventInfo.Handlers) {
                    var handlerLabel = new Label(handler) {
                        style = {
                            width = 300,
                            height = yHeight,
                            backgroundColor = new Color(0, 0, 0, 0.2f),
                            color = Color.white,
                            whiteSpace = WhiteSpace.Normal,
                            marginRight = 8,
                            borderTopLeftRadius = 8,
                            borderTopRightRadius = 8,
                            borderBottomLeftRadius = 8,
                            borderBottomRightRadius = 8,
                            unityTextAlign = TextAnchor.MiddleCenter,
                            // All border width 2
                            borderLeftWidth = 2,
                            borderRightWidth = 2,
                            borderTopWidth = 2,
                            borderBottomWidth = 2,
                            // All white borders
                            borderLeftColor = new Color(0, 0, 0, 0.1f),
                            borderRightColor = new Color(0, 0, 0, 0.1f),
                            borderTopColor = new Color(0, 0, 0, 0.1f),
                            borderBottomColor = new Color(0, 0, 0, 0.1f)
                        }
                    };
                    row.Add(handlerLabel);
                }

                column.Add(row);
            }

            scrollView.Add(column);
            return scrollView;
        }

    }
}