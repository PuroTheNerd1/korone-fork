using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc;
using Roblox.Exceptions;
using Roblox.Services.App.FeatureFlags;
using Roblox.Website.WebsiteModels.Users;

namespace Roblox.Website.Controllers;

[ApiController]
[Route("/apisite/privatemessages/v1")]
public class PrivateMessagesControllerV1 : ControllerBase
{
    private void FeatureCheck()
    {
        FeatureFlags.FeatureCheck(FeatureFlag.PrivateMessagesEnabled);
    }

    public static List<dynamic> GlobalMessages = new List<dynamic>()
    {
        new
        {
            id = 1,
            sender = new
            {
                id = 443,
                name = "supra",
                displayName = "supra",
            },
            subject = "Welcome to Korone!",
            body = `Hello, and welcome to Korone! My name is Supra. I started Korone so you and your friends can experience just about anything you could possibly imagine across immersive user-generated 3D worlds, whether you’re sailing across the open seas, exploring the farthest reaches of outer space, or hanging out with your friends in a virtual club. I’m here to make sure your experience stays fun, safe, and creative. 

Before you jump in and start playing, here are a few tips. You can [customize your avatar](/My/Avatar) using our massive catalog of clothing and accessory options. Once you’re set, pick something to play by checking out [our most popular games](/games)! Did you know you can also play games with your friends across different devices at the same time, even if you’re on a computer and they’re using their phone or VR headset? Finding friends on Korone is easy! Join or create a group, or invite others to play a game with you by sending them a chat message.

That’s all there is to it! Now, get ready for an epic adventure. We hope you have a blast!

Sincerely,

Supra, CEO of Korone`,
            created = "2021-01-13T12:00:00.42Z",
            updated = "2021-01-13T12:00:00.42Z",
        }
    };
    [HttpGet("announcements/metadata")]
    public dynamic GetAnnouncementsMetadata()
    {
        return new
        {
            numOfAnnouncements = GlobalMessages.Count,
        };
    }

    [HttpGet("announcements")]
    public async Task<dynamic> GetAnnouncements()
    {
        var userInfo = await services.users.GetUserById(safeUserSession.userId);
        var collection = GlobalMessages.Select(m => m.subject == "Welcome to Korone!" ? new
        {
            m.id,
            m.sender,
            m.subject,
            m.body,
            created = userInfo.created,
            updated = userInfo.created,
        } : (dynamic)m).ToList();
        return new
        {
            collection,
            totalCollectionSize = collection.Count,
        };
    }

    [HttpGet("messages/unread/count")]
    public async Task<dynamic> GetUnreadMessagesCount()
    {
        FeatureCheck();
        var res = await services.privateMessages.CountUnreadMessages(safeUserSession.userId);
        return new
        {
            count = res,
        };
    }

    [HttpPost("messages/send")]
    public async Task<dynamic> SendMessage([Required,FromBody] SendMessageRequest request)
    {
        FeatureCheck();
        if (await services.privateMessages.IsFloodChecked(safeUserSession.userId))
        {
            return new
            {
                success = false,
                shortMessage = "FloodCheck",
                message = "Too many messages. Try again in a few minutes.",
            };
        }

        try
        {
            await services.privateMessages.CreateMessage(request.recipientid, safeUserSession.userId, request.subject,
                request.body, request.replyMessageId, request.includePreviousMessage);
        }
        catch (ArgumentException e)
        {
            return new
            {
                success = false,
                shortMessage = "GeneralError",
                message = e.Message,
            };
        }
        return new
        {
            success = true,
            shortMessage = "Success",
            message = "Successfully sent message.",
        };
    }

    [HttpPost("messages/mark-read")]
    public async Task MarkMessagesAsRead([Required, FromBody] MultiUpdateMessagesRequest request)
    {
        FeatureCheck();
        foreach (var item in request.messageIds)
        {
            var info = await services.privateMessages.GetMessageById(item);
            if (info.receiverId != safeUserSession.userId)
                throw new ArgumentException("Invalid messageId");
            await services.privateMessages.SetReadStatus(item, true);
        }
    }

    [HttpPost("messages/mark-unread")]
    public async Task MarkMessagesAsUnread([Required, FromBody] MultiUpdateMessagesRequest request)
    {
        FeatureCheck();
        foreach (var item in request.messageIds)
        {
            var info = await services.privateMessages.GetMessageById(item);
            if (info.receiverId != safeUserSession.userId)
                throw new ArgumentException("Invalid messageId");
            await services.privateMessages.SetReadStatus(item, false);
        }
    }

    [HttpPost("messages/archive")]
    public async Task MarkMessagesAsArchived([Required, FromBody] MultiUpdateMessagesRequest request)
    {
        FeatureCheck();
        foreach (var item in request.messageIds)
        {
            var info = await services.privateMessages.GetMessageById(item);
            if (info.receiverId != safeUserSession.userId)
                throw new ArgumentException("Invalid messageId");
            await services.privateMessages.SetArchiveStatus(item, true);
        }
    }

    [HttpPost("messages/unarchive")]
    public async Task MarkMessagesAsUnarchived([Required, FromBody] MultiUpdateMessagesRequest request)
    {
        FeatureCheck();
        foreach (var item in request.messageIds)
        {
            var info = await services.privateMessages.GetMessageById(item);
            if (info.receiverId != safeUserSession.userId)
                throw new ArgumentException("Invalid messageId");
            await services.privateMessages.SetArchiveStatus(item, false);
        }
    }

    [HttpGet("messages")]
    public async Task<dynamic> GetMessages(string messageTab, int pageSize, int pageNumber)
    {
        FeatureCheck();
        if (pageSize is > 100 or < 0) pageSize = 10;
        return await services.privateMessages.GetMessages(safeUserSession.userId, messageTab, pageSize,
            pageSize * pageNumber);
    }

    [HttpGet("messages/{messageId:long}")]
    public async Task<dynamic> GetMessageById(long messageId)
    {
        FeatureCheck();
        var msg = await services.privateMessages.GetMessageById(messageId);
        if (msg.receiverId != safeUserSession.userId && msg.senderId != safeUserSession.userId)
        {
            throw new BadRequestException();
        }

        return new
        {
            id = messageId,
            sender = new
            {
                id = msg.senderId,
                name = msg.senderUsername,
                displayName = msg.senderUsername,
            },
            recipient = new
            {
                id = msg.receiverId,
                name = msg.receiverUsername,
                displayName = msg.receiverUsername,
            },
            subject = msg.subject,
            body = msg.body,
            created = msg.created,
            updated = msg.updated,
            isRead = msg.isRead,
            isSystemMessage = msg.isSystemMessage,
            isReportAbuseDisplayed = msg.isReportAbuseDisplayed,
        };
    }

}