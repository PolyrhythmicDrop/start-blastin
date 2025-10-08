@tool
extends EditorPlugin

var _plugin: SafeResourcePickerInspectorPlugin

func _enter_tree() -> void:
	_plugin = SafeResourcePickerInspectorPlugin.new()
	add_inspector_plugin(_plugin)

func _exit_tree() -> void:
	remove_inspector_plugin(_plugin)
