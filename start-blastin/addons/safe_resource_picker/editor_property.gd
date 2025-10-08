class_name SafeResourcePickerEditorProperty extends EditorProperty

var _picker: EditorResourcePicker

func _init(type: String) -> void:
	_picker = EditorResourcePicker.new()
	_picker.base_type = type
	_picker.resource_changed.connect(_on_resource_changed)
	add_child(_picker)

func _on_resource_changed(r: Resource) -> void:
	if r == null:
		emit_changed(get_edited_property(), null)
	elif r.resource_path == "":
		printerr(SRP_HINT.LOCAL_RESOURCE_ERROR_STRING)
		return
	else:
		emit_changed(
			get_edited_property(),
			ResourceUID.id_to_text(
				ResourceLoader.get_resource_uid(r.resource_path)
			)
		)

func _update_property() -> void:
	var current_path: String = get_edited_object()[get_edited_property()]
	if current_path:
		_picker.edited_resource = load(current_path)
	else:
		_picker.edited_resource = null
