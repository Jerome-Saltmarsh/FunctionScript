using UnityEngine;
using UnityEngine.UI;

public class Example02 : MonoBehaviour
{
    public Text text;
    public AudioClip audioClip;
    public AudioSource audioSource;
    
    void Start()
    {
        this.script()
            .waitUntilClicked(text)
            .perform(() => audioSource.PlayOneShot(audioClip))
            .perform(() => text.text = "Self destruct in 3...")
            .wait(1)
            .perform(() => text.text = "Self destruct in 2...")
            .wait(1)
            .perform(() => text.text = "Self destruct in 1...")
            .wait(1)
            .destroy(text.gameObject);
    }

}
