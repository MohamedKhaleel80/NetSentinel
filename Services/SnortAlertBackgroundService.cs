using System.Text.Json;
using NetSentinel.API.DTOs.Snort;
using NetSentinel.API.Models.Settings;
using NetSentinel.API.Services.Interfaces;
using Microsoft.Extensions.Options;

namespace NetSentinel.API.Services;

public class SnortAlertBackgroundService
    : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<SnortAlertBackgroundService> _logger;
    private readonly SnortSettings _settings;

    private long _position;

    public SnortAlertBackgroundService(
        IServiceScopeFactory scopeFactory,
        ILogger<SnortAlertBackgroundService> logger,
        IOptions<SnortSettings> options)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
        _settings = options.Value;
    }

    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        if (!_settings.Enabled)
        {
            _logger.LogInformation(
                "Snort background service is disabled.");

            return;
        }

        _logger.LogInformation(
            "Snort local file watcher started for {AlertFilePath} with a {PollIntervalMilliseconds} ms poll interval.",
            Path.GetFullPath(_settings.AlertFilePath),
            _settings.PollIntervalMilliseconds);

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessFileAsync(
                    Path.GetFullPath(_settings.AlertFilePath),
                    stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Error while processing Snort alert file.");
            }

            await Task.Delay(
                _settings.PollIntervalMilliseconds,
                stoppingToken);
        }
    }

    private async Task ProcessFileAsync(
        string alertFilePath,
        CancellationToken cancellationToken)
    {
        if (!File.Exists(
            alertFilePath))
        {
            return;
        }

        var fileInfo =
            new FileInfo(alertFilePath);

        if (fileInfo.Length < _position)
        {
            _position = 0;
        }

        if (fileInfo.Length == _position)
        {
            return;
        }

        using var stream =
            new FileStream(
                alertFilePath,
                FileMode.Open,
                FileAccess.Read,
                FileShare.ReadWrite);

        stream.Seek(
            _position,
            SeekOrigin.Begin);

        using var reader = new StreamReader(stream);
        string? line;
        while ((line = await reader.ReadLineAsync(cancellationToken)) != null)
        {
            if (string.IsNullOrWhiteSpace(line))
            {
                continue;
            }

            await ProcessLineAsync(
                line);
        }

        _position = stream.Position;
    }

    private async Task ProcessLineAsync(
        string line)
    {
        try
        {
            var alert =
                JsonSerializer.Deserialize<SnortAlertDto>(
                    line,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

            if (alert == null)
            {
                return;
            }

            using var scope =
                _scopeFactory.CreateScope();

            var snortService =
                scope.ServiceProvider
                    .GetRequiredService<ISnortService>();

            await snortService.ProcessAlertAsync(
                alert);
        }
        catch (JsonException ex)
        {
            _logger.LogWarning(
                ex,
                "Invalid Snort JSON line ignored.");
        }
    }
}