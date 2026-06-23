namespace GameHelper.Plugin
{
    using System;
    using System.Collections.Generic;

    /// <summary>Upstream authors and source links for bundled plugins.</summary>
    internal static class PluginCredits
    {
        private static readonly Dictionary<string, PluginCreditInfo> Credits = new(StringComparer.OrdinalIgnoreCase)
        {
            ["AutoHotKeyTrigger"] = new("Gordin", "GameHelper2 upstream", "Configurable hotkey triggers."),
            ["Radar"] = new("Gordin", "GameHelper2 upstream", "Radar overlay for entities and map awareness."),
            ["HealthBars"] = new("Gordin", "GameHelper2 upstream", "Custom health bars on screen."),
            ["PreloadAlert"] = new("Gordin", "GameHelper2 upstream", "Alerts for preloaded map areas."),
            ["LootValue"] = new("Gordin", "GameHelper2 upstream", "Loot value overlay from poe.ninja."),
            ["Atlas"] = new("yokkenUA", "yokkenUA/Atlas", "Endgame atlas overlay."),
            ["LootTracker"] = new("yokkenUA", "yokkenUA/LootTracker", "Session loot tracking overlay."),
            ["RunecraftHelper"] = new("yokkenUA", "yokkenUA/RunecraftHelper", "Runeshape prices in Runecraft UI."),
            ["SekhemaHelper"] = new("yokkenUA", "yokkenUA/SekhemaHelper", "Sekhema trial path helper."),
            ["RitualHelper"] = new("caio", "MordWraith/RitualHelper port", "Ritual reward prices in Ritual panel."),
            ["Autopot"] = new("MordWraith", "MordWraith/Autopot", "Automatic flask usage and safety logout."),
            ["AuraTracker"] = new("Skrip", "MordWraith/AuraTracker port", "Nearby enemies: HP/ES, buffs, DPS."),
            ["AmanamuVoidAlert"] = new("1k4ru5g3", "MordWraith/AmanamuVoidAlert port", "Abyss / Amanamu void cloud tracker."),
            ["PlayerBuffBar"] = new("MordWraith", "MordWraith/PlayerBuffBar", "Player buff watchlist overlay."),
            ["Hiveblood"] = new("MordWraith", "MordWraith/Hiveblood", "Genesis Tree Hiveblood tracker."),
            ["SimpleBars"] = new("Reynbow", "MordWraith/SimpleBars port", "Lightweight on-screen health bars."),
        };

        internal static string GetOriginalAuthor(string pluginName) =>
            Credits.TryGetValue(pluginName, out var credit) ? credit.Author : "upstream";

        internal static string GetCatalogDescription(string pluginName) =>
            Credits.TryGetValue(pluginName, out var credit) ? credit.Description : string.Empty;

        internal static string GetSourceUrl(string pluginName)
        {
            var fromJson = PluginSources.GetSourceUrl(pluginName);
            return !string.IsNullOrWhiteSpace(fromJson) ? fromJson : string.Empty;
        }

        private readonly record struct PluginCreditInfo(string Author, string Notes, string Description);
    }
}
