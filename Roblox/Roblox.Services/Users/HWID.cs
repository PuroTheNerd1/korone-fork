using Dapper;
using Roblox.Dto;
using Roblox.Dto.Friends;
using Roblox.Dto.Users;
using Roblox.Libraries.Cursor;
using Roblox.Metrics;
using Roblox.Models;
using Roblox.Models.Users;
using Roblox.Services.Exceptions;

namespace Roblox.Services;

public class HWID : ServiceBase, IService
{
    public async Task<bool> CheckDuplicateHWID(long userId, string HWID)
    {
        var isDuplicate = await db.QueryFirstOrDefaultAsync<int>(
            "SELECT COUNT(*) FROM hwid WHERE user_id = @userId AND hwid = @HWID",
            new { user_id = userId, HWID }
        );
        if(isDuplicate == 0)
        {
            return true;
        }
        return false;
    }

    public async Task AddHWID(long userId, string HWID)
    {
        await db.ExecuteAsync(
            "INSERT INTO HWID (user_id, HWID) VALUES (@UserId, @HWID)",
            new { user_id = userId, HWID }
        );
        return;
    }
    public async Task<bool> GetHWIDStatus(string HWID)
    {
        var isBanned = await db.QueryFirstOrDefaultAsync<bool>(
            "SELECT is_banned FROM HWID WHERE HWID = @HWID",
            new { HWID }
        );
        return isBanned;
    }    

    public async Task<bool> CheckHWID(long userId, string HWID)
    {
        bool CheckExist = await CheckDuplicateHWID(userId, HWID);
        bool status = await GetHWIDStatus(HWID);
        if(CheckExist){
            Console.WriteLine($"HWID Is already in the database {HWID}");
            Console.WriteLine($"Status: {status} ");
            return status;
        }
        await AddHWID(userId, HWID);
        return false;
    }
    public bool IsThreadSafe()
    {
        return true;
    }
    public bool IsReusable()
    {
        return false;
    }
}