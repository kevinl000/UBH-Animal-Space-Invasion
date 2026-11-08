using System.Collections;
using System.IO;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;
using UnityEngine.UI;

public class SilvermanLibraryRenderCapture
{
    [UnityTest]
    public IEnumerator CaptureConnectedLibraryAndApprovedMoonInUnity()
    {
        yield return SceneManager.LoadSceneAsync("SilvermanLuna");
        yield return null;
        yield return null;
        var visuals = Object.FindObjectsByType<UnionPrimeVisualTest>(FindObjectsSortMode.None);
        var visual = System.Array.Find(visuals, item => item.SiteCount == 12);
        Assert.AreEqual(12, visual.SiteCount);
        Assert.IsTrue(visual.transform.Find("Moon").gameObject.activeSelf);
        foreach (var name in new[] { "Globe", "Body", "SocketFronts" })
        {
            var image = visual.transform.Find(name).GetComponent<Image>();
            Assert.IsNotNull(image.sprite);
            Assert.AreEqual(FilterMode.Point, image.sprite.texture.filterMode);
            Assert.AreEqual(1, image.sprite.texture.mipmapCount);
            Assert.AreEqual(new Vector2(1254, 1254), image.sprite.rect.size);
        }
        var canvas = visual.GetComponentInParent<Canvas>();
        var camera = Object.FindAnyObjectByType<Camera>();
        var zoom = Object.FindAnyObjectByType<PlanetPageZoom>();
        Capture("empty", 1672, 941, canvas, camera);
        foreach (var item in visuals) for (int i = 0; i < item.SiteCount; i++) Assert.IsTrue(item.TryPlace(i));
        yield return null;
        Capture("occupied", 1672, 941, canvas, camera);
        foreach (var angle in new[] { -60, 60 })
        {
            foreach (var item in visuals) item.SetAim(angle);
            Capture("aim" + angle, 1672, 941, canvas, camera);
        }
        foreach (var item in visuals) item.SetAim(0);
        Capture("occupied", 1280, 720, canvas, camera);
        Capture("occupied", 1280, 960, canvas, camera);
        Capture("occupied", 2560, 1080, canvas, camera);
        zoom.SetZoom(zoom.maximumZoom, true);
        Capture("maximum-zoom", 1672, 941, canvas, camera);
        LogAssert.NoUnexpectedReceived();
    }

    private static void Capture(string name, int width, int height, Canvas canvas, Camera camera)
    {
        var scaler = canvas.GetComponent<CanvasScaler>();
        var target = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        target.Create();
        camera.targetTexture = target;
        camera.cullingMask = ~0;
        canvas.renderMode = RenderMode.ScreenSpaceCamera;
        canvas.worldCamera = camera;
        canvas.planeDistance = 1;
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
        scaler.scaleFactor = Mathf.Min(width / 1672f, height / 941f);
        canvas.scaleFactor = scaler.scaleFactor;
        Canvas.ForceUpdateCanvases();
        RenderPipeline.SubmitRenderRequest(camera, new RenderPipeline.StandardRequest { destination = target });
        var previous = RenderTexture.active;
        RenderTexture.active = target;
        var pixels = new Texture2D(width, height, TextureFormat.RGB24, false);
        pixels.ReadPixels(new Rect(0, 0, width, height), 0, 0);
        pixels.Apply();
        var data = pixels.GetPixels32();
        bool varies = false;
        for (int i = 0; i < data.Length; i += 97)
            if (!data[i].Equals(data[0])) { varies = true; break; }
        Assert.IsTrue(varies, "Capture must contain rendered scene pixels.");
        var directory = Path.GetFullPath(".git/menu-work/silverman-moon-shape/unity-captures");
        Directory.CreateDirectory(directory);
        File.WriteAllBytes(Path.Combine(directory, name + "-" + width + "x" + height + ".png"), pixels.EncodeToPNG());
        RenderTexture.active = previous;
        camera.targetTexture = null;
        target.Release();
        Object.Destroy(target);
        Object.Destroy(pixels);
    }
}



