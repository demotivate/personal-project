using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Photon.Pun;

public class Pickup : MonoBehaviourPunCallbacks
{
    public Gun weapon;
    public float cooldown;
    public GameObject gunDisplay;
    public List<GameObject> targets;
    public Manager manager;

    private bool isDisabled;
    private float wait;
    private float pass;

    private void Start() {
        foreach(Transform t in gunDisplay.transform) {
            Destroy(t.gameObject);
        }

        GameObject newDisplay = Instantiate(weapon.display, gunDisplay.transform.position, gunDisplay.transform.rotation) as GameObject;
        newDisplay.transform.SetParent(gunDisplay.transform);

        pass = 0f;
    }

    private void Update() {
        if(manager.state == GameState.Ending) {
            pass = 0f;
            Enable();
        }

        if (isDisabled) {
            if (wait >= 0) {
                wait -= Time.deltaTime;
            }
            else {
                Enable();
            }
            pass = 0f;
        }
        else {
            pass += Time.deltaTime;
            if (pass >= cooldown) {
                pass -= cooldown;
            }
        }
    }

    private void OnTriggerEnter(Collider other) {
        if (other.attachedRigidbody == null) return;

        if (other.attachedRigidbody.gameObject.tag.Equals("Player")) {
            Weapon weaponController = other.attachedRigidbody.gameObject.GetComponent<Weapon>();
            if (weaponController.CheckLoadoutForWeapon(weapon.name)) return;
            weaponController.photonView.RPC("PickupWeapon", RpcTarget.All, weapon.name);
            photonView.RPC("Disable", RpcTarget.All);
        }
    }

    [PunRPC]
    public void Disable() {
        wait = cooldown - pass;
        isDisabled = true;

        foreach(GameObject a in targets) {
            a.SetActive(false);
        }
    }

    private void Enable() {
        wait = 0;
        isDisabled = false;

        foreach (GameObject a in targets) {
            a.SetActive(true);
        }
    }
}
