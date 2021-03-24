﻿using System;

using CustomFloorPlugin.Configuration;

using UnityEngine;

using Zenject;


namespace CustomFloorPlugin
{
    internal class MultiplayerLobbyHelper : IInitializable, IDisposable
    {
        private readonly PluginConfig _config;
        private readonly AssetLoader _assetLoader;
        private readonly PlatformManager _platformManager;
        private readonly PlatformSpawner _platformSpawner;
        private readonly IMultiplayerSessionManager _multiplayerSessionManager;

        public MultiplayerLobbyHelper(PluginConfig config,
                                      AssetLoader assetLoader,
                                      PlatformManager platformManager,
                                      PlatformSpawner platformSpawner,
                                      IMultiplayerSessionManager multiplayerSessionManager)
        {
            _config = config;
            _assetLoader = assetLoader;
            _platformManager = platformManager;
            _platformSpawner = platformSpawner;
            _multiplayerSessionManager = multiplayerSessionManager;
        }

        public void Initialize()
        {
            _multiplayerSessionManager.connectedEvent += HandleConnected;
            _multiplayerSessionManager.disconnectedEvent += HandleDisconnected;
        }

        public void Dispose()
        {
            _multiplayerSessionManager.connectedEvent -= HandleConnected;
            _multiplayerSessionManager.disconnectedEvent -= HandleDisconnected;
        }

        private void HandleConnected()
        {
            _platformSpawner.isMultiplayer = true;
            _assetLoader.heart.SetActive(false);
            _platformSpawner.ChangeToPlatform(0);
        }

        private void HandleDisconnected(DisconnectedReason reason)
        {
            _platformSpawner.isMultiplayer = false;
            _assetLoader.heart.SetActive(_config.ShowHeart);
            _assetLoader.heart.GetComponent<InstancedMaterialLightWithId>().ColorWasSet(Color.magenta);
            if (_config.ShowInMenu)
            {
                int platformIndex = _config.ShufflePlatforms
                    ? _platformSpawner.RandomPlatformIndex
                    : _platformManager.GetIndexForType(PlatformType.Singleplayer);
                _platformSpawner.ChangeToPlatform(platformIndex);
            }
        }
    }
}