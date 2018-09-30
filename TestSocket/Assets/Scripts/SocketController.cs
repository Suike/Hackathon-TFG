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
    public int isdead;
    public int guncount;

    public event Action<PlayerData> OnUpdate;
    public event Action<PlayerData> OnDead;
    public event Action<PlayerData> OnRespawned;

    public void Update(PlayerData p)
    {
        name = p.name;
        socketid = p.socketid;
        inputs = p.inputs;
        pos = p.pos;
        rot = p.rot;
        vel = p.vel;
        if (isdead == 0 && p.isdead == 1)
            OnDead(this);
        if (isdead == 1 && p.isdead == 0)
            OnRespawned(this);
        isdead = p.isdead;
        guncount = p.guncount;
        OnUpdate(this);
    }
}
public class PlayerShot{
    public string owner;
    public Vector2 versor;
    public Vector2 minpoint;
    public Vector2 maxpoint;
    public float Angle { get { return Vector2.Angle(minpoint, maxpoint); } }
    public int id;

    public event Action<PlayerShot> OnUpdate;

    public void Update(PlayerShot s)
    {
        owner = s.owner;
        versor = s.versor;
        minpoint = s.minpoint;
        maxpoint = s.maxpoint;
        id = s.id;
        OnUpdate(this);
    }
}
public class SocketController : MonoBehaviour {

    private SocketIOComponent socket;
    public PlayerData me;

    public Dictionary<PlayerData, GameObject> players = new Dictionary<PlayerData, GameObject>();
    public Dictionary<PlayerShot, GameObject> shots = new Dictionary<PlayerShot, GameObject>();

    public UnityEngine.UI.Text nameInput;
    public GameObject setupUIRoot;
    public GameObject PlayerPrefab { get { return Resources.Load<GameObject>("Prefabs/Player"); } }
    public GameObject ShotPrefab { get { return Resources.Load<GameObject>("Prefabs/Shot"); } }
    public Transform playersRoot;
    public Transform shotsRoot;
    public bool IsConnected = false;
    //Local Inputs
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

        socket.On("joined", InstantiatePlayer);
        socket.On("left", RemovePlayer);
        socket.On("shotsfired", InstantiateShot);
        socket.On("shotdespawn", RemoveShot);

        socket.On("players", UpdatePlayers);
        socket.On("shots", UpdateShots);

        socket.On("dead", OnPlayerDead);
        socket.On("respawned", OnPlayerRespawned);
    }

    public void Update()
    {
        if(Input.GetKeyDown(KeyCode.W))
            Accel = 1;
        if(Input.GetKeyUp(KeyCode.W))
            Accel = 0;
        if(Input.GetKeyDown(KeyCode.S))
            Accel = -1;
        if(Input.GetKeyUp(KeyCode.S))
            Accel = 0;
        if (Input.GetKeyDown(KeyCode.D))
            Rot = 1;
        if (Input.GetKeyUp(KeyCode.D))
            Rot = 0;
        if (Input.GetKeyDown(KeyCode.A))
            Rot = -1;
        if (Input.GetKeyUp(KeyCode.A))
            Rot = 0;
        if(Input.GetKeyDown(KeyCode.Space))
            Gun = 1;
        if (Input.GetKeyUp(KeyCode.Space))
            Gun = 0;

    }
    public void UpdatePlayers(SocketIOEvent e)
    {
        //update player data structures
        foreach(var player in players.Keys)
        {
            var casted = JsonUtility.FromJson<PlayerData>(e.data.GetField(player.name).ToString());
            player.Update(casted);
        }
    }
    public void UpdateShots(SocketIOEvent e)
    {
        //update shots data structured
        foreach (var shot in shots.Keys)
        {
            var casted = JsonUtility.FromJson<PlayerShot>(e.data.GetField(shot.id.ToString()).ToString());
            shot.Update(casted);
        }
    }
    private void SendUpdatedInputs()
    {
        //emit inputs to server
        var jsonToSend = JSONObject.Create(JsonUtility.ToJson(me.inputs));
        socket.Emit("uinput", jsonToSend);
    }
    public void OnPlayerDead(SocketIOEvent e)
    {
        //Redundant, death can be inffered from change in isdead on player update
        Debug.Log("Ï am dead");
    }
    public void OnPlayerRespawned(SocketIOEvent e)
    {
        //Redundant, respawn can be inffered from change in isdead on player update
        Debug.Log("Ï respawned");

    }
    public void InstantiateShot(SocketIOEvent e)
    {
        Debug.Log("shoted " + e.data.ToString());
        var shot = JsonUtility.FromJson<PlayerShot>(e.data.ToString());
        
        var obj = Instantiate(ShotPrefab, shot.minpoint, Quaternion.LookRotation(Vector3.forward, shot.versor), shotsRoot);
        shots.Add(shot, obj);
        shot.OnUpdate += (s) =>
        {
            obj.transform.position = s.minpoint;
            obj.transform.rotation = Quaternion.LookRotation(Vector3.forward, shot.versor);
        };
    }
    public void RemoveShot(SocketIOEvent e)
    {
        Debug.Log("removed shot " + e.data.str);
        var id = int.Parse(e.data.GetField("id").str);
        var key = shots.Keys.FirstOrDefault(x => x.id == id);
        Destroy(shots[key]);
        shots.Remove(key);
    }
    public void InstantiatePlayer(SocketIOEvent e)
    {
        var joinedPlayer = JsonUtility.FromJson<PlayerData>(e.data.GetField("player").ToString());
        AddPlayer(joinedPlayer);

    }
    public void RemovePlayer(SocketIOEvent e)
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
            setupUIRoot.SetActive(false);
            me = JsonUtility.FromJson<PlayerData>(e.data.GetField("player").ToString());
            AddPlayer(me);
        }
        else
        {
            Debug.Log("not ok");
        }
    }


    private void AddPlayer(PlayerData data)
    {
        var obj = Instantiate(PlayerPrefab, data.pos, Quaternion.AngleAxis(data.rot * Mathf.Rad2Deg, -Vector3.forward), playersRoot);
        obj.GetComponentInChildren<SpriteRenderer>().color = new Color(UnityEngine.Random.Range(0.0f, 1.0f),
                                                                       UnityEngine.Random.Range(0.0f, 1.0f), 
                                                                       UnityEngine.Random.Range(0.0f, 1.0f), 1.0f);
        players.Add(data, obj);
        var playerShip = obj.GetComponent<PlayerShip>();
        playerShip.OnCreate(data, data == me);

        data.OnUpdate += playerShip.OnUpdate;
        data.OnDead += playerShip.OnDead;
        data.OnRespawned += playerShip.OnRespawned;
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
