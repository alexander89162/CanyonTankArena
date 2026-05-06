using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.Audio;

[RequireComponent(typeof(UIDocument))]
public class MenuSoundManager : MonoBehaviour
{
    [Header("UI Sounds")]
    [SerializeField] private AudioClip hoverSound;
    [SerializeField] private AudioClip clickSound;

    [SerializeField] private float hoverVolume = 0.6f;
    [SerializeField] private float clickVolume = 0.9f;

    [Header("Audio Routing")]
    [SerializeField] private AudioMixerGroup sfxMixerGroup;

    private AudioSource audioSource;

    private void Awake()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.outputAudioMixerGroup = sfxMixerGroup;
    }

    private void OnEnable()
    {
        if (TryGetComponent<UIDocument>(out var doc) && doc.rootVisualElement != null)
        {
            RegisterAllUIButtons(doc.rootVisualElement);
        }
    }

    private void RegisterAllUIButtons(VisualElement root)
    {
        root.Query<Button>().Build().ForEach(RegisterButtonSounds);
    }

    private void RegisterButtonSounds(Button button)
    {
        if (button == null) return;

        button.RegisterCallback<PointerEnterEvent>(evt => PlayHover());
        button.RegisterCallback<ClickEvent>(evt => PlayClick());
    }

    public void PlayHover()
    {
        if (hoverSound != null)
            audioSource.PlayOneShot(hoverSound, hoverVolume);
    }

    public void PlayClick()
    {
        if (clickSound != null)
            audioSource.PlayOneShot(clickSound, clickVolume);
    }

    public void SetVolume(float volume)
    {
        if (audioSource != null)
            audioSource.volume = volume;
    }
}