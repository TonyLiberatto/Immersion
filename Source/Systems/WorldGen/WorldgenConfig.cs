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

        public override void StartServerSide(ICoreServerAPI api)
        {
            string wPath = Path.Combine("immersion", "worldgen.json");

            try
            {
                ImmersionWorldgenConfig config = api.LoadModConfig<ImmersionWorldgenConfig>(wPath);

                GenAquifers = genAquifers = config?.genAquifers ?? true;
                GenRivers = genRivers = config?.genRivers ?? true;
                GenPalms = genPalms = config?.genPalms ?? true;
                GenDeepOreBits = genDeepOreBits = config?.genDeepOreBits ?? true;
            }
            catch (Exception)
            {
            }

            api.StoreModConfig(this, wPath);
        }
    }
}
