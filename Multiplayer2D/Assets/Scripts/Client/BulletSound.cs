using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BulletSound : MonoBehaviour
{
    public AudioClip explosionFX;
    AudioSource source;

    private void Awake()
    {
        source = GetComponent<AudioSource>();
    }

    /*public void PlayExplosionFX()
    {
        
    }*/

    private void OnDestroy()
    {
        source.PlayOneShot(explosionFX);
    }
}
