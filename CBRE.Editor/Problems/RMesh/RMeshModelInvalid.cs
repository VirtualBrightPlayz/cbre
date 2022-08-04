using CBRE.DataStructures.MapObjects;
using CBRE.Editor.Actions;
using CBRE.Editor.Actions.MapObjects.Entities;
using CBRE.Editor.Actions.MapObjects.Operations;
using CBRE.FileSystem;
using CBRE.Providers.Model;
using CBRE.Settings;
using System.Collections.Generic;
using System.Linq;

namespace CBRE.Editor.Problems.RMesh {
    public class RMeshModelInvalid : IProblemCheck {
        public IEnumerable<Problem> Check(Map map, bool visibleOnly) {
            var models = map.WorldSpawn.Find(x => x is Entity && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)))
                .OfType<Entity>().Where(x => x.ClassName.ToLowerInvariant() == "model");
            foreach (var model in models) {
                string key = model.GameData.Behaviours.FirstOrDefault(p => p.Name == "model").Values.FirstOrDefault();
                string val = model.EntityData.GetPropertyValue(key);
                string path = Directories.GetModelPath(val);
                if (string.IsNullOrWhiteSpace(path)) {
                    yield return new Problem(GetType(), map, new [] { model }, Fix, "Model has empty path", "More than one model entity was found with an empty model path. Fixing this problem will remove the model entities.");
                }
            }
        }

        public IAction Fix(Problem problem) {
            return new Delete(problem.Objects.Select(x => x.ID));
        }
    }
}