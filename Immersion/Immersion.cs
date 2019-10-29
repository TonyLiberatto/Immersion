using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory;
using Vintagestory.Client;
using System;
using System.IO;
using Vintagestory.API.Server;
using System.Collections.Generic;

[assembly: ModInfo("Immersion",
    Description  = "This mod Requires New World Creation. Adds more Animals, Plants, blocks and tools",
    Website      = "https://github.com/TonyLiberatto/Immersion",
    Authors      = new []{ "Tony Liberatto","Novocain","Balduranne","BunnyViking" },
    Contributors = new []{ "Tyron", "Milo", "Stroam", "Elwood", "copygirl", "MarcAFK", "Balduranne", "Demmon1" })]

namespace Immersion
{
    public partial class Immersion : ModSystem
    {
        ICoreAPI Api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        public override void StartServerSide(ICoreServerAPI Api)
        {
            sapi = Api;
        }

        public override void StartClientSide(ICoreClientAPI Api)
        {
            capi = Api;
            Api.Event.BlockTexturesLoaded += ReloadTextures;
        }

        public void ReloadTextures()
        {
            if (capi.Settings.Int["maxTextureAtlasSize"] < 4096)
            {
                capi.Settings.Int["maxTextureAtlasSize"] = 4096;
            }
        }

        public override void Start(ICoreAPI Api)
        {
            this.Api = Api;
            RegisterClasses();
        }
    }
}