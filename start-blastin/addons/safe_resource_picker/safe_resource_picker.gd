@tool
class_name SafeResourcePickerInspectorPlugin extends EditorInspectorPlugin

func _can_handle(_object: Object) -> bool:
	return true

func _parse_property(_object: Object, type: Variant.Type, name: String, hint_type: PropertyHint, hint_string: String, _usage_flags: int, _wide: bool) -> bool:
	if hint_type != SRP_HINT.RESOURCE_PATH:
		return false
	if type == TYPE_STRING || type == TYPE_STRING_NAME:
		var prop := SafeResourcePickerEditorProperty.new(hint_string)
		add_property_editor(name, prop)
		return true
	elif type == TYPE_ARRAY || type == TYPE_PACKED_STRING_ARRAY:
		var prop := SafeResourcePickerArrayEditorProperty.new(type, hint_string)
		add_property_editor(name, prop)
		return true
	else:
		printerr(tr(SRP_HINT.TYPE_NOT_SUPPORTED_ERROR_STRING) % [
			name, tr(type_string(type))
		])
		return false
