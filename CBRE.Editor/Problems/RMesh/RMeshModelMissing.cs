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
    public class RMeshModelMissing : IProblemCheck {
        public IEnumerable<Problem> Check(Map map, bool visibleOnly) {
            var models = map.WorldSpawn.Find(x => x is Entity && (!visibleOnly || (!x.IsVisgroupHidden && !x.IsCodeHidden)))
                .OfType<Entity>().Where(x => x.ClassName.ToLowerInvariant() == "model");
            foreach (var model in models) {
                string key = model.GameData.Behaviours.FirstOrDefault(p => p.Name == "model").Values.FirstOrDefault();
                string val = model.EntityData.GetPropertyValue(key);
                string path = Directories.GetModelPath(val);
                if (string.IsNullOrWhiteSpace(path)) {
                    continue;
                }
                if (!path.EndsWith(val)) {
                    yield return new Problem(GetType(), map, new [] { model }, Fix, $"Model has invalid path (\"{val}\")", "More than one model entity was found with an invalid model path. Fixing this problem will change the path of the model on the model entities.");
                }
            }
        }

        public IAction Fix(Problem problem) {
            var e = new EditEntityData();
            foreach (var obj in problem.Objects) {
                if (obj is Entity ent) {
                    var newData = ent.EntityData.Clone();
                    string key = ent.GameData.Behaviours.FirstOrDefault(p => p.Name == "model").Values.FirstOrDefault();
                    string val = ent.EntityData.GetPropertyValue(key);
                    string path = System.IO.Path.GetFileName(Directories.GetModelPath(val));
                    newData.SetPropertyValue(key, path);
                    e.AddEntity(ent, newData);
                }
            }
            return e;
        }
    }
}