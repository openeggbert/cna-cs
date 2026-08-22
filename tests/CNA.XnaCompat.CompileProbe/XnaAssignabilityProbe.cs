using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Audio;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;
using System.Resources;

namespace XnaCompatibilityCompileProbe;

// This project deliberately contains no runtime assertions. Its successful compilation is the
// assertion: these are source-level relationships used by ordinary XNA game code and generic APIs.
internal static class XnaAssignabilityProbe
{
    internal static void CoreHierarchy(
        DrawableGameComponent drawable,
        DynamicVertexBuffer dynamicVertexBuffer,
        DynamicIndexBuffer dynamicIndexBuffer,
        DynamicSoundEffectInstance dynamicSound,
        ResourceContentManager resourceContent)
    {
        GameComponent gameComponent = drawable;
        VertexBuffer vertexBuffer = dynamicVertexBuffer;
        IndexBuffer indexBuffer = dynamicIndexBuffer;
        SoundEffectInstance sound = dynamicSound;
        ContentManager content = resourceContent;

        _ = (gameComponent, vertexBuffer, indexBuffer, sound, content);
    }

    internal static void GraphicsHierarchy(
        Texture texture,
        Texture2D texture2D,
        Texture3D texture3D,
        TextureCube textureCube,
        RenderTarget2D renderTarget2D,
        RenderTargetCube renderTargetCube,
        VertexBuffer vertexBuffer,
        IndexBuffer indexBuffer,
        Effect effect,
        VertexDeclaration vertexDeclaration,
        SpriteBatch spriteBatch,
        BlendState blendState,
        DepthStencilState depthStencilState,
        RasterizerState rasterizerState,
        SamplerState samplerState)
    {
        GraphicsResource[] resources =
        [
            texture,
            texture2D,
            texture3D,
            textureCube,
            renderTarget2D,
            renderTargetCube,
            vertexBuffer,
            indexBuffer,
            effect,
            vertexDeclaration,
            spriteBatch,
            blendState,
            depthStencilState,
            rasterizerState,
            samplerState,
        ];

        _ = resources;
    }

    internal static void GenericCollections(
        GameComponentCollection components,
        CurveKeyCollection keys,
        ModelEffectCollection effects)
    {
        ICollection<IGameComponent> componentContract = components;
        ICollection<CurveKey> keyContract = keys;
        IEnumerable<Effect> effectContract = effects;

        _ = (componentContract, keyContract, effectContract);
    }

    internal static ContentManager ResourceManagerIsContentManager(
        IServiceProvider services,
        ResourceManager resources) =>
        new ResourceContentManager(services, resources);
}
