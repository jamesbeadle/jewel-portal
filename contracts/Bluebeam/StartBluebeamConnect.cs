using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Bluebeam;

// Begins the one-time "Connect Bluebeam" flow (admin only): returns the Bluebeam consent URL the
// browser must be sent to. The grant comes back to the api's /bluebeam/callback redirect, which
// stores the tokens — from then on every extraction runs through the connected account.
public sealed record StartBluebeamConnect : ICommand<BluebeamConnectStart>;
