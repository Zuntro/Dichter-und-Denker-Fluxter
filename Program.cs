using Fluxer.Net;
using Fluxer.Net.Data.Models;
using Serilog;

var logger = new LoggerConfiguration()
.MinimumLevel.Information()
.WriteTo.Console()
.CreateLogger();

const string token = "Bot 1475458888321384734.Z105OZh3FNHjakCDp1wiYZ--c9ihj2y6guQfGYDSTZM";

var config = new FluxerConfig
{
    Serilog = logger,
    EnableRateLimiting = true
};

var apiClient = new ApiClient(token, config);
var gatewayClient = new GatewayClient(token, config);

gatewayClient.Ready += (data) =>
{
    logger.Information("Bot ist bereit! Eingeloggt als {Username}", data.User.Username);
};

gatewayClient.MessageCreate += async (msg) =>
{
    try
    {
        logger.Information("Nachricht erhalten: {Content} von {Author} (Bot: {IsBot}) in Channel {ChannelId}", 
            msg.Content, msg.Author.Username, msg.Author.IsBot, msg.ChannelId);
        
        // Eigene Nachrichten ignorieren (Bot selbst)
        if (msg.Author.IsBot || msg.Author.Username == "Dichter_und_Denker")
        {
            logger.Information("Nachricht von Bot ignoriert");
            return;
        }

        if (string.IsNullOrWhiteSpace(msg.Content))
        {
            logger.Information("Leere Nachricht ignoriert");
            return;
        }

        if (!msg.Content.StartsWith("!quote"))
        {
            logger.Information("Kein !quote Befehl");
            return;
        }

        var content = msg.Content.Substring(6).Trim();
        var parts = content.Split('|');

        if (parts.Length < 2)
        {
            logger.Information("Ungültiges Format - weniger als 2 Teile");
            return;
        }

        var quoteText = parts[0].Trim();
        var authorName = parts[1].Trim();
        var dateText = parts.Length > 2 ? parts[2].Trim() : "";

        var formatted = dateText.Length > 0 
            ? $"> **{quoteText}**\n— *{authorName}*, {dateText}"
            : $"> **{quoteText}**\n— *{authorName}*";

        // Aktuelles Datum hinzufügen wenn kein Datum angegeben
        if (dateText.Length == 0)
        {
            var today = DateTime.Now.ToString("dd.MM.yyyy");
            formatted = $"> **{quoteText}**\n— *{authorName}*, {today}";
        }

        logger.Information("Sende Zitat: {Formatted}", formatted);
        
        var reply = new Message { Content = formatted };
        await apiClient.SendMessage(msg.ChannelId, reply);
        
        // Ursprüngliche Nachricht löschen
        await apiClient.DeleteMessage(msg.ChannelId, msg.Id);
        
        logger.Information("Zitat erfolgreich gesendet");
    }
    catch (Exception ex)
    {
        logger.Error(ex, "Fehler beim Verarbeiten der Nachricht");
    }
};

try
{
    await gatewayClient.ConnectAsync();
    logger.Information("Bot läuft und verbunden mit Fluxer");
}
catch (Exception ex)
{
    logger.Error(ex, "Fehler beim Verbinden mit Fluxer Gateway");
    return;
}

await Task.Delay(-1);
