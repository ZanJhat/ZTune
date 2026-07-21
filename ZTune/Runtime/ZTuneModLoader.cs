using Engine;
using System;
using System.Collections.Generic;

namespace Game
{
    public class ZTuneModLoader : ModLoader
    {
        public override void __ModInitialize()
        {
            ModsManager.RegisterHook("OnLoadingFinished", this);
        }

        public override void OnLoadingFinished(List<Action> actions)
        {
            actions.Add(() =>
            {
                ZTuneManager.Initialize();
            });
        }
    }
}
