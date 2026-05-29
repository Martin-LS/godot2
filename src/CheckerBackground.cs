using Godot;

namespace Godot1;

public partial class CheckerBackground : Node3D
{
    public override void _Ready()
    {
        var shader = new Shader();
        shader.Code = @"
shader_type spatial;
render_mode unshaded, cull_disabled;

varying vec3 world_pos;

void vertex() {
    world_pos = (MODEL_MATRIX * vec4(VERTEX, 1.0)).xyz;
}

void fragment() {
    vec2 tile = floor(world_pos.xz / 16.0);
    float c = mod(tile.x + tile.y, 2.0);
    ALBEDO = mix(vec3(0.18, 0.18, 0.18), vec3(0.28, 0.28, 0.28), c);
}
";
        var mat = new ShaderMaterial { Shader = shader };
        var plane = new PlaneMesh { Size = new Vector2(10000f, 10000f) };
        plane.Material = mat;

        AddChild(new MeshInstance3D { Mesh = plane });
    }
}
