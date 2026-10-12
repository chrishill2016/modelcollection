using Bunit;
using ModelCollection.Features.CarDetail;

namespace ModelCollection.Tests.Components;

public class PhotoPickerTests : ComponentTestBase
{
    [Fact]
    public void Take_photo_opens_the_rear_camera()
    {
        var cut = Render<PhotoPicker>();

        var camera = cut.Find("[data-testid=camera-input]");
        Assert.Equal("environment", camera.GetAttribute("capture"));
        Assert.Equal("image/*", camera.GetAttribute("accept"));
        Assert.Equal(camera.Id, cut.Find("label.camera-only").GetAttribute("for"));
    }

    [Fact]
    public void Choose_photos_allows_several_from_the_gallery()
    {
        var cut = Render<PhotoPicker>();

        var gallery = cut.Find("[data-testid=gallery-input]");
        Assert.True(gallery.HasAttribute("multiple"));
        Assert.False(gallery.HasAttribute("capture"));
        Assert.Equal(gallery.Id, cut.Find("label.gallery-button").GetAttribute("for"));
    }

    [Fact]
    public void Picked_files_are_passed_on()
    {
        IReadOnlyList<Microsoft.AspNetCore.Components.Forms.IBrowserFile>? received = null;
        var cut = Render<PhotoPicker>(p => p.Add(c => c.FilesSelected, files => received = files));

        cut.FindComponents<Microsoft.AspNetCore.Components.Forms.InputFile>()[0]
            .UploadFiles(InputFileContent.CreateFromBinary([1, 2, 3], "car.jpg", contentType: "image/jpeg"));

        Assert.Equal("car.jpg", Assert.Single(received!).Name);
    }

    [Fact]
    public void Inputs_are_disabled_while_saving()
    {
        var cut = Render<PhotoPicker>(p => p.Add(c => c.Busy, true));

        Assert.All(cut.FindAll("input[type=file]"), i => Assert.True(i.HasAttribute("disabled")));
        Assert.Contains("Saving…", cut.Markup);
    }
}
