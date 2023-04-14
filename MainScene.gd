extends Control

var tableQueue = []

func _ready() -> void:
	randomize()
	TttAi.SetTable.connect(queueTable)

func queueTable(table, win, winline):
	tableQueue.append([table, win, winline])

func _process(delta: float) -> void:
	if tableQueue.size() > 0:
		var newTable = tableQueue[0]
		setTable(newTable[0], newTable[1], newTable[2])
		tableQueue.remove_at(tableQueue.find(newTable))

func setTable(table:Dictionary, win:bool, winline:String = ""):
	for line in $WinLines.get_children():
		line.visible = false
	for slot in table.keys():
		var value:String = table[slot]
		if not (value is String):
			print_rich("[color=red]Table Error: Invalid Values[/color]")
			return
		value = value.to_lower()
		var setImg = load("res://"+value+"_"+str(randi() % 4)+".png")
		var setTo:TextureRect = get_node_or_null(slot)
		if setTo == null:
			print_rich("[color=red]Table Error: Invalid Slots[/color]")
			return
		if setImg == null:
			print_rich("[color=red]Table Error: Invalid Texture[/color]")
			return
		if not (setTo is TextureRect):
			print_rich("[color=red]Table Error: Node Injection Invalidated[/color]")
			return
		setTo.texture = setImg
		if win:
			var showLine = get_node_or_null("WinLines/Line"+winline)
			if showLine == null:
				print_rich("[color=red]Table Error: Win Condition Invalid[/color]")
				return
			showLine.visible = true
