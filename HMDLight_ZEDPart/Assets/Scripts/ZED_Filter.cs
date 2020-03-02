using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ZED_Filter : MonoBehaviour
{
    public float filteredRange = 1.2f;
    public List<Renderer> renderers = new List<Renderer>();

    // Update is called once per frame
    void Update()
    {
        for(int i = 0 ; i < renderers.Count; i++){
            renderers[i].material.SetFloat("_Range", filteredRange);
        }
    }
}
