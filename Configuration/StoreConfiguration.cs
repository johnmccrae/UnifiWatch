namespace UnifiWatch.Configuration;

public static class StoreConfiguration
{
    public static readonly Dictionary<string, string> ModernStores = new()
    {
        { "Europe", "eu" },
        { "USA", "us" },
        { "UK", "uk" }
    };

    public static readonly Dictionary<string, string> ModernStoreLinks = new()
    {
        { "Europe", "https://eu.store.ui.com/eu/en" },
        { "USA", "https://store.ui.com/us/en" },
        { "UK", "https://uk.store.ui.com/uk/en" }
    };

    public static readonly Dictionary<string, string> LegacyStores = new()
    {
        { "Brazil", "https://br.store.ui.com" },
        { "India", "https://store-ui.in" },
        { "Japan", "https://jp.store.ui.com" },
        { "Taiwan", "https://tw.store.ui.com" },
        { "Singapore", "https://sg.store.ui.com" },
        { "Mexico", "https://mx.store.ui.com" },
        { "China", "https://store.ui.com.cn" }
    };

    public static readonly Dictionary<string, string> LegacyCollections = new()
    {
        { "Protect", "unifi-protect" },
        { "ProtectNVR", "unifi-protect-nvr" },
        { "ProtectAccessories", "unifi-protect-accessories" },
        { "NetworkOS", "unifi-network-unifi-os-consoles" },
        { "NetworkRoutingSwitching", "unifi-network-routing-switching" },
        { "NetworkSmartPower", "unifi-network-smartpower" },
        { "NetworkRoutingOffload", "unifi-network-routing-offload" },
        { "NetworkHost", "unifi-network-host" },
        { "NetworkSwitching", "unifi-network-switching" },
        { "NetworkWifi", "unifi-network-wireless" },
        { "UnifiAccessories", "unifi-accessories" },
        { "EarlyAccess", "early-access" },
        { "EarlyAccessDoorAccess", "early-access-door-access" },
        { "EarlyAccessConnect", "early-access-connect" },
        { "EarlyAccessSmartpower", "early-access-smartpower" },
        { "EarlyAccessUispFiber", "early-access-uisp-fiber" },
        { "EarlyAccessUispWired", "early-access-uisp-wired" },
        { "EarlyAccessUispWireless", "early-access-uisp-wireless" },
        { "EarlyAccessUnifiNetworkHost", "early-access-unifi-network-host" },
        { "UnifiConnect", "unifi-connect" },
        { "UnifiDoorAccess", "unifi-door-access" },
        { "OperatorAirmaxAndLtu", "operator-airmax-and-ltu" },
        { "OperatorIspInfrastructure", "operator-isp-infrastructure" },
        { "UnifiPhoneSystem", "unifi-phone-system" }
    };

    // Mapping from collection slugs to category names for modern stores
    public static readonly Dictionary<string, string> CollectionToCategory = new()
    {
        // Direct collections
        { "uisp-accessories-cabling", "AccessoriesCabling" },
        { "unifi-accessory-tech-access-point-mounting", "AccessPointMounting" },
        { "unifi-accessory-tech-access-point-skins", "AccessPointSkins" },
        { "unifi-accessory-tech-cable-box", "CableBox" },
        { "unifi-accessory-tech-cable-patch", "CablePatch" },
        { "unifi-accessory-tech-cable-sfp", "CableSFP" },
        { "unifi-accessory-tech-camera-enhancers", "CameraEnhancers" },
        { "unifi-camera-security-bullet-dslr", "CameraSecurityBulletDSLR" },
        { "unifi-camera-security-bullet-high-performance", "CameraSecurityBulletHighPerformance" },
        { "unifi-camera-security-bullet-standard", "CameraSecurityBulletStandard" },
        { "unifi-camera-security-compact-poe-wired", "CameraSecurityCompactPoEWired" },
        { "unifi-camera-security-compact-wifi-connected", "CameraSecurityCompactWiFiConnected" },
        { "unifi-camera-security-dome-360", "CameraSecurityDome360" },
        { "unifi-camera-security-dome-slim", "CameraSecurityDomeSlim" },
        { "unifi-camera-security-door-access-accessories", "CameraSecurityDoorAccessAccessories" },
        { "unifi-camera-security-door-access-readers", "CameraSecurityDoorAccessReaders" },
        { "unifi-camera-security-door-access-starter-kit", "CameraSecurityDoorAccessStarterKit" },
        { "unifi-camera-security-interior-design", "CameraSecurityInteriorDesign" },
        { "unifi-camera-security-nvr-large-scale", "CameraSecurityNVRLargeScale" },
        { "unifi-camera-security-nvr-mid-scale", "CameraSecurityNVRMidScale" },
        { "unifi-camera-security-ptz", "CameraSecurityPTZ" },
        { "unifi-camera-security-special-chime", "CameraSecuritySpecialChime" },
        { "unifi-camera-security-special-sensor", "CameraSecuritySpecialSensor" },
        { "unifi-camera-security-special-viewport", "CameraSecuritySpecialViewport" },
        { "unifi-camera-security-special-wifi-doorbell", "CameraSecuritySpecialWiFiDoorbell" },
        { "unifi-accessory-tech-camera-skins", "CameraSkins" },
        { "unifi-accessory-tech-desktop-stands", "DesktopStands" },
        { "unifi-accessory-tech-device-mounting", "DeviceMounting" },
        { "unifi-dream-machine", "DreamMachine" },
        { "unifi-dream-router", "DreamRouter" },
        { "unifi-accessory-tech-hdd-storage", "HDDStorage" },
        { "unifi-accessory-tech-hosting-and-gateways-cloud", "HostingAndGatewaysCloud" },
        { "unifi-accessory-tech-hosting-and-gateways-large-scale", "HostingAndGatewaysLargeScale" },
        { "unifi-accessory-tech-hosting-and-gateways-small-scale", "HostingAndGatewaysSmallScale" },
        { "unifi-accessory-tech-installations-rackmount", "InstallationsRackmount" },
        { "unifi-internet-backup", "InternetBackup" },
        { "unifi-new-integrations-av-display-mounting", "NewIntegrationsAVDisplayMounting" },
        { "unifi-new-integrations-av-giant-poe-touchscreens", "NewIntegrationsAVGiantPoETouchscreens" },
        { "unifi-new-integrations-phone-ata", "NewIntegrationsPhoneATA" },
        { "unifi-new-integrations-phone-compact", "NewIntegrationsPhoneCompact" },
        { "unifi-new-integrations-phone-executive", "NewIntegrationsPhoneExecutive" },
        { "unifi-accessory-tech-poe-and-power", "PoEAndPower" },
        { "unifi-accessory-tech-poe-power", "PoEPower" },
        { "unifi-power-tech-power-redundancy", "PowerTechPowerRedundancy" },
        { "unifi-power-tech-uninterruptible-poe", "PowerTechUninterruptiblePoE" },
        { "unifi-switching-enterprise-aggregation", "SwitchingEnterpriseAggregation" },
        { "unifi-switching-enterprise-power-over-ethernet", "SwitchingEnterprisePoE" },
        { "unifi-switching-pro-ethernet", "SwitchingProEthernet" },
        { "unifi-switching-pro-power-over-ethernet", "SwitchingProPoE" },
        { "unifi-switching-standard-ethernet", "SwitchingStandardEthernet" },
        { "unifi-switching-standard-power-over-ethernet", "SwitchingStandardPoE" },
        { "unifi-switching-utility-10-gbps-ethernet", "SwitchingUtility10GbpsEthernet" },
        { "unifi-switching-utility-mini", "SwitchingUtilityMini" },
        { "unifi-switching-utility-poe", "SwitchingUtilityPoE" },
        { "unifi-wifi-building-bridge-10-gigabit", "WiFiBuildingBridge10Gigabit" },
        { "unifi-wifi-flagship-compact", "WiFiFlagshipCompact" },
        { "unifi-wifi-flagship-high-capacity", "WiFiFlagshipHighCapacity" },
        { "unifi-wifi-inwall-outlet-mesh", "WiFiInWallOutletMesh" },
        { "unifi-accessory-tech-wifiman", "WiFiMan" },
        { "unifi-wifi-outdoor-flexible", "WiFiOutdoorFlexible" },
        { "unifi-wifi-flagship-long-range", "WiFiFlagshipLongRange" },
        { "unifi-wifi-outdoor-long-range", "WiFiOutdoorLongRange" },
        { "unifi-new-integrations-mobile-routing", "NewIntegrationsMobileRouting" },
        { "unifi-camera-security-door-access-hub", "CameraSecurityDoorAccessHub" },
        { "unifi-new-integrations-av-digital-signage", "NewIntegrationsAVDigitalSignage" },
        { "unifi-switching-utility-industrial", "SwitchingUtilityIndustrial" },
        { "unifi-switching-utility-indoor-outdoor", "SwitchingUtilityIndoorOutdoor" },
        { "unifi-switching-enterprise-10-gbps-ethernet", "SwitchingEnterprise10GbpsEthernet" },
        { "unifi-switching-utility-hi-power-poe", "SwitchingUtilityHiPowerPoE" },
        { "unifi-wifi-mega-capacity", "WiFiMegaCapacity" },
        { "unifi-power-tech-uninterruptible-power", "PowerTechUninterruptiblePower" },
        { "unifi-power-tech-power-distribution", "PowerTechPowerDistribution" },
        { "unifi-camera-security-special-floodlight", "CameraSecuritySpecialFloodlight" },
        { "unifi-dream-wall", "DreamWall" },
        { "unifi-new-integrations-ev-charging", "NewIntegrationsEVCharging" },
        { "cloud-key-rack-mount", "CloudKeyRackMount" },
        { "amplifi-mesh", "AmpliFiMesh" },
        { "amplifi-alien", "AmpliFiAlien" },
        { "unifi-wifi-inwall-compact", "WiFiInWallCompact" },
        { "unifi-wifi-inwall-high-capacity", "WiFiInWallHighCapacity" },
        { "unifi-accessory-tech-access-point-antennas", "AccessPointAntennas" },
        { "unifi-camera-security-bullet-enhanced-ai", "CameraSecurityBulletEnhancedAI" },
        { "unifi-wifi-building-bridge-gigabit", "WiFiBuildingBridgeGigabit" },

        // Organizational collections
        { "accessories-cabling", "Cabling" },
        { "accessory-tech-access-point-mounting", "AccessPointMounting" },
        { "accessory-tech-access-point-skins", "AccessPointSkins" },
        { "accessory-tech-cable-sfp", "CableSFP" },
        { "accessory-tech-camera-enhancers", "CameraEnhancers" },
        { "accessory-tech-camera-skins", "CameraSkins" },
        { "accessory-tech-device-mounting", "DeviceMounting" },
        { "accessory-tech-display-mounting", "DisplayMounting" },
        { "accessory-tech-hosting-and-gateways-cloud", "HostingAndGatewaysCloud" },
        { "accessory-tech-hosting-and-gateways-large-scale", "HostingAndGatewaysLargeScale" },
        { "accessory-tech-hosting-and-gateways-small-scale", "HostingAndGatewaysSmallScale" },
        { "accessory-tech-poe-and-power", "PoEAndPower" },
        { "accessory-tech-poe-power", "PoEPower" },
        { "accessory-tech-wifiman", "WiFiManager" },
        { "internet-backup", "InternetBackup" },
        { "power-tech-power-redundancy", "PowerRedundancy" },
        { "switching-pro-ethernet", "ProEthernetSwitching" },
        { "switching-standard-ethernet", "StandardEthernetSwitching" },
        { "switching-standard-power-over-ethernet", "StandardPoESwitching" },
        { "switching-utility-10-gbps-ethernet", "10GbpsEthernetSwitching" },
        { "switching-utility-poe", "PoESwitching" },
        { "wifi-flagship-compact", "FlagshipCompactWiFi" },
        { "wifi-flagship-high-capacity", "FlagshipHighCapacityWiFi" },
        { "wifi-inwall-high-capacity", "InWallHighCapacityWiFi" },
        { "wifi-outdoor-flexible", "OutdoorFlexibleWiFi" },
        { "ui-care", "UICare" },

        // Accessories - Pro Line
        { "accessories-pro-access-point", "AccessoriesProAccessPoint" },
        { "accessories-pro-rack-mount", "AccessoriesProRackMount" },
        { "accessories-pro-camera", "AccessoriesProCamera" },
        { "accessories-pro-box-cables", "AccessoriesProBoxCables" },
        { "accessories-pro-patch-cables", "AccessoriesProPatchCables" },
        { "accessories-pro-poe-and-power", "AccessoriesProPoEAndPower" },
        { "accessories-pro-storage", "AccessoriesProStorage" },
        { "accessories-pro-single-mode-optical-fiber", "AccessoriesProSingleModeOpticalFiber" },
        { "accessories-pro-multi-mode-optical-fiber", "AccessoriesProMultiModeOpticalFiber" },
        { "accessories-pro-door-access", "AccessoriesProDoorAccess" },
        { "accessories-pro-installations", "AccessoriesProInstallations" },
        { "accessories-pro-direct-attach-cables", "AccessoriesProDirectAttachCables" },
        { "accessories-pro-cwdm", "AccessoriesProCWDM" },
        { "accessories-ethernet-repeater", "AccessoriesEthernetRepeater" },

        // Camera Security - Additional Categories
        { "unifi-camera-security-bullet", "CameraSecurityBullet" },
        { "unifi-camera-security-dome-turret", "CameraSecurityDomeTurret" },
        { "unifi-camera-security-compact", "CameraSecurityCompact" },
        { "unifi-camera-security-door-access-intercoms-viewers", "CameraSecurityDoorAccessIntercoms" },
        { "unifi-camera-security-interior-design-hub", "CameraSecurityInteriorDesignHub" },
        { "unifi-camera-security-interior-design-kit", "CameraSecurityInteriorDesignKit" },
        { "unifi-camera-security-interior-design-lens", "CameraSecurityInteriorDesignLens" },
        { "unifi-camera-security-nvr", "CameraSecurityNVR" },
        { "unifi-camera-security-special", "CameraSecuritySpecial" },

        // WiFi - Additional Categories
        { "unifi-wifi-outdoor", "WiFiOutdoor" },
        { "unifi-wifi-enterprise", "WiFiEnterprise" },
        { "unifi-wifi-flagship", "WiFiFlagship" },
        { "unifi-wifi-inwall", "WiFiInWall" },
        { "unifi-wifi-special-devices", "WiFiSpecialDevices" },
        { "unifi-wifi-building-bridge", "WiFiBuildingBridge" },

        // Switching - Professional and Standard
        { "switching-professional", "SwitchingProfessional" },
        { "switching-enterprise", "SwitchingEnterprise" },
        { "switching-standard", "SwitchingStandard" },
        { "switching-utility", "SwitchingUtility" },

        // Cloud and Internet Solutions
        { "cloud-gateways-wifi-integrated", "CloudGatewaysWiFiIntegrated" },
        { "cloud-gateway-compact", "CloudGatewayCompact" },
        { "enterprise-fortress-gateway", "EnterpriseFortressGateway" },
        { "pro-internet-solutions", "ProInternetSolutions" },

        // Pro Store and Other Pro Categories
        { "pro-store-doorbells-chimes", "ProStoreDoorbellsChimes" },
        { "pro-ai-theta-lenses", "ProAIThetaLenses" },
        { "pro-ai-theta-audio", "ProAIThetaAudio" },

        // UniFi Advanced Features
        { "unifi-switching-pro-max", "SwitchingProMax" },
        { "unifi-switching-wan", "SwitchingWAN" },
        { "unifi-talk-business-voip", "TalkBusinessVoIP" },
        { "unifi-new-integrations-digital-signage", "NewIntegrationsDigitalSignage" },
        { "unifi-new-integrations-network-storage", "NewIntegrationsNetworkStorage" },
        { "unifi-new-integrations-premium-audio", "NewIntegrationsPremiumAudio" },
        { "unifi-cloudkeys-gateways", "CloudKeysGateways" },

        // Add-ons and Special Categories
        { "add-ons-wifi", "AddOnsWiFi" },
        { "apparel", "Apparel" },
        { "power-tech", "PowerTech" },

        // Camera and Mounting Accessories
        { "camera-junction-boxes", "CameraJunctionBoxes" },
        { "g5-ptz-mounts", "G5PTZMounts" },

        // UISP Accessories
        { "uisp-accessory-tech-mounting", "UISPAccessoryTechMounting" },
        { "uisp-accessory-tech-poe-surge-converters", "UISPAccessoryTechPoESurgeConverters" },
        { "uisp-accessory-tech-poe-surge-protect", "UISPAccessoryTechPoESurgeProtect" }
    };
}
