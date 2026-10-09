using Game.Core.Save;
using Godot;
using TechDomination.Save;

namespace TechDomination.UI;

/// <summary>
/// Esc-Menü mit Fortsetzen, Speichern und Laden. Öffnen pausiert das Spiel; Fortsetzen hebt nur eine Pause auf,
/// die das Menü selbst gesetzt hat. Nach dem Laden bleibt das Spiel pausiert.
/// </summary>
public partial class PauseMenu : Control
{
    private Control _mainPage = null!;
    private Control _savePage = null!;
    private Control _loadPage = null!;
    private Label _statusLabel = null!;
    private LineEdit _nameEdit = null!;
    private Label _saveHint = null!;
    private ItemList _saveList = null!;
    private Button _confirmLoadButton = null!;
    private ConfirmationDialog _overwriteDialog = null!;
    private AcceptDialog _errorDialog = null!;
    private SimulationDriver _driver = null!;

    private IReadOnlyList<SaveFiles.Entry> _entries = [];
    private string? _nameToOverwrite;
    private bool _pausedByMenu;

    public override void _Ready()
    {
        _driver = GetNode<SimulationDriver>("/root/SimulationDriver");

        _mainPage = GetNode<Control>("Panel/Pages/MainPage");
        _savePage = GetNode<Control>("Panel/Pages/SavePage");
        _loadPage = GetNode<Control>("Panel/Pages/LoadPage");
        _statusLabel = GetNode<Label>("Panel/Pages/MainPage/StatusLabel");
        _nameEdit = GetNode<LineEdit>("Panel/Pages/SavePage/NameEdit");
        _saveHint = GetNode<Label>("Panel/Pages/SavePage/HintLabel");
        _saveList = GetNode<ItemList>("Panel/Pages/LoadPage/SaveList");
        _confirmLoadButton = GetNode<Button>("Panel/Pages/LoadPage/Buttons/ConfirmLoadButton");
        _overwriteDialog = GetNode<ConfirmationDialog>("OverwriteDialog");
        _errorDialog = GetNode<AcceptDialog>("ErrorDialog");

        GetNode<Button>("Panel/Pages/MainPage/ResumeButton").Pressed += Close;
        GetNode<Button>("Panel/Pages/MainPage/SaveButton").Pressed += ShowSavePage;
        GetNode<Button>("Panel/Pages/MainPage/LoadButton").Pressed += ShowLoadPage;
        GetNode<Button>("Panel/Pages/SavePage/Buttons/ConfirmSaveButton").Pressed += ConfirmSave;
        GetNode<Button>("Panel/Pages/SavePage/Buttons/BackButton").Pressed += ShowMainPage;
        GetNode<Button>("Panel/Pages/LoadPage/Buttons/BackButton").Pressed += ShowMainPage;
        _confirmLoadButton.Pressed += ConfirmLoad;
        _nameEdit.TextSubmitted += _ => ConfirmSave();
        _saveList.ItemSelected += _ => UpdateLoadButton();
        _saveList.ItemActivated += _ => ConfirmLoad();
        _overwriteDialog.Confirmed += () => SaveAs(_nameToOverwrite!);

        Visible = false;
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        if (@event is InputEventKey { Keycode: Key.Escape, Pressed: true, Echo: false })
        {
            if (!Visible)
            {
                Open();
            }
            else if (!_mainPage.Visible)
            {
                ShowMainPage();
            }
            else
            {
                Close();
            }

            GetViewport().SetInputAsHandled();
        }
        else if (Visible && @event is InputEventKey)
        {
            // Solange das Menü offen ist, gehen keine Tasten (z. B. Leertaste für Pause) an das Spiel.
            GetViewport().SetInputAsHandled();
        }
    }

    private void Open()
    {
        if (_driver.Session is { IsPaused: false } session)
        {
            _pausedByMenu = session.Pause(_driver.LocalPlayer).IsValid;
        }

        _statusLabel.Text = "";
        Visible = true;
        ShowMainPage();
    }

    private void Close()
    {
        if (_pausedByMenu && _driver.Session is { IsPaused: true } session)
        {
            session.Resume(_driver.LocalPlayer);
        }

        _pausedByMenu = false;
        Visible = false;
    }

    private void ShowMainPage() => ShowPage(_mainPage);

    private void ShowSavePage()
    {
        _nameEdit.Text = $"Spielstand {DateTime.Now:yyyy-MM-dd HH-mm}";
        _saveHint.Text = "";
        ShowPage(_savePage);
        _nameEdit.GrabFocus();
        _nameEdit.SelectAll();
    }

    private void ShowLoadPage()
    {
        _entries = SaveFiles.List();
        _saveList.Clear();
        foreach (var entry in _entries)
        {
            string details = entry.Summary is { } summary && _driver.Data is { } data
                ? $"Spielzeit {UiFormat.PlayTime(summary.Tick, data)}"
                : "beschädigt";
            _saveList.AddItem($"{entry.Name}   –   {entry.Modified.ToLocalTime():dd.MM.yyyy HH:mm}   –   {details}");
        }

        UpdateLoadButton();
        ShowPage(_loadPage);
    }

    private void ShowPage(Control page)
    {
        _mainPage.Visible = page == _mainPage;
        _savePage.Visible = page == _savePage;
        _loadPage.Visible = page == _loadPage;
    }

    private void ConfirmSave()
    {
        string name = _nameEdit.Text.Trim();
        var validation = SaveNames.Validate(name);
        if (!validation.IsValid)
        {
            _saveHint.Text = validation.Reason;
            return;
        }

        if (SaveFiles.Exists(name))
        {
            _nameToOverwrite = name;
            _overwriteDialog.DialogText = $"Der Spielstand „{name}“ existiert bereits. Überschreiben?";
            _overwriteDialog.PopupCentered();
            return;
        }

        SaveAs(name);
    }

    private void SaveAs(string name)
    {
        try
        {
            _driver.Save(name);
            _statusLabel.Text = $"Gespeichert: {name}";
            ShowMainPage();
        }
        catch (IOException e)
        {
            ShowError(e.Message);
        }
    }

    private void ConfirmLoad()
    {
        int[] selected = _saveList.GetSelectedItems();
        if (selected.Length == 0)
        {
            return;
        }

        try
        {
            _driver.Load(_entries[selected[0]].Name);

            // Nach dem Laden bleibt das Spiel pausiert, auch wenn das Menü geschlossen wird.
            _pausedByMenu = false;
            Close();
        }
        catch (Exception e) when (e is SaveGameException or IOException)
        {
            ShowError(e.Message);
        }
    }

    private void UpdateLoadButton()
    {
        int[] selected = _saveList.GetSelectedItems();
        _confirmLoadButton.Disabled = selected.Length == 0 || _entries[selected[0]].Summary is null;
    }

    private void ShowError(string message)
    {
        _errorDialog.DialogText = message;
        _errorDialog.PopupCentered();
    }
}
