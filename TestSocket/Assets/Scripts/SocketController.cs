#region License
/*
 * TestSocketIO.cs
 *
 * The MIT License
 *
 * Copyright (c) 2014 Fabio Panettieri
 *
 * Permission is hereby granted, free of charge, to any person obtaining a copy
 * of this software and associated documentation files (the "Software"), to deal
 * in the Software without restriction, including without limitation the rights
 * to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
 * copies of the Software, and to permit persons to whom the Software is
 * furnished to do so, subject to the following conditions:
 *
 * The above copyright notice and this permission notice shall be included in
 * all copies or substantial portions of the Software.
 *
 * THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
 * IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
 * FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
 * AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
 * LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
 * OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
 * THE SOFTWARE.
 */
#endregion
using System;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SocketIO;
[System.Serializable]
public class PlayerData
{
    [System.Serializable]
    public class PlayerInputs
    {
        public int accel = 0;
        public int rot = 0;
        public int gun = 0;
    }
    [System.Serializable]
    public class ShipVelocity
    {
        public Vector2 lin;
        public float ang;
    }
    public string name;
    public string socketid;
    public PlayerInputs inputs;
    public Vector2 pos;
    public float rot;
    public ShipVelocity vel;

    public event Action<PlayerData> OnUpdate;
    public void Update(PlayerData p)
    {
        name = p.name;
        socketid = p.socketid;
        inputs = p.inputs;
        pos = p.pos;
        rot = p.rot;
        vel = p.vel;
        OnUpdate(this);
    }
}

public class SocketController : MonoBehaviour {

    private SocketIOComponent socket;
    public PlayerData me;
    public Dictionary<PlayerData, GameObject> players = new Dictionary<PlayerData, GameObject>();
    public UnityEngine.UI.Text nameInput;
    public GameObject setupUIRoot;
    public GameObject playerPrefab { get { return Resources.Load<GameObject>("prefabs/player"); }}
    public Transform playersRoot;
    public bool IsConnected = false;
    public string logmsg;
    public int Accel { 
        get { return me.inputs.accel; } 
        set
        {
            if (value != me.inputs.accel)
            {
                me.inputs.accel = value;
                SendUpdatedInputs();
            }
        } 
    }
    public int Rot { 
        get { return me.inputs.rot; } 
        set
        {
            if (value != me.inputs.rot)
            {
                me.inputs.rot = value; 
                SendUpdatedInputs();
            }
        } 
    }
    public int Gun { 
        get { return me.inputs.gun; } 
        set 
        { 
            if(value != me.inputs.gun)
            {
                me.inputs.gun = value; 
                SendUpdatedInputs();
            } 
        } 
    }
    public void Start()
    {
        GameObject go = GameObject.Find("SocketIO");
        socket = go.GetComponent<SocketIOComponent>();

        socket.On("open", TestOpen);
        socket.On("error", TestError);
        socket.On("uerror", UError);
        socket.On("close", TestClose);
        socket.On("named", AssignOwnName);
        socket.On("joined", OnPlayerJoin);
        socket.On("left", OnPlayerLeft);
        socket.On("players", UpdatePlayers);
    }

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.W))
        {
            Accel = 1;
        }
        if(Input.GetKeyUp(KeyCode.W))
        {
            Accel = 0;
        }
        if(Input.GetKeyDown(KeyCode.S))
        {
            Accel = -1;
        }
        if(Input.GetKeyUp(KeyCode.S))
        {
            Accel = 0;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            Rot = 1;
        }
        if (Input.GetKeyUp(KeyCode.D))
        {
            Rot = 0;
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            Rot = -1;
        }
        if (Input.GetKeyUp(KeyCode.A))
        {
            Rot = 0;
        }
        if(Input.GetKeyDown(KeyCode.Space))
        {
            Gun = 1;
        }
        if (Input.GetKeyUp(KeyCode.Space))
        {
            Gun = 0;
        }

    }
    public void UpdatePlayers(SocketIOEvent e)
    {
        me.Update(JsonUtility.FromJson<PlayerData>(e.data.GetField(me.name).ToString()));
        foreach(var player in players.Keys)
        {
            var casted = JsonUtility.FromJson<PlayerData>(e.data.GetField(player.name).ToString());
            player.Update(casted);
            players[player].transform.position = player.pos;
            players[player].transform.rotation = Quaternion.AngleAxis(player.rot * Mathf.Rad2Deg, -Vector3.forward);
        }
    }
    private void SendUpdatedInputs()
    {
        var jsonToSend = JSONObject.Create(JsonUtility.ToJson(me.inputs));
        socket.Emit("uinput", jsonToSend);
    }
    public void OnPlayerJoin(SocketIOEvent e)
    {
        var joinedPlayer = JsonUtility.FromJson<PlayerData>(e.data.GetField("player").ToString());
        AddPlayer(joinedPlayer);

    }
    public void OnPlayerLeft(SocketIOEvent e)
    {
        Debug.Log("Player left "  + e.data.GetField("player").GetField("name").str);
        var key = players.Keys.FirstOrDefault(x => x.name == e.data.GetField("player").GetField("name").str);
        Destroy(players[key]);
        players.Remove(key);
    }
    public void OnClickSendName()
    {
        var json = new JSONObject();
        json.AddField("name", nameInput.text);
        socket.Emit("setname", json);
    }

    public void AssignOwnName(SocketIOEvent e)
    {
        var isOk = int.Parse(e.data.GetField("ok").ToString());
        if (isOk == 1)
        {
            IsConnected = true;
            me = JsonUtility.FromJson<PlayerData>(e.data.GetField("player").ToString());
            setupUIRoot.SetActive(false);
            logmsg = "connected";
            AddPlayer(me);
            logmsg = "added player";
        }
        else
        {
            Debug.Log("not ok");
        }
    }
    public void AddPlayer(PlayerData data)
    {
        logmsg = playerPrefab.name + " play prefab";
        var obj = GameObject.Instantiate(playerPrefab, data.pos, Quaternion.AngleAxis(data.rot * Mathf.Rad2Deg, -Vector3.forward), playersRoot);
        logmsg = obj.name + " instance";
        players.Add(data, obj);
        obj.GetComponentInChildren<SpriteRenderer>().color = new Color(UnityEngine.Random.RandomRange(0.0f, 1.0f), UnityEngine.Random.RandomRange(0.0f, 1.0f), UnityEngine.Random.RandomRange(0.0f, 1.0f), 1.0f);
    }
    public void UError(SocketIOEvent e)
    {
        Debug.Log("UError received: " + e.data.GetField("message").ToString());
    }
    public void TestOpen(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Open received: " + e.name + " " + e.data);
    }

    public void TestError(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Error received: " + e.name + " " + e.data);
    }

    public void TestClose(SocketIOEvent e)
    {
        Debug.Log("[SocketIO] Close received: " + e.name + " " + e.data);
    }


    public void OnGUI()
    {
        GUI.Label(new Rect(0, 0, 200, 50), IsConnected.ToString());
        GUI.Label(new Rect(0, 50, 200, 50), logmsg);
    }
    private static System.Random random = new System.Random();
    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
          .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}
