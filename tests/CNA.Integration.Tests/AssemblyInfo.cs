using Xunit;

// The native runtime allows exactly one C-owned game at a time -- `cna_game_create` answers
// `InvalidState: Only one C-owned CNA game may be active at a time` for the second. xUnit runs test
// *classes* in parallel by default, so the moment this project had two classes that construct a
// Game, nine of twelve tests failed at once and the three that passed were whichever won the race.
//
// That is not a defect: a single global game is the canonical XNA shape, and native says so
// clearly rather than corrupting state. It does mean these tests are inherently serial, and it is
// the reason this file exists rather than a per-class collection attribute -- every test here
// touches the same single global runtime, not just the ones that share a fixture.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
