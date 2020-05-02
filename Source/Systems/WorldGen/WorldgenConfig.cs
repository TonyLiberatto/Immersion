using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Common;
using Vintagestory.API.Server;
using Newtonsoft.Json;
using System.IO;

namespace Immersion
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    class ImmersionWorldgenConfig : ModSystem
    {
        [JsonProperty]
        public bool genAquifers { get; set; } = true;
        [JsonProperty]
        public bool genRivers { get; set; } = true;
        [JsonProperty]
        public bool genPalms { get; set; } = true;
        [JsonProperty]
        public bool genDeepOreBits { get; set; } = true;

        public static bool GenAquifers { get; set; } = true;
        public static bool GenRivers { get; set; } = true;
        public static bool GenPalms { get; set; } = true;
        public static bool GenDeepOreBits { get; set; } = true;


        public override double ExecuteOrder() => 0;
        public override bool ShouldLoad(EnumAppSide forSide) => forSide == EnumAppSide.Server;
        ICoreServerAPI sapi;

        public override void StartServerSide(ICoreServerAPI api)
        {
            this.sapi = api;
            GetWorldConfig();
            api.Event.GameWorldSave += SaveWorldConfig;
            sapi.RegisterCommand("immersionconfig", "Immersion Config", "", (player, group, args) =>
            {
                string word = args.PopWord();
                bool? state = args.PopBool();
                bool saveGlobal = args.PopBool() ?? false;
                switch (word)
                {
                    case "aquifers":
                        sapi.World.Config.SetBool("genAquifers", GenAquifers = genAquifers = state ?? !GenAquifers);
                        player.SendMessage(0, string.Format("Worldgen parameter {0} set to {1}", word, GenAquifers), EnumChatType.OwnMessage);
                        break;
                    case "rivers":
                        sapi.World.Config.SetBool("genRivers", GenRivers = genRivers = state ?? !GenRivers);
                        player.SendMessage(0, string.Format("Worldgen parameter {0} set to {1}", word, GenRivers), EnumChatType.OwnMessage);
                        break;
                    case "palms":
                        sapi.World.Config.SetBool("genPalms", GenPalms = genPalms = state ?? !GenPalms);
                        player.SendMessage(0, string.Format("Worldgen parameter {0} set to {1}", word, GenPalms), EnumChatType.OwnMessage);
                        break;
                    case "deeporebits":
                        sapi.World.Config.SetBool("genDeepOreBits", GenDeepOreBits = genDeepOreBits = state ?? !GenDeepOreBits);
                        player.SendMessage(0, string.Format("Worldgen parameter {0} set to {1}", word, GenDeepOreBits), EnumChatType.OwnMessage);
                        break;
                    default:
                        break;
                }
                if (saveGlobal)
                {
                    string wPath = Path.Combine("immersion", "worldgen.json");
                    sapi.StoreModConfig(this, wPath);
                }
                SaveWorldConfig();
            }, Privilege.controlserver);
        }

        public void GetWorldConfig()
        {
            byte[] configBytes = (sapi.WorldManager.SaveGame.GetData("ImmersionWorldGen"));
            ImmersionWorldgenConfig storedConfig = null;
            if (configBytes != null) storedConfig = JsonUtil.FromBytes<ImmersionWorldgenConfig>(configBytes);

            string wPath = Path.Combine("immersion", "worldgen.json");
            try
            {
                ImmersionWorldgenConfig config = sapi.LoadModConfig<ImmersionWorldgenConfig>(wPath);

                GenAquifers = genAquifers = config?.genAquifers ?? true;
                GenRivers = genRivers = config?.genRivers ?? true;
                GenPalms = genPalms = config?.genPalms ?? true;
                GenDeepOreBits = genDeepOreBits = config?.genDeepOreBits ?? true;
            }
            catch (Exception)
            {
            }

            sapi.StoreModConfig(this, wPath);

            GenAquifers = genAquifers = sapi.World.Config.TryGetBool("genAquifers") ?? storedConfig?.genAquifers ?? true;
            GenRivers = genRivers = sapi.World.Config.TryGetBool("genRivers") ?? storedConfig?.genRivers ?? true;
            GenPalms = genPalms = sapi.World.Config.TryGetBool("genPalms") ?? storedConfig?.genPalms ?? true;
            GenDeepOreBits = genDeepOreBits = sapi.World.Config.TryGetBool("genDeepOreBits") ?? storedConfig?.genDeepOreBits ?? true;

            SaveWorldConfig();
        }

        public void SaveWorldConfig() => sapi.WorldManager.SaveGame.StoreData("ImmersionWorldGen", JsonUtil.ToBytes(this));
    }
}
