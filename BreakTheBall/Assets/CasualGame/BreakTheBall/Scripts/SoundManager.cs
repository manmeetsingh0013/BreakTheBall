using UnityEngine;
using System.Collections;
using MightyDoodle.Rewards.RewardGames;

namespace PaintTheRings
{
    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager Instance { get; private set; }

        [System.Serializable]
        public class Sound
        {
            public AudioClip clip;
            [HideInInspector]
            public int simultaneousPlayCount = 0;
        }

        // List of sounds used in this game
        public Sound background;
        public Sound button;
        public Sound throwBall;
        public Sound paintRingPiece;
        public Sound finishedRing;
        public Sound passLevel;
        public Sound gameOver;

        public delegate void OnMuteStatusChanged(bool isMuted);

        public static event OnMuteStatusChanged MuteStatusChanged;

        public delegate void OnMusicStatusChanged(bool isOn);

        public static event OnMusicStatusChanged MusicStatusChanged;

        enum PlayingState
        {
            Playing,
            Paused,
            Stopped
        }

        public AudioSource AudioSource
        {
            get
            {
                if (audioSource == null)
                {
                    audioSource = GetComponent<AudioSource>();
                }

                return audioSource;
            }
        }

        private AudioSource audioSource;
        private PlayingState musicState = PlayingState.Stopped;

        void Awake()
        {
            if (Instance)
                DestroyImmediate(gameObject);
            else
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
            }
        }

        private void Start()
        {
            RewardGameManager.GameSessionFinish += OnGameSessionFinish;
        }

        void OnGameSessionFinish()
        {
            RewardGameManager.GameSessionFinish -= OnGameSessionFinish;
            Destroy(gameObject);
        }

        /// <summary>
        /// Plays the given sound with option to progressively scale down volume of multiple copies of same sound playing at
        /// the same time to eliminate the issue that sound amplitude adds up and becomes too loud.
        /// </summary>
        /// <param name="sound">Sound.</param>
        /// <param name="autoScaleVolume">If set to <c>true</c> auto scale down volume of same sounds played together.</param>
        /// <param name="maxVolumeScale">Max volume scale before scaling down.</param>
        public void PlaySound(Sound sound, bool autoScaleVolume = true, float maxVolumeScale = 1f)
        {
            StartCoroutine(CRPlaySound(sound, autoScaleVolume, maxVolumeScale));
        }

        IEnumerator CRPlaySound(Sound sound, bool autoScaleVolume = true, float maxVolumeScale = 1f)
        {
            if (sound.simultaneousPlayCount >= 7)
                yield break;
            
            sound.simultaneousPlayCount++;

            float vol = maxVolumeScale;
            // Scale down volume of same sound played subsequently
            if (autoScaleVolume && sound.simultaneousPlayCount > 0)
                vol = vol / (float)(sound.simultaneousPlayCount);
            
            AudioSource.PlayOneShot(sound.clip, vol);

            // Wait til the sound almost finishes playing then reduce play count
            float delay = sound.clip.length * 0.7f;
            yield return new WaitForSeconds(delay);

            sound.simultaneousPlayCount--;
        }

        /// <summary>
        /// Plays the given music.
        /// </summary>
        /// <param name="music">Music.</param>
        /// <param name="loop">If set to <c>true</c> loop.</param>
        public void PlayMusic(Sound music, bool loop = true)
        {
            try
            {
                AudioSource.clip = music.clip;
                AudioSource.loop = loop;
                AudioSource.Play();
                musicState = PlayingState.Playing;
            }
            catch { }
        }

        /// <summary>
        /// Pauses the music.
        /// </summary>
        public void PauseMusic()
        {
            if (musicState == PlayingState.Playing)
            {
                AudioSource.Pause();
                musicState = PlayingState.Paused;
            }    
        }

        /// <summary>
        /// Resumes the music.
        /// </summary>
        public void ResumeMusic()
        {
            if (musicState == PlayingState.Paused)
            {
                AudioSource.UnPause();
                musicState = PlayingState.Playing;
            }
        }

        /// <summary>
        /// Stop music.
        /// </summary>
        public void StopMusic()
        {
            AudioSource.Stop();
            musicState = PlayingState.Stopped;
        }
    }
}