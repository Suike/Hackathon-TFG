using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerShip : MonoBehaviour {

    public GameObject explosion;
    public GameObject graphics;

    public void OnCreate(PlayerData pdata, bool isOwner){

        var outline = graphics.GetComponent<SpriteOutline>();
        if (!isOwner)
        {
            outline.color = Color.red;
        }
        else
        {
            outline.color = Color.green;
        }
    }
    public void OnUpdate(PlayerData pdata)
    {
        if (pdata.isdead == 0)
        {
            transform.position = pdata.pos;
            transform.rotation = Quaternion.AngleAxis(pdata.rot * Mathf.Rad2Deg, -Vector3.forward);
        }
    }
    public void OnDead(PlayerData pdata)
    {
        graphics.SetActive(false);
        explosion.SetActive(true);
        StartCoroutine(WaitAndDeactivate)
    }
    public void OnRespawned(PlayerData pdata)
    {
        graphics.SetActive(true);
    }

    private IEnumerator WaitAndDeactivate()
    {
        yield return new WaitForSeconds(1.0f);
        explosion.SetActive(false);
    }


}
