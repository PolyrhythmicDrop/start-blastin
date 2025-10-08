class_name EditorReorderButton extends Button

signal dragged(from: int, to: int)

var idx: int

func _ready() -> void:
	icon = get_theme_icon(&"TripleBar", &"EditorIcons")
	mouse_default_cursor_shape = Control.CURSOR_DRAG

func _get_drag_data(_at_position: Vector2) -> Variant:
	return idx

func _can_drop_data(_at_position: Vector2, data: Variant) -> bool:
	return typeof(data) == TYPE_INT

func _drop_data(_at_position: Vector2, data: Variant) -> void:
	dragged.emit(data, idx)
