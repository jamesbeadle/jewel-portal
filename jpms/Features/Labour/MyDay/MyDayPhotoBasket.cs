using Jewel.JPMS.Features.Progress;
using Microsoft.AspNetCore.Components.Forms;

namespace Jewel.JPMS.Features.Labour.MyDay;

/// <summary>The photographs gathered for a day before it is saved. Every Choose adds to the basket
/// rather than replacing it, so a worker taking pictures with the camera one after another keeps
/// them all (Jeremy on Jack's phone, 30 Sep 2026: the second photo dropped the first).</summary>
public sealed class MyDayPhotoBasket
{
    private readonly List<ChosenPhoto> photos = new();

    public IReadOnlyList<ChosenPhoto> Photos => photos;
    public int Count => photos.Count;
    public bool HasAny => photos.Count > 0;
    public long TotalBytes => photos.Sum(photo => photo.Size);

    public async Task AddAsync(IReadOnlyList<IBrowserFile> files, CancellationToken cancellationToken)
    {
        foreach (var file in files.Take(MyDayPhotoUploader.MaxPerBatch - Count))
            photos.Add(await ChosenPhoto.ReadAsync(file, cancellationToken));
    }

    public void Clear() => photos.Clear();
}
