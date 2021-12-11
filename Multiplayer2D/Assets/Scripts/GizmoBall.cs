using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GizmoBall : MonoBehaviour
{
    [SerializeField] Color NormalDraw, SelectedDraw;
    [SerializeField] float size = 1.0f;

    private void OnDrawGizmos()
    {
        Gizmos.color = NormalDraw;
        Gizmos.DrawSphere(transform.position, size);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = SelectedDraw;
        Gizmos.DrawSphere(transform.position, size);
    }
}
