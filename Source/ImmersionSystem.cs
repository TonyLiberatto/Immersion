using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory;
using Vintagestory.Client;
using System;
using System.IO;
using Vintagestory.API.Server;
using System.Collections.Generic;
using Vintagestory.API.MathTools;
using Vintagestory.Server;
using System.Linq;

[assembly: ModInfo("Immersion",
    Description  = "This mod Requires New World Creation. Adds more Animals, Plants, blocks and tools",
    Website      = "https://github.com/TonyLiberatto/Immersion",
    Authors      = new []{ "Tony Liberatto","Novocain","Balduranne","BunnyViking" },
    Contributors = new []{ "Tyron", "Milo", "Stroam", "Elwood", "copygirl", "MarcAFK", "Balduranne", "Demmon1" })]

namespace Immersion
{
    public partial class ImmersionSystem : ModSystem
    {
        ICoreAPI Api;
        ICoreClientAPI capi;
        ICoreServerAPI sapi;

        public override void StartServerSide(ICoreServerAPI Api)
        {
            sapi = Api;
            sapi.Event.PlayerJoin += (player) =>
            {
                ServerMain server = (sapi.World as ServerMain);
                var PlayerGroups = server.GetField<ServerMain, ServerSystem[]>("Systems").OfType<ServerySystemPlayerGroups>().Single();
                var serverAdmin = (object)(new ServerPlayer(server, ServerWorldPlayerData.CreateNew("!!!ImmersionAdmin", "!!!ImmersionAdmin")));
                if (!PlayerGroups.PlayerGroupsByUid.Any(g => g.Value.Name == "suplayer"))
                {
                    PlayerGroups.CallMethod("CmdCreategroup", (object)(serverAdmin), (object)0, new CmdArgs("suplayer"));
                }
                var group = PlayerGroups.PlayerGroupsByUid.Where(t => t.Value.Name == "suplayer").Single().Value;
                if (player.Privileges.Contains(Privilege.ban))
                {
                    server.PlayerDataManager.PlayerDataByUid[player.PlayerUID].JoinGroup(group, EnumPlayerGroupMemberShip.Op);
                }
                else
                {
                    server.PlayerDataManager.PlayerDataByUid[player.PlayerUID].JoinGroup(group, EnumPlayerGroupMemberShip.Member);
                }

                server.PlayerDataManager.playerDataDirty = true;
                server.PlayerDataManager.playerGroupsDirty = true;
            };

            sapi.RegisterCommand("setmaterial", "Set the material of currently looked at chiseled block", "[block code]", (player, groupId, args) =>
            {
                BlockPos pos = player.CurrentBlockSelection.Position;
                BlockEntityChisel bet = sapi.World.BlockAccessor.GetBlockEntity(pos) as BlockEntityChisel;
                Block block = sapi.World.GetBlock(new AssetLocation(args.PopWord()));

                if (bet == null)
                {
                    player.SendMessage(groupId, "Not looking at a chiseled block. Must look at one to set its material", EnumChatType.CommandError);
                    return;
                }
                if (block == null)
                {
                    player.SendMessage(groupId, "Did not supply a valid block code", EnumChatType.CommandError);
                    return;
                }

                bet.MaterialIds = new int[] { block.Id };
                bet.MarkDirty(true);
                player.SendMessage(groupId, "Material set.", EnumChatType.CommandError);
            }
            , Privilege.controlserver);
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