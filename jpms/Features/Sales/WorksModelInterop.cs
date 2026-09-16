using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

namespace Jewel.JPMS.Features.Sales;

/// <summary>The works view's calls into wwwroot/js/works-model.js, one method per verb the
/// page presses, so the panel reads as what it does rather than as interop.</summary>
public sealed class WorksModelInterop
{
    private readonly IJSRuntime runtime;
    private readonly ElementReference host;

    public WorksModelInterop(IJSRuntime runtime, ElementReference host) { this.runtime = runtime; this.host = host; }

    public Task Play() => Call("play");
    public Task Pause() => Call("pause");
    public Task SetPhase(int index) => Call("setPhase", index);
    public Task SetTradeVisible(string trade, bool isVisible) => Call("setTradeVisible", trade, isVisible);
    public Task SetGhost(bool isGhosting) => Call("setGhost", isGhosting);
    public Task SetShell(bool showsShell) => Call("setShell", showsShell);
    public Task StartRecording() => Call("startRecording");
    public Task StopRecording() => Call("stopRecording");
    public Task ToggleFullscreen() => Call("toggleFullscreen");
    public Task Dispose() => Call("dispose");

    public Task Mount(string definitionJson, DotNetObjectReference<LeadWorksModelPanel> page) =>
        Call("mount", definitionJson, page);

    private Task Call(string verb, params object[] arguments) =>
        runtime.InvokeVoidAsync($"jpmsWorksModel.{verb}", new object[] { host }.Concat(arguments).ToArray()).AsTask();
}
