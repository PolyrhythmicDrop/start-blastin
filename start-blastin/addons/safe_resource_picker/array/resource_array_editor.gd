@tool
class_name ResourceArrayEditor extends PanelContainer

signal on_data_changed(data: Array)

var data: Array = []:
	set(value):
		data = value
		if !is_node_ready():
			await ready
		_pagination.data = data
		_array_size.set_value_no_signal(data.size())

var _type := "Resource"
var _current_page: Array

@onready var _records: VBoxContainer = %Records
@onready var _add_element_button: Button = %AddElementButton
@onready var _array_size: SpinBox = %ArraySize
@onready var _pagination: EditorArrayPagination = %EditorArrayPagination
@onready var _uid_loader := ResourceUidLoader.new()

func _ready() -> void:
	add_child(_uid_loader)
	_add_element_button.icon = get_theme_icon(&"Add", &"EditorIcons")
	_add_element_button.pressed.connect(_add_row)
	_array_size.value_changed.connect(_on_array_size_manually_changed)
	_uid_loader.resource_loaded.connect(_on_resource_loaded)
	_pagination.page_changed.connect(_on_page_changed)

func initialize(type: String) -> void:
	_type = type

func _on_page_changed(start_idx: int, is_last_page: bool, current_page: Array) -> void:
	_current_page = current_page
	for r in _records.get_children():
		_records.remove_child(r)
		r.queue_free()
	for uid: String in current_page:
		_create_initial_row(start_idx)
		_uid_loader.load_resource(uid)
	_add_element_button.visible = is_last_page

func _on_resource_loaded(uid: String, resource: Resource) -> void:
	var idx := _current_page.find(uid)
	var row: EditorArrayResourceRow = _records.get_child(idx)
	row.set_resource(resource)

func _on_array_size_manually_changed(new_amount: float) -> void:
	var curr_count := data.size()
	if curr_count < new_amount:
		for i in (new_amount - curr_count):
			data.append("")
	elif curr_count > new_amount:
		for i in (curr_count - new_amount):
			data.pop_back()
	on_data_changed.emit(data)
	_array_size.set_value_no_signal(curr_count)

func _add_row() -> void:
	var row := EditorArrayResourceRow.new(data.size(), _type)
	_bind_row_and_add(row)
	data.append("")
	_on_row_changed(row.idx, null)

func _create_initial_row(start_idx: int) -> void:
	var row := EditorArrayResourceRow.new(start_idx + _records.get_child_count(), _type)
	_bind_row_and_add(row)

func _bind_row_and_add(row: EditorArrayResourceRow) -> void:
	row.resource_changed.connect(_on_row_changed)
	row.removed.connect(_on_row_removed)
	row.dragged.connect(_on_row_reordered)
	_records.add_child(row)

func _on_row_reordered(from: int, to: int) -> void:
	var temp: String = data[from]
	data[from] = data[to]
	data[to] = temp
	on_data_changed.emit(data)

func _on_row_changed(idx: int, resource: Resource) -> void:
	if resource == null:
		data[idx] = ""
	elif resource.resource_path == "":
		printerr(SRP_HINT.LOCAL_RESOURCE_ERROR_STRING)
		return
	else:
		data[idx] = ResourceUID.id_to_text(
			ResourceLoader.get_resource_uid(resource.resource_path)
		)
	on_data_changed.emit(data)
	_array_size.set_value_no_signal(data.size())

func _on_row_removed(idx: int, row: EditorArrayResourceRow) -> void:
	data.remove_at(idx)
	_records.remove_child(row)
	_redraw_row_counts()

func _redraw_row_counts() -> void:
	var idx := 0
	for row: EditorArrayResourceRow in _records.get_children():
		row.idx = idx
		idx += 1
