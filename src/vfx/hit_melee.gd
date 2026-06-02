extends Node3D

func _ready() -> void:
	get_tree().create_timer(0.6).timeout.connect(queue_free)
