using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;
using System.Text.RegularExpressions;
using rnd = UnityEngine.Random;

public class sugarSkulls : MonoBehaviour
{
    public new KMAudio audio;
    public KMBombInfo bomb;
    public KMBombModule module;

    public KMSelectable[] buttons;
    public TextMesh[] texts;
    public Renderer[] bases;
    public Color[] colors;

    private int[] order = new int[3];
    private int[] positions = new int[3];
    private int[] baseColors = new int[3];
    private string configuration;
    private int solution;

    private static readonly string[] configurations = new string[] { "012", "345", "678", "036", "147", "258", "048", "246" };
    private static readonly string[] cells = new string[] { "ACE", "GIK", "MOP", "RTV", "XZb", "dfh", "jln", "prt", "vxz" };
    private static readonly string[] positionNames = new string[] { "left", "middle", "right" };
    private static readonly string[] buttonPositionNames = new string[] { "top", "left", "right" };
    private static readonly string[] colorNames = new string[] { "red", "orange", "purple" };

    private static int moduleIdCounter = 1;
    private int moduleId;
    private bool moduleSolved;
    private bool cantPress;
    private bool firstTime = true;

    void Awake()
    {
        moduleId = moduleIdCounter++;
        foreach (KMSelectable button in buttons)
            button.OnInteract += delegate () { PressButton(button); return false; };
    }

    void Start()
    {
        order = Enumerable.Range(0, 3).ToList().Shuffle().ToArray();
        positions = Enumerable.Range(0, 3).ToList().Shuffle().ToArray();
        baseColors = Enumerable.Range(0, 3).ToList().Shuffle().ToArray();
        configuration = configurations.PickRandom();
        for (int i = 0; i < 3; i++)
        {
            if (firstTime)
                texts[i].text = cells[int.Parse(configuration[order[i]].ToString())][positions[i]].ToString();
            bases[i].material.color = colors[baseColors[i]];
            Debug.LogFormat("[Sugar Skulls #{0}] The {1} button has a skull in the {2} position of cell {3}.", moduleId, buttonPositionNames[i], positionNames[positions[i]], int.Parse(configuration[order[i]].ToString()) + 1);
        }
        var centerPos = Array.IndexOf(order, 1);
        var centerPosPos = positions[centerPos];
        Debug.LogFormat("[Sugar Skulls #{0}] The {1} button has the middle skull, which is in the {2} position in its own cell.", moduleId, buttonPositionNames[centerPos], positionNames[positions[centerPos]]);
        Debug.LogFormat("[Sugar Skulls #{0}] That skull's base has the color {1}.", moduleId, colorNames[baseColors[centerPos]]);
        switch (colorNames[baseColors[centerPos]])
        {
            case "red":
                Debug.LogFormat("[Sugar Skulls #{0}] Leave the position alone.", moduleId);
                break;
            case "orange":
                Debug.LogFormat("[Sugar Skulls #{0}] Move one step left.", moduleId);
                centerPosPos--;
                break;
            case "purple":
                Debug.LogFormat("[Sugar Skulls #{0}] Move one step right.", moduleId);
                centerPosPos++;
                break;
            default:
                throw new System.ArgumentException("The color has an unexpected value.");
        }
        if (centerPosPos == -1)
            centerPosPos = 2;
        if (centerPosPos == 3)
            centerPosPos = 0;
        solution = Array.IndexOf(positions, centerPosPos);
        Debug.LogFormat("[Sugar Skulls #{0}] Press the button with the skull that has the position {1} in its own cell, which is the {2} button.", moduleId, positionNames[centerPosPos], buttonPositionNames[solution]);
        StartCoroutine(TextChange());
    }

    void PressButton(KMSelectable button)
    {
        button.AddInteractionPunch(.5f);
        audio.PlayGameSoundAtTransform(KMSoundOverride.SoundEffect.ButtonPress, button.transform);
        if (moduleSolved || cantPress)
            return;
        var ix = Array.IndexOf(buttons, button);
        Debug.LogFormat("[Sugar Skulls #{0}] You pressed the {1} button.", moduleId, buttonPositionNames[ix]);
        if (ix == solution)
        {
            Debug.LogFormat("[Sugar Skulls #{0}] That was correct. Module solved!", moduleId);
            module.HandlePass();
            audio.PlaySoundAtTransform("solve", transform);
            moduleSolved = true;
            StartCoroutine(TextChange());
        }
        else
        {
            Debug.LogFormat("[Sugar Skulls #{0}] That was incorrect. Strike! Resetting...", moduleId);
            module.HandleStrike();
            Start();
            StartCoroutine(TextChange());
        }
    }

    IEnumerator TextChange()
    {
        cantPress = true;
        if (!firstTime)
        {
            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(ColorChange(texts[i], texts[i].color, Color.white));
                yield return new WaitForSeconds(.5f);
            }
        }
        for (int i = 0; i < 3; i++)
            texts[i].text = cells[int.Parse(configuration[order[i]].ToString())][positions[i]].ToString();
        yield return new WaitForSeconds(1f);
        if (!moduleSolved)
        {
            for (int i = 0; i < 3; i++)
            {
                StartCoroutine(ColorChange(texts[i], texts[i].color, Color.black));
                yield return new WaitForSeconds(.5f);
            }
        }
        firstTime = false;
        cantPress = false;
    }

    IEnumerator ColorChange(TextMesh display, Color startColor, Color endColor)
    {
        var elapsed = 0f;
        var duration = .75f;
        while (elapsed < duration)
        {
            display.color = Color.Lerp(startColor, endColor, elapsed / duration);
            yield return null;
            elapsed += Time.deltaTime;
        }
        display.color = endColor;
    }

    // Twitch Plays
#pragma warning disable 414
    private readonly string TwitchHelpMessage = "!{0} <top/left/right> [Presses that skull.]";
#pragma warning restore 414

    IEnumerator ProcessTwitchCommand(string input)
    {
        input = input.Trim().ToLowerInvariant();
        var inputs = new string[] { "top", "left", "right" };
        if (inputs.Any(x => x == input))
        {
            yield return null;
            buttons[Array.IndexOf(inputs, input)].OnInteract();
        }
        else
            yield break;
    }

    IEnumerator TwitchHandleForcedSolve()
    {
        while (cantPress)
            yield return true;
        buttons[solution].OnInteract();
    }
}
