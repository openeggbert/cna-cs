namespace CNA.Graphics;

/// <summary>
/// The shading dialect a source-based <see cref="Effect"/>'s text must be written in --
/// <c>graphics.h</c>'s <c>CNA_SHADER_DIALECT_*</c> identities.
///
/// Not an XNA type, and it has no XNA counterpart because XNA had one shader language. A
/// source-based effect here is renderer-specific text, and the header is explicit that the renderer
/// identity is <em>not</em> a safe way to infer which text to supply -- a build carrying more than
/// one backend makes that inference wrong. So a game that ships shader source has to ask.
/// </summary>
public enum ShaderDialect : uint
{
    /// <summary>The active renderer declares none. Do not guess one -- supplying text for a dialect
    /// the renderer did not name is how a shader compiles on one machine and not another.</summary>
    Unknown = 0,

    /// <summary>Desktop OpenGL GLSL (<c>#version 3xx core</c> / <c>4xx core</c>).</summary>
    GlslDesktop = 1,

    /// <summary>OpenGL ES / WebGL GLSL (<c>#version 100</c> / <c>300 es</c>).</summary>
    GlslEs = 2,

    /// <summary>GLSL compiled to SPIR-V, where <c>location</c>/<c>set</c>/<c>binding</c> are
    /// mandatory.</summary>
    GlslVulkan = 3,
}
