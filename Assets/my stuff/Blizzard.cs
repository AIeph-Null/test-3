using UnityEngine;
using UnityEngine.UI;
public class Blizzard : MonoBehaviour
{
    public Slider iceSlider;

    public AudioSource blizzardAudio;

    public float threshold = -18f;


    private bool isPlaying = false;


    private void Update()
    {
        if (iceSlider.value > threshold)
        {
            if (!isPlaying)
            {
                blizzardAudio.Play();
                isPlaying = true;

            }
        }
        else
        {
            if (isPlaying)
            {
                blizzardAudio.Stop();
                isPlaying = false;
            }
        }
    }
}
