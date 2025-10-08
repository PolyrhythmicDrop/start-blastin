class_name ResourceUidLoader extends Node

signal resource_loaded(uid: String, resource: Resource)
signal resource_errored(uid: String, error: ResourceLoader.ThreadLoadStatus)

var _loading_resources: Array[String] = []

func load_resource(uid: String, finished_signal := resource_loaded) -> void:
	var res := ResourceLoader.load_threaded_request(uid)
	if res == OK:
		if !_loading_resources.has(uid):
			_loading_resources.append(uid)
	else:
		resource_errored.emit(uid, ResourceLoader.THREAD_LOAD_FAILED)

func _process(_delta: float) -> void:
	var to_remove: Array[String] = []
	for uid in _loading_resources:
		var status := ResourceLoader.load_threaded_get_status(uid)
		if status == ResourceLoader.THREAD_LOAD_IN_PROGRESS:
			continue
		if status == ResourceLoader.THREAD_LOAD_FAILED || status == ResourceLoader.THREAD_LOAD_INVALID_RESOURCE:
			resource_errored.emit(uid, status)
		elif status == ResourceLoader.THREAD_LOAD_LOADED:
			resource_loaded.emit(uid, ResourceLoader.load_threaded_get(uid))
		to_remove.append(uid)
	for uid in to_remove:
		_loading_resources.erase(uid)
