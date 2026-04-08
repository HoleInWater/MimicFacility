using UnityEngine;

namespace MimicFacility.UI
{
    public class AudioCorruptor : MonoBehaviour
    {
        [Header("Corruption Over Time")]
        [SerializeField] private float startPitch = 1.0f;
        [SerializeField] private float endPitch = 0.75f;
        [SerializeField] private float pitchCorruptionDuration = 90f;

        [Header("Stutter")]
        [SerializeField] private float stutterChance = 0.02f;
        [SerializeField] private float stutterMinDuration = 0.05f;
        [SerializeField] private float stutterMaxDuration = 0.3f;
        [SerializeField] private float stutterRampStart = 40f;
        [SerializeField] private float stutterRampEnd = 80f;

        [Header("Warble")]
        [SerializeField] private float warbleAmount = 0.03f;
        [SerializeField] private float warbleSpeed = 3f;
        [SerializeField] private float warbleRampStart = 20f;

        [Header("Volume Drops")]
        [SerializeField] private float dropChance = 0.01f;
        [SerializeField] private float dropDuration = 0.1f;
        [SerializeField] private float dropRampStart = 50f;

        [Header("Reverb Buildup")]
        [SerializeField] private float reverbStart = 0f;
        [SerializeField] private float reverbEnd = 1f;
        [SerializeField] private float reverbDuration = 80f;

        private AudioSource source;
        private AudioReverbFilter reverb;
        private float elapsed;
        private float baseVolume;
        private bool isStuttering;
        private float stutterEndTime;
        private float stutterPauseTime;
        private bool isDropped;
        private float dropEndTime;

        void Start()
        {
            source = GetComponent<AudioSource>();
            if (source == null) return;

            baseVolume = source.volume;

            reverb = GetComponent<AudioReverbFilter>();
            if (reverb == null)
                reverb = gameObject.AddComponent<AudioReverbFilter>();
            reverb.reverbPreset = AudioReverbPreset.Hallway;
            reverb.dryLevel = 0f;
            reverb.room = -10000;
        }

        void Update()
        {
            if (source == null || !source.isPlaying) return;

            elapsed += Time.deltaTime;
            float t = elapsed;

            // Pitch decay — song gradually slows down like a dying machine
            float pitchProgress = Mathf.Clamp01(t / pitchCorruptionDuration);
            float pitchDecay = Mathf.Lerp(startPitch, endPitch, pitchProgress * pitchProgress);

            // Warble — pitch wobble that increases over time
            float warbleIntensity = Mathf.Clamp01((t - warbleRampStart) / 30f);
            float warble = Mathf.Sin(t * warbleSpeed) * warbleAmount * warbleIntensity;
            warble += Mathf.Sin(t * warbleSpeed * 2.7f) * warbleAmount * 0.3f * warbleIntensity;

            source.pitch = pitchDecay + warble;

            // Stutter — brief pauses that get more frequent
            float stutterIntensity = Mathf.Clamp01((t - stutterRampStart) / (stutterRampEnd - stutterRampStart));
            if (!isStuttering && Random.value < stutterChance * stutterIntensity)
            {
                isStuttering = true;
                stutterEndTime = Time.time + Random.Range(stutterMinDuration, stutterMaxDuration * stutterIntensity);
                stutterPauseTime = source.time;
                source.Pause();
            }
            if (isStuttering && Time.time >= stutterEndTime)
            {
                isStuttering = false;
                source.time = stutterPauseTime;
                source.UnPause();
            }

            // Volume drops — brief silence glitches
            float dropIntensity = Mathf.Clamp01((t - dropRampStart) / 30f);
            if (!isDropped && Random.value < dropChance * dropIntensity)
            {
                isDropped = true;
                dropEndTime = Time.time + dropDuration;
                source.volume = 0f;
            }
            if (isDropped && Time.time >= dropEndTime)
            {
                isDropped = false;
                source.volume = baseVolume;
            }

            // Reverb buildup — gets more echoey like the sound is in a bigger space
            if (reverb != null)
            {
                float reverbProgress = Mathf.Clamp01(t / reverbDuration);
                float reverbAmount = Mathf.Lerp(reverbStart, reverbEnd, reverbProgress);
                reverb.room = Mathf.Lerp(-10000, -1000, reverbAmount);
                reverb.dryLevel = Mathf.Lerp(0, -2000, reverbAmount * 0.5f);
            }
        }

        public void SetCorruptionSpeed(float multiplier)
        {
            pitchCorruptionDuration /= multiplier;
            stutterRampStart /= multiplier;
            stutterRampEnd /= multiplier;
            warbleRampStart /= multiplier;
            dropRampStart /= multiplier;
            reverbDuration /= multiplier;
        }
    }
}
