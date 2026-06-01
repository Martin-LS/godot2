"""
Scales hat pieces up 1.2x and re-exports hat GLBs with colours applied.
Run via: blender --background --python tools/resize_hats.py
"""
import bpy
import os

OUT = r"C:\work\my\github\godot1\assets\models\equipment"

def hex_to_linear(h):
    r = int(h[0:2], 16) / 255
    g = int(h[2:4], 16) / 255
    b = int(h[4:6], 16) / 255
    def to_lin(c):
        return c / 12.92 if c <= 0.04045 else ((c + 0.055) / 1.055) ** 2.4
    return (to_lin(r), to_lin(g), to_lin(b), 1.0)

def make_material(name, hex_color):
    mat = bpy.data.materials.get(name)
    if mat:
        bpy.data.materials.remove(mat)
    mat = bpy.data.materials.new(name=name)
    mat.use_nodes = True
    bsdf = mat.node_tree.nodes.get("Principled BSDF")
    if bsdf:
        bsdf.inputs["Base Color"].default_value = hex_to_linear(hex_color)
        bsdf.inputs["Roughness"].default_value = 0.85
        bsdf.inputs["Metallic"].default_value = 0.0
    return mat

def scale_and_apply(obj_names, factor):
    """Scale objects by factor from their individual origins, then apply scale."""
    bpy.ops.object.select_all(action='DESELECT')
    for name in obj_names:
        obj = bpy.context.scene.objects.get(name)
        if obj:
            obj.select_set(True)
            bpy.context.view_layer.objects.active = obj
    bpy.ops.transform.resize(value=(factor, factor, factor),
                              orient_type='GLOBAL',
                              center_override=(0, 0, 0),
                              use_proportional_edit=False)
    bpy.ops.object.transform_apply(scale=True)
    print(f"  Scaled {obj_names} by {factor}x and applied")

def assign_material(obj_name, mat):
    obj = bpy.context.scene.objects.get(obj_name)
    if obj is None:
        print(f"  WARNING: object not found: {obj_name}")
        return
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    print(f"  Material {mat.name} → {obj_name}")

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
    print(f"  Exported: {os.path.basename(filepath)}")

# ── Heavy ─────────────────────────────────────────────────────────────────────
print("\n=== hat_heavy ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "armour_heavy.blend"))

hat_names = ["HeavyHelm_Main", "HeavyHelm_Slit"]
scale_and_apply(hat_names, 1.2)

mat_base = make_material("Heavy_Base", "2A2E32")
mat_gold = make_material("Heavy_Gold", "8C680A")
assign_material("HeavyHelm_Main", mat_base)
assign_material("HeavyHelm_Slit", mat_gold)

export_selected(set(hat_names), os.path.join(OUT, "hat_heavy.glb"))

# ── Medium ────────────────────────────────────────────────────────────────────
print("\n=== hat_medium ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "armour_medium.blend"))

hat_names = ["MedHood_Main", "MedHood_Drape"]
scale_and_apply(hat_names, 1.2)

mat_base  = make_material("Med_Base",  "4A6050")
mat_drape = make_material("Med_Drape", "364838")
assign_material("MedHood_Main",  mat_base)
assign_material("MedHood_Drape", mat_drape)

export_selected(set(hat_names), os.path.join(OUT, "hat_medium.glb"))

# ── Light ─────────────────────────────────────────────────────────────────────
print("\n=== hat_light ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "armour_light.blend"))

hat_names = ["LightCap_Main", "LightCap_Brim"]
scale_and_apply(hat_names, 1.2)

mat_base = make_material("Light_Base", "7AA0B4")
mat_brim = make_material("Light_Brim", "4A6878")
assign_material("LightCap_Main", mat_base)
assign_material("LightCap_Brim", mat_brim)

export_selected(set(hat_names), os.path.join(OUT, "hat_light.glb"))

print("\nAll hat exports complete.")
