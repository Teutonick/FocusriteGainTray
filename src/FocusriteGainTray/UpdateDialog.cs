using System.Diagnostics;

namespace FocusriteGainTray;

internal sealed class UpdateDialog : Form
{
    private readonly Icon _ownedIcon = AppIcons.Load();
    private readonly Label _status;
    private readonly Button _open;
    private readonly CancellationTokenSource _cancel = new();
    private Uri _page = new(ProductInfo.ReleasesUrl);
    public UpdateDialog()
    {
        Text = Ui.T("FocGrain — обновления", "FocGrain — updates");
        Icon = _ownedIcon;
        ClientSize = new Size(490, 235);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MaximizeBox = false;
        MinimizeBox = false;
        StartPosition = FormStartPosition.CenterParent;
        Controls.Add(new Label { Text = $"FocGrain {ProductInfo.VersionText}\nGitHub: {ProductInfo.Repository}", Left = 20, Top = 18, Width = 450, Height = 44 });
        _status = new Label { Text = Ui.T("Проверяем GitHub Releases…", "Checking GitHub Releases…"), Left = 20, Top = 78, Width = 450, Height = 80 };
        _open = new Button { Text = Ui.T("Открыть GitHub Releases", "Open GitHub Releases"), Left = 20, Top = 180, Width = 250, Height = 32, Enabled = false };
        _open.Click += (_, _) =>
        {
            try { Process.Start(new ProcessStartInfo(_page.AbsoluteUri) { UseShellExecute = true }); }
            catch (Exception ex) { _status.Text = Ui.T("Не удалось открыть браузер: ", "Could not open the browser: ") + ex.Message; }
        };
        var close = new Button { Text = Ui.T("Закрыть", "Close"), Left = 350, Top = 180, Width = 115, Height = 32, DialogResult = DialogResult.Cancel };
        Controls.Add(_status); Controls.Add(_open); Controls.Add(close);
        CancelButton = close;
        Shown += async (_, _) => await CheckAsync();
        FormClosed += (_, _) => _cancel.Cancel();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _cancel.Cancel();
        base.Dispose(disposing);
        if (disposing) _ownedIcon.Dispose();
    }

    private async Task CheckAsync()
    {
        try
        {
            using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(10), MaxResponseContentBufferSize = 1024 * 1024 };
            var result = await new UpdateClient(http).CheckAsync(ProductInfo.CurrentVersion, _cancel.Token);
            if (IsDisposed) return;
            _status.Text = result.Status switch
            {
                UpdateStatus.Available => Ui.T($"Доступна версия {result.Release!.Version.ToString(3)}.\nСкачайте setup со страницы релиза и запустите его для обновления.", $"Version {result.Release!.Version.ToString(3)} is available.\nDownload its setup from the release page and run it to update."),
                UpdateStatus.Current => Ui.T("Установлена актуальная версия (или новее опубликованной).", "You have the latest version (or a newer local build)."),
                UpdateStatus.NoRelease => Ui.T("Опубликованный релиз не найден или репозиторий недоступен.\nПроверка не подтверждает отсутствие обновлений.", "No published release was found, or the repository is unavailable.\nThis does not confirm that you are up to date."),
                _ => Ui.T("GitHub ограничил запрос. Повторите проверку позже.", "GitHub limited this request. Please try again later.")
            };
            if (result.Release is not null) _page = result.Release.Page;
            _open.Enabled = true;
        }
        catch (OperationCanceledException) when (_cancel.IsCancellationRequested) { }
        catch (Exception ex)
        {
            if (!IsDisposed)
            {
                _status.Text = Ui.T("Не удалось проверить обновления. Проверьте сеть и повторите попытку.\n", "Could not check for updates. Check your connection and try again.\n") + ex.Message;
                _open.Enabled = true;
            }
        }
    }
}
