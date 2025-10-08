class_name EditorArrayResourceRow extends HBoxContainer

signal dragged(from: int, to: int)
signal resource_changed(idx: int, resource: Resource)
signal removed(idx: int, row: EditorArrayResourceRow)

var idx := 0:
	set(value):
		idx = value
		if !is_node_ready():
			await ready
		_label.text = str(idx)
		_reorder_button.idx = idx

var _type: String
var _reorder_button: EditorReorderButton

@onready var _label := Label.new()
@onready var _editor := EditorResourcePicker.new()

func _init(index: int, t: String) -> void:
	idx = index
	_type = t

func _ready() -> void:
	_reorder_button = EditorReorderButton.new()
	_reorder_button.idx = idx
	_reorder_button.dragged.connect(_on_dragged)
	add_child(_reorder_button)

	_label.size_flags_horizontal = Control.SIZE_EXPAND
	add_child(_label)

	_editor.base_type = _type
	_editor.size_flags_horizontal = Control.SIZE_EXPAND_FILL
	_editor.edited_resource = null
	_editor.custom_minimum_size.x = 100.0
	_editor.resource_changed.connect(func(new_resource: Resource) -> void:
		resource_changed.emit(idx, new_resource)
	)
	add_child(_editor)

	var trash_button := Button.new()
	trash_button.icon = get_theme_icon(&"Remove", &"EditorIcons")
	trash_button.pressed.connect(_remove_row)
	add_child(trash_button)

func set_resource(r: Resource) -> void:
	_editor.edited_resource = r

func _on_dragged(from: int, to: int) -> void:
	dragged.emit(from, to)

func _remove_row() -> void:
	removed.emit(idx, self)
	queue_free()
