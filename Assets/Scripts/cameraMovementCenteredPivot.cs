using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cameraMovementCenteredPivot : MonoBehaviour
{
    public float leanAngle;
    public float leanSpeed;
    public Weapon w;

    public int leanSetting;

    public float crouchSpeed;
    // 0 = left lean, 1 = no lean, 2 = right lean;

    public Quaternion[] leanRotations = new Quaternion[3];

    //public GameObject modelAnim;
    public GameObject modelAnim;
    private Animator anim;

    // Start is called before the first frame update
    void Start()
    {
        leanRotations[0] = Quaternion.Euler(0f, 0f, leanAngle);
        leanRotations[1] = Quaternion.Euler(0f, 0f, 0f);
        leanRotations[2] = Quaternion.Euler(0f, 0f, leanAngle * -1);

        anim = modelAnim.GetComponentInChildren<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        //w = gameObject.GetComponent<Weapon>();
        if (Input.GetKeyDown(KeyCode.Q)){
            leanSetting = (leanSetting >= 1) ? 0 : 1;
        }
        if(Input.GetKeyDown(KeyCode.E)){
            leanSetting = (leanSetting <= 1) ? 2 : 1;
        }

        transform.localRotation = Quaternion.Slerp(transform.localRotation, leanRotations[leanSetting], leanSpeed * Time.deltaTime);
        anim = modelAnim.GetComponentInChildren<Animator>();
        anim.SetFloat("Lean", Mathf.Lerp(anim.GetFloat("Lean"), (float)leanSetting / 2f, leanSpeed * Time.deltaTime));
        //Debug.Log(anim.GetFloat("Lean"));

        // float origY = GameObject.Find("player").transform.position.y;
        float origY = transform.parent.position.y;

        Vector3 finalY;
        if(Input.GetKey(KeyCode.LeftControl)){
            finalY = new Vector3(transform.position.x, origY - 1f, transform.position.z);
        }
        else{
            finalY = new Vector3(transform.position.x, origY, transform.position.z);
        }
        transform.position = Vector3.Lerp(transform.position, finalY, crouchSpeed * Time.deltaTime);
    }
}
