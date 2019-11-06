using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices.ComTypes;
using System.Text;
using System.Threading;
using NetMQ;
using NetMQ.Sockets;
using Newtonsoft.Json;
using UnityEngine;



public static class Comm
{
    private static bool _clientCommInstantiated;
    private static bool _hostCommInstantiated;
    public static void client_comm()    //For the host, communicates TO the client
    {
        if (_clientCommInstantiated) return;
        _clientCommInstantiated = true;
        const string repAddress = "tcp://*:5560";
        var rep = new ResponseSocket();
        rep.Bind(repAddress);

        while (true)
        {
            var recvStr = rep.ReceiveFrameString();
            
            //Add/update data 
            //Debug.Log(recvStr);
            var recvJson = JsonConvert.DeserializeObject<Dictionary<string, string>>(recvStr);

            if (int.Parse(recvJson["ID"]) == 0) //ID 0 means it's a client
            {
                var jsonOut = JsonConvert.SerializeObject(HostAndClientInput.goObjectPose, Formatting.Indented);
                var msg = new Msg();
                msg.InitGC(Encoding.ASCII.GetBytes(jsonOut), Encoding.ASCII.GetBytes(jsonOut).Length);
                rep.Send(ref msg, false);
            }
            else if (HostAndClientInput.FalconIDs.Contains(int.Parse(recvJson["ID"])))    //IDs greater than 0 are Falcons
            {
                if (!HostAndClientInput.HipForces.ContainsKey(int.Parse(recvJson["ID"])))
                    HostAndClientInput.HipForces.Add(int.Parse(recvJson["ID"]), recvJson);
                else
                    HostAndClientInput.HipForces[int.Parse(recvJson["ID"])] = recvJson;
                //Debug.Log(recvStr);

                //ack
                var msg = new Msg();
                msg.InitGC(Encoding.ASCII.GetBytes(recvJson["ID"]), Encoding.ASCII.GetBytes(recvJson["ID"]).Length);
                rep.Send(ref msg, false);
            }

            if (HostAndClientInput.killThread) break; //Because Unity3D editor doesn't kill this by itself.
        }
    }

    public static void host_comm()
    {
        if (_hostCommInstantiated) return;
        _hostCommInstantiated = true;
        var reqAddress = "tcp://" + HostAndClientInput.hostIp + ":5560";
        var req = new RequestSocket();
        req.Connect(reqAddress);

        while (true)
        {
            var msg = new Msg();
            msg.InitGC(Encoding.ASCII.GetBytes("{\"ID\":0}"), Encoding.ASCII.GetBytes("{\"ID\":0}").Length);
            req.Send(ref msg, false);
            
            var recvStr = req.ReceiveFrameString();
            HostAndClientInput.receivedGoObjectPoses = JsonConvert.DeserializeObject<ConcurrentDictionary<string, ConcurrentDictionary<string, float>>>(recvStr);
        }
    }
}

public class HostAndClientInput : MonoBehaviour
{
    private Camera[] cameras;
    public static readonly List<int> FalconIDs = new List<int>();
    public static bool isClient = false;
    public int id;
    //public static string hostIp = "10.203.54.198";
    public static string hostIp = "localhost";
    public bool xReversed, yReversed, zReversed;
    public static GameObject[] gameObjects;
    public static ConcurrentDictionary<string, ConcurrentDictionary<string, float>> receivedGoObjectPoses;
    public static ConcurrentDictionary<string, ConcurrentDictionary<string, float>> goObjectPose = new ConcurrentDictionary<string, ConcurrentDictionary<string, float>>();
    private static readonly Dictionary<int, Rigidbody> HipRigidbodies = new Dictionary<int, Rigidbody>();
    public static readonly Dictionary<int, Dictionary<string, string>> HipForces = new Dictionary<int, Dictionary<string, string>>();
    public static bool killThread;
    // Start is called before the first frame update
    private void Start()
    {
        gameObjects = FindObjectsOfType<GameObject>() ;
        cameras = FindObjectsOfType<Camera>();
        //if (!isClient) cameras[1].enabled = false;//Not very general, but gets the job done for now. For more than 2 people, idk.
        //else cameras[0].enabled = false;
        if (isClient)
        {
            new Thread(Comm.host_comm).Start();
            return;
        }
        HipRigidbodies.Add(id, GetComponent<Rigidbody>());
        FalconIDs.Add(id);
        new Thread(Comm.client_comm).Start();
        foreach (var go in gameObjects)
        {
            if (goObjectPose.ContainsKey(go.name)) continue;
            goObjectPose.TryAdd(go.name, new ConcurrentDictionary<string, float>());
            var position = go.transform.position;
            var eulerAngles = go.transform.rotation.eulerAngles;
            goObjectPose[go.name].TryAdd("x_pos", position.x);
            goObjectPose[go.name].TryAdd("y_pos", position.y);
            goObjectPose[go.name].TryAdd("z_pos", position.z);
            goObjectPose[go.name].TryAdd("x_rot", eulerAngles.x);
            goObjectPose[go.name].TryAdd("y_rot", eulerAngles.x);
            goObjectPose[go.name].TryAdd("z_rot", eulerAngles.x);
        }
    }

    private void OnDestroy()
    {
        killThread = true;    //Makes sure that client_comm thread closes
    }

    private void Update()
    {
        if (!isClient)
        {
            foreach (var go in gameObjects)
            {
                var position = go.transform.position;
                var eulerAngles = go.transform.rotation.eulerAngles;
                goObjectPose[go.name]["x_pos"] = position.x;
                goObjectPose[go.name]["y_pos"] = position.y;
                goObjectPose[go.name]["z_pos"] = position.z;
                goObjectPose[go.name]["x_rot"] = eulerAngles.x;
                goObjectPose[go.name]["y_rot"] = eulerAngles.y;
                goObjectPose[go.name]["z_rot"] = eulerAngles.z;
            }
        }

        if (isClient)
        {
            foreach (var go in gameObjects)
            {
                if (!receivedGoObjectPoses.ContainsKey(go.name)) continue;
                var newPos = new Vector3(
                    receivedGoObjectPoses[go.name]["x_pos"],
                    receivedGoObjectPoses[go.name]["y_pos"],
                    receivedGoObjectPoses[go.name]["z_pos"]);
                var newRot = new Vector3(
                    receivedGoObjectPoses[go.name]["x_rot"],
                    receivedGoObjectPoses[go.name]["y_rot"],
                    receivedGoObjectPoses[go.name]["z_rot"]);
                go.transform.position = newPos;
                go.transform.eulerAngles = newRot;
            }
        }
    }

    private void FixedUpdate()
    {
        if (isClient) return;
        if (!HipForces.ContainsKey(id)) return; //Check if any data for HIP exists for HIP associated with Falcon
        var x = float.Parse(HipForces[id]["force_x"]);
        var y = float.Parse(HipForces[id]["force_y"]);
        var z = float.Parse(HipForces[id]["force_z"]);
        HipRigidbodies[id].AddRelativeForce(x * (xReversed ? -1 : 1), y * (yReversed ? -1 : 1), z * (zReversed ? -1 : 1));
    }
}
