local clay = require("clay")

local some_text = "This is a text edit"
local val = 50
local gain = 0.5

function layout(dt)
	clay.checkbox(clay.id("demo_checkbox1"), "Do you exist?", false, nil)
	
	clay.createTextElement("What is your favorite fruit?", { fontId=1, fontSize=22, textColor={r=230,g=230,b=240,a=255} })
	clay.createElement(clay.id("RadioGroup1"), {
        layout = {
            layoutDirection = clay.LEFT_TO_RIGHT,
            sizing = { width = clay.sizingFit(), height = clay.sizingFit() },
            childGap = 10,
            childAlignment = { x = clay.ALIGN_X_LEFT }
        }
	}, function()
		-- exactly one of these can be selected
		local v1 = clay.radio(clay.id("fruit-apple"),  "Apple",  "RadioGroup1", 1, true,  nil)  -- default selected first run
		local v2 = clay.radio(clay.id("fruit-banana"), "Banana", "RadioGroup1", 2, false, nil)
		local v3 = clay.radio(clay.id("fruit-cherry"), "Cherry", "RadioGroup1", 3, false, nil)
	end)
	
	clay.createTextElement("What is your favorite game?", { fontId=1, fontSize=22, textColor={r=230,g=230,b=240,a=255} })
	clay.createElement(clay.id("RadioGroup2"), {
        layout = {
            layoutDirection = clay.LEFT_TO_RIGHT,
            sizing = { width = clay.sizingFit(), height = clay.sizingFit() },
            childGap = 10,
            childAlignment = { x = clay.ALIGN_X_LEFT }
        }
	}, function()
		-- exactly one of these can be selected
		local cfg = { allowNone=true }
		local v1 = clay.radio(clay.id("game-fallout"),  "Fallout",  "RadioGroup2", 1, false,  cfg)  -- default selected first run
		local v2 = clay.radio(clay.id("game-fallout2"), "Fallout 2", "RadioGroup2", 2, false, cfg)
		local v3 = clay.radio(clay.id("game-fallout3"), "Fallout 3", "RadioGroup2", 3, false, cfg)
	end)
	
	local edited = false
	some_text, edited = clay.edit(clay.id("editbox1"), some_text, {filter="ascii", maxChars=50})

	clay.createElement(clay.id("Group1"), {
        layout = {
            layoutDirection = clay.LEFT_TO_RIGHT,
            sizing = { width = clay.sizingFit(), height = clay.sizingFit() },
            childGap = 10,
            childAlignment = { x = clay.ALIGN_X_LEFT }
        }
	}, function()
		local changed = false
		-- 0..100 int slider with ticks
		val, changed = clay.slider(clay.id("vol"), val, {
		  min = 0, max = 100, step = 5, tickCount = 6, width = 220, height = 28, pad = 0
		})
		
		clay.createTextElement(tostring(val), { fontId=1, fontSize=16, textColor={r=230,g=230,b=240,a=255} })
	end)
	
	  -- float property with custom formatter
	  gain, _ = clay.property(clay.id("prop-gain"), "Gain", gain, {
		min = 0.0, max = 1.0, step = 0.1, width = 180, height = 32, buttonW = 28, cornerRadius = 10,
		format = function(v) return string.format("Gain: %.3f", v) end
	  })
end
