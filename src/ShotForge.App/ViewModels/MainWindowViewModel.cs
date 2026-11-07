using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ShotForge.App.Services;
using ShotForge.Core.Models;
using ShotForge.Core.Services;
using ShotForge.Data;
using ShotForge.Data.Repositories;

namespace ShotForge.App.ViewModels
{
    public partial class MainWindowViewModel : ObservableObject
    {
        /// <summary>
        /// New shots start with NO style line. The style is the user's own
        /// (typed into the visible Style box) and then carries over from shot
        /// to shot, so every clip in a scene shares one look.
        /// </summary>
        private const string DefaultStyleLockTag = "";

        private readonly ProjectRepository _projectRepository;
        private readonly SceneRepository _sceneRepository;
        private readonly ShotRepository _shotRepository;
        private readonly CharacterRepository _characterRepository;
        private readonly LicenseService _licenseService;

        [ObservableProperty]
        private ObservableCollection<Project> _projects = new();

        [ObservableProperty]
        private string _newProjectName = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddOrUpdateProjectButtonText))]
        private bool _isEditingProject;

        /// <summary>The Project being edited, or null when adding a new one.</summary>
        private Project? _editingProject;

        public string AddOrUpdateProjectButtonText => IsEditingProject ? "Update Project" : "Add Project";

        [ObservableProperty]
        private Project? _selectedProject;

        [ObservableProperty]
        private ObservableCollection<Scene> _scenes = new();

        [ObservableProperty]
        private string _newSceneTitle = string.Empty;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddOrUpdateSceneButtonText))]
        private bool _isEditingScene;

        private Scene? _editingScene;

        public string AddOrUpdateSceneButtonText => IsEditingScene ? "Update Scene" : "Add Scene";

        [ObservableProperty]
        private Scene? _selectedScene;

        [ObservableProperty]
        private ObservableCollection<Shot> _shots = new();

        [ObservableProperty]
        private string _newShotDescription = string.Empty;

        [ObservableProperty]
        private string _newShotNegativePrompt = string.Empty;

        [ObservableProperty]
        private decimal? _newShotDurationSeconds = 5;

        [ObservableProperty]
        private string _newShotStyleLockTag = DefaultStyleLockTag;

        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(AddOrUpdateShotButtonText))]
        private bool _isEditingShot;

        private Shot? _editingShot;

        public string AddOrUpdateShotButtonText => IsEditingShot ? "Update Shot" : "Add Shot";

        /// <summary>
        /// Every character belonging to the selected Project, each
        /// wrapped with a checkbox-friendly IsSelected flag for picking
        /// which ones appear in the shot currently being created. Reloads
        /// whenever the selected Project changes (Characters live at the
        /// Project level, same as Scenes).
        /// </summary>
        [ObservableProperty]
        [NotifyPropertyChangedFor(nameof(HasNoAvailableCharacters))]
        private ObservableCollection<CharacterSelectionItem> _availableCharacters = new();

        /// <summary>
        /// Backs the "no characters yet" hint in the Shot form. A
        /// computed property rather than a binding-side "!Count" check -
        /// negating an int isn't valid C#, and compiled bindings turn
        /// XAML expressions into real C# code, so that would have been
        /// a build error, not just a runtime quirk.
        /// </summary>
        public bool HasNoAvailableCharacters => AvailableCharacters.Count == 0;

        /// <summary>
        /// Raised when the user clicks "Copy Prompt" on a shot. The View
        /// owns the actual clipboard call (a platform concern), so this
        /// ViewModel stays free of Avalonia UI types.
        /// </summary>
        public event Action<string>? CopyToClipboardRequested;

        /// <summary>
        /// Raised when the user clicks "Export Project". Carries the
        /// base file name (no extension) plus BOTH the .txt and .json
        /// versions of the content - the View shows a Save dialog with
        /// both format choices and writes whichever one the user
        /// actually picks. Both platform concerns (dialog, disk write)
        /// stay in the View, same separation as the clipboard event.
        /// </summary>
        public event Action<string, string, string>? ExportRequested;

        /// <summary>
        /// Raised when the user clicks "Manage Characters". Carries the
        /// selected project's Id and Name - the View owns constructing
        /// and showing the CharactersWindow (a platform/window-management
        /// concern), same split as the other events on this ViewModel.
        /// </summary>
        public event Action<int, string>? ManageCharactersRequested;

        /// <summary>
        /// "Copied prompt" checkboxes (bool? because CheckBox.IsChecked is nullable and a
        /// compiled binding rejects the mismatch). Saved between launches.
        /// </summary>
        [ObservableProperty]
        private bool? _includeCharacterDescriptions = true;

        [ObservableProperty]
        private bool? _includeNegativeInPrompt = false;

        [ObservableProperty]
        private bool? _includeDuration = true;

        private PromptOptions CurrentPromptOptions =>
            new(IncludeCharacterDescriptions ?? true, IncludeNegativeInPrompt ?? false, IncludeDuration ?? true);

        partial void OnIncludeCharacterDescriptionsChanged(bool? value) =>
            PromptOptionsService.Save(CurrentPromptOptions);

        partial void OnIncludeNegativeInPromptChanged(bool? value) =>
            PromptOptionsService.Save(CurrentPromptOptions);

        partial void OnIncludeDurationChanged(bool? value) =>
            PromptOptionsService.Save(CurrentPromptOptions);

        /// <summary>
        /// Bound to the Appearance toggle. bool? (not bool) because the
        /// ToggleSwitch's IsChecked is a nullable bool and a compiled
        /// binding rejects the mismatch. True = Dark, False = Light.
        /// </summary>
        [ObservableProperty]
        private bool? _isDarkMode = true;

        partial void OnIsDarkModeChanged(bool? value)
        {
            var isDark = value ?? true;
            ThemeService.Apply(isDark);
            ThemeService.Save(isDark);
        }

        /// <summary>
        /// The Shots column header, built in ONE place so it always reads
        /// "Shots \u2014 Scene N: Title" for every scene. Exposed as a single
        /// plain string (rather than several separately-updating pieces in
        /// the XAML) because that is the simplest binding the UI can have.
        /// </summary>
        public string ShotsHeaderText =>
            SelectedScene is null
                ? "Shots"
                : $"Shots \u2014 Scene {SelectedScene.SceneNumber}: {SelectedScene.Title}";

        /// <summary>True on the Free version - drives the visibility of the Unlock Pro button.</summary>
        public bool IsFreeTier => _licenseService.Tier == LicenseTier.Free;

        /// <summary>Shown under the project box when a Free user hits the project limit.</summary>
        [ObservableProperty]
        private string _limitMessage = string.Empty;

        /// <summary>
        /// Raised when the user clicks "Unlock Pro". Carries the ViewModel for the
        /// licence window; the View owns creating and showing that window.
        /// </summary>
        public event Action<LicenseWindowViewModel>? UnlockRequested;

        /// <summary>Re-reads the licence state into the UI (badge text, button visibility, limit message).</summary>
        public void RefreshLicenseState()
        {
            OnPropertyChanged(nameof(LicenseBadgeText));
            OnPropertyChanged(nameof(IsFreeTier));
            if (!IsFreeTier) LimitMessage = string.Empty;
        }

        [RelayCommand]
        private void UnlockPro()
        {
            UnlockRequested?.Invoke(new LicenseWindowViewModel(_licenseService));
        }

        /// <summary>
        /// Window title. In a DEBUG build it also says whether the developer Pro
        /// override is active, which makes "is my test setup actually working?"
        /// answerable at a glance. Release builds just show the product name.
        /// </summary>
        public string WindowTitle
        {
            get
            {
#if DEBUG
                return _licenseService.IsDeveloperOverrideActive
                    ? "ShotForge  [Debug build - Pro override ON]"
                    : "ShotForge  [Debug build - Pro override off]";
#else
                return "ShotForge";
#endif
            }
        }

        /// <summary>Shown in the UI so the tier is always visible, not hidden.</summary>
        public string LicenseBadgeText =>
            _licenseService.Tier == LicenseTier.Pro ? "PRO" : "FREE";

        public MainWindowViewModel(LicenseService licenseService)
        {
            _licenseService = licenseService;
            // Start the toggle in the saved position (sets the field directly so
            // it doesn't re-trigger Apply/Save during construction).
            _isDarkMode = ThemeService.LoadIsDark();
            var savedPromptOptions = PromptOptionsService.Load();
            _includeCharacterDescriptions = savedPromptOptions.IncludeCharacterDescriptions;
            _includeNegativeInPrompt = savedPromptOptions.IncludeNegativeInPrompt;
            _includeDuration = savedPromptOptions.IncludeDuration;
            _licenseService.StateChanged += RefreshLicenseState;

            // All four repositories share one DbContext for this
            // ViewModel's lifetime, so every operation sees the same
            // connection and change-tracking state.
            var db = new ShotForgeDbContext();
            _projectRepository = new ProjectRepository(db);
            _sceneRepository = new SceneRepository(db);
            _shotRepository = new ShotRepository(db);
            _characterRepository = new CharacterRepository(db);

            _ = LoadProjectsAsync();
        }

        private async Task LoadProjectsAsync()
        {
            var projects = await _projectRepository.GetAllAsync();
            Projects = new ObservableCollection<Project>(projects);
        }

        [RelayCommand]
        private async Task AddProjectAsync()
        {
            if (string.IsNullOrWhiteSpace(NewProjectName))
                return;

            if (_editingProject is not null)
            {
                // Mutating the SAME object reference that's already in the
                // Projects collection (and possibly SelectedProject) - its
                // Name setter now raises PropertyChanged (ObservableModelBase),
                // so the ListBox updates live with no reload, and no risk
                // of losing the current selection.
                _editingProject.Name = NewProjectName;
                await _projectRepository.UpdateAsync(_editingProject);
                _editingProject = null;
                IsEditingProject = false;
            }
            else
            {
                if (!_licenseService.CanAddProject(Projects.Count))
                {
                    var n = _licenseService.FreeProjectLimit;
                    LimitMessage = $"The free version includes {n} project{(n == 1 ? "" : "s")}. Unlock Pro for unlimited projects.";
                    return;
                }

                LimitMessage = string.Empty;
                var created = await _projectRepository.CreateAsync(NewProjectName);
                Projects.Add(created);
            }

            NewProjectName = string.Empty;
        }

        [RelayCommand]
        private void EditProject(Project? project)
        {
            if (project is null) return;

            _editingProject = project;
            NewProjectName = project.Name;
            IsEditingProject = true;
        }

        [RelayCommand]
        private void CancelEditProject()
        {
            _editingProject = null;
            NewProjectName = string.Empty;
            IsEditingProject = false;
        }

        [RelayCommand]
        private async Task DeleteProjectAsync(Project? project)
        {
            if (project is null) return;

            // If the project being deleted is the one currently selected,
            // clear the Scenes panel first so it doesn't point at a
            // project that no longer exists.
            if (SelectedProject?.Id == project.Id)
            {
                SelectedProject = null;
            }

            // Same guard for an in-progress edit - don't let "Update
            // Project" later save over a row that's already gone.
            if (_editingProject?.Id == project.Id)
            {
                CancelEditProject();
            }

            await _projectRepository.DeleteAsync(project.Id);
            Projects.Remove(project);
        }

        /// <summary>
        /// CommunityToolkit.Mvvm calls this automatically whenever
        /// SelectedProject changes (source-generated from the
        /// [ObservableProperty] above) - this is what makes the Scenes
        /// panel follow whichever project is currently selected.
        /// </summary>
        partial void OnSelectedProjectChanged(Project? value)
        {
            // Switching projects while mid-edit would otherwise leave the
            // form showing a stale edit unrelated to what's now selected.
            if (_editingProject is not null && _editingProject.Id != value?.Id)
            {
                CancelEditProject();
            }

            NewSceneTitle = string.Empty;
            SelectedScene = null;
            AddSceneCommand.NotifyCanExecuteChanged();
            ExportProjectCommand.NotifyCanExecuteChanged();
            ManageCharactersCommand.NotifyCanExecuteChanged();

            if (value is null)
            {
                Scenes = new ObservableCollection<Scene>();
                AvailableCharacters = new ObservableCollection<CharacterSelectionItem>();
                return;
            }

            _ = LoadScenesAsync(value.Id);
            _ = LoadAvailableCharactersAsync(value.Id);
        }

        private async Task LoadAvailableCharactersAsync(int projectId)
        {
            var characters = await _characterRepository.GetByProjectIdAsync(projectId);
            AvailableCharacters = new ObservableCollection<CharacterSelectionItem>(
                characters.Select(c => new CharacterSelectionItem(c)));
        }

        /// <summary>
        /// Called by the View after the Characters window closes, since
        /// that window edits characters through its own separate
        /// DbContext - this ViewModel's own AvailableCharacters list
        /// doesn't know about those changes until explicitly refreshed.
        /// </summary>
        public async Task RefreshAvailableCharactersAsync()
        {
            if (SelectedProject is not null)
            {
                await LoadAvailableCharactersAsync(SelectedProject.Id);
            }
        }

        private async Task LoadScenesAsync(int projectId)
        {
            var scenes = await _sceneRepository.GetByProjectIdAsync(projectId);
            Scenes = new ObservableCollection<Scene>(scenes);
        }

        /// <summary>Adding a scene requires a project to attach it to.</summary>
        private bool CanAddScene() => SelectedProject is not null;

        [RelayCommand(CanExecute = nameof(CanAddScene))]
        private async Task AddSceneAsync()
        {
            if (SelectedProject is null || string.IsNullOrWhiteSpace(NewSceneTitle))
                return;

            if (_editingScene is not null)
            {
                _editingScene.Title = NewSceneTitle;
                await _sceneRepository.UpdateAsync(_editingScene);
                if (SelectedScene?.Id == _editingScene.Id)
                    OnPropertyChanged(nameof(ShotsHeaderText));
                _editingScene = null;
                IsEditingScene = false;
            }
            else
            {
                var nextSceneNumber = Scenes.Count + 1;
                var created = await _sceneRepository.CreateAsync(
                    SelectedProject.Id, nextSceneNumber, NewSceneTitle);
                Scenes.Add(created);
            }

            NewSceneTitle = string.Empty;
        }

        [RelayCommand]
        private void EditScene(Scene? scene)
        {
            if (scene is null) return;

            _editingScene = scene;
            NewSceneTitle = scene.Title;
            IsEditingScene = true;
        }

        [RelayCommand]
        private void CancelEditScene()
        {
            _editingScene = null;
            NewSceneTitle = string.Empty;
            IsEditingScene = false;
        }

        [RelayCommand]
        private async Task DeleteSceneAsync(Scene? scene)
        {
            if (scene is null) return;

            // Same guard as DeleteProjectAsync - don't leave the Shots
            // panel pointing at a scene that's about to be gone.
            if (SelectedScene?.Id == scene.Id)
            {
                SelectedScene = null;
            }

            if (_editingScene?.Id == scene.Id)
            {
                CancelEditScene();
            }

            await _sceneRepository.DeleteAsync(scene.Id);
            Scenes.Remove(scene);
        }

        /// <summary>
        /// CommunityToolkit.Mvvm calls this automatically whenever
        /// SelectedScene changes - this is what makes the Shots panel
        /// follow whichever scene is currently selected, exactly the
        /// same pattern as OnSelectedProjectChanged above.
        /// </summary>
        partial void OnSelectedSceneChanged(Scene? value)
        {
            OnPropertyChanged(nameof(ShotsHeaderText));

            if (_editingScene is not null && _editingScene.Id != value?.Id)
            {
                CancelEditScene();
            }

            NewShotDescription = string.Empty;
            NewShotNegativePrompt = string.Empty;
            NewShotDurationSeconds = 5;
            ClearCharacterSelections();
            AddShotCommand.NotifyCanExecuteChanged();

            if (_editingShot is not null)
            {
                _editingShot = null;
                IsEditingShot = false;
            }

            if (value is null)
            {
                // Nothing selected (e.g. switched project): start the style fresh.
                NewShotStyleLockTag = DefaultStyleLockTag;
                Shots = new ObservableCollection<Shot>();
                return;
            }

            _ = LoadShotsAsync(value.Id);
        }

        private async Task LoadShotsAsync(int sceneId)
        {
            var shots = await _shotRepository.GetBySceneIdAsync(sceneId);
            Shots = new ObservableCollection<Shot>(shots);
            ApplySceneStyleToForm();
        }

        /// <summary>Adding a shot requires a scene to attach it to.</summary>
        private bool CanAddShot() => SelectedScene is not null;

        [RelayCommand(CanExecute = nameof(CanAddShot))]
        private async Task AddShotAsync()
        {
            if (SelectedScene is null || string.IsNullOrWhiteSpace(NewShotDescription))
                return;

            var durationSeconds = (int)(NewShotDurationSeconds ?? 5);
            var selectedCharacterIds = AvailableCharacters
                .Where(c => c.IsSelected)
                .Select(c => c.Character.Id)
                .ToList();
            var negativePromptOrNull = string.IsNullOrWhiteSpace(NewShotNegativePrompt)
                ? null : NewShotNegativePrompt;

            if (_editingShot is not null)
            {
                // Characters is a many-to-many relationship, so this goes
                // through the dedicated, properly-tracked UpdateDetailsAsync
                // rather than the simple scalar UpdateAsync - see the
                // comment on that method for why. It returns a FRESH
                // tracked entity rather than mutating _editingShot in
                // place, so we replace it by index in the Shots collection
                // instead (same end result, correctly tracked).
                var updated = await _shotRepository.UpdateDetailsAsync(
                    _editingShot.Id,
                    NewShotDescription,
                    negativePromptOrNull,
                    durationSeconds,
                    NewShotStyleLockTag,
                    selectedCharacterIds);

                if (updated is not null)
                {
                    var index = Shots.IndexOf(_editingShot);
                    if (index >= 0)
                    {
                        Shots[index] = updated;
                    }
                }

                _editingShot = null;
                IsEditingShot = false;
            }
            else
            {
                var nextShotNumber = Shots.Count + 1;
                var created = await _shotRepository.CreateAsync(
                    SelectedScene.Id,
                    nextShotNumber,
                    NewShotDescription,
                    durationSeconds,
                    NewShotStyleLockTag,
                    selectedCharacterIds,
                    negativePromptOrNull);

                Shots.Add(created);
            }

            NewShotDescription = string.Empty;
            NewShotNegativePrompt = string.Empty;
            ClearCharacterSelections();
            // Duration and style lock deliberately NOT reset here - most
            // shots in a row share the same duration/style, so leaving
            // them filled in saves retyping for the next shot.
        }

        [RelayCommand]
        private void EditShot(Shot? shot)
        {
            if (shot is null) return;

            _editingShot = shot;
            NewShotDescription = shot.Description;
            NewShotNegativePrompt = shot.NegativePrompt ?? string.Empty;
            NewShotDurationSeconds = shot.DurationSeconds;
            NewShotStyleLockTag = shot.StyleLockTag;
            IsEditingShot = true;

            // Pre-check whichever characters are already attached to this
            // shot, so editing shows the current state rather than a
            // blank picker.
            var attachedIds = shot.Characters.Select(c => c.Id).ToHashSet();
            foreach (var item in AvailableCharacters)
            {
                item.IsSelected = attachedIds.Contains(item.Character.Id);
            }
        }

        [RelayCommand]
        private void CancelEditShot()
        {
            _editingShot = null;
            NewShotDescription = string.Empty;
            NewShotNegativePrompt = string.Empty;
            NewShotDurationSeconds = 5;
            ApplySceneStyleToForm();
            ClearCharacterSelections();
            IsEditingShot = false;
        }

        /// <summary>
        /// Puts the style used by the scene's most recent shot into the Style box,
        /// so new shots match the rest of the scene. If the scene has no shots yet,
        /// whatever is already in the box is left alone.
        /// </summary>
        private void ApplySceneStyleToForm()
        {
            if (_editingShot is not null || Shots.Count == 0) return;
            var lastStyle = Shots[Shots.Count - 1].StyleLockTag;
            if (!string.IsNullOrWhiteSpace(lastStyle))
            {
                NewShotStyleLockTag = lastStyle;
            }
        }

        private void ClearCharacterSelections()
        {
            foreach (var item in AvailableCharacters)
            {
                item.IsSelected = false;
            }
        }

        [RelayCommand]
        private async Task DeleteShotAsync(Shot? shot)
        {
            if (shot is null) return;

            if (_editingShot?.Id == shot.Id)
            {
                CancelEditShot();
            }

            await _shotRepository.DeleteAsync(shot.Id);
            Shots.Remove(shot);
        }

        /// <summary>
        /// This is the actual payoff feature: turn a shot into its
        /// ready-to-paste prompt block and hand the text to the View to
        /// place on the clipboard.
        /// </summary>
        [RelayCommand]
        private void CopyPrompt(Shot? shot)
        {
            if (shot is null) return;
            CopyToClipboardRequested?.Invoke(shot.BuildPromptText(CurrentPromptOptions));
        }

        /// <summary>Exporting requires a project to actually export.</summary>
        private bool CanExportProject() => SelectedProject is not null;

        /// <summary>
        /// Loads the full Project → Scenes → Shots graph fresh from the
        /// database (SelectedProject alone doesn't carry that graph),
        /// builds the complete shot list text, and hands it to the View
        /// to save - same event-based split as CopyPrompt, since writing
        /// a file is also a platform concern the ViewModel shouldn't own.
        /// </summary>
        [RelayCommand(CanExecute = nameof(CanExportProject))]
        private async Task ExportProjectAsync()
        {
            if (SelectedProject is null) return;

            var fullProject = await _projectRepository.GetByIdAsync(SelectedProject.Id);
            if (fullProject is null) return;

            var textContent = ProjectExportService.BuildProjectText(fullProject, CurrentPromptOptions);
            var jsonContent = ProjectExportService.BuildProjectJson(fullProject, CurrentPromptOptions);
            var fileNameBase = ProjectExportService.SuggestedFileNameBase(fullProject);

            ExportRequested?.Invoke(fileNameBase, textContent, jsonContent);
        }

        /// <summary>Managing characters requires a project to attach them to.</summary>
        private bool CanManageCharacters() => SelectedProject is not null;

        [RelayCommand(CanExecute = nameof(CanManageCharacters))]
        private void ManageCharacters()
        {
            if (SelectedProject is null) return;
            ManageCharactersRequested?.Invoke(SelectedProject.Id, SelectedProject.Name);
        }
    }
}
