using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class FollowMe : MonoBehaviour {
    public SocketController controller;
	// Update is called once per frame
	void Update () {

        if (!controller.IsConnected)
            return;
        var xy = (Vector2)controller.players[controller.me].transform.position;
        transform.position = new Vector3(xy.x, xy.y, transform.position.z);
	}
}
