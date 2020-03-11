using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Common;
using Vintagestory.GameContent;

namespace Immersion
{
    public class TransitionConditions
    {
        public TransitionConditions(int RequiredSunlight)
        {
            this.RequiredSunlight = RequiredSunlight;
        }
        public int RequiredSunlight { get; set; } = -1;
    }

    public class BEBehaviorTransient : BlockEntityBehavior
    {
        double transitionAtHour = -1;
        BlockPos Pos { get => Blockentity.Pos; }
        string fromCode;
        string toCode;
        public Block OwnBlock { get => Blockentity.Block; }
        public TransitionConditions conditions;

        public BEBehaviorTransient(BlockEntity blockentity) : base(blockentity)
        {
        }

        public override void Initialize(ICoreAPI api, JsonObject properties)
        {
            base.Initialize(api, properties);
            prevTime = Api.World.Calendar.TotalHours;
            if (transitionAtHour <= 0)
            {
                transitionAtHour = api.World.Calendar.TotalHours + properties["inGameHours"].AsFloat(24);
            }

            fromCode = properties["convertFrom"].AsString()?.WithDomain(OwnBlock.Code.Domain);
            toCode = properties["convertTo"].AsString()?.WithDomain(OwnBlock.Code.Domain);
            conditions = properties["transitionConditions"]?.AsObject<TransitionConditions>(null);

            if (fromCode != null && toCode != null)
            {
                Blockentity.RegisterGameTickListener(CheckTransition, api.Side.IsClient() ? 30 : 2000);
            }
            else api.World.BlockAccessor.RemoveBlockEntity(Pos);
        }

        double prevTime;

        public void CheckTransition(float dt)
        {
            int light = Api.World.BlockAccessor.GetLightLevel(Blockentity.Pos, EnumLightLevelType.TimeOfDaySunLight);
            bool running = (Api.World.Calendar as GameCalendar).IsRunning;
            if (!running) return;

            if (light < (conditions?.RequiredSunlight ?? -1))
            {
                transitionAtHour += (Api.World.Calendar.TotalHours - prevTime);
            }

            prevTime = Api.World.Calendar.TotalHours;
            if (Api.Side.IsServer())
            {
                if (prevTime < transitionAtHour) return;

                Block block = Api.World.BlockAccessor.GetBlock(Pos);
                Block tblock;

                if (!toCode.Contains("*"))
                {
                    tblock = Api.World.GetBlock(new AssetLocation(toCode));
                    if (tblock == null) return;

                    Api.World.BlockAccessor.SetBlock(tblock.BlockId, Pos);
                    return;
                }

                AssetLocation blockCode = block.WildCardReplace(
                    new AssetLocation(fromCode),
                    new AssetLocation(toCode)
                );

                tblock = Api.World.GetBlock(blockCode);
                if (tblock == null) return;
                Api.World.BlockAccessor.SetBlock(tblock.BlockId, Pos);
            }
        }

        public override void FromTreeAtributes(ITreeAttribute tree, IWorldAccessor worldForResolving)
        {
            base.FromTreeAtributes(tree, worldForResolving);

            transitionAtHour = tree.GetDouble("transitionAtHour");
        }

        public override void ToTreeAttributes(ITreeAttribute tree)
        {
            base.ToTreeAttributes(tree);

            tree.SetDouble("transitionAtHour", transitionAtHour);
        }

        public override void GetBlockInfo(IPlayer forPlayer, StringBuilder dsc)
        {
            /*
            ICoreClientAPI capi = (Api as ICoreClientAPI);
            GameCalendar cal = (GameCalendar)capi.World.Calendar;
            double hourOfDay = (transitionAtHour % (double)cal.HoursPerDay);
            int fullHour = (int)(hourOfDay);
            string hour = fullHour < 10 ? "0" + fullHour : "" + fullHour;
            int m = (int)(60 * (hourOfDay - fullHour));
            string minute = m < 10 ? "0" + m : "" + m;
            string time = hour + ":" + minute;

            double TotalDays = transitionAtHour / cal.HoursPerDay;
            int DayOfYear = (int)(TotalDays % (double)cal.DaysPerYear);
            int year = 1386 + (int)(TotalDays / (double)cal.DaysPerYear);


            dsc.AppendLine("Transitions at: " + time + " on " + DayOfYear + "/" + cal.DaysPerYear + ", " + year);
            */
            AssetLocation loc = new AssetLocation(toCode);

            ICoreClientAPI capi = (Api as ICoreClientAPI);
            int hours = (int)Math.Round(transitionAtHour - prevTime);
            hours = hours < 0 ? 0 : hours;

            if (toCode.Contains("*"))
            {
                loc = Blockentity.Block.WildCardReplace(new AssetLocation(fromCode), new AssetLocation(toCode));
            }

            string transition = null;
            string a = null;

            if (loc != null)
            {
                transition = Lang.GetMatching(loc.Domain + ":block-" + loc.Path);
                string allowed = "aeiouAEIOU";

                a = allowed.Contains(transition[0]) ? "an " : "a ";
            }

            string transitionsinto = transition == null || a == null ? "Transitions in " : "Transitions into " + a + transition + " in ";

            dsc.Append(transitionsinto + hours.ToString() + " Hours.").AppendLine();
            base.GetBlockInfo(forPlayer, dsc);
        }

    }
}
