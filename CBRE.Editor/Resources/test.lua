Name = "LuaBox"

function Create(box)
    local b = newbox(box.Start - box.Center, box.End - box.Center)
    local b2 = newbox(box.Start + box.Center, box.End + box.Center)
    for _,v in ipairs(b2) do
        table.insert(b, v)
    end
    return b
end

function newbox(Start, End)
    local topLeftBack = Vector3(Start[1], End[2], End[3])
    local topRightBack = End
    local topLeftFront = Vector3(Start[1], Start[2], End[3])
    local topRightFront = Vector3(End[1], Start[2], End[3])

    local bottomLeftBack = Vector3(Start[1], End[2], Start[3])
    local bottomRightBack = Vector3(End[1], End[2], Start[3])
    local bottomLeftFront = Start
    local bottomRightFront = Vector3(End[1], Start[2], Start[3])
    return
    {
        {topLeftFront, topRightFront, bottomRightFront, bottomLeftFront},
        {topRightBack, topLeftBack, bottomLeftBack, bottomRightBack},
        {topLeftBack, topLeftFront, bottomLeftFront, bottomLeftBack},
        {topRightFront, topRightBack, bottomRightBack, bottomRightFront},
        {topLeftBack, topRightBack, topRightFront, topLeftFront},
        {bottomLeftFront, bottomRightFront, bottomRightBack, bottomLeftBack}
    }
end