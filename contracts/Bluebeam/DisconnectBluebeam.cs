using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Bluebeam;

// Drops the shared Bluebeam connection (admin only). Extractions start failing with a clear
// "connect Bluebeam" message until someone connects again; nothing already extracted is touched.
public sealed record DisconnectBluebeam : ICommand<BluebeamStatus>;
