using UnityEngine;

public class Underwater : MonoBehaviour
{
    public AudioSource underwaterSound;


    private void OnTriggerEnter(Collider other)
    {
        if(other.CompareTag("Player"))
        {
            underwaterSound.Play();
        }
    }


    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            underwaterSound.Stop();
        }
    }
}
