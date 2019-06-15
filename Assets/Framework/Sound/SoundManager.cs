using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//public class SoundManager : ApplicationSingleton<SoundManager>
public class SoundManager : MonoBehaviour
{
    const int MAX_MUSIC_CHANNELS = 2;

    [SerializeField]
    int _MaxSFXChannelPoolSize = 20;
    int _CurrentMusicChannel = 0;

    float _MusicFadeOutTime = 1;
    float _MusicFadeInTime = 1;

    class MusicChannelState
    {
        public AudioSource Source;
        public float FadeFactor;
        public float FadeTime;
    }

    class MusicTask
    {
        public bool Loop;
        public bool FadeIn;
        public AudioClip Clip;
    }

    int _MaxMusicTaskQueueLenth = 4;
    Queue<MusicTask> _MusicTaskQueue = new Queue<MusicTask>();

    List<MusicChannelState> _MusicChannels = new List<MusicChannelState>();
    List<AudioSource> _WorkingChannels = new List<AudioSource>();

    List<AudioSource> _ChannelPool = new List<AudioSource>();

    [SerializeField]
    GameObject _ChannelPoolObject;

    float _MusicVol = 1;
    float _SFXVol = 1;


    private void Awake()
    {
        if (_ChannelPoolObject == null)
        {
            _ChannelPoolObject = new GameObject("SoundChannelPool");
            _ChannelPoolObject.transform.parent = transform;
        }
        else
        {
            _ChannelPoolObject.GetComponentsInChildren<AudioSource>(_ChannelPool);
        }

        for (int i = _ChannelPool.Count; i < _MaxSFXChannelPoolSize + MAX_MUSIC_CHANNELS; i++)
        {
            _ChannelPool.Add(_ChannelPoolObject.AddComponent<AudioSource>());
        }

        for (int i = 0; i < MAX_MUSIC_CHANNELS; i++)
        {
            var channel = _ChannelPool[0];
            _ChannelPool.RemoveAt(0);
            _MusicChannels.Add(new MusicChannelState() { Source = channel, FadeFactor = 1, FadeTime = 0 });
        }
    }


    public void PlaySFX(AudioClip sfx)
    {
        var channel = GetChannel();
        if (channel != null)
        {
            _WorkingChannels.Add(channel);

            channel.loop = false;
            channel.enabled = true;
            channel.PlayOneShot(sfx);
        }
    }

    public void PlayMusic(AudioClip music, bool loop = true, bool fadeIn = false)
    {
        var task = new MusicTask()
        {
            Clip = music,
            Loop = loop,
            FadeIn = fadeIn,
        };

        if (_MusicTaskQueue.Count < _MaxMusicTaskQueueLenth)
        {
            _MusicTaskQueue.Enqueue(task);
        }
        else
        {

        }
    }

    AudioSource GetChannel()
    {
        if (_ChannelPool.Count > 0)
        {
            var channel = _ChannelPool[0];
            _ChannelPool.RemoveAt(0);
            return channel;
        }
        return null;
    }


    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        UpdateFSXChannel();
        UpdateMusicChannel();
    }

    void UpdateFSXChannel()
    {
        for (int i = 0; i < _WorkingChannels.Count;)
        {
            if (!_WorkingChannels[i].isPlaying)
            {
                var channel = _WorkingChannels[i];
                _WorkingChannels.RemoveAt(i);
                channel.enabled = false;
                _ChannelPool.Add(channel);
            }
            else
            {
                i++;
            }
        }
    }


    bool _IsMusicPlaying = false;

    public bool IsMusicPlaying { get => _IsMusicPlaying; }

    void UpdateMusicChannel()
    {
        var deltaTime = Time.unscaledDeltaTime;
        var preMusicChannel = (_CurrentMusicChannel + 1) % MAX_MUSIC_CHANNELS;

        // update preview music
        if (_MusicChannels[preMusicChannel].FadeFactor > 0 && _MusicChannels[preMusicChannel].Source.isPlaying)
        {
            _MusicChannels[preMusicChannel].FadeTime += deltaTime;
            _MusicChannels[preMusicChannel].FadeFactor = 1.0f - Mathf.Clamp01(_MusicChannels[preMusicChannel].FadeTime / _MusicFadeOutTime);
            _MusicChannels[preMusicChannel].Source.volume = _MusicVol * _MusicChannels[preMusicChannel].FadeFactor;
            if (_MusicChannels[preMusicChannel].FadeFactor <= 0)
            {
                _MusicChannels[preMusicChannel].Source.Stop();
                _MusicChannels[preMusicChannel].Source.enabled = false;
            }
        }


        if (_MusicChannels[_CurrentMusicChannel].FadeFactor < 1 && _MusicChannels[_CurrentMusicChannel].Source.isPlaying)
        {
            var music = _MusicChannels[_CurrentMusicChannel];
            music.FadeTime += deltaTime;
            music.FadeFactor = Mathf.Clamp01(music.FadeTime / _MusicFadeInTime);
            music.Source.volume = _MusicVol * music.FadeFactor;
            if (music.FadeFactor >= 1)
            {
                music.FadeTime = 0;
            }
        }

        var fadeOutDone = _MusicChannels[preMusicChannel].FadeFactor <= 0;
        var fadeInDone = _MusicChannels[_CurrentMusicChannel].FadeFactor >= 1;

        if (fadeInDone && fadeInDone)
        {
            if (_MusicTaskQueue.Count > 0)
            {
                var task = _MusicTaskQueue.Dequeue();
                _CurrentMusicChannel = (_CurrentMusicChannel + 1) % MAX_MUSIC_CHANNELS;
                preMusicChannel = (_CurrentMusicChannel + 1) % MAX_MUSIC_CHANNELS;

                var music = _MusicChannels[_CurrentMusicChannel];
                music.Source.clip = task.Clip;
                music.Source.loop = task.Loop;
                music.FadeTime = 0;
                music.FadeFactor = 0;
                music.Source.enabled = true;

                if (_MusicChannels[preMusicChannel].Source.enabled)
                {
                    music.Source.PlayScheduled(_MusicFadeOutTime);
                }
                else
                {
                    music.Source.Play();
                }

                _IsMusicPlaying = true;
            }
            else
            {
                if (!_MusicChannels[_CurrentMusicChannel].Source.isPlaying && _MusicChannels[_CurrentMusicChannel].Source.enabled)
                {
                    _IsMusicPlaying = false;
                }
            }
        }
    }
}

