using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Dalamud.Divination.Common.Api.Dalamud;
using Divination.FaloopIntegration.Faloop.Model;

namespace Divination.FaloopIntegration.Faloop;

public class FaloopSession : IDisposable
{
    private readonly FaloopApiClient client = new();

    public bool IsLoggedIn { get; private set; }

    public string? SessionId { get; private set; }

    public string? Token { get; private set; }

    public async Task<bool> LoginAsync(string username, string password)
    {
        Logout();

        var initialSession = await client.RefreshAsync();
        if (initialSession is not { Success: true })
        {
            DalamudLog.Log.Debug("LoginAsync: initialSession is not success");
            return false;
        }

        var login = await client.LoginAsync(username, password, initialSession.Data.SessionId, initialSession.Data.Token);
        if (login is not { Success: true })
        {
            DalamudLog.Log.Debug("LoginAsync: login is not success");
            return false;
        }

        IsLoggedIn = true;
        SessionId = login.Data.SessionId;
        Token = login.Data.Token;
        return true;
    }

    public async Task<List<MobReportData>> GetActiveReportsAsync(string dataCenter)
    {
        if (!IsLoggedIn)
        {
            DalamudLog.Log.Debug("GetActiveReportsAsync: Not logged in");
            return [];
        }

        if (Token is null)
        {
            DalamudLog.Log.Debug("GetActiveReportsAsync: Token is null");
            return [];
        }

        var dcData = await client.GetDCDataAsync(Token, dataCenter);


        if (dcData is not { Success: true })
        {
            DalamudLog.Log.Debug("GetDCDataAsync: GetDcData Failed");
            return [];
        }

        var options = new JsonSerializerOptions();
        options.Converters.Add(new ActiveSpawnConverter());

        var spawnsData = dcData.Data["status"]["spawns"].Deserialize<List<MobReportData>>(options);

        return spawnsData;
    }

    private void Logout()
    {
        IsLoggedIn = false;
        SessionId = default;
    }

    public void Dispose()
    {
        client.Dispose();
    }
}
