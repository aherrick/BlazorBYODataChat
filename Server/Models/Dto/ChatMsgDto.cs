﻿using Server.Models;

namespace Server.Models.Dto;

public class ChatMsgDto()
{
    public string Message { get; set; }

    public Enum Author { get; set; }

    public List<ChatMsgSource> Sources { get; set; } = [];
}

public class ChatMsgSource
{
    public string Title { get; set; }

    public string Url { get; set; }
}