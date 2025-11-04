local clay = require("clay")

function layout(dt)
    clay.createTextElement("Welcome to Clay Lua! Syntax and usage are nearly identical to the original Clay. However, there are a few differences:", { fontId=1, fontSize=16, textColor=COLOR_WHITE })
    clay.createTextElement("1. For 'clip' config, you must omit childOffset if you want to use Clay_GetScrollOffset; binding will handle it.", { fontId=1, fontSize=16, textColor=COLOR_WHITE })
    clay.createTextElement("\n2. Children must be wrapped in a function, this is because children need to be deffered till after element is open and config is applied.", { fontId=1, fontSize=16, textColor=COLOR_WHITE })
end
