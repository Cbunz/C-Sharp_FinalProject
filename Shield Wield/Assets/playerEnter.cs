using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class playerEnter : MonoBehaviour
{
    public GameObject boulder;

	private void OnTriggerEnter2D()
    {
        boulder.SetActive(true);
    }
}
