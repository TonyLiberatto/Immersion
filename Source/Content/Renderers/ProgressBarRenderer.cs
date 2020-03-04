using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ProtoBuf;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Vintagestory.API;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;
using Vintagestory.API.Server;
using Vintagestory.Client.NoObf;
using Vintagestory.ServerMods.NoObf;

namespace Immersion
{
    class ProgressBarRenderer : IRenderer
    {
        ICoreClientAPI capi;
        IShaderProgram prog;
        MeshRef quadRef;

        public ProgressBarRenderer(ICoreClientAPI capi, int shaderid)
        {
            this.capi = capi;
            var shader = capi.Shader.GetProgram(shaderid);
            MeshData quadMesh = QuadMeshUtil.GetQuad();
            quadMesh.Rgba = null;
            quadRef = capi.Render.UploadMesh(quadMesh);

            if (shader.Compile())
            {
                prog = shader;
            }
            else
            {
                Dispose();
            }
        }

        public double RenderOrder => 1.0;

        public int RenderRange => 99;

        public void Dispose()
        {
            capi?.Event.UnregisterRenderer(this, EnumRenderStage.Ortho);
            prog?.Dispose();
            quadRef?.Dispose();
        }

        public void OnRenderFrame(float deltaTime, EnumRenderStage stage)
        {
            if (prog?.Disposed ?? true) return;
            capi.Render.GlToggleBlend(true);
            IShaderProgram curShader = capi.Render.CurrentActiveShader;

            curShader?.Stop();

            prog.Use();
            prog.Uniform("iTime", capi.World.ElapsedMilliseconds / 500f);
            prog.Uniform("iResolution", new Vec2f(capi.Render.FrameWidth, capi.Render.FrameHeight));
            prog.Uniform("iProgressBar", capi.ModLoader.GetModSystem<InWorldCraftingSystem>().progress);
            capi.Render.RenderMesh(quadRef);
            prog.Stop();

            curShader?.Use();
        }
    }
}
