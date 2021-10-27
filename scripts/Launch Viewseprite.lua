local ws
local dlg = Dialog()
local spr = app.activeSprite

local buf = Image(spr.width, spr.height, ColorMode.RGB)

local PING_ID = string.byte("P")

local IMAGE_ID = string.byte("I")
local META_ID = string.byte("M")

local ADDCHANNEL_ID = string.byte("A")
local REMOVECHANNEL_ID = string.byte("R")
local LAYERMASK_ID = string.byte("L")

local layerMask
local onSpriteChange
local onSiteChange
local channels = {}

local function finish()
    if ws ~= nil then
        ws:close()
    end
    if dlg ~= nil then
        dlg:close()
    end
    spr.events:off(onSpriteChange)
    app.events:off(onSiteChange)
    spr = nil
    dlg = nil
end

local function sendMetadata()
    local mes = string.pack("<LLL", META_ID, #spr.layers, #channels)
    for i, layer in ipairs(spr.layers) do
        mes = mes .. layer.name .. "\n"
    end

    for i, channel in ipairs(channels) do
        -- fix layerMask
        local layers = channel["layers"]
        local newMask = 0
        for j, layer in ipairs(spr.layers) do
            for k, maskedLayer in ipairs(layers) do
                if maskedLayer ~= nul and layer == maskedLayer then
                    newMask = newMask | (1 << (j - 1))
                    break
                end
            end
        end

        channel["layerMask"] = newMask

        mes = mes .. string.format("%d", channel["layerMask"]) .. "\n"
    end
    ws:sendBinary(mes);
end

local function sendImage()
    if buf.width ~= spr.width or buf.height ~= spr.height then
        buf:resize(spr.width, spr.height)
    end

    for i, channel in ipairs(channels) do
        buf:clear()

        local layerMask = channel["layerMask"]
        if layerMask == 0 then
            buf:drawSprite(spr, app.activeFrame.frameNumber)
        else
            for j, layer in ipairs(spr.layers) do
                if (layerMask & (1 << j - 1)) ~= 0 then
                    local cel = layer:cel(app.activeFrame.frameNumber)
                    if cel ~= nil then
                        -- TODO: Alpha blending. Currently using multiple specified layers is not supported
                        buf:drawImage(cel.image, cel.position)
                    end
                end
            end
        end

        ws:sendBinary(string.pack("<LLLL", IMAGE_ID, i - 1, buf.width, buf.height), buf.bytes)
    end
end

local function sendPing()
    ws:sendBinary(string.pack("<L", PING_ID))
end

onSpriteChange = function()
    sendMetadata()
    sendImage()
end

local frame = -1
onSiteChange = function()
    if app.activeSprite ~= spr then
        for _, s in ipairs(app.sprites) do
            if s == spr then
                return
            end
        end

        finish()
    else
        if app.activeFrame.frameNumber ~= frame then
            frame = app.activeFrame.frameNumber
            sendImage()
        end
    end
end

local function receive(mt, data)
    if mt == WebSocketMessageType.BINARY then
        local id = string.unpack("<L", data, 0 + 1)
        if id == ADDCHANNEL_ID then
            -- add channel
            -- print("Add channel")
            table.insert(channels, {
                layerMask = 0,
                layers = {}
            })
        elseif id == REMOVECHANNEL_ID then
            -- remove channel
            local channelId = string.unpack("<L", data, 4 + 1)
            -- print("Remove channel " .. channelId .. " channels: " .. #channels)
            table.remove(channels, channelId + 1)
        elseif id == LAYERMASK_ID then
            -- set layermask
            local channelId = string.unpack("<L", data, 4 + 1)
            local layerMask = string.unpack("<L", data, 8 + 1)
            -- print("Channel: " .. channelId .. " LayerMask: " .. string.format("%d", layerMask))
            local channel = channels[channelId + 1]
            channel["layerMask"] = layerMask
            local layers = {}
            for i, layer in ipairs(spr.layers) do
                if (layerMask & (1 << (i - 1))) ~= 0 then
                    table.insert(layers, layer)
                end
            end
            channel["layers"] = layers
        end

    elseif mt == WebSocketMessageType.OPEN then
        dlg:modify{
            id = "viewseprite",
            text = "Active"
        }
        spr.events:on('change', onSpriteChange)
        app.events:on('sitechange', onSiteChange)
        onSpriteChange()
        sendPing()

    elseif mt == WebSocketMessageType.CLOSE and dlg ~= nil then
        dlg:modify{
            id = "viewseprite",
            text = "No connection"
        }
        spr.events:off(onSpriteChange)
        app.events:off(onSiteChange)
    end
end

ws = WebSocket {
    url = "http://localhost:6435/",
    onreceive = receive,
    deflate = false
}

dlg:label{
    id = "viewseprite",
    text = "Connecting..."
}
dlg:button{
    text = "Cancel",
    onclick = finish
}

ws:connect()
dlg:show{
    wait = false
}
