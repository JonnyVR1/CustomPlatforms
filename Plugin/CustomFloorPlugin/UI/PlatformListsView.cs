using System.Collections.Generic;

using BeatSaberMarkupLanguage.Attributes;
using BeatSaberMarkupLanguage.Components;
using BeatSaberMarkupLanguage.ViewControllers;

using CustomFloorPlugin.Configuration;

using HMUI;

using Zenject;


namespace CustomFloorPlugin.UI
{
    /// <summary>
    /// A <see cref="BSMLAutomaticViewController"/> generated by Zenject and maintained by BSML at runtime.<br/>
    /// BSML uses the <see cref="ViewDefinitionAttribute"/> to determine the Layout of the GameObjects and their Components<br/>
    /// Tagged functions and variables from this class may be used/called by BSML if the .bsml file mentions them.<br/>
    /// </summary>
    [ViewDefinition("CustomFloorPlugin.Views.PlatformLists.bsml")]
    [HotReload(RelativePathToLayout = "CustomFloorPlugin/Views/PlatformLists.bsml")]
    internal class PlatformListsView : BSMLAutomaticViewController
    {
        private PluginConfig? _config;
        private AssetLoader? _assetLoader;
        private PlatformManager? _platformManager;
        private PlatformSpawner? _platformSpawner;

        /// <summary>
        /// The table of currently loaded Platforms for singleplayer
        /// </summary>
        [UIComponent("singleplayer-platforms-list")]
        private readonly CustomListTableData? _singleplayerPlatformListTable = null;

        /// <summary>
        /// The table of currently loaded Platforms for multiplayer
        /// </summary>
        [UIComponent("multiplayer-platforms-list")]
        private readonly CustomListTableData? _multiplayerPlatformListTable = null;

        /// <summary>
        /// The table of currently loaded Platforms for multiplayer
        /// </summary>
        [UIComponent("a360-platforms-list")]
        private readonly CustomListTableData? _a360PlatformListTable = null;

        private CustomListTableData[]? _allListTables;
        private PlatformType _selectedPlatformType;

        /// <summary>
        /// Indicates whether the loading symbol should be shown or not
        /// </summary>
        [UIValue("loading-indicator-active")]
        public bool LoadingIndicatorActive
        {
            get => _loadingIndicatorActive;
            set
            {
                _loadingIndicatorActive = value;
                NotifyPropertyChanged();
            }
        }
        private bool _loadingIndicatorActive = true;

        [Inject]
        public void Construct(PluginConfig config, AssetLoader assetLoader, PlatformSpawner platformSpawner, PlatformManager platformManager)
        {
            _config = config;
            _assetLoader = assetLoader;
            _platformManager = platformManager;
            _platformSpawner = platformSpawner;
        }

        [UIAction("select-cell")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private async void TabSelect(SegmentedControl segmentedControl, int _1)
        {
            _selectedPlatformType = (PlatformType)segmentedControl.selectedCellNumber;
            int index = await _platformManager!.GetIndexForTypeAsync(_selectedPlatformType);
            await _platformSpawner!.ChangeToPlatformAsync(index);
            _allListTables![segmentedControl.selectedCellNumber].tableView.ScrollToCellWithIdx(index, TableView.ScrollPositionType.Center, true);
            _allListTables![segmentedControl.selectedCellNumber].tableView.SelectCellWithIdx(index);
        }

        /// <summary>
        /// Called when a <see cref="CustomPlatform"/> is selected by the user
        /// </summary>
        /// <param name="_">I love how optimised BSML is</param>
        /// <param name="idx">Cell index of the users selection</param>
        [UIAction("platform-select")]
        // ReSharper disable once UnusedMember.Local
        // ReSharper disable once UnusedParameter.Local
        private async void PlatformSelect(TableView _, int idx)
        {
            await _platformSpawner!.SetPlatformAndShowAsync(idx, _selectedPlatformType);
        }

        /// <summary>
        /// Changing to the current platform when the menu is shown<br/>
        /// [Called by Beat Saber]
        /// </summary>
        protected override async void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            int platformIndex = await _platformManager!.GetIndexForTypeAsync(_selectedPlatformType);
            await _platformSpawner!.ChangeToPlatformAsync(platformIndex);
        }

        /// <summary>
        /// Swapping back to the standard menu environment or to the selected singleplayer platform<br/>
        /// when the menu is closed<br/>
        /// [Called by Beat Saber]
        /// </summary>
        protected override async void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            int platformIndex = 0;
            if (_config!.ShowInMenu)
            {
                platformIndex = _config.ShufflePlatforms
                    ? await _platformSpawner!.GetRandomPlatformIndexAsync()
                    : await _platformManager!.GetIndexForTypeAsync(PlatformType.Singleplayer);
            }

            await _platformSpawner!.ChangeToPlatformAsync(platformIndex);
        }

        /// <summary>
        /// (Re-)Loading the tables for the ListView of available platforms<br/>
        /// [Called by BSML]
        /// </summary>
        [UIAction("#post-parse")]
        // ReSharper disable once UnusedMember.Local
        private async void PostParse()
        {
            List<CustomPlatform> allPlatforms = await _platformManager!.PlatformsLoadingTask;
            allPlatforms.Sort(1, allPlatforms.Count - 1, null);
            _allListTables = new[] { _singleplayerPlatformListTable!, _multiplayerPlatformListTable!, _a360PlatformListTable! };
            LoadingIndicatorActive = false;
            foreach (CustomPlatform platform in allPlatforms)
                AddCellForPlatform(platform, false);
            for (int i = 0; i < _allListTables.Length; i++)
            {
                _allListTables[i].tableView.ReloadData();
                int idx = await _platformManager.GetIndexForTypeAsync((PlatformType)i);
                _allListTables[i].tableView.ScrollToCellWithIdx(idx, TableView.ScrollPositionType.Center, true);
                _allListTables[i].tableView.SelectCellWithIdx(idx);
            }
        }

        internal void AddCellForPlatform(CustomPlatform platform, bool forceReload)
        {
            if (_allListTables == null) return;
            CustomListTableData.CustomCellInfo cell = new(platform.platName, platform.platAuthor, platform.icon ? platform.icon : _assetLoader!.FallbackCover);
            foreach (CustomListTableData listTable in _allListTables)
            {
                listTable.data.Add(cell);
                if (forceReload)
                    listTable.tableView.ReloadData();
            }
        }

        internal async void RemoveCellForPlatform(CustomPlatform platform)
        {
            if (_allListTables == null) return;
            List<CustomPlatform> allPlatforms = await _platformManager!.PlatformsLoadingTask;
            int platformIndex = allPlatforms.IndexOf(platform);
            for (int i = 0; i < _allListTables.Length; i++)
            {
                _allListTables[i].data.RemoveAt(platformIndex);
                _allListTables[i].tableView.ReloadData();
                if (await _platformManager!.GetIndexForTypeAsync((PlatformType)i) == platformIndex)
                {
                    _allListTables[i].tableView.SelectCellWithIdx(0);
                    _allListTables[i].tableView.ScrollToCellWithIdx(0, TableView.ScrollPositionType.Center, true);
                }
            }
        }
    }
}