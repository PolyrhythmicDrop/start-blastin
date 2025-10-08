@tool
class_name SafeResourcePickerArrayEditorProperty extends EditorProperty

const _ARRAY_EDITOR_SCENE := preload("uid://cnhv2yvr3dka0")

var _desc_button := Button.new()
var _picker: ResourceArrayEditor
var _type_name := ""
var _unfolded := false

func _init(type: Variant.Type, hint_string: String) -> void:
	use_folding = true # TODO: what does this do??
	_type_name = hint_string
	_desc_button.pressed.connect(_on_desc_button_pressed)
	add_child(_desc_button)
	_picker = _ARRAY_EDITOR_SCENE.instantiate()
	_picker.visible = false
	_picker.initialize(hint_string)
	_picker.on_data_changed.connect(_on_data_changed)
	add_child(_picker)
	set_bottom_editor(_picker)

func _on_desc_button_pressed() -> void:
	_unfolded = !_unfolded
	_picker.visible = _unfolded

func _on_data_changed(r: Array) -> void:
	emit_changed(get_edited_property(), r)
	_update_count(r.size())

func _update_count(count: int) -> void:
	_desc_button.text = "Array[%s] (%s %d)" % [_type_name, tr("Size"), count]

func _update_property() -> void:
	var paths: Array = get_edited_object()[get_edited_property()]
	_picker.data = paths.duplicate()
	_update_count(paths.size())
