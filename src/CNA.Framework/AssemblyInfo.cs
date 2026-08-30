using System.Runtime.CompilerServices;

// CNA.XnaCompat is built by subclassing/wrapping CNA.Framework types (see docs/architecture.md),
// so it needs access to the small set of protected-internal construction seams below. It never
// gets access to CNA.Interop directly -- that grant stops at this assembly, which is what keeps
// "CNA.XnaCompat never references CNA.Interop directly" (plan.md invariant #5) true at compile time.
[assembly: InternalsVisibleTo("CNA.XnaCompat")]
[assembly: InternalsVisibleTo("CNA.Framework.Tests")]
[assembly: InternalsVisibleTo("CNA.OwnershipStress")]

// The integration suite needs internals for measurements that only a real device can make -- A1's
// native-vs-managed glyph comparison is the first. Safe here in a way it is not for CNA.XnaCompat:
// every member the integration tests override on a CNA.Framework type is plain `protected`, so the
// grant cannot change which modifier an override has to use. The `protected internal` members in
// this assembly are on DrawableGameComponent, which that suite does not subclass.
[assembly: InternalsVisibleTo("CNA.Integration.Tests")]
