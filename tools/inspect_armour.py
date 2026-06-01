"""
Prints bounding box min/max of all armour pieces.
Run via: blender --background --python tools/inspect_armour.py
"""
import bpy
import os

BLEND_DIR = r"C:\work\my\github\godot1\assets\models\equipment"

def inspect(blend_file):
    bpy.ops.wm.open_mainfile(filepath=os.path.join(BLEND_DIR, blend_file))
    print(f"\n=== {blend_file} ===")
    for obj in sorted(bpy.context.scene.objects, key=lambda o: o.name):
        if obj.type != 'MESH':
            continue
        bb = obj.bound_box
        xs = [v[0] for v in bb]; ys = [v[1] for v in bb]; zs = [v[2] for v in bb]
        print(f"  {obj.name:28s}  X[{min(xs):+.2f}..{max(xs):+.2f}]  Y[{min(ys):+.2f}..{max(ys):+.2f}]  Z[{min(zs):+.2f}..{max(zs):+.2f}]")

inspect("armour_heavy.blend")
inspect("armour_medium.blend")
inspect("armour_light.blend")
