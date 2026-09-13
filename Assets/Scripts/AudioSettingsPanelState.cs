using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class AudioSettingsPanelState : MonoBehaviour
{
    private RectTransform box;
    private TMP_Text track, musicMute, effectsMute, haptics, mute;
    public void Initialize(RectTransform panel, TMP_Text trackLabel, TMP_Text musicMuteLabel,
        TMP_Text effectsMuteLabel, TMP_Text hapticsLabel, TMP_Text muteLabel)
    {
        box = panel; track = trackLabel; musicMute = musicMuteLabel; effectsMute = effectsMuteLabel;
        haptics = hapticsLabel; mute = muteLabel;
        Update();
    }
    private float tickerOffset;
    private string lastTrack;
    private void Update()
    {
        if (box == null) return;
        string caption="TERAZ GRA:   "+CasinoAudio.TrackNames[CasinoAudio.SelectedTrack]+"   •   "+CasinoAudio.SelectedCredit;
        float viewWidth=((RectTransform)track.transform.parent).rect.width;
        if(lastTrack!=caption){lastTrack=caption;track.text=caption;tickerOffset=viewWidth;}
        float textWidth=Mathf.Max(viewWidth,track.preferredWidth+24);
        track.rectTransform.sizeDelta=new Vector2(textWidth,58);
        tickerOffset-=Time.unscaledDeltaTime*48f;
        if(tickerOffset < -textWidth)tickerOffset=viewWidth;
        track.rectTransform.anchoredPosition=new Vector2(tickerOffset,0);
        var next=box.Find("UtilityNextTrack");if(next!=null){next.GetComponent<Button>().interactable=CasinoAudio.CanSkip;next.GetComponentInChildren<TMP_Text>().text=CasinoAudio.CanSkip?"NASTĘPNY":"TYLKO HOST";}
        musicMute.text = GameAudioSettings.MusicMuted ? "WŁĄCZ MUZYKĘ" : "WYCISZ MUZYKĘ";
        effectsMute.text = GameAudioSettings.EffectsMuted ? "WŁĄCZ SFX" : "WYCISZ SFX";
        haptics.text = GameAudioSettings.Haptics ? "WIBRACJE: WŁĄCZONE" : "WIBRACJE: WYŁĄCZONE";
        mute.text = GameAudioSettings.Muted ? "DŹWIĘK: WYCISZONY" : "DŹWIĘK: WŁĄCZONY";
        RectTransform root = GetComponentInParent<Canvas>().rootCanvas.transform as RectTransform;
        if (Screen.width <= 0 || Screen.height <= 0) return;
        Rect safe = Screen.safeArea;
        float scale = Mathf.Min(1.65f, (safe.width * root.rect.width / Screen.width - 48f) / box.sizeDelta.x,
            (safe.height * root.rect.height / Screen.height - 48f) / box.sizeDelta.y);
        var available=(RectTransform)transform;
        scale=Mathf.Min(scale,(available.rect.width-40)/box.sizeDelta.x,(available.rect.height-40)/box.sizeDelta.y);
        box.localScale = Vector3.one * Mathf.Max(0.1f, scale);
        box.anchoredPosition = Vector2.zero;
    }
}
