using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using BepInEx.Configuration;

namespace COM3D25_OpenXRHandsPOC2
{
    internal class PluginConfig
    {
        public ConfigEntry<bool> enableHandTracking;

        public ConfigEntry<bool> enableSmoothing;
        public ConfigEntry<float> smoothingMinCutoff;
        public ConfigEntry<float> smoothingBeta;

        public PluginConfig(ConfigFile config)
        {
            var instance = COM3D25_OpenXRHandsPOC2Plugin.Instance;
            enableHandTracking = config.Bind("General", "EnableHandTracking", true, new ConfigDescription("Enable hand tracking"));


            enableSmoothing = config.Bind("Smoothing", "EnableSmoothing", true, new ConfigDescription("Enable OneEuro smoothing for aim position tracking"));

            smoothingMinCutoff = config.Bind("Smoothing", "MinCutoff", 0.1f, new ConfigDescription("Controls the amount of smoothing at low speeds. A smaller value will introduce more smoothing and potential lag, helping to reduce low-frequency jitter. A larger value may feel more responsive but can let through more jitter."));

            smoothingBeta = config.Bind("Smoothing", "Beta", 0.2f, new ConfigDescription("Determines the filter's adjustment to speed changes. A smaller value provides consistent smoothing, while a larger one introduces more aggressive adjustments for speed changes, offering responsive filtering at high speeds."));
        }
    }
}
