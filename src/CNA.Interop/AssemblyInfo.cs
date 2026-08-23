using System.Runtime.CompilerServices;

// CNA.Interop is intentionally all-internal (see docs/architecture.md): it is the only
// project allowed to talk to native code directly. CNA.Framework is the sole consumer.
[assembly: InternalsVisibleTo("CNA.Framework")]
[assembly: InternalsVisibleTo("CNA.Interop.Tests")]
[assembly: InternalsVisibleTo("CNA.Framework.Tests")]
[assembly: InternalsVisibleTo("CNA.AbiVerify")]
