using System.ComponentModel;
using System.Linq;

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
    internal class PlatformListsView : BSMLAutomaticViewController, INotifyPropertyChanged
    {
        private PluginConfig _config;
        private AssetLoader _assetLoader;
        private PlatformManager _platformManager;
        private PlatformSpawner _platformSpawner;

        [Inject]
        public void Construct(PluginConfig config, AssetLoader assetLoader, PlatformSpawner platformSpawner, PlatformManager platformManager)
        {
            _config = config;
            _assetLoader = assetLoader;
            _platformManager = platformManager;
            _platformSpawner = platformSpawner;
        }

        [UIComponent("requirements-modal")]
        internal readonly ModalView requirementsModal;

        /// <summary>
        /// The table of currently loaded Platforms for singleplayer
        /// </summary>
        [UIComponent("singleplayer-platforms-list")]
        internal readonly CustomListTableData singleplayerPlatformListTable;

        /// <summary>
        /// The table of currently loaded Platforms for multiplayer
        /// </summary>
        [UIComponent("multiplayer-platforms-list")]
        internal readonly CustomListTableData multiplayerPlatformListTable;

        /// <summary>
        /// The table of currently loaded Platforms for multiplayer
        /// </summary>
        [UIComponent("a360-platforms-list")]
        internal readonly CustomListTableData a360PlatformListTable;

        /// <summary>
        /// List of requirements or suggestions for the current platform
        /// </summary>
        [UIComponent("requirements-list")]
        internal readonly CustomListTableData requirementsListTable;

        /// <summary>
        /// An <see cref="System.Array"/> holding all <see cref="CustomListTableData"/>s
        /// </summary>
        internal CustomListTableData[] allListTables;

        /// <summary>
        /// Used to hide the button if there's no requirement or suggestion
        /// </summary>
        [UIValue("req-button-active")]
        internal bool ReqButtonActive
        {
            get => _ReqButtonActive;
            set
            {
                _ReqButtonActive = value;
                NotifyPropertyChanged();
            }
        }
        private bool _ReqButtonActive;

        [UIAction("select-cell")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by BSML")]
        private void TabSelect(SegmentedControl segmentedControl, int _1)
        {
            PlatformType type = (PlatformType)segmentedControl.selectedCellNumber;
            int index = _platformManager.GetIndexForType(type);
            singleplayerPlatformListTable.tableView.ScrollToCellWithIdx(index, TableView.ScrollPositionType.Beginning, false);
            UpdateRequirementsForPlatform(_platformManager.allPlatforms[index]);

            if (index != _platformManager.GetIndexForType(_platformManager.currentPlatformType))
                _platformSpawner.ChangeToPlatform(index);
            _platformManager.currentPlatformType = type;
        }

        /// <summary>
        /// Called when a <see cref="CustomPlatform"/> is selected by the user<br/>
        /// </summary>
        /// <param name="_1">I love how optimised BSML is</param>
        /// <param name="idx">Cell index of the users selection</param>
        [UIAction("singleplayer-select")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by BSML")]
        private void SingleplayerSelect(TableView _1, int idx)
        {
            UpdateRequirementsForPlatform(_platformManager.allPlatforms[idx]);
            _platformSpawner.SetPlatformAndShow(idx, PlatformType.Singleplayer);
        }

        /// <summary>
        /// Called when a <see cref="CustomPlatform"/> is selected by the user<br/>
        /// </summary>
        /// <param name="_1">I love how optimised BSML is</param>
        /// <param name="idx">Cell index of the users selection</param>
        [UIAction("multiplayer-select")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by BSML")]
        private void MultiplayerSelect(TableView _1, int idx)
        {
            UpdateRequirementsForPlatform(_platformManager.allPlatforms[idx]);
            _platformSpawner.SetPlatformAndShow(idx, PlatformType.Multiplayer);
        }

        /// <summary>
        /// Called when a <see cref="CustomPlatform"/> is selected by the user<br/>
        /// </summary>
        /// <param name="_1">I love how optimised BSML is</param>
        /// <param name="idx">Cell index of the users selection</param>
        [UIAction("a360-select")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by BSML")]
        private void A360Select(TableView _1, int idx)
        {
            UpdateRequirementsForPlatform(_platformManager.allPlatforms[idx]);
            _platformSpawner.SetPlatformAndShow(idx, PlatformType.A360);
        }

        /// <summary>
        /// Changing to the current platform when the menu is shown<br/>
        /// [Called by Beat Saber]
        /// </summary>
        protected override void DidActivate(bool firstActivation, bool addedToHierarchy, bool screenSystemEnabling)
        {
            base.DidActivate(firstActivation, addedToHierarchy, screenSystemEnabling);
            int platformIndex = _platformManager.GetIndexForType(_platformManager.currentPlatformType);
            int tableIndex = (int)_platformManager.currentPlatformType;
            allListTables[tableIndex].tableView.ScrollToCellWithIdx(platformIndex, TableView.ScrollPositionType.Beginning, false);
            UpdateRequirementsForPlatform(_platformManager.allPlatforms[platformIndex]);
            _platformSpawner.ChangeToPlatform(platformIndex);
        }

        /// <summary>
        /// Swapping back to the standard menu environment when the menu is closed<br/>
        /// [Called by Beat Saber]
        /// </summary>
        protected override void DidDeactivate(bool removedFromHierarchy, bool screenSystemDisabling)
        {
            base.DidDeactivate(removedFromHierarchy, screenSystemDisabling);
            requirementsModal.gameObject.SetActive(false);
            int platformIndex = 0;
            if (_config.ShowInMenu)
                platformIndex = _config.ShufflePlatforms
                ? _platformSpawner.RandomPlatformIndex
                : _platformManager.GetIndexForType(PlatformType.Singleplayer);
            _platformSpawner.ChangeToPlatform(platformIndex);
        }

        /// <summary>
        /// (Re-)Loading the tables for the ListView of available platforms.<br/>
        /// [Called by BSML]
        /// </summary>
        [UIAction("#post-parse")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("CodeQuality", "IDE0051:Remove unused private members", Justification = "Called by BSML")]
        private void SetupLists()
        {
            _platformManager.allPlatforms.Sort(1, _platformManager.allPlatforms.Count - 1, null);
            allListTables = new CustomListTableData[] { singleplayerPlatformListTable, multiplayerPlatformListTable, a360PlatformListTable };
            foreach (CustomPlatform platform in _platformManager.allPlatforms)
            {
                CustomListTableData.CustomCellInfo cell = new(platform.platName, platform.platAuthor, platform.icon);
                foreach (CustomListTableData listTable in allListTables)
                    listTable.data.Add(cell);
            }
            for (int i = 0; i < allListTables.Length; i++)
            {
                allListTables[i].tableView.ReloadData();
                int idx = _platformManager.GetIndexForType((PlatformType)i);
                if (!allListTables[i].tableView.visibleCells.Any(x => x.selected))
                    allListTables[i].tableView.ScrollToCellWithIdx(idx, TableView.ScrollPositionType.Beginning, false);
                allListTables[i].tableView.SelectCellWithIdx(idx);
            }
        }

        private void UpdateRequirementsForPlatform(CustomPlatform platform)
        {
            if (platform.requirements.Count == 0 && platform.suggestions.Count == 0)
            {
                ReqButtonActive = false;
                return;
            }

            ReqButtonActive = true;
            requirementsListTable.data.Clear();
            foreach (string req in platform.requirements)
            {
                CustomListTableData.CustomCellInfo cell = _platformManager.allPluginNames.Contains(req)
                    ? new CustomListTableData.CustomCellInfo(req, "Required", _assetLoader.greenCheck)
                    : new CustomListTableData.CustomCellInfo(req, "Required", _assetLoader.redX);
                requirementsListTable.data.Add(cell);
            }
            foreach (string sug in platform.suggestions)
            {
                CustomListTableData.CustomCellInfo cell = _platformManager.allPluginNames.Contains(sug)
                    ? new CustomListTableData.CustomCellInfo(sug, "Suggestion", _assetLoader.yellowCheck)
                    : new CustomListTableData.CustomCellInfo(sug, "Suggestion", _assetLoader.yellowX);
                requirementsListTable.data.Add(cell);
            }
            requirementsListTable.tableView.ReloadData();
        }
    }
}