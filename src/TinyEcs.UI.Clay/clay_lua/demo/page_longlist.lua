local clay = require("clay")

function layout(dt)
    for i=1,200 do
        clay.createTextElement(("Item #%d"):format(i), {
            fontId=1, fontSize=16,
            textColor = COLOR_WHITE
        })
    end
end
