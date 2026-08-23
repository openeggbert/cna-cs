using System.Runtime.CompilerServices;

// CNA.XnaCompat is built by subclassing/wrapping CNA.Framework types (see docs/architecture.md),
// so it needs access to the small set of protected-internal construction seams below. It never
// gets access to CNA.Interop directly -- that grant stops at this assembly, which is what keeps
// "CNA.XnaCompat never references CNA.Interop directly" (plan.md invariant #5) true at compile time.
[assembly: InternalsVisibleTo("CNA.XnaCompat")]
[assembly: InternalsVisibleTo("CNA.Framework.Tests")]
[assembly: InternalsVisibleTo("CNA.OwnershipStress")]
