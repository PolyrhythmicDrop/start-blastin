@tool
class_name EditorArrayPagination extends HBoxContainer

const _ITEMS_PER_PAGE := 10

signal page_changed(start_idx: int, is_last_page: bool, page_data: Array)

var data: Array:
	set(value):
		data = value
		_refresh_pagination()

var _current_page := 1
var _total_pages := 1

@onready var _first_page: Button = %FirstPage
@onready var _previous_page: Button = %PreviousPage
@onready var _page_number: LineEdit = %PageNumber
@onready var _max_page_label: Label = %MaxPageLabel
@onready var _next_page: Button = %NextPage
@onready var _last_page: Button = %LastPage

func _ready() -> void:
	_first_page.icon = get_theme_icon(&"PageFirst", &"EditorIcons")
	_first_page.pressed.connect(func() -> void:
		_current_page = 1
		_update_pagination()
	)
	_previous_page.icon = get_theme_icon(&"PagePrevious", &"EditorIcons")
	_previous_page.pressed.connect(func() -> void:
		_current_page -= 1
		_update_pagination()
	)
	_next_page.icon = get_theme_icon(&"PageNext", &"EditorIcons")
	_next_page.pressed.connect(func() -> void:
		_current_page += 1
		_update_pagination()
	)
	_last_page.icon = get_theme_icon(&"PageLast", &"EditorIcons")
	_last_page.pressed.connect(func() -> void:
		_current_page = _total_pages
		_update_pagination()
	)
	_refresh_pagination()

func _emit_page() -> void:
	var start_idx := (_current_page - 1) * _ITEMS_PER_PAGE
	var end_idx := start_idx + _ITEMS_PER_PAGE
	page_changed.emit(start_idx, _current_page == _total_pages, data.slice(start_idx, end_idx))

func _refresh_pagination() -> void:
	if !is_node_ready():
		return
	_total_pages = ceili(float(data.size()) / _ITEMS_PER_PAGE)
	_max_page_label.text = "/%d" % _total_pages
	if _current_page > _total_pages:
		_current_page = _total_pages
	if _current_page <= 0:
		_current_page = 1
	_update_pagination()

func _update_pagination() -> void:
	_page_number.text = str(_current_page)
	_max_page_label.text = "/%d" % _total_pages
	_first_page.disabled = _current_page == 1
	_previous_page.disabled = _current_page == 1
	_next_page.disabled = _current_page == _total_pages
	_last_page.disabled = _current_page == _total_pages
	visible = _total_pages > 1
	_emit_page()
