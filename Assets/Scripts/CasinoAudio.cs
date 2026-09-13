using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Photon.Pun;

public sealed class CasinoAudio : MonoBehaviour
{
    public static readonly string[] TrackNames = { "Midnight Ashes 2", "Whispers in the Smoke", "Jazz Restaurant Music I", "Jazz Restaurant Music II", "Lofi Jazz Study Music", "Samba Jazz Restaurant", "Swing Jazz Sunny Cafe", "Talking" };
    private static readonly string[] TrackPaths = { "MidnightAshes2", "WhispersInTheSmoke", "alex-morgan-jazz-restaurant-music-556244", "alex-morgan-jazz-restaurant-music-563578", "alex-morgan-lofi-jazz-study-music-564256", "alex-morgan-samba-jazz-restaurant-567538", "alex-morgan-swing-jazz-sunny-cafe-568159", "divonoise-matth-samm-talking-423822" };
    private static CasinoAudio instance;
    private static int localGameplayScene = -1;
    private AudioSource music, effects;
    private readonly AudioClip[] clips = new AudioClip[TrackNames.Length];
    private AudioClip click, turnSignal;
    private int currentTrack = -1;
    private float fade = 1f, lastClick = -1f;
    private Coroutine changeRoutine;
    private float urgencyUntil, nextUrgencyCue;
    public static void SetUrgency(bool active)
    {
        if(instance==null)return;
        instance.urgencyUntil=active?Time.unscaledTime+.25f:0;
    }
    public static bool IsGameplay => SceneManager.GetActiveScene().name == "Game" || SceneManager.GetActiveScene().handle == localGameplayScene;
    private const string MusicTrackKey="music.track.v1",MusicStartKey="music.start.v1";
    private int localTrack;
    private double currentStart=-1;
    private string currentRoom;
    private float nextSync;
    private static bool SharedMusic=>PhotonNetwork.InRoom&&SceneManager.GetActiveScene().name=="Game";
    public static bool CanSkip=>!SharedMusic||PhotonNetwork.IsMasterClient;
    public static int SelectedTrack=>instance==null?0:Mathf.Max(0,instance.currentTrack);
    public static string SelectedCredit=>SelectedTrack<2?"Mircea Iancu (Surprising_Media) / Pixabay":SelectedTrack<7?"Alex Morgan / Pixabay":"Divonoise · Matth Samm / Pixabay";
    public static void NextTrack()
    {
        Initialize();if(!CanSkip)return;
        int next=(SelectedTrack+1)%TrackNames.Length;
        if(SharedMusic)PublishTrack(next);else instance.localTrack=next;
    }
    private static void PublishTrack(int track)
    {
        if(!PhotonNetwork.IsMasterClient||!PhotonNetwork.InRoom)return;
        PhotonNetwork.CurrentRoom.SetCustomProperties(new ExitGames.Client.Photon.Hashtable{{MusicTrackKey,track},{MusicStartKey,PhotonNetwork.Time}});
    }
    public static void BeginLocalGame() { localGameplayScene = SceneManager.GetActiveScene().handle; Initialize(); instance.localTrack=1; }

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        if (instance != null) return;
        GameObject obj = new GameObject("CasinoAudio");
        DontDestroyOnLoad(obj);
        instance = obj.AddComponent<CasinoAudio>();
    }
    private void Awake()
    {
        music = gameObject.AddComponent<AudioSource>();
        effects = gameObject.AddComponent<AudioSource>();
        music.playOnAwake = effects.playOnAwake = false;
        music.loop = false;
        music.spatialBlend = effects.spatialBlend = 0f;
        click = AudioClip.Create("Soft UI click", 1500, 1, 22050, false);
        float[] data = new float[1500];
        for (int i = 0; i < data.Length; i++)
        {
            double t = i / 22050.0;
            data[i] = (float)(Math.Sin(2 * Math.PI * 680 * t) * Math.Exp(-t * 95) * Math.Min(1, t * 1400) * 0.24);
        }
        click.SetData(data, 0);
        GameAudioSettings.Changed += ApplyVolume;
        ApplyVolume();
    }
    private void Update()
    {
        bool urgent=IsGameplay && Time.unscaledTime<urgencyUntil;
        music.pitch=1f;
        if(urgent && Time.unscaledTime>=nextUrgencyCue){nextUrgencyCue=Time.unscaledTime+1.5f;PlayLocalTurn();}
        int selected=localTrack;double start=-1;
        if(SharedMusic)
        {
            var properties=PhotonNetwork.CurrentRoom.CustomProperties;
            if(!(properties[MusicTrackKey] is int index)||index<0||index>=TrackNames.Length||!(properties[MusicStartKey] is double stamp))
            {if(PhotonNetwork.IsMasterClient&&Time.unscaledTime>=nextSync){nextSync=Time.unscaledTime+1;PublishTrack(1);}return;}
            selected=index;start=stamp;
        }
        string room=SharedMusic?PhotonNetwork.CurrentRoom.Name:null;
        if(selected!=currentTrack||start!=currentStart||room!=currentRoom)
        {
            currentTrack=selected;currentStart=start;currentRoom=room;
            if(changeRoutine!=null)StopCoroutine(changeRoutine);
            changeRoutine=StartCoroutine(ChangeMusic(selected));
            return;
        }
        if(changeRoutine!=null||music.clip==null)return;
        double elapsed=SharedMusic?Math.Max(0,PhotonNetwork.Time-currentStart):music.time;
        bool finished=SharedMusic?elapsed>=music.clip.length:!music.isPlaying;
        if(finished&&CanSkip&&Time.unscaledTime>=nextSync){nextSync=Time.unscaledTime+1;NextTrack();}
        else if(SharedMusic&&!finished&&Time.unscaledTime>=nextSync)
        {
            nextSync=Time.unscaledTime+2;
            if(Math.Abs(music.time-elapsed)>1.5)music.time=(float)elapsed;
            if(!music.isPlaying)music.Play();
        }
    }    private IEnumerator ChangeMusic(int selected)
    {
        if (clips[selected] == null)
        {
            ResourceRequest request = Resources.LoadAsync<AudioClip>("Audio/Music/" + TrackPaths[selected]);
            yield return request;
            clips[selected] = request.asset as AudioClip;
        }
        if (clips[selected] == null)
        {
            Debug.LogError("Missing music asset: " + TrackNames[selected]);
            changeRoutine = null;
            yield break;
        }
        while (music.isPlaying && fade > 0f)
        {
            fade = Mathf.MoveTowards(fade, 0f, Time.unscaledDeltaTime / .35f);
            ApplyVolume();
            yield return null;
        }
        music.Stop();
        music.clip = clips[selected];
        fade = 0f;
        ApplyVolume();
        if(SharedMusic)music.time=Mathf.Clamp((float)(PhotonNetwork.Time-currentStart),0,Mathf.Max(0,music.clip.length-.05f));
        music.Play();
        while (fade < 1f)
        {
            fade = Mathf.MoveTowards(fade, 1f, Time.unscaledDeltaTime / .6f);
            ApplyVolume();
            yield return null;
        }
        changeRoutine = null;
    }
    private void ApplyVolume()
    {
        music.volume = GameAudioSettings.MusicGain * .38f * fade;
        effects.volume = GameAudioSettings.EffectsGain * .40f;
    }
    public static void PlayClick()
    {
        Initialize();
        if (Time.unscaledTime - instance.lastClick < .08f || GameAudioSettings.Muted) return;
        instance.lastClick = Time.unscaledTime;

        instance.effects.PlayOneShot(instance.click);
    }
    public static void PlayLocalTurn()
    {
        Initialize();
        if (GameAudioSettings.Muted || GameAudioSettings.EffectsGain <= 0f) return;
        if (instance.turnSignal == null) instance.turnSignal = Resources.Load<AudioClip>("Audio/SFX/LocalTurn");
        if (instance.turnSignal != null) instance.effects.PlayOneShot(instance.turnSignal);
    }
    private void OnDestroy()
    {
        GameAudioSettings.Changed -= ApplyVolume;
        if (click != null) Destroy(click);
        if (instance == this) instance = null;
    }
}
