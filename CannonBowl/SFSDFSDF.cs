using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using UnityEngine;

using NetMQ;
using NetMQ.Sockets;

using Newtonsoft.Json;

public class SFSDFSDF : MonoBehaviour
{
    public int id;

    public string pubAddress = "tcp://*:5577";
    public string subAddress = "tcp://localhost:5578";

    private Rigidbody _hip;

    private readonly SubscriberSocket _sub = new SubscriberSocket();
    private readonly PublisherSocket _pub = new PublisherSocket();

    private const float ZmqPubRate = 30; //30 Hz
    private float _timeSinceLastPub;

    // Start is called before the first frame update
    private void Start()
    {
        _hip = GetComponent<Rigidbody>();

        _pub.Bind(pubAddress);

        _sub.Connect(subAddress);
        _sub.SubscribeToAnyTopic();
        _sub.Options.ReceiveHighWatermark = 1;
    }

    // Update is called once per frame
    private void Update()
    {
        _timeSinceLastPub += Time.deltaTime;
        if (_timeSinceLastPub >= 1 / ZmqPubRate)
        {
            var pubMsg = new Msg();
            var position = _hip.position;
            var velocity = _hip.velocity;
            var msgData = new MsgData
            {
                Position = new[] { position.x, position.y, position.z },
                Velocity = new[] { velocity.x, velocity.y, velocity.z },
                ID = id
            };
            var pubMsgPayload = Encoding.ASCII.GetBytes(JsonConvert.SerializeObject(msgData));
            pubMsg.InitGC(pubMsgPayload, pubMsgPayload.Length);
            _pub.Send(ref pubMsg, false);
            _timeSinceLastPub = 0;
        }

        string recvStr;
        if (_sub.TryReceiveFrameString(out recvStr))
        {
            Debug.Log(recvStr);
        }
    }

    private void OnDestroy()
    {
        _pub.Close();
        _sub.Close();
    }
}

public class MsgData
{
    public float[] Position { get; set; }
    public float[] Velocity { get; set; }
    public int ID { get; set; }
}
