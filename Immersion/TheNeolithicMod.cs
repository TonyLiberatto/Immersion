using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory;
using Vintagestory.Client;
using System;
using System.IO;
using Vintagestory.API.Server;
using System.Collections.Generic;

[assembly: ModInfo("The Neolithic Mod",
    Description  = "This mod Requires New World Creation. Adds more Animals, Plants, blocks and tools",
    Website      = "https://github.com/TonyLiberatto/The-Neolithic-Mod",
    Authors      = new []{ "Tony Liberatto","Novocain","Balduranne","BunnyViking" },
    Contributors = new []{ "Tyron", "Milo", "Stroam", "Elwood", "copygirl", "MarcAFK", "Balduranne", "Demmon1" })]

namespace Neolithic
{
    public partial class TheNeolithicMod : ModSystem
    {
        ICoreAPI api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
        }

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            api.Event.BlockTexturesLoaded += ReloadTextures;
        }

        public void ReloadTextures()
        {
            if (capi.Settings.Int["maxTextureAtlasSize"] < 4096)
            {
                capi.Settings.Int["maxTextureAtlasSize"] = 4096;
            }
        }

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            RegisterClasses();
        }
    }
}