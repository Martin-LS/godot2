"""
Exports split hat/body GLBs from each armour blend file.
Run via: blender --background --python export_armour_split.py
"""
import bpy
import os

OUT = r"C:\work\my\github\godot1\assets\models\equipment"

SPLITS = [
    ("armour_medium.blend",
     {"MedHood_Main", "MedHood_Drape"},
     {"MedChest_Main", "MedChest_PaulL", "MedChest_PaulR"},
     "hat_medium.glb", "body_medium.glb"),
    ("armour_light.blend",
     {"LightCap_Main", "LightCap_Brim"},
     {"LightChest_Main", "LightChest_TrimL", "LightChest_TrimR"},
     "hat_light.glb", "body_light.glb"),
]

def export_selected(names, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    for obj in bpy.context.scene.objects:
        if obj.name in names:
            obj.select_set(True)
    bpy.ops.export_scene.gltf(
        filepath=filepath,
        use_selection=True,
        export_format='GLB',
        export_yup=True,
    )
    print(f"Exported: {os.path.basename(filepath)}")

for blend_file, hat_names, body_names, hat_out, body_out in SPLITS:
    bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, blend_file))
    export_selected(hat_names,  os.path.join(OUT, hat_out))
    export_selected(body_names, os.path.join(OUT, body_out))
    print(f"Done: {blend_file}")

print("All exports complete.")
