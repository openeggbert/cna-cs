using System.Runtime.CompilerServices;

// The strict facade's surface is checked member for member against XNA's own metadata, so nothing
// that is not XNA may be public here -- api-compat reports an extra public member as a gate
// failure, which is how the survey hook below was caught being public in the first place.
//
// tools/content-survey measures how much of a real game's compiled content this binding can read.
// It asks the loader's own reader resolution rather than reimplementing it, because a survey that
// drifted from the loader would report a number nobody could act on.
//
// The name is the assembly's, not the project's. No grant is given to the test assembly: XNA's
// reader hooks are `protected internal`, and widening them for the tests would change which access
// modifier a test's own ContentTypeReader subclass has to use, which is not a thing a test should
// have to know.
[assembly: InternalsVisibleTo("cna-content-survey")]
