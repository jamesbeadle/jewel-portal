namespace Jewel.JPMS.Features.Sales;

/// <summary>Where the 3D model's build-up has got to — the viewer's own words for it
/// (wwwroot/js/house-model/build-sequence/playback-state.js), read back off the callback.</summary>
public enum HouseModelPlayback
{
    Ready,
    Playing,
    Paused,
    Finished
}

public static class HouseModelPlaybackExtensions
{
    public static HouseModelPlayback ParsePlayback(this string state) => state switch
    {
        "playing" => HouseModelPlayback.Playing,
        "paused" => HouseModelPlayback.Paused,
        "finished" => HouseModelPlayback.Finished,
        _ => HouseModelPlayback.Ready
    };
}
