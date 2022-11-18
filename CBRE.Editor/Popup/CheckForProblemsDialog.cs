using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CBRE.Common.Mediator;
using CBRE.DataStructures.Geometric;
using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Compiling.Lightmap;
using CBRE.Editor.Compiling.Lightmap.Legacy;
using CBRE.Editor.Documents;
using CBRE.Editor.Problems;
using CBRE.Editor.Rendering;
using CBRE.Graphics;
using CBRE.Providers.Map;
using CBRE.Settings;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Num = System.Numerics;

namespace CBRE.Editor.Popup {
    public class CheckForProblemsPopup : PopupUI {
        protected override bool canBeDefocused => true;
        protected override bool hasOkButton => true;

        private readonly Document document;
        private bool visibleOnly = false;
        private List<Problem> problems;

        public CheckForProblemsPopup(Document document) : base("Problems") {
            this.document = document;
            problems = ProblemChecker.Check(document.Map, visibleOnly).ToList();
        }

        protected override void ImGuiLayout(out bool shouldBeOpen) {
            ImGui.SetWindowSize(new Num.Vector2(300,200), ImGuiCond.FirstUseEver);
            shouldBeOpen = true;

            ImGui.Text("Visible Only");
            ImGui.SameLine();
            ImGui.Checkbox("##visibleOnly", ref visibleOnly);

            using (ColorPush.RedButton()) {
                if (ImGui.Button("Fix All")) {
                    IAction[] actions = new IAction[problems.Count];
                    for (int i = 0; i < problems.Count; i++) {
                        var problem = problems[i];
                        actions[i] = problem.Fix.Invoke(problem);
                    }
                    document.PerformAction("Fix All Problems", new ActionCollection(actions));
                    problems = ProblemChecker.Check(document.Map, visibleOnly).ToList();
                }
            }

            ImGui.Text("Problems");
            ImGui.Separator();

            if (ImGui.BeginChild("Problems", new Num.Vector2(0, ImGui.GetWindowHeight() * 0.5f))) {
                for (int i = 0; i < problems.Count; i++) {
                    var problem = problems[i];

                    using (ColorPush.RedButton()) {
                        if (ImGui.Button($"Fix##problemsFix{i}")) {
                            document.PerformAction(problem.Message, problem.Fix.Invoke(problem));
                            problems = ProblemChecker.Check(document.Map, visibleOnly).ToList();
                            break;
                        }
                    }
                    
                    ImGui.SameLine();

                    if (ImGui.Selectable(problem.Message, false)) {
                        document.Selection.Clear();
                        document.Selection.Select(problem.Objects);
                        document.Selection.Select(problem.Faces);
                        // for (int j = 0; j < ViewportManager.Viewports.Length; j++) {
                            // if (ViewportManager.Viewports[j] is Viewport3D viewport3D) {
                                // viewport3D.Camera.LookPosition = problem.Objects.
                            // }
                        // }
                    }
                    if (ImGui.IsItemHovered()) {
                        ImGui.BeginTooltip();
                        ImGui.SetTooltip(problem.Description);
                        ImGui.EndTooltip();
                    }
                }
            }
            ImGui.EndChild();
            ImGui.Separator();
        }
    }
}
