"""
Print vertex X positions of MedHood_Main to check for ear-like geometry.
Run via: blender --background --python tools/inspect_mesh_verts.py
"""
import bpy, os

bpy.ops.wm.open_mainfile(filepath=r"C:\work\my\github\godot1\assets\models\equipment\armour_medium.blend")

obj = bpy.context.scene.objects.get("MedHood_Main")
mesh = obj.data
xs = sorted(set(round(v.co.x, 3) for v in mesh.vertices))
ys = sorted(set(round(v.co.y, 3) for v in mesh.vertices))
zs = sorted(set(round(v.co.z, 3) for v in mesh.vertices))
print(f"MedHood_Main: {len(mesh.vertices)} verts, {len(mesh.polygons)} faces")
print(f"  Unique X: {xs}")
print(f"  Unique Y: {ys}")
print(f"  Unique Z: {zs}")
