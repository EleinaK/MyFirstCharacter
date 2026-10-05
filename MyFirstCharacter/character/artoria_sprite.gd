extends AnimatedSprite2D


func _ready():
	animation_finished.connect(_on_animation_finished)


func _on_animation_finished():
	if animation != "idle" and animation != "die":
		play("idle")
