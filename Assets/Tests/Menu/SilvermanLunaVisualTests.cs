using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

public class SilvermanLunaVisualTests
{
    [UnityTest]
    public IEnumerator MainAndMoonKeepPlacementAimAndSizeIndependent()
    {
        yield return SceneManager.LoadSceneAsync("SilvermanLuna");
        yield return null;
        yield return null;
        var visuals = Object.FindObjectsByType<UnionPrimeVisualTest>(FindObjectsSortMode.None);
        Assert.AreEqual(2, visuals.Length);
        var main = System.Array.Find(visuals, item => item.SiteCount == 12);
        var moon = System.Array.Find(visuals, item => item.SiteCount == 4);
        Assert.IsNotNull(main);
        Assert.IsNotNull(moon);
        Assert.AreSame(main.transform, moon.transform.parent);
        Assert.AreEqual(180, ((RectTransform)moon.transform).rect.width);
        Assert.AreEqual(600, ((RectTransform)main.transform.Find("Body")).rect.width);
        Assert.AreEqual(main.ArtRoot.lossyScale.x * main.towerScale,
            moon.ArtRoot.lossyScale.x * moon.towerScale, 0.0001f);
        for (int i = 0; i < main.SiteCount; i++) Assert.IsTrue(main.TryPlace(i));
        Assert.AreEqual(0, moon.PlacedCount);
        for (int i = 0; i < moon.SiteCount; i++) Assert.IsTrue(moon.TryPlace(i));
        Assert.IsFalse(main.TryPlace(0));
        Assert.IsFalse(moon.TryPlace(0));
        foreach (var visual in visuals)
        {
            var foundation = visual.ArtRoot.GetChild(1).Find("Base and operator");
            var original = foundation.rotation;
            visual.SetAim(60);
            Assert.Less(Quaternion.Angle(original, foundation.rotation), .001f);
            Assert.Greater(visual.transform.Find("SocketFronts").GetSiblingIndex(),
                visual.preparedTowerLayer.GetSiblingIndex());
        }
        Assert.IsTrue(moon.RemoveAt(0));
        Assert.AreEqual(12, main.PlacedCount);
        Assert.AreEqual(3, moon.PlacedCount);
        yield return SceneManager.LoadSceneAsync("SilvermanLuna");
        yield return null;
        foreach (var visual in Object.FindObjectsByType<UnionPrimeVisualTest>(FindObjectsSortMode.None))
            Assert.AreEqual(0, visual.PlacedCount);
        LogAssert.NoUnexpectedReceived();
    }

    [UnityTest]
    public IEnumerator PointerHitsEveryMainAndMoonSocketWithoutCrossPlacement()
    {
        var background = InputSystem.settings.backgroundBehavior;
        var editorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
        var mouse = InputSystem.AddDevice<Mouse>("SilvermanSocketMouse");
        try
        {
            yield return SceneManager.LoadSceneAsync("SilvermanLuna");
            yield return null;
            yield return null;
            Canvas.ForceUpdateCanvases();
            var visuals = Object.FindObjectsByType<UnionPrimeVisualTest>(FindObjectsSortMode.None);
            foreach (var visual in visuals)
            {
                for (int i = 0; i < visual.SiteCount; i++)
                {
                    Vector2 point = RectTransformUtility.WorldToScreenPoint(null,
                        visual.ArtRoot.TransformPoint(visual.LocalMount(i)));
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                    yield return null;
                    yield return null;
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point }.WithButton(MouseButton.Left));
                    yield return null;
                    yield return null;
                    InputSystem.QueueStateEvent(mouse, new MouseState { position = point });
                    yield return null;
                    Assert.IsTrue(visual.IsOccupied(i), visual.name + " / site " + (i+1));
                }
            }
            var main = System.Array.Find(visuals, item => item.SiteCount == 12);
            var moon = System.Array.Find(visuals, item => item.SiteCount == 4);
            Assert.AreEqual(12, main.PlacedCount);
            Assert.AreEqual(4, moon.PlacedCount);
            LogAssert.NoUnexpectedReceived();
        }
        finally
        {
            InputSystem.RemoveDevice(mouse);
            InputSystem.settings.backgroundBehavior = background;
            InputSystem.settings.editorInputBehaviorInPlayMode = editorInput;
        }
    }
}
