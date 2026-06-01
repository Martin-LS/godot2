"""
Sets armour tier materials and re-exports split hat/body GLBs.
Run via: blender --background --python tools/set_armour_colours.py
"""
import bpy
import os

OUT = r"C:\work\my\github\godot1\assets\models\equipment"
BLEND_DIR = OUT

def hex_to_linear(h):
    """Convert 6-char hex string to linear-space RGBA tuple for Blender."""
    r = int(h[0:2], 16) / 255
    g = int(h[2:4], 16) / 255
    b = int(h[4:6], 16) / 255
    # sRGB to linear
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

def assign_material(obj_name, mat):
    obj = bpy.context.scene.objects.get(obj_name)
    if obj is None:
        print(f"  WARNING: object not found: {obj_name}")
        return
    obj.data.materials.clear()
    obj.data.materials.append(mat)
    print(f"  Assigned {mat.name} → {obj_name}")

def export_selected(names, filepath):
    bpy.ops.object.select_all(action='DESELECT')
    found = []
    for obj in bpy.context.scene.objects:
        if obj.name in names:
            obj.select_set(True)
            found.append(obj.name)
    missing = names - set(found)
    if missing:
        print(f"  WARNING: objects not found for export: {missing}")
    bpy.ops.export_scene.gltf(
        filepath=filepath,
        use_selection=True,
        export_format='GLB',
        export_yup=True,
    )
    print(f"  Exported: {os.path.basename(filepath)}")

# ── Heavy Armour ─────────────────────────────────────────────────────────────
print("\n=== armour_heavy.blend ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(BLEND_DIR, "armour_heavy.blend"))

mat_heavy_base  = make_material("Heavy_Base",  "2A2E32")  # Iron Ore Shadow
mat_heavy_trim  = make_material("Heavy_Trim",  "4A4E52")  # Iron Ore Base (pauldrons)
mat_heavy_gold  = make_material("Heavy_Gold",  "8C680A")  # Dark Gold (visor slit)

assign_material("HeavyHelm_Main",  mat_heavy_base)
assign_material("HeavyHelm_Slit",  mat_heavy_gold)
assign_material("HeavyChest_Main", mat_heavy_base)
assign_material("HeavyChest_PaulL", mat_heavy_trim)
assign_material("HeavyChest_PaulR", mat_heavy_trim)

export_selected({"HeavyHelm_Main", "HeavyHelm_Slit"},
                os.path.join(OUT, "hat_heavy.glb"))
export_selected({"HeavyChest_Main", "HeavyChest_PaulL", "HeavyChest_PaulR"},
                os.path.join(OUT, "body_heavy.glb"))

# ── Medium Armour ─────────────────────────────────────────────────────────────
print("\n=== armour_medium.blend ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(BLEND_DIR, "armour_medium.blend"))

mat_med_base    = make_material("Med_Base",    "4A6050")  # Moss Stone Highlight
mat_med_drape   = make_material("Med_Drape",   "364838")  # Moss Stone Base (darker drape)
mat_med_paul    = make_material("Med_Paul",    "5A7852")  # Moss Accent (pauldrons)

assign_material("MedHood_Main",    mat_med_base)
assign_material("MedHood_Drape",   mat_med_drape)
assign_material("MedChest_Main",   mat_med_base)
assign_material("MedChest_PaulL",  mat_med_paul)
assign_material("MedChest_PaulR",  mat_med_paul)

export_selected({"MedHood_Main", "MedHood_Drape"},
                os.path.join(OUT, "hat_medium.glb"))
export_selected({"MedChest_Main", "MedChest_PaulL", "MedChest_PaulR"},
                os.path.join(OUT, "body_medium.glb"))

# ── Light Armour ──────────────────────────────────────────────────────────────
print("\n=== armour_light.blend ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(BLEND_DIR, "armour_light.blend"))

mat_light_base  = make_material("Light_Base",  "7AA0B4")  # Ice Highlight
mat_light_brim  = make_material("Light_Brim",  "4A6878")  # Ice Base (darker brim)
mat_light_trim  = make_material("Light_Trim",  "B8D8E8")  # Ice Shimmer (trim strips)

assign_material("LightCap_Main",   mat_light_base)
assign_material("LightCap_Brim",   mat_light_brim)
assign_material("LightChest_Main", mat_light_base)
assign_material("LightChest_TrimL", mat_light_trim)
assign_material("LightChest_TrimR", mat_light_trim)

export_selected({"LightCap_Main", "LightCap_Brim"},
                os.path.join(OUT, "hat_light.glb"))
export_selected({"LightChest_Main", "LightChest_TrimL", "LightChest_TrimR"},
                os.path.join(OUT, "body_light.glb"))

print("\nAll armour exports complete.")
