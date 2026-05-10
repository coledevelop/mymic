using System;
using System.Net;
using System.Net.Http;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using Velopack;
using Velopack.Sources;

namespace MyMic.Updates;

public enum UpdaterState
{
    Idle,
    Checking,
    UpToDate,
    Downloading,
    Ready,
    Failed,
    Unavailable,
}

public sealed class UpdaterService
{
    private const string RepoUrl = "https://github.com/coledevelop/mymic";

    private readonly UpdateManager? _manager;
    private UpdateInfo? _pendingUpdate;
    private bool _busy;

    public UpdaterState State { get; private set; } = UpdaterState.Idle;
    public string ButtonText { get; private set; } = "Check for updates";
    public bool ButtonEnabled { get; private set; } = true;
    public string StatusText { get; private set; } = "";

    public event EventHandler? Changed;

    public UpdaterService()
    {
        try
        {
            var source = new GithubSource(RepoUrl, accessToken: null, prerelease: false);
            var channel = RuntimeInformation.OSArchitecture switch
            {
                Architecture.Arm64 => "osx-arm64",
                Architecture.X64 => "osx-x64",
                _ => null,
            };
            var opts = channel is null ? null : new UpdateOptions { ExplicitChannel = channel };
            _manager = new UpdateManager(source, opts);
        }
        catch
        {
            _manager = null;
        }

        if (_manager is null || !_manager.IsInstalled)
        {
            State = UpdaterState.Unavailable;
            ButtonText = "Updates unavailable";
            ButtonEnabled = false;
            StatusText = _manager is null
                ? "Could not initialize the updater."
                : "Run the installed app to check for updates.";
        }
    }

    public async Task TriggerAsync()
    {
        if (_manager is null || !_manager.IsInstalled) return;

        if (_pendingUpdate is not null)
        {
            try
            {
                _manager.ApplyUpdatesAndRestart(_pendingUpdate);
            }
            catch (Exception ex)
            {
                State = UpdaterState.Failed;
                StatusText = $"Restart failed: {ex.Message}";
                Notify();
            }
            return;
        }

        await CheckAndDownloadAsync();
    }

    public async Task CheckAndDownloadAsync()
    {
        if (_manager is null || !_manager.IsInstalled || _busy) return;

        _busy = true;
        try
        {
            State = UpdaterState.Checking;
            ButtonText = "Checking…";
            ButtonEnabled = false;
            StatusText = "";
            Notify();

            UpdateInfo? info = null;
            try
            {
                info = await _manager.CheckForUpdatesAsync();
            }
            catch (HttpRequestException ex) when (ex.StatusCode == HttpStatusCode.NotFound)
            {
                // No GitHub Release published yet (or none for this channel).
                info = null;
            }
            catch (Exception ex) when (ex.InnerException is HttpRequestException http
                                       && http.StatusCode == HttpStatusCode.NotFound)
            {
                info = null;
            }

            if (info is null)
            {
                State = UpdaterState.UpToDate;
                ButtonText = "Check for updates";
                ButtonEnabled = true;
                StatusText = $"Up to date (current: {SafeCurrentVersion()})";
                Notify();
                return;
            }

            var version = info.TargetFullRelease.Version.ToString();
            State = UpdaterState.Downloading;
            ButtonText = $"Downloading v{version}…";
            ButtonEnabled = false;
            Notify();

            await _manager.DownloadUpdatesAsync(info);
            _pendingUpdate = info;

            State = UpdaterState.Ready;
            ButtonText = $"Restart to install v{version}";
            ButtonEnabled = true;
            StatusText = "";
            Notify();
        }
        catch (Exception ex)
        {
            State = UpdaterState.Failed;
            ButtonText = "Check for updates";
            ButtonEnabled = true;
            StatusText = $"{ex.GetType().Name}: {ex.Message}";
            Notify();
        }
        finally
        {
            _busy = false;
        }
    }

    private string SafeCurrentVersion()
    {
        try { return _manager?.CurrentVersion?.ToString() ?? "unknown"; }
        catch { return "unknown"; }
    }

    private void Notify() => Changed?.Invoke(this, EventArgs.Empty);
}
