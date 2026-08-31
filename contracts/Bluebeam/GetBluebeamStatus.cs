using Jewel.JPMS.Contracts.Cqrs;
using Jewel.JPMS.Models;

namespace Jewel.JPMS.Contracts.Bluebeam;

// The shared Bluebeam connection's state — any signed-in user may ask (the drawing pages need it
// to enable or disable their Extract buttons); only admins can change it.
public sealed record GetBluebeamStatus : IQuery<BluebeamStatus>;
