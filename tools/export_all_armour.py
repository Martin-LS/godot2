"""
Exports hat and body GLBs for all three tiers from armour_heavy.blend.
Medium and light are the same model as heavy, recoloured.
Run via: blender --background --python tools/export_all_armour.py
"""
import bpy, os

BLEND = r"C:\work\my\github\godot1\assets\models\equipment\armour_heavy.blend"
OUT   = r"C:\work\my\github\godot1\assets\models\equipment"
SCALE = 1.1

HAT_PIECES  = ["HeavyHelm_Main"]
BODY_PIECES = ["HeavyChest_Main", "HeavyChest_PaulL", "HeavyChest_PaulR"]

TIERS = [
    ("heavy",  "hat_heavy.glb",  "body_heavy.glb",  "2A2E32", "4A4E52"),
    ("medium", "hat_medium.glb", "body_medium.glb", "4A6050", "5A7852"),
    ("light",  "hat_light.glb",  "body_light.glb",  "7AA0B4", "B8D8E8"),
]

def hex_to_linear(h):
    def c(v): return v/12.92 if v<=0.04045 else ((v+0.055)/1.055)**2.4
    r,g,b = int(h[0:2],16)/255, int(h[2:4],16)/255, int(h[4:6],16)/255
    return (c(r), c(g), c(b), 1.0)

def set_colour(obj_name, hex_col):
    obj = bpy.context.scene.objects.get(obj_name)
    if not obj: return
    mat = bpy.data.materials.new(name=f"M_{obj_name}")
    mat.use_nodes = True
    mat.node_tree.nodes["Principled BSDF"].inputs["Base Color"].default_value = hex_to_linear(hex_col)
    mat.node_tree.nodes["Principled BSDF"].inputs["Roughness"].default_value = 0.85
    obj.data.materials.clear()
    obj.data.materials.append(mat)

def export(names, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.name in names:
            obj.select_set(True)
    bpy.ops.export_scene.gltf(filepath=filepath, use_selection=True,
                               export_format='GLB', export_yup=True)
    print(f"  {os.path.basename(filepath)}")

bpy.ops.wm.open_mainfile(filepath=BLEND)

# Scale all pieces 1.1x from own origin once
all_pieces = HAT_PIECES + BODY_PIECES
for name in all_pieces:
    obj = bpy.context.scene.objects.get(name)
    if not obj: continue
    obj.scale = (SCALE, SCALE, SCALE)
    bpy.ops.object.select_all(action='DESELECT')
    obj.select_set(True)
    bpy.context.view_layer.objects.active = obj
    bpy.ops.object.transform_apply(scale=True)

for tier, hat_file, body_file, main_col, trim_col in TIERS:
    print(f"\n=== {tier} ===")
    set_colour("HeavyHelm_Main",   main_col)
    set_colour("HeavyChest_Main",  main_col)
    set_colour("HeavyChest_PaulL", trim_col)
    set_colour("HeavyChest_PaulR", trim_col)
    export(set(HAT_PIECES),  os.path.join(OUT, hat_file))
    export(set(BODY_PIECES), os.path.join(OUT, body_file))

print("\nDone.")
