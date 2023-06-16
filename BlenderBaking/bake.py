import bpy, thelightmapper, sys, os

argv = sys.argv
argv = argv[argv.index("--") + 1:]

print("import " + argv[0])
print(bpy.context.scene.TLM_EngineProperties.tlm_lightmap_savedir)
print(argv[1])
bpy.ops.import_scene.gltf(filepath=argv[0])

bpy.context.view_layer.update()

for obj in bpy.data.objects:
   if obj.type == "MESH":
      obj.TLM_ObjectProperties.tlm_mesh_lightmap_use = True
      obj.TLM_ObjectProperties.tlm_use_default_channel = False
      obj.TLM_ObjectProperties.tlm_uv_channel = "UVMap.001"
      obj.TLM_ObjectProperties.tlm_mesh_lightmap_resolution = argv[2]

thelightmapper.addon.utility.build.prepare_build(0, True)
print("done " + argv[1])

dirpath = os.path.join(os.path.dirname(bpy.data.filepath), bpy.context.scene.TLM_EngineProperties.tlm_lightmap_savedir)
f = open(argv[1], "w")
f.write(dirpath)
f.close()