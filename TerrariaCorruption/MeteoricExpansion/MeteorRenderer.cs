using Vintagestory.API.Client;
using Vintagestory.API.Common.Entities;
using Vintagestory.GameContent;

namespace TerrariaCorruption.EntityRenderers
{
    class MeteorRenderer : EntityShapeRenderer
    {
        /// <summary>Creates the meteor shape renderer and sets its render distance.</summary>
        public MeteorRenderer(Entity entity, ICoreClientAPI api) : base(entity, api)
        {
            //IRenderer.RenderRange = 512;
            //this.RenderRange = 512;
        }
    }
}
