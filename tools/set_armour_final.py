"""
Applies 1.1x scale to ALL armour pieces, moves hat pieces down to sit on head,
applies colours, and re-exports all 6 GLBs.
Run via: blender --background --python tools/set_armour_final.py
"""
import bpy
import os

OUT       = r"C:\work\my\github\godot1\assets\models\equipment"
SCALE     = 1.1       # 10% bigger than original
HAT_Z_DN  = 0.0       # no translation needed — hat attaches at correct head bone height

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

def scale_from_own_origin(obj_names):
    """Scale each object 1.1x from its own origin (doesn't shift position)."""
    for name in obj_names:
        obj = bpy.context.scene.objects.get(name)
        if obj is None:
            print(f"  WARNING: {name} not found")
            continue
        obj.scale = (SCALE, SCALE, SCALE)
        bpy.ops.object.select_all(action='DESELECT')
        obj.select_set(True)
        bpy.context.view_layer.objects.active = obj
        bpy.ops.object.transform_apply(scale=True)
        print(f"  Scaled {name}  loc={obj.location.z:.3f}z")

def move_hat_down(obj_names):
    """Translate hat pieces down (Blender -Z) to seat on head bone."""
    for name in obj_names:
        obj = bpy.context.scene.objects.get(name)
        if obj is None:
            continue
        obj.location.z += HAT_Z_DN
        print(f"  Moved  {name}  → z={obj.location.z:.3f}")

def assign_material(obj_name, mat):
    obj = bpy.context.scene.objects.get(obj_name)
    if obj is None:
        print(f"  WARNING: material target not found: {obj_name}")
        return
    obj.data.materials.clear()
    obj.data.materials.append(mat)

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
print("\n=== armour_heavy.blend ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "armour_heavy.blend"))

hat_heavy  = ["HeavyHelm_Main"]
body_heavy = ["HeavyChest_Main", "HeavyChest_PaulL", "HeavyChest_PaulR"]

scale_from_own_origin(hat_heavy + body_heavy)
move_hat_down(hat_heavy)

m_hbase = make_material("Heavy_Base", "2A2E32")
m_hgold = make_material("Heavy_Gold", "8C680A")
m_htrim = make_material("Heavy_Trim", "4A4E52")
assign_material("HeavyHelm_Main",   m_hbase)
assign_material("HeavyHelm_Slit",   m_hgold)
assign_material("HeavyChest_Main",  m_hbase)
assign_material("HeavyChest_PaulL", m_htrim)
assign_material("HeavyChest_PaulR", m_htrim)

export_selected(set(hat_heavy),  os.path.join(OUT, "hat_heavy.glb"))
export_selected(set(body_heavy), os.path.join(OUT, "body_heavy.glb"))

# ── Medium ────────────────────────────────────────────────────────────────────
print("\n=== armour_medium.blend ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "armour_medium.blend"))

hat_medium  = ["MedHood_Main"]          # drape excluded — it hangs below hood bottom and projects as ears from perspective camera
body_medium = ["MedChest_Main", "MedChest_PaulL", "MedChest_PaulR"]

scale_from_own_origin(hat_medium + body_medium)
move_hat_down(hat_medium)

m_mbase  = make_material("Med_Base",  "4A6050")
m_mpaul  = make_material("Med_Paul",  "5A7852")
assign_material("MedHood_Main",    m_mbase)
assign_material("MedChest_Main",   m_mbase)
assign_material("MedChest_PaulL",  m_mpaul)
assign_material("MedChest_PaulR",  m_mpaul)

export_selected(set(hat_medium),  os.path.join(OUT, "hat_medium.glb"))
export_selected(set(body_medium), os.path.join(OUT, "body_medium.glb"))

# ── Light ─────────────────────────────────────────────────────────────────────
print("\n=== armour_light.blend ===")
bpy.ops.wm.open_mainfile(filepath=os.path.join(OUT, "armour_light.blend"))

hat_light  = ["LightCap_Main"]
body_light = ["LightChest_Main", "LightChest_TrimL", "LightChest_TrimR"]

scale_from_own_origin(hat_light + body_light)
move_hat_down(hat_light)

m_lbase = make_material("Light_Base", "7AA0B4")
m_lbrim = make_material("Light_Brim", "4A6878")
m_ltrim = make_material("Light_Trim", "B8D8E8")
assign_material("LightCap_Main",    m_lbase)
assign_material("LightCap_Brim",    m_lbrim)
assign_material("LightChest_Main",  m_lbase)
assign_material("LightChest_TrimL", m_ltrim)
assign_material("LightChest_TrimR", m_ltrim)

export_selected(set(hat_light),  os.path.join(OUT, "hat_light.glb"))
export_selected(set(body_light), os.path.join(OUT, "body_light.glb"))

print("\nAll armour exports complete.")
