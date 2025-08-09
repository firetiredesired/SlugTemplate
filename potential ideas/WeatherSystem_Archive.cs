// Weather-based Forecast slugcat system - ARCHIVED
// This was the original weather-sensing system that gave speed/jump buffs based on rain timing

using System;
using BepInEx;
using UnityEngine;

namespace BountyHunter
{
    // --- Weather-Based Forecast Buff State ---
    private bool preRainBuffActive = false;
    private bool rainBuffActive = false;
    // You can adjust these thresholds and multipliers as needed
    private const int PreRainThreshold = 30 * 40; // 30 seconds left (40 ticks per second)
    private const float PreRainSpeedMult = 3.0f; // Extreme for testing
    private const float PreRainJumpMult = 2.5f;  // Extreme for testing
    private const float RainSpeedMult = 6.0f;    // Extreme for testing
    private const float RainJumpMult = 5.0f;     // Extreme for testing

    // Helper: Detect if rain has started (timer <= 0)
    private bool IsRainActive(RainCycle rainCycle)
    {
        if (rainCycle == null) return false;
        return rainCycle.timer <= 0;
    }

    private void Forecast_PlayerUpdate(On.Player.orig_Update orig, Player self, bool eu)
    {
        orig(self, eu);

        // Only apply to bountyhunter slugcat (matches SlugBase id)
        if (self.slugcatStats.name.value != "bountyhunter") return;

        // TEST: Always apply running speed buff for testing
        ApplyForecastBuff(self, PreRainSpeedMult, 1f); // Only speed buff, normal jump
        Debug.Log($"Forecast: Running speed buff applied! Speed: {PreRainSpeedMult}");
       

        // Defensive: check for nulls
        if (self.room == null || self.room.world == null || self.room.world.rainCycle == null) return;

        var rainCycle = self.room.world.rainCycle;
        int timer = rainCycle.timer;
        int cycleLength = rainCycle.cycleLength;
        // Stage 1: Pre-rain buff (when timer is below threshold, but rain hasn't started)
        if (!preRainBuffActive && timer <= PreRainThreshold && !IsRainActive(rainCycle))
        {
            preRainBuffActive = true;
            rainBuffActive = false;
            ApplyForecastBuff(self, PreRainSpeedMult, PreRainJumpMult);
            Debug.Log("Forecast: Pre-rain buff applied!");
        }
        // Stage 2: Rain buff (when rain starts)
        else if (!rainBuffActive && IsRainActive(rainCycle))
        {
            rainBuffActive = true;
            preRainBuffActive = false;
            ApplyForecastBuff(self, RainSpeedMult, RainJumpMult);
            Debug.Log("Forecast: RAIN buff applied!");
        }
        // Reset buffs if cycle restarts (timer resets)
        else if (timer > PreRainThreshold && (preRainBuffActive || rainBuffActive))
        {
            preRainBuffActive = false;
            rainBuffActive = false;
            ApplyForecastBuff(self, 1f, 1f); // Reset to normal
            Debug.Log("Forecast: Buffs reset.");
        }
    }

    private void ApplyForecastBuff(Player self, float speedMult, float jumpMult)
    {
        // Adjust run speed
        self.slugcatStats.runspeedFac = speedMult;
        // Adjust jump by setting a multiplier for jumpBoost (applied in Update)
        self.jumpBoost = jumpMult;
        // You can add more stat changes here if needed
    }
}
