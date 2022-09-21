using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class mouseLook : MonoBehaviourPunCallbacks
{

    public float mouseSensitivity;
    public Transform playerBody;
    public float xRotation;

    public GameObject modelAnim;
    private Animator anim;

    //public bool mouseLock;

    void Start() {
        Cursor.lockState = CursorLockMode.Locked;
        anim = modelAnim.GetComponentInChildren<Animator>();
    }


    // Update is called once per frame
    void Update()
    {
        if (!photonView.IsMine || Pause.paused) return;

        anim = modelAnim.GetComponentInChildren<Animator>();

        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, -90f, 90f);
        transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        playerBody.Rotate(Vector3.up * mouseX);
        if (anim != null) anim.SetFloat("YDeg", xRotation / -90f);
    }
}
