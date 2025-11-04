local clay = require("clay")
local glfw = require("ffi.ffi_glfw")

local draggingItem = nil
local dragStartX, dragStartY = 0, 0
local dragSource = nil        -- "inventory" or "slot:<key>"
local dragStarted = false
local dragThreshold = 5
local lastMouseDown = false

local inventoryItems = {
    "10mm Pistol",
    "Laser Rifle",
    "Combat Armor",
    "Leather Jacket",
    "Stimpack",
    "Bottlecap",
    "Super Sledge",
    "Power Armor",
    "Plasma Grenade",
}

local equipped = {
    head = nil,
    chest = nil,
    legs = nil,
    leftHand = nil,
    rightHand = nil,
}

-------------------------------------------------
-- Inventory item component
-------------------------------------------------
local function InventoryItem(i, label, mouse_down)
    local id = clay.id("InventoryItem", i)
    local hovered = clay.pointerOver(id[1])
    local bg = hovered and {r=90,g=90,b=130,a=255} or {r=60,g=60,b=90,a=255}

    clay.createElement(id, {
        layout = {
            sizing = { width = clay.sizingGrow(), height = clay.sizingFixed(36) },
            padding = clay.paddingAll(8),
            childAlignment = { y = clay.ALIGN_Y_CENTER }
        },
        backgroundColor = bg,
        border = { color = {r=255,g=255,b=255,a=20}, width = { left=0,right=0,top=1,bottom=1 } }
    }, function()
        clay.createTextElement(label, { fontId=1, fontSize=16, textColor=COLOR_WHITE })

        if hovered and mouse_down and not draggingItem then
            glfw.GetCursorPos(device.win, mouse_x, mouse_y)
            dragStartX, dragStartY = mouse_x[0], mouse_y[0]
            draggingItem = label
            dragSource = "inventory"
            dragStarted = false
        end
    end)
end

-------------------------------------------------
-- Equipment slot component
-------------------------------------------------
local function EquipmentSlot(i, name, key, mouse_down)
    local id = clay.id("EquipSlot", i)
    local item = equipped[key]
    local hovered = clay.pointerOver(id[1])
    local bg = hovered and {r=80,g=80,b=110,a=255} or {r=50,g=50,b=80,a=255}

    clay.createElement(id, {
        layout = {
            sizing = { width = clay.sizingFixed(140), height = clay.sizingFixed(40) },
            childAlignment = { x = clay.ALIGN_X_CENTER, y = clay.ALIGN_Y_CENTER }
        },
        backgroundColor = bg,
        border = { color = {r=200,g=200,b=255,a=60}, width = { left=1,right=1,top=1,bottom=1 } }
    }, function()
        clay.createTextElement(name .. ": " .. (item or "[empty]"), {
            fontId=1, fontSize=14, textColor=COLOR_WHITE
        })

        -- start drag from slot
        if hovered and mouse_down and not draggingItem and item then
            glfw.GetCursorPos(device.win, mouse_x, mouse_y)
            dragStartX, dragStartY = mouse_x[0], mouse_y[0]
            draggingItem = item
            dragSource = "slot:" .. key
            dragStarted = false
        end

        -- drop from inventory onto slot
        if hovered and not mouse_down and draggingItem and dragStarted then
            -- clear item from inventory if coming from inventory
            if dragSource == "inventory" then
                for j = #inventoryItems, 1, -1 do
                    if inventoryItems[j] == draggingItem then
                        table.remove(inventoryItems, j)
                        break
                    end
                end
            elseif dragSource:match("^slot:") then
                local oldKey = dragSource:match("slot:(.+)")
                equipped[oldKey] = nil
            end

            equipped[key] = draggingItem
            draggingItem = nil
            dragStarted = false
        end
    end)
end

function layout(dt)
    glfw.GetCursorPos(device.win, mouse_x, mouse_y)
    local mx, my = mouse_x[0], mouse_y[0]
    local mouse_down = glfw.GetMouseButton(device.win, glfw.MOUSE_BUTTON_LEFT) == glfw.PRESS

    -- detect drag start
    if draggingItem and not dragStarted and mouse_down then
        local dx = mx - dragStartX
        local dy = my - dragStartY
        if math.sqrt(dx*dx + dy*dy) > dragThreshold then
            dragStarted = true
        end
    end

    clay.createElement(clay.id("InventoryRoot"), {
        layout = {
            layoutDirection = clay.LEFT_TO_RIGHT,
            sizing = { width = clay.sizingGrow(), height = clay.sizingGrow() },
            childGap = 12,
        }
    }, function()

        -- Left inventory
        clay.createElement(clay.id("InventoryScroll"), {
            layout = {
                sizing = { width = clay.sizingFixed(240), height = clay.sizingGrow() },
                layoutDirection = clay.TOP_TO_BOTTOM,
                padding = clay.paddingAll(8),
                childGap = 8
            },
            clip = { vertical = true, horizontal = false, childOffset = clay.getScrollOffset() },
            backgroundColor = {r=40,g=60,b=90,a=255},
            border = { color = COLOR_WHITE_24, width = { left=0,right=1,top=0,bottom=0 } }
        }, function()
            for i, item in ipairs(inventoryItems) do
                InventoryItem(i, item, mouse_down)
            end

            -- drop from slot back to inventory
            if clay.hovered() and not mouse_down and draggingItem and dragStarted then
                if dragSource and dragSource:match("^slot:") then
                    local oldKey = dragSource:match("slot:(.+)")
                    equipped[oldKey] = nil
                    table.insert(inventoryItems, draggingItem)
                end
                draggingItem = nil
                dragStarted = false
            end
        end)

        -- Right paper doll
        clay.createElement(clay.id("EquipPanel"), {
            layout = {
                layoutDirection = clay.TOP_TO_BOTTOM,
                sizing = { width = clay.sizingGrow(), height = clay.sizingGrow() },
                childGap = 12,
                padding = clay.paddingAll(16)
            },
            backgroundColor = {r=30,g=50,b=80,a=255}
        }, function()
            clay.createTextElement("Paper Doll (mock)", { fontId=1, fontSize=18, textColor=COLOR_WHITE })
            EquipmentSlot(1, "Head", "head", mouse_down)
            EquipmentSlot(2, "Chest", "chest", mouse_down)
            EquipmentSlot(3, "Legs", "legs", mouse_down)
            EquipmentSlot(4, "Left Hand", "leftHand", mouse_down)
            EquipmentSlot(5, "Right Hand", "rightHand", mouse_down)
        end)
    end)

    -- floating ghost follows mouse while dragging
    if draggingItem and dragStarted then
        clay.createElement(clay.id("FloatingItem"), {
            floating = {
                attachTo = clay.ATTACH_TO_ROOT,
                attachPoints = {
                    element = clay.ATTACH_POINT_LEFT_TOP,
                    parent  = clay.ATTACH_POINT_LEFT_TOP,
                },
                offset = { x = mx + 8, y = my + 8 },
                zIndex = 1000,
                pointerCaptureMode = clay.POINTER_CAPTURE_MODE_PASSTHROUGH,
                clipTo = clay.CLIP_TO_NONE,
            },
            layout = {
                sizing = { width = clay.sizingFixed(160), height = clay.sizingFixed(32) },
                childAlignment = { y = clay.ALIGN_Y_CENTER }
            },
            backgroundColor = { r=255, g=255, b=255, a=32 },
            border = { color = COLOR_WHITE_45, width = { left=1,right=1,top=1,bottom=1 } }
        }, function()
            clay.createTextElement(draggingItem, { fontId=1, fontSize=14, textColor=COLOR_WHITE })
        end)
    end

    -- mouse release cleanup
    if not mouse_down and lastMouseDown and draggingItem then
        if dragStarted then
            -- release in void → revert
            if dragSource == "inventory" then
                -- back to inventory (already there)
            elseif dragSource and dragSource:match("^slot:") then
                -- revert slot item
                local oldKey = dragSource:match("slot:(.+)")
                equipped[oldKey] = draggingItem
            end
        end
        draggingItem = nil
        dragStarted = false
        dragSource = nil
    end

    lastMouseDown = mouse_down
end
