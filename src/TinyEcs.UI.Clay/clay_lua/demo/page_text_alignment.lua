local clay = require("clay")

function layout(dt)
    local longText =
        "Hello world! This is a sentence that should wrap nicely after around 200 pixels. But does it? Is it working?"

    clay.createElement(
        clay.id("LeftAlignContainer"),
        {
            layout = {
                layoutDirection = clay.TOP_TO_BOTTOM,
                sizing = {width = clay.sizingFixed(200), height = clay.sizingFit()},
                childGap = 10,
                childAlignment = {x = clay.ALIGN_X_LEFT}
            }
        },
        function()
            clay.createTextElement(
                "LEFT ALIGNED:",
                {
                    fontSize = 14,
                    textColor = {r = 150, g = 150, b = 150, a = 255}
                }
            )

            clay.createElement(
                clay.id("LeftTextBox"),
                {
                    layout = {
                        sizing = {width = clay.sizingFixed(200), height = clay.sizingFit()},
                        padding = {left = 10, right = 10, top = 10, bottom = 10},
                        childAlignment = {x = clay.ALIGN_X_LEFT}
                    },
                    backgroundColor = {r = 50, g = 50, b = 50, a = 255},
                    border = {
                        color = {r = 100, g = 100, b = 100, a = 255},
                        width = {left = 1, right = 1, top = 1, bottom = 1}
                    }
                },
                function()
                    clay.createTextElement(
                        longText,
                        {
                            fontId = 1,
                            fontSize = 16,
                            textColor = COLOR_WHITE,
                            wrapMode = clay.TEXT_WRAP_WORDS,
                            textAlignment = clay.TEXT_ALIGN_LEFT,
                            sizing = {width = clay.sizingGrow(), height = clay.sizingFit()}
                        }
                    )
                end
            )
        end
    )

    clay.createElement(
        clay.id("CenterAlignContainer"),
        {
            layout = {
                layoutDirection = clay.TOP_TO_BOTTOM,
                sizing = {width = clay.sizingFixed(200), height = clay.sizingFit()},
                childGap = 10,
                childAlignment = {x = clay.ALIGN_X_CENTER}
            }
        },
        function()
            clay.createTextElement(
                "CENTER ALIGNED:",
                {
                    fontSize = 14,
                    textColor = {r = 150, g = 150, b = 150, a = 255}
                }
            )

            clay.createElement(
                clay.id("CenterTextBox"),
                {
                    layout = {
                        sizing = {width = clay.sizingFixed(200), height = clay.sizingFit()},
                        padding = {left = 10, right = 10, top = 10, bottom = 10},
                        childAlignment = {x = clay.ALIGN_X_CENTER}
                    },
                    backgroundColor = {r = 50, g = 50, b = 50, a = 255},
                    border = {
                        color = {r = 100, g = 100, b = 100, a = 255},
                        width = {left = 1, right = 1, top = 1, bottom = 1}
                    }
                },
                function()
                    clay.createTextElement(
                        longText,
                        {
                            fontId = 1,
                            fontSize = 16,
                            textColor = COLOR_WHITE,
                            wrapMode = clay.TEXT_WRAP_WORDS,
                            textAlignment = clay.TEXT_ALIGN_CENTER,
                            sizing = {width = clay.sizingGrow(), height = clay.sizingFit()}
                        }
                    )
                end
            )
        end
    )

    clay.createElement(
        clay.id("RightAlignContainer"),
        {
            layout = {
                layoutDirection = clay.TOP_TO_BOTTOM,
                sizing = {width = clay.sizingFixed(200), height = clay.sizingFit()},
                childGap = 10,
                childAlignment = {x = clay.ALIGN_X_RIGHT}
            }
        },
        function()
            clay.createTextElement(
                "RIGHT ALIGNED:",
                {
                    fontSize = 14,
                    textColor = {r = 150, g = 150, b = 150, a = 255}
                }
            )

            clay.createElement(
                clay.id("RightTextBox"),
                {
                    layout = {
                        sizing = {width = clay.sizingFixed(200), height = clay.sizingFit()},
                        padding = {left = 10, right = 10, top = 10, bottom = 10},
                        childAlignment = {x = clay.ALIGN_X_RIGHT}
                    },
                    backgroundColor = {r = 50, g = 50, b = 50, a = 255},
                    border = {
                        color = {r = 100, g = 100, b = 100, a = 255},
                        width = {left = 1, right = 1, top = 1, bottom = 1}
                    }
                },
                function()
                    clay.createTextElement(
                        longText,
                        {
                            fontId = 1,
                            fontSize = 16,
                            textColor = COLOR_WHITE,
                            wrapMode = clay.TEXT_WRAP_WORDS,
                            textAlignment = clay.TEXT_ALIGN_RIGHT,
                            sizing = {width = clay.sizingGrow(), height = clay.sizingFit()}
                        }
                    )
                end
            )
        end
    )
end
