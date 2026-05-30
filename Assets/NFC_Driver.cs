using System;
using UnityEngine;
using PCSC;
using PCSC.Exceptions;
using PCSC.Monitoring;
using UnityEngine.Video; // For VideoPlayer
using System.Collections;
using System.Collections.Generic; // For Dictionary
using UnityEngine.UI;


public class ACR1252Reader : MonoBehaviour
{
    private string _readerName;
    private ISCardContext _context;
    private ISCardMonitor _monitor;
    private volatile bool _tagPresent = false;
    private bool isPlayingOutro = false;

    // A dictionary to associate specific NFC UIDs with videos
    public VideoPlayer videoPlayer;  // Assign this in the Inspector
    public VideoClip[] videoClips;   // List of video clips to assign for different UIDs
    private Dictionary<string, VideoClip> uidVideoMap;

    public GameObject overlayAnimUI; // RawImage GameObject for overlay

    void Start()
    {
        try
        {
            // Establish a context for the NFC reader
            _context = ContextFactory.Instance.Establish(SCardScope.System);

            // Get the list of available readers
            string[] readers = _context.GetReaders();

            if (readers.Length == 0)
            {
                Debug.LogError("No NFC readers found!");
                return;
            }

            // Use the first available reader
            _readerName = readers[0];
            Debug.Log("Connected to NFC Reader: " + _readerName);

            // Initialize the UID to Video mapping
            InitializeUidVideoMapping();

            // Start monitoring for NFC tag events
            StartMonitoring();
        }
        catch (Exception e)
        {
            Debug.LogError("Error initializing NFC reader: " + e.Message);
        }
    }

    void StartMonitoring()
    {
        var monitorFactory = MonitorFactory.Instance;
        _monitor = monitorFactory.Create(SCardScope.System);

        _monitor.CardInserted += (sender, args) =>
        {
            Debug.Log("NFC Tag Detected! Reading UID...");
            _tagPresent = true;

            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (videoPlayer != null)
                {
                    videoPlayer.Pause(); // Immediately pause to prevent flash
                }

                ReadNfcTag();
            });
        };

        _monitor.CardRemoved += (sender, args) =>
        {
            Debug.Log("NFC Tag Removed!");
            _tagPresent = false;

            // Immediately pause video to avoid flicker
            UnityMainThreadDispatcher.Instance.Enqueue(() =>
            {
                if (videoPlayer != null)
                {
                    videoPlayer.Pause();  // Pause right away
                }

                StartCoroutine(PlayOutroThenDefault());  // Then play the animation
            });
        };


        Debug.Log("Starting card monitoring...");
        _monitor.Start(_readerName);
    }

    void InitializeUidVideoMapping()
    {
        // Initialize the dictionary to map UID to videos
        uidVideoMap = new Dictionary<string, VideoClip>();

        // associate specific UIDs to specific videos
        uidVideoMap.Add("0448AE031101899000", videoClips[0]);
        uidVideoMap.Add("04FCC0031101899000", videoClips[1]);
        uidVideoMap.Add("04B0B1031101899000", videoClips[2]);
        uidVideoMap.Add("04B75D031101899000", videoClips[3]);
        uidVideoMap.Add("0409F8021101899000", videoClips[4]);
        uidVideoMap.Add("0456B4021101899000", videoClips[5]);
        uidVideoMap.Add("040F1B031101899000", videoClips[6]);
    }

    void ReadNfcTag()
    {
        using (var reader = new SCardReader(_context))
        {
            var result = reader.Connect(_readerName, SCardShareMode.Shared, SCardProtocol.Any);
            if (result != SCardError.Success)
            {
                Debug.LogError("Failed to connect to the NFC tag: " + result);
                return;
            }

            byte[] getUidCommand = { 0xFF, 0xCA, 0x00, 0x00, 0x00 };
            byte[] response = new byte[256];
            int responseLength = response.Length;

            result = reader.Transmit(getUidCommand, getUidCommand.Length, response, ref responseLength);

            if (result == SCardError.Success)
            {
                string uid = BitConverter.ToString(response, 0, responseLength).Replace("-", "");
                Debug.Log("NFC Tag UID: " + uid);

                if (_tagPresent && uidVideoMap.ContainsKey(uid))
                {
                    StartCoroutine(PlayIntroThenVideo(uidVideoMap[uid]));
                }
                else
                {
                    Debug.Log("No action mapped for this card.");
                }
            }
            else
            {
                Debug.LogError("Failed to read NFC tag: " + result);
            }

            reader.Disconnect(SCardReaderDisposition.Leave);
        }
    }

    public VideoClip defaultVideoClip; 

    void PlayVideo(VideoClip mainClip)
    {
        UnityMainThreadDispatcher.Instance.Enqueue(() =>
        {
            if (videoPlayer != null)
            {
                videoPlayer.clip = mainClip;
                videoPlayer.isLooping = true;
                videoPlayer.Play();
            }
        });
    }


    private IEnumerator PlayOutroThenDefault()
    {
        if (overlayAnimUI != null)
        {
            overlayAnimUI.SetActive(true);

            // Make sure RawImage is fully opaque before animation
            RawImage img = overlayAnimUI.GetComponent<RawImage>();
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);

            // Play fade animation (optional visual cue)
            Animator anim = overlayAnimUI.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("fade_anim");
            }
        }

        // Wait for animation to finish (or remove this if using a static opaque overlay)
        yield return new WaitForSeconds(1f);

        // Now switch the video
        if (videoPlayer != null && defaultVideoClip != null)
        {
            videoPlayer.clip = defaultVideoClip;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        // Wait one frame to ensure video has swapped before removing overlay
        yield return null;

        // Fade out or disable overlay after video has started
        if (overlayAnimUI != null)
        {
            Animator anim = overlayAnimUI.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("fade_anim"); // optional if you have a fade-out animation
                yield return new WaitForSeconds(1f); // wait for fade-out animation
            }

            overlayAnimUI.SetActive(false);
        }
    }

    private IEnumerator PlayIntroThenVideo(VideoClip clipToPlay)
    {
        if (overlayAnimUI != null)
        {
            overlayAnimUI.SetActive(true);

            // Ensure full alpha at start
            RawImage img = overlayAnimUI.GetComponent<RawImage>();
            img.color = new Color(img.color.r, img.color.g, img.color.b, 1f);

            Animator anim = overlayAnimUI.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("fade_anim"); // Your animation here
            }

            yield return new WaitForSeconds(1f); // Wait for animation
        }

        // Now switch video
        if (videoPlayer != null && clipToPlay != null)
        {
            videoPlayer.clip = clipToPlay;
            videoPlayer.isLooping = true;
            videoPlayer.Play();
        }

        // Optionally wait a frame
        yield return null;

        // Hide overlay
        if (overlayAnimUI != null)
        {
            Animator anim = overlayAnimUI.GetComponent<Animator>();
            if (anim != null)
            {
                anim.Play("fade_anim"); // optional if you have a fade-out animation
                yield return new WaitForSeconds(1f); // wait for fade-out animation
            }

            overlayAnimUI.SetActive(false);
        }
    }







    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Q))
        {
            QuitApplication();
        }
    }

    void QuitApplication()
    {
        Debug.Log("Quitting Application...");
        Application.Quit();

        // If running in the Unity Editor, stop play mode
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#endif
    }


    void OnApplicationQuit()
    {
        // Clean up resources
        _monitor?.Dispose();
        _context?.Dispose();
    }
}

