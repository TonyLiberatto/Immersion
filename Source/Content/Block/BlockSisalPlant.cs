using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.Client.NoObf;
using Vintagestory.GameContent;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    class BlockSisalPlant : BlockCrop
    {
        public override void OnJsonTesselation(ref MeshData sourceMesh, BlockPos pos, int[] chunkExtIds, ushort[] chunkLightExt, int extIndex3d)
        {
            bool off = (chunkLightExt[extIndex3d] & 31) < 14;
            setLeaveWaveFlags(sourceMesh, off);
        }

        private void setLeaveWaveFlags(MeshData sourceMesh, bool off)
        {
            int leaveWave = VertexFlags.LeavesWindWaveBitMask;
            int clearFlags = (~VertexFlags.LeavesWindWaveBitMask);

            // Iterate over each element face
            for (int vertexNum = 0; vertexNum < sourceMesh.GetVerticesCount(); vertexNum++)
            {
                float y = sourceMesh.xyz[vertexNum * 3 + 1];

                sourceMesh.Flags[vertexNum] &= clearFlags;

                if (!off && y > 1.0)
                {
                    sourceMesh.Flags[vertexNum] |= leaveWave;
                }
            }
        }
    }
}
