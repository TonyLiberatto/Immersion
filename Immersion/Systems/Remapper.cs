using Newtonsoft.Json;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;
using Vintagestory.Server;

namespace Neolithic
{
    [ProtoContract(ImplicitFields = ImplicitFields.AllPublic)]
    public class Message
    {
        public string Assets;
    }

    class Remapper : ModSystem
    {
        ICoreServerAPI sapi;
        ICoreClientAPI capi;
        IClientNetworkChannel cChannel;
        IServerNetworkChannel sChannel;
        bool canExecuteRemap = true;

        string nl = Environment.NewLine;

        public List<AssetLocation> MissingBlocks { get; set; } = new List<AssetLocation>();
        public List<AssetLocation> MissingItems { get; set; } = new List<AssetLocation>();

        public List<AssetLocation> NotMissingBlocks { get; set; } = new List<AssetLocation>();
        public List<AssetLocation> NotMissingItems { get; set; } = new List<AssetLocation>();

        Dictionary<AssetLocation, AssetLocation> MostLikely { get; set; } = new Dictionary<AssetLocation, AssetLocation>();

        public override void StartClientSide(ICoreClientAPI api)
        {
            capi = api;
            cChannel = capi.Network.RegisterChannel("remapperchannel")
                .RegisterMessageType(typeof(Message))
                .SetMessageHandler<Message>(a =>
                {
                    capi = api;
                    MostLikely = JsonConvert.DeserializeObject<Dictionary<AssetLocation, AssetLocation>>(a.Assets);
                    foreach (var item in MostLikely)
                    {
                        if (item.Key.GetBlock(capi) != null && item.Value.GetBlock(capi) != null)
                        {
                            capi.SendChatMessage("/bir remap " + item.Value + " " + item.Key + " force");
                        }
                        else if (item.Key.GetItem(capi) != null && item.Value.GetItem(capi) != null)
                        {
                            capi.SendChatMessage("/iir remap " + item.Value + " " + item.Key + " force");
                        }
                    }
                });
        }

        public override void StartServerSide(ICoreServerAPI api)
        {
            sapi = api;
            sChannel = sapi.Network.RegisterChannel("remapperchannel").RegisterMessageType(typeof(Message));

            sapi.RegisterCommand("remapper", "Remapper", "", (p, g, a) =>
            {
                string arg = a.PopWord();
                NLMissing nLMissing = api.ModLoader.GetModSystem<NLMissing>();
                switch (arg)
                {
                    case "exportmissing":
                        ExportMissing(p, g);
                        break;
                    case "exportmatches":
                        string dl1 = a.PopWord();
                        bool DL1 = dl1 == "dl" ? true : false;
                        ExportMatches(p, DL1);
                        break;
                    case "tryremap":
                        if (canExecuteRemap)
                        {
                            canExecuteRemap = false;
                            string dl = a.PopWord();
                            bool DL = dl == "dl" ? true : false;
                            TryRemapMissing(p, DL);
                        }
                        break;
                    case "frombuild":
                        sChannel.SendPacket(new Message() { Assets = nLMissing.@object }, p);
                        p.SendMessage(GlobalConstants.GeneralChatGroup, "Remapping from built in list...", EnumChatType.CommandError);
                        break;
                    case "loadfrombuild":
                        MostLikely = JsonConvert.DeserializeObject<Dictionary<AssetLocation, AssetLocation>>(nLMissing.@object);
                        p.SendMessage(GlobalConstants.GeneralChatGroup, "Okay, loaded list from build.", EnumChatType.CommandError);
                        break;
                    case "loadfromfile":
                        ImportMatches();
                        p.SendMessage(GlobalConstants.GeneralChatGroup, "Okay, loaded list from file.", EnumChatType.CommandError);
                        break;
                    case "finddupes":
                        List<AssetLocation> dupes = new List<AssetLocation>();
                        var duplicates = MostLikely.GroupBy(x => x.Value).Where(x => x.Count() > 1);
                        foreach (var val in duplicates)
                        {
                            dupes.Add(val.Key);
                        }

                        using (TextWriter tW = new StreamWriter("dupes.json"))
                        {
                            tW.Write(JsonConvert.SerializeObject(dupes, Formatting.Indented));
                            tW.Close();
                        }
                        p.SendMessage(GlobalConstants.GeneralChatGroup, "Okay, exported list of duplicate entries.", EnumChatType.CommandError);
                        break;
                    default:
                        break;
                }
            }, Privilege.controlserver);
        }

        public void ImportMatches()
        {
            try
            {
                using (TextReader tW = new StreamReader("matches.json"))
                {
                    MostLikely = JsonConvert.DeserializeObject<Dictionary<AssetLocation, AssetLocation>>(tW.ReadToEnd());
                }
            }
            catch (Exception)
            {
                sapi.World.Logger.Error("Empty Or Broken Matches JSON");
            }
        }

        public void RePopulate()
        {
            MissingBlocks.Clear();
            MissingItems.Clear();
            NotMissingBlocks.Clear();
            NotMissingItems.Clear();

            for (int i = 0; i < sapi.World.Blocks.Count; i++)
            {
                if (sapi.World.Blocks[i].IsMissing)
                {
                    MissingBlocks.Add(sapi.World.Blocks[i].Code);
                }
                else
                {
                    NotMissingBlocks.Add(sapi.World.Blocks[i].Code);
                }
            }
            for (int i = 0; i < sapi.World.Items.Count; i++)
            {
                if (sapi.World.Items[i].IsMissing)
                {
                    MissingItems.Add(sapi.World.Items[i].Code);
                }
                else
                {
                    NotMissingItems.Add(sapi.World.Items[i].Code);
                }
            }
        }

        public void ExportMissing(IServerPlayer player, int groupID)
        {
            RePopulate();
            List<AssetLocation> combined = MissingBlocks.Concat(MissingItems).ToList();
            string a = JsonConvert.SerializeObject(combined, Formatting.Indented);
            using (TextWriter tW = new StreamWriter("missingcollectibles.json"))
            {
                tW.Write(a);
                tW.Close();
            }
            player.SendMessage(groupID, "Okay, exported list of missing things.", EnumChatType.CommandError);
        }

        public void ExportMatches(IServerPlayer player, bool DL = false)
        {
            FindMatches(player, DL);

            using (TextWriter tW = new StreamWriter("matches.json"))
            {
                tW.Write(JsonConvert.SerializeObject(MostLikely, Formatting.Indented));
                tW.Close();
            }
            player.SendMessage(GlobalConstants.GeneralChatGroup, "Okay, exported list of matching things.", EnumChatType.CommandError);
        }

        public void FindMatches(IServerPlayer player, bool DL = false)
        {
            MostLikely.Clear();
            RePopulate();
            Search(player, MissingBlocks, NotMissingBlocks, "Block", DL);
            Search(player, MissingItems, NotMissingItems, "Item", DL);
        }

        public void Search(IPlayer player, List<AssetLocation> missing, List<AssetLocation> notmissing, string type = "Block", bool DL = false)
        {
            for (int i = 0; i < missing.Count; i++)
            {
                List<int> distance = new List<int>();
                for (int j = 0; j < notmissing.Count; j++)
                {
                    if (missing[i] == null || notmissing[j] == null)
                    {
                        distance.Add(999999999);
                        continue;
                    }
                    if (DL)
                    {
                        distance.Add(missing[i].ToString().Replace(missing[i].Domain + ":", "").ComputeDLDistance(notmissing[j].ToString().Replace(notmissing[j].Domain + ":", "")));
                    }
                    else
                    {
                        distance.Add(missing[i].ToString().Replace(missing[i].Domain + ":", "").ComputeDistance(notmissing[j].ToString().Replace(notmissing[j].Domain + ":", "")));
                    }
                }
                int index = distance.IndexOfMin();

                if (!MostLikely.ContainsValue(notmissing[index]))
                {
                    MostLikely.Add(missing[i], notmissing[index]);
                    notmissing.RemoveAt(index);
                }

                sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Finding Closest " + type + " Matches... " + Math.Round(i / (float)missing.Count * 100, 2) + "%", EnumChatType.Notification);
            }
            sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Finding Closest " + type + " Matches... 100%", EnumChatType.Notification);
        }

        public void TryRemapMissing(IServerPlayer player, bool DL = false)
        {
            RePopulate();
            ImportMatches();

            if (MostLikely.Count < 1)
            {
                sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Empty or Missing JSON, Will Search For Matches Instead Of Loading, Server May Lag For Bit.", EnumChatType.Notification);
                ExportMatches(player, DL);
            }


            sapi.SendMessage(player, GlobalConstants.InfoLogChatGroup, "Begin Remapping", EnumChatType.Notification);

            sChannel.SendPacket(new Message() { Assets = JsonConvert.SerializeObject(MostLikely) }, player);
            canExecuteRemap = true;
        }
    }
}
