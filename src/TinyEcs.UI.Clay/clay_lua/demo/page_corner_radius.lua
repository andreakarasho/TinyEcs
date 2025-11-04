local clay = require("clay")
local window = require("window")

local radi = 0
local radii = 1
local last_time = 0
function layout(dt)
    clay.createTextElement("Corner Radius", { fontId=1, fontSize=16, textColor = { r=220, g=220, b=240, a=255 } })
    clay.createTextElement("Shader-driven radius rounding", {
        fontId=1, fontSize=16, textColor = COLOR_WHITE, textAlignment = clay.TEXT_ALIGN_CENTER
    })
    clay.createElement(clay.id("round_box"),{
        layout = {
            sizing = { width = clay.sizingFixed(50), height = clay.sizingFixed(50) },
            layoutDirection = clay.TOP_TO_BOTTOM,
        },
        backgroundColor = { r=255, g=0, b=0, a=255 },
		border = {
			color = COLOR_WHITE,
			width = { left=1, right=1, top=1, bottom=1 }
		},
		cornerRadius = { topLeft =8, topRight = 8, bottomLeft = 8, bottomRight = 8}
    })
    clay.createElement(clay.id("round_circle1"),{
        layout = {
            sizing = { width = clay.sizingFixed(50), height = clay.sizingFixed(50) },
            layoutDirection = clay.TOP_TO_BOTTOM,
            childAlignment ={ x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER }
        },
        backgroundColor = { r=255, g=0, b=0, a=255 },
		cornerRadius = { topLeft =radi, topRight = radi, bottomLeft = radi, bottomRight = radi}
    }, function()
		clay.createTextElement(tostring(radi), { fontId=1, fontSize=16, textColor = { r=220, g=220, b=240, a=255 }, textAlignment = clay.TEXT_ALIGN_CENTER })    
    end)
    clay.createElement(clay.id("round_circle2"),{
        layout = {
            sizing = { width = clay.sizingFixed(50), height = clay.sizingFixed(50) },
            layoutDirection = clay.TOP_TO_BOTTOM,
        },
        backgroundColor = { r=255, g=0, b=0, a=255 },
		border = {
			color = COLOR_WHITE,
			width = { left=1, right=1, top=1, bottom=1 }
		},
		cornerRadius = { topLeft =radi, topRight = radi, bottomLeft = radi, bottomRight = radi}
    })
    
    if window.time() > last_time + 0.05 then
		last_time = window.time()
		radi = radi + radii
		if radi > 55 then
			radii = -1
		elseif radi < 0 then
			radii = 1
		end
    end
end
